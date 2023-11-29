using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.DBRelated
{
    public enum DBCatagory
    {
        Account,
        Lobby,
        Game,
        Finance,
        LobbyLog,
        GameLog,
        Log,
    }
    public class DBManager : IQueryEventListener
    {
        private const int _linesForEachDb = 10;
        private static DBManager _instance;
        private Dictionary<DBCatagory, Queue<DBLoader>> _dbAvailableLoaderTable = new Dictionary<DBCatagory, Queue<DBLoader>>();
        private Dictionary<DBCatagory, Dictionary<int, DBLoader>> _dbBusyLoaderTable = new Dictionary<DBCatagory, Dictionary<int, DBLoader>>();
        private IFiber _dbManagerFiber = new PoolFiber();

        private Dictionary<DBCatagory, Queue<Action<DBLoader>>> _waitingTaskQueueTable = new Dictionary<DBCatagory, Queue<Action<DBLoader>>>();

        public static DBManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DBManager();
                }
                return _instance;
            }
        }

        private DBManager()
        {
            _dbManagerFiber.Start();
        }

        public static bool AddDbParam(Dictionary<string, object> parameterTable, string keyName, object item)
        {
            bool retvalue = false;
            if (item != null && !parameterTable.ContainsKey(keyName))
            {
                parameterTable.Add(keyName, item);
                retvalue = true;
            }
            return retvalue;
        }

        public static bool GetDbParam<T>(Dictionary<string, object> parameterTable, string keyName, out T value)
        {
            value = default;

            if (parameterTable.TryGetValue(keyName, out object parameterValue))
            {
                value = (T)parameterValue;

                if (value != null)
                    return true;

                DebugLog.LogFormat("DbParam {0} is different types at {1}", JsonConvert.SerializeObject(parameterTable), keyName);
                return false;
            }

            DebugLog.LogFormat("DbParam {0} has not value at {1}", JsonConvert.SerializeObject(parameterTable), keyName);
            return false;
        }

        public static object GetDbParam(Dictionary<string, object> parameterTable, string keyName)
        {
            object retvalue = null;
            parameterTable.TryGetValue(keyName, out retvalue);
            return retvalue;
        }

        public void CreatDbConnection(DBCatagory catagory, string dbHost, string dbPort,
                                      string dbUser, string dbPass, string dbName, IFiber loaderFiber)
        {
            _dbManagerFiber.Schedule(() =>
            {
                for (int i = 0; i < _linesForEachDb; ++i)
                {
                    DBLoader loader = new DBLoader(catagory, i, loaderFiber, dbHost, dbPort, dbUser, dbPass, dbName, this);
                    //DebugLog.Log("CreatDbConnection: catagory=" + catagory);
                    Queue<DBLoader> dbLoaderQueue;
                    if (!_dbAvailableLoaderTable.TryGetValue(catagory, out dbLoaderQueue))
                    {
                        dbLoaderQueue = new Queue<DBLoader>();
                        _dbAvailableLoaderTable.Add(catagory, dbLoaderQueue);
                    }
                    dbLoaderQueue.Enqueue(loader);
                }

                Queue<Action<DBLoader>> actionQueue;
                if (!_waitingTaskQueueTable.TryGetValue(catagory, out actionQueue) || actionQueue == null)
                {
                    actionQueue = new Queue<Action<DBLoader>>();
                    _waitingTaskQueueTable.Add(catagory, actionQueue);
                }
            }, 0);
        }

        public void ExecuteMultiNonQuery(DBCatagory catagory, List<string> sqlStringList,
            List<Dictionary<string, object>> parameters, IFiber fiber, Action<int> resultFunction, bool reconnect = true)
        {
            _dbManagerFiber.Schedule(() =>
            {
                Queue<DBLoader> dbLoaderQueue;
                if (_dbAvailableLoaderTable.TryGetValue(catagory, out dbLoaderQueue) && dbLoaderQueue != null)
                {
                    if (dbLoaderQueue.Count > 0)
                    {
                        DBLoader loader = dbLoaderQueue.Dequeue();
                        loader.ExecuteMultiNonQuery(sqlStringList, parameters, fiber, resultFunction, reconnect);

                        Dictionary<int, DBLoader> busyDbLoaderQueue;
                        if (!_dbBusyLoaderTable.TryGetValue(catagory, out busyDbLoaderQueue) || busyDbLoaderQueue == null)
                        {
                            busyDbLoaderQueue = new Dictionary<int, DBLoader>();
                            _dbBusyLoaderTable[catagory] = busyDbLoaderQueue;
                        }
                        busyDbLoaderQueue.Add(loader.DbIndex, loader);
                    }
                    else
                    {
                        if (_waitingTaskQueueTable.ContainsKey(catagory))
                        {
                            _waitingTaskQueueTable[catagory].Enqueue((loader) =>
                            {
                                loader.ExecuteMultiNonQuery(sqlStringList, parameters, fiber, resultFunction, reconnect);
                            });
                        }
                    }
                }
                else
                {
                    if (resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(0), 0L);
                    }
                }
            }, 0);
        }

        public void ExecuteMultiReader(DBCatagory catagory, List<string> sqlStringList,
            List<Dictionary<string, object>> parametersList,
            IFiber fiber, Action<List<Dictionary<string, object>>> resultFunction, bool reconnect = true)
        {
            _dbManagerFiber.Schedule(() =>
            {
                Queue<DBLoader> dbLoaderQueue;
                if (_dbAvailableLoaderTable.TryGetValue(catagory, out dbLoaderQueue) && dbLoaderQueue != null)
                {
                    if (dbLoaderQueue.Count > 0)
                    {
                        DBLoader loader = dbLoaderQueue.Dequeue();
                        loader.ExecuteMultiReader(sqlStringList, parametersList, fiber, resultFunction, reconnect);

                        Dictionary<int, DBLoader> busyDbLoaderQueue;
                        if (!_dbBusyLoaderTable.TryGetValue(catagory, out busyDbLoaderQueue) || busyDbLoaderQueue == null)
                        {
                            busyDbLoaderQueue = new Dictionary<int, DBLoader>();
                            _dbBusyLoaderTable[catagory] = busyDbLoaderQueue;
                        }
                        busyDbLoaderQueue.Add(loader.DbIndex, loader);
                    }
                    else
                    {
                        if (_waitingTaskQueueTable.ContainsKey(catagory))
                        {
                            _waitingTaskQueueTable[catagory].Enqueue((loader) =>
                            {
                                loader.ExecuteMultiReader(sqlStringList, parametersList, fiber, resultFunction, reconnect);
                            });
                        }
                    }
                }
                else
                {
                    if (resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(null), 0L);
                    }
                }
            }, 0);
        }

        public void ExecuteNonQuery(DBCatagory catagory, string sqlString, Dictionary<string, object> parameters,
                                    IFiber fiber, Action<int> resultFunction, bool reconnect = true)
        {
            _dbManagerFiber.Schedule(() =>
            {
                Queue<DBLoader> dbLoaderQueue;
                if (_dbAvailableLoaderTable.TryGetValue(catagory, out dbLoaderQueue) && dbLoaderQueue != null)
                {
                    if (dbLoaderQueue.Count > 0)
                    {
                        DBLoader loader = dbLoaderQueue.Dequeue();
                        loader.ExecuteNonQuery(sqlString, parameters, fiber, resultFunction, reconnect);

                        Dictionary<int, DBLoader> busyDbLoaderQueue;
                        if (!_dbBusyLoaderTable.TryGetValue(catagory, out busyDbLoaderQueue) || busyDbLoaderQueue == null)
                        {
                            busyDbLoaderQueue = new Dictionary<int, DBLoader>();
                            _dbBusyLoaderTable[catagory] = busyDbLoaderQueue;
                        }
                        busyDbLoaderQueue.Add(loader.DbIndex, loader);
                    }
                    else
                    {
                        if (_waitingTaskQueueTable.ContainsKey(catagory))
                        {
                            _waitingTaskQueueTable[catagory].Enqueue((loader) =>
                            {
                                loader.ExecuteNonQuery(sqlString, parameters, fiber, resultFunction, reconnect);
                            });
                        }
                    }
                }
                else
                {
                    if (resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(0), 0L);
                    }
                }
            }, 0);
        }

        public void ExecuteReader(DBCatagory catagory, string sqlString, Dictionary<string, object> parameters,
            IFiber fiber, Action<List<Dictionary<string, object>>> resultFunction, bool reconnect = true)
        {
            _dbManagerFiber.Schedule(() =>
            {
                Queue<DBLoader> dbLoaderQueue;
                if (_dbAvailableLoaderTable.TryGetValue(catagory, out dbLoaderQueue) && dbLoaderQueue != null)
                {
                    if (dbLoaderQueue.Count > 0)
                    {
                        DBLoader loader = dbLoaderQueue.Dequeue();
                        loader.ExecuteReader(sqlString, parameters, fiber, resultFunction, reconnect);

                        Dictionary<int, DBLoader> busyDbLoaderQueue;
                        if (!_dbBusyLoaderTable.TryGetValue(catagory, out busyDbLoaderQueue) || busyDbLoaderQueue == null)
                        {
                            busyDbLoaderQueue = new Dictionary<int, DBLoader>();
                            _dbBusyLoaderTable[catagory] = busyDbLoaderQueue;
                        }
                        busyDbLoaderQueue.Add(loader.DbIndex, loader);
                    }
                    else
                    {
                        if (_waitingTaskQueueTable.ContainsKey(catagory))
                        {
                            _waitingTaskQueueTable[catagory].Enqueue((loader) =>
                            {
                                loader.ExecuteReader(sqlString, parameters, fiber, resultFunction, reconnect);
                            });
                        }
                    }
                }
                else
                {
                    if (resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(null), 0L);
                    }
                }
            }, 0);
        }

        public void ShutDown()
        {
            _dbManagerFiber.Schedule(() =>
            {
                var enumerator = _dbAvailableLoaderTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    for (int i = 0; i < enumerator.Current.Value.Count; ++i)
                    {
                        var secondIter = enumerator.Current.Value.GetEnumerator();
                        while (secondIter.MoveNext())
                        {
                            secondIter.Current.Dispose();
                        }
                        secondIter.Dispose();
                    }
                }
                enumerator.Dispose();

                var iter = _dbBusyLoaderTable.GetEnumerator();
                while (iter.MoveNext())
                {
                    var secondIter = iter.Current.Value.GetEnumerator();
                    while (secondIter.MoveNext())
                    {
                        secondIter.Current.Value.Dispose();
                    }
                    secondIter.Dispose();
                }
                iter.Dispose();

                _dbManagerFiber.Dispose();
            }, 0);
        }

        public void QueryDoneCallback(DBCatagory dbCatagory, int index)
        {
            _dbManagerFiber.Schedule(() =>
            {
                DBLoader loader = null;
                Dictionary<int, DBLoader> specifiedDbLoaderTable;
                if (_dbBusyLoaderTable.TryGetValue(dbCatagory, out specifiedDbLoaderTable))
                {
                    specifiedDbLoaderTable.TryGetValue(index, out loader);
                }

                Queue<Action<DBLoader>> taskQueue;
                if (_waitingTaskQueueTable.TryGetValue(dbCatagory, out taskQueue))
                {
                    if (taskQueue.Count > 0)
                    {
                        if (loader != null)
                        {
                            var function = taskQueue.Dequeue();
                            function(loader);
                            return;
                        }
                    }
                }

                if (specifiedDbLoaderTable != null)
                {
                    specifiedDbLoaderTable.Remove(index);
                }

                Queue<DBLoader> dbLoaderQueue;
                if (_dbAvailableLoaderTable.TryGetValue(dbCatagory, out dbLoaderQueue))
                {
                    dbLoaderQueue.Enqueue(loader);
                }
            }, 0);
        }
    }
}