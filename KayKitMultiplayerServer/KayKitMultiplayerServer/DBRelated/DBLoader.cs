using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace KayKitMultiplayerServer.DBRelated
{
    public class DBLoader : IDisposable
    {
        private MySqlConnection _connection;
        private IFiber _selfCreatedFiber;
        private IFiber _fiber;
        private IQueryEventListener _queryEventListener;

        public DBCatagory Catagory { get; private set; }
        public string DbHost { get; private set; }//資料庫位址
        public string DbPort { get; private set; }
        public string DbUser { get; private set; }//資料庫使用者帳號
        public string DbPass { get; private set; }//資料庫使用者密碼
        public string DbName { get; private set; }//資料庫名稱
        public int DbIndex { get; private set; }

        public DBLoader(DBCatagory catagory, int index, IFiber loaderFiber, string dbHost, string dbPort,
                        string dbUser, string dbPass, string dbName, IQueryEventListener listener)
        {
            if (loaderFiber == null)
            {
                _selfCreatedFiber = new PoolFiber();
                _selfCreatedFiber.Start();
                _fiber = _selfCreatedFiber;
            }
            else
            {
                _fiber = loaderFiber;
            }
            Catagory = catagory;
            DbIndex = index;
            DbHost = dbHost;
            DbPort = dbPort;
            DbUser = dbUser;
            DbPass = dbPass;
            DbName = dbName;
            string connStr = "server=" + dbHost + ";user=" + dbUser + ";password=" + dbPass + ";database=" + dbName + ";port=" + dbPort;
            _connection = new MySqlConnection(connStr);
            _connection.Open();

            _queryEventListener = listener;
        }

        public void ExecuteMultiNonQuery(List<string> sqlStringList, List<Dictionary<string, object>> parametersList,
                                         IFiber fiber, Action<int> resultFunction, bool reconnect)
        {
            _fiber.Schedule(() =>
            {
                int rowEffected = 0;
                int i = 0;
                try
                {
                    //_connection.Open();

                    for (; i < sqlStringList.Count; ++i)
                    {
                        using (MySqlCommand command = new MySqlCommand(sqlStringList[i], _connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            var enumerator = parametersList[i].GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                command.Parameters.AddWithValue(enumerator.Current.Key, enumerator.Current.Value);
                            }
                            enumerator.Dispose();

                            while (true)
                            {
                                try
                                {
                                    rowEffected = command.ExecuteNonQuery();
                                    break;
                                }
                                catch (Exception e)
                                {
                                    DebugLog.LogError("Db connection Lost : " + e.ToString());
                                    try
                                    {
                                        _connection.Close();
                                    }
                                    catch (Exception unableCloseDbException)
                                    {
                                        DebugLog.LogError("Db connection unable close : " + " " + unableCloseDbException.ToString());
                                    }
                                    finally
                                    {
                                        string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                        _connection = new MySqlConnection(connStr);
                                        command.Connection = _connection;
                                    }
                                    while (true)
                                    {
                                        try
                                        {
                                            _connection.Open();
                                            if (reconnect)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                        catch (Exception unableOpenDbException)
                                        {
                                            DebugLog.LogError("Db connection unable open : " + " " + unableOpenDbException.ToString());
                                            string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                            _connection = new MySqlConnection(connStr);
                                            command.Connection = _connection;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    rowEffected = -1;
                    DebugLog.LogError("DB ExecuteNonQuery StoredProcedure : " + sqlStringList[i] + " exception : " + ex.ToString());
                }
                finally
                {
                    _queryEventListener.QueryDoneCallback(Catagory, DbIndex);
                    //_connection.Close();
                    if (fiber != null && resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(rowEffected), 0);
                    }
                }
            }, 0);
        }

        public void ExecuteMultiReader(List<string> storeprocedureNameList, List<Dictionary<string, object>> parametersList,
                                       IFiber fiber, Action<List<Dictionary<string, object>>> resultFunction, bool reconnect)
        {
            _fiber.Schedule(() =>
            {
                int i = 0;
                MySqlDataReader rdr = null;
                List<Dictionary<string, object>> readData = new List<Dictionary<string, object>>();
                try
                {
                    //_connection.Open();
                    for (; i < storeprocedureNameList.Count; ++i)
                    {
                        using (MySqlCommand command = new MySqlCommand(storeprocedureNameList[i], _connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            var enumerator = parametersList[i].GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                command.Parameters.AddWithValue(enumerator.Current.Key, enumerator.Current.Value);
                            }
                            enumerator.Dispose();

                            while (true)
                            {
                                try
                                {
                                    rdr = command.ExecuteReader();
                                    break;
                                }
                                catch (Exception e)
                                {
                                    DebugLog.LogError("Db connection Lost : " + e.ToString());
                                    try
                                    {
                                        _connection.Close();
                                    }
                                    catch (Exception unableCloseDbException)
                                    {
                                        DebugLog.LogError("Db connection unable close : " + " " + unableCloseDbException.ToString());
                                    }
                                    finally
                                    {
                                        string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                        _connection = new MySqlConnection(connStr);
                                        command.Connection = _connection;
                                    }

                                    while (true)
                                    {
                                        try
                                        {
                                            _connection.Open();
                                            if (reconnect)
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                        catch (Exception unableOpenDbException)
                                        {
                                            DebugLog.LogError("Db connection unable open : " + " " + unableOpenDbException.ToString());
                                            string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                            _connection = new MySqlConnection(connStr);
                                            command.Connection = _connection;
                                        }
                                    }
                                }
                            }

                            while (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    Dictionary<string, object> data = new Dictionary<string, object>();
                                    for (int j = 0; j < rdr.FieldCount; ++j)
                                    {
                                        data.Add(rdr.GetName(j), rdr[j]);
                                    }
                                    readData.Add(data);
                                }
                                rdr.NextResult();
                            }
                            rdr.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.LogError("DB ExecuteReader StoredProcedure : " + storeprocedureNameList[i] + " exception : " + ex.ToString());
                }
                finally
                {
                    //_connection.Close();
                    _queryEventListener.QueryDoneCallback(Catagory, DbIndex);
                    if (fiber != null && resultFunction != null)
                    {
                        fiber.Schedule(() =>
                        {
                            resultFunction(readData);
                        }, 0);
                    }
                }
            }, 0);
        }

        public void ExecuteNonQuery(string sqlString, Dictionary<string, object> parameters,
            IFiber fiber, Action<int> resultFunction, bool reconnect)
        {
            _fiber.Schedule(() =>
            {
                int rowEffected = 0;
                try
                {
                    //_connection.Open();
                    using (MySqlCommand command = new MySqlCommand(sqlString, _connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        var enumerator = parameters.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            command.Parameters.AddWithValue(enumerator.Current.Key, enumerator.Current.Value);
                        }
                        enumerator.Dispose();

                        while (true)
                        {
                            try
                            {
                                rowEffected = command.ExecuteNonQuery();
                                break;
                            }
                            catch (Exception e)
                            {
                                DebugLog.LogError("Db connection Lost : " + sqlString + " " + e.ToString());
                                try
                                {
                                    _connection.Close();
                                }
                                catch (Exception unableCloseDbException)
                                {
                                    DebugLog.LogError("Db connection unable close : " + sqlString + " " + unableCloseDbException.ToString());
                                }
                                finally
                                {
                                    string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                    _connection = new MySqlConnection(connStr);
                                    command.Connection = _connection;
                                }

                                while (true)
                                {
                                    try
                                    {
                                        _connection.Open();
                                        if (reconnect)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                    catch (Exception unableOpenDbException)
                                    {
                                        DebugLog.LogError("Db connection unable open : " + sqlString + " " + unableOpenDbException.ToString());
                                        string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                        _connection = new MySqlConnection(connStr);
                                        command.Connection = _connection;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    rowEffected = -1;
                    DebugLog.LogError("DB ExecuteNonQuery StoredProcedure : " + sqlString + " exception : " + ex.ToString());
                }
                finally
                {
                    //_connection.Close();
                    _queryEventListener.QueryDoneCallback(Catagory, DbIndex);
                    if (fiber != null && resultFunction != null)
                    {
                        fiber.Schedule(() => resultFunction(rowEffected), 0);
                    }
                }
            }, 0);
        }

        public void ExecuteReader(string storeprocedureName, Dictionary<string, object> parameters,
            IFiber fiber, Action<List<Dictionary<string, object>>> resultFunction, bool reconnect)
        {
            _fiber.Schedule(() =>
            {
                MySqlDataReader rdr = null;
                List<Dictionary<string, object>> readData = new List<Dictionary<string, object>>();
                try
                {
                    //_connection.Open();
                    using (MySqlCommand command = new MySqlCommand(storeprocedureName, _connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        var enumerator = parameters.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            command.Parameters.AddWithValue(enumerator.Current.Key, enumerator.Current.Value);
                        }
                        enumerator.Dispose();

                        while (true)
                        {
                            try
                            {
                                rdr = command.ExecuteReader();
                                break;
                            }
                            catch (Exception e)
                            {
                                DebugLog.LogError("Db connection Lost : " + storeprocedureName + " " + e.ToString());
                                try
                                {
                                    _connection.Close();
                                }
                                catch (Exception unableCloseDbException)
                                {
                                    DebugLog.LogError("Db connection unable close : " + storeprocedureName + " " + unableCloseDbException.ToString());
                                }
                                finally
                                {
                                    string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                    _connection = new MySqlConnection(connStr);
                                    command.Connection = _connection;
                                }

                                while (true)
                                {
                                    try
                                    {
                                        _connection.Open();
                                        if (reconnect)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                    catch (Exception unableOpenDbException)
                                    {
                                        DebugLog.LogError("Db connection unable open : " + storeprocedureName + " " + unableOpenDbException.ToString());
                                        string connStr = "server=" + DbHost + ";user=" + DbUser + ";password=" + DbPass + ";database=" + DbName + ";port=" + DbPort;
                                        _connection = new MySqlConnection(connStr);
                                        command.Connection = _connection;
                                    }
                                }
                            }
                        }

                        while (rdr.HasRows)
                        {
                            while (rdr.Read())
                            {
                                Dictionary<string, object> data = new Dictionary<string, object>();
                                for (int i = 0; i < rdr.FieldCount; ++i)
                                {
                                    data.Add(rdr.GetName(i), rdr[i]);
                                }
                                readData.Add(data);
                            }
                            rdr.NextResult();
                        }
                        rdr.Close();
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.LogError("DB ExecuteReader StoredProcedure : " + storeprocedureName + " exception : " + ex.ToString());
                }
                finally
                {
                    //_connection.Close();
                    _queryEventListener.QueryDoneCallback(Catagory, DbIndex);
                    if (fiber != null && resultFunction != null)
                    {
                        fiber.Schedule(() =>
                        {
                            resultFunction(readData);
                        }, 0);
                    }
                }
            }, 0);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                if (_selfCreatedFiber != null)
                {
                    _selfCreatedFiber.Dispose();

                }
                if (_connection != null)
                {
                    _connection.Dispose();
                    //DebugLog.Log("DBLoader.dispose: category=" + Catagory);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DBLoader() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}