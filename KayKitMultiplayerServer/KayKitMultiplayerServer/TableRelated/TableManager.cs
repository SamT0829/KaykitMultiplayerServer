using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;

namespace KayKitMultiplayerServer.TableRelated
{
    public class TableManager
    {
        private static TableManager _instance;
        private Dictionary<Type, TableBase> _staticTableTable = new Dictionary<Type, TableBase>();
        private Dictionary<string, Dictionary<Type, TableBase>> _staticFirmTableTable =
            new Dictionary<string, Dictionary<Type, TableBase>>();
        private ReaderWriterLock _lock = new ReaderWriterLock();

        public static TableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TableManager();
                }
                return _instance;
            }
        }

        public string DefaultFolderName { get; set; }

        private TableManager()
        {
        }

        public void CreateTable<T>(string filePath)
            where T : TableBase, new()
        {
            string fileText = File.ReadAllText(filePath);

            T table = new T();
            table.Parsing(fileText);
            _lock.AcquireWriterLock(1000);
            _staticTableTable[table.GetType()] = table;

            _lock.ReleaseWriterLock();
        }

        public void CreateTableFromFileStream<T>(string fileStream)
            where T : TableBase, new()
        {
            T table = new T();
            table.Parsing(fileStream);
            _lock.AcquireWriterLock(1000);
            _staticTableTable[table.GetType()] = table;
            _lock.ReleaseWriterLock();
        }

        public void CreateTablesFromDictionary(Dictionary<string, string> tableTable, string path)
        {
            Dictionary<string, string>.Enumerator enumerator = tableTable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string fileText = File.ReadAllText(path + enumerator.Current.Value);

                Type t = Type.GetType(enumerator.Current.Key);
                TableBase table = Activator.CreateInstance(t) as TableBase;
                if (table == null)
                {
                    DebugLog.LogError("TableManager.CreateTablesFromDictionary error on create enumerator.Current.Key.");
                    break;
                }
                table.Parsing(fileText);
                _staticTableTable[table.GetType()] = table;
            }
            enumerator.Dispose();
            _lock.ReleaseWriterLock();
        }

        public void CreateFirmTablesFromDictionary(Dictionary<string, string> tableTable, string path)
        {
            _lock.AcquireWriterLock(1000);
            string[] firmDirectorys = Directory.GetDirectories(path);

            for (int i = 0; i < firmDirectorys.Length; ++i)
            {
                string[] pathElement = firmDirectorys[i].Split(Path.DirectorySeparatorChar);
                string veryLastDirectory = pathElement[pathElement.Length - 1];
                if (!_staticFirmTableTable.TryGetValue(veryLastDirectory, out Dictionary<Type, TableBase> staticTableTable) ||
                    staticTableTable == null)
                {
                    staticTableTable = new Dictionary<Type, TableBase>();
                    _staticFirmTableTable[veryLastDirectory] = staticTableTable;
                }
                Dictionary<string, string>.Enumerator enumerator = tableTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string filePath = Path.Combine(path, veryLastDirectory, enumerator.Current.Value);
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }
                    string fileText = File.ReadAllText(filePath);

                    Type t = Type.GetType(enumerator.Current.Key);
                    DebugLog.Log("Attach Table = " + enumerator.Current.Key);

                    TableBase table = Activator.CreateInstance(t) as TableBase;
                    if (table == null)
                    {
                        DebugLog.LogError("TableManager.CreateFirmTablesFromDictionary error on create enumerator.Current.Key.");
                        break;
                    }
                    table.Parsing(fileText);
                    staticTableTable[table.GetType()] = table;
                }
                enumerator.Dispose();
            }
            _lock.ReleaseWriterLock();
        }

        public void CreateTableFromFileStream(TableBase table, string fileStream)
        {
            _lock.AcquireWriterLock(1000);
            if (table == null)
            {
                DebugLog.LogError("TableManager.CreateTableFromFileStream error on create enumerator.Current.Key.");
            }
            table.Parsing(fileStream);

            _staticTableTable[table.GetType()] = table;

            _lock.ReleaseWriterLock();
        }

        public void AddCustomTable(TableBase table)
        {
            _lock.AcquireWriterLock(1000);
            _staticTableTable[table.GetType()] = table;
            _lock.ReleaseWriterLock();
        }

        public T GetTable<T>()
            where T : TableBase
        {
            TableBase table;
            _lock.AcquireReaderLock(1000);
            if (_staticTableTable.TryGetValue(typeof(T), out table) && table != null)
            {
                _lock.ReleaseReaderLock();
                T retTable = table as T;
                if (retTable != null)
                {
                    return retTable;
                }
            }
            else
            {
                _lock.ReleaseReaderLock();
            }
            return default(T);
        }

        public T GetTable<T>(string firm)
            where T : TableBase
        {
            TableBase table;
            _lock.AcquireReaderLock(1000);

            if (!_staticFirmTableTable.TryGetValue(firm, out Dictionary<Type, TableBase> staticTableTable) ||
                staticTableTable == null || !staticTableTable.ContainsKey(typeof(T)))
            {
                _staticFirmTableTable.TryGetValue(DefaultFolderName, out staticTableTable);
            }

            if (staticTableTable.TryGetValue(typeof(T), out table) && table != null)
            {
                _lock.ReleaseReaderLock();
                T retTable = table as T;
                if (retTable != null)
                {
                    return retTable;
                }
            }
            else
            {
                _lock.ReleaseReaderLock();
            }
            return default(T);
        }
    }
}