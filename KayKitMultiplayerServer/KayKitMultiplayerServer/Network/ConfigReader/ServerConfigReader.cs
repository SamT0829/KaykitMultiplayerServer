using System;
using System.Collections.Generic;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.TableRelated;
using KayKitMultiplayerServer.Utility;


namespace KayKitMultiplayerServer.Network.ConfigReader
{
    public class ServerConfigReader : CompositeKeyTable
    {
        private const string _configGroupKeyName = "ConfigGroupKeyName";
        private const string _configKeyName = "ConfigKeyName";
        private const string _configValue = "ConfigValue";

        // group key names //
        private const string _thisServer = "ThisServer";
        private const string _connectServer = "ConnectServers";
        private const string _connectDB = "ConnectDB";
        private const string _subSystems = "RequiredSubSystems";
        private const string _table = "RequiredTables";

        private Dictionary<string, Action<string, string>> _configNameParseFunctionTable = new Dictionary<string, Action<string, string>>();

        public string ServerName { get; private set; }
        public RemoteConnetionType SeverType { get; private set; }
        public Dictionary<string, RemoteConnetionType> ConnectServersTable { get; private set; }
        public Dictionary<string, DBCatagory> ConnectDBTable { get; private set; }
        public List<string> SubsystemList { get; private set; }
        public Dictionary<string, string> TableTable { get; private set; }

        public ServerConfigReader()
        {
            ConnectServersTable = new Dictionary<string, RemoteConnetionType>();
            ConnectDBTable = new Dictionary<string, DBCatagory>();
            SubsystemList = new List<string>();
            TableTable = new Dictionary<string, string>();

            _configNameParseFunctionTable.Add(_thisServer, OnParseServerType);
            _configNameParseFunctionTable.Add(_connectServer, OnParseConnectServers);
            _configNameParseFunctionTable.Add(_connectDB, OnParseConnectDB);
            _configNameParseFunctionTable.Add(_subSystems, OnParseRequiredSubsystem);
            _configNameParseFunctionTable.Add(_table, OnParseRequiredTables);
        }

        protected override void OnCompositeKeyDealed(List<object> rowContent)
        {
            ValueTypeWrapper<string> configGroupKeyName = rowContent[GetColumnNameIndex(_configGroupKeyName)] as ValueTypeWrapper<string>;
            ValueTypeWrapper<string> configKeyName = rowContent[GetColumnNameIndex(_configKeyName)] as ValueTypeWrapper<string>;
            ValueTypeWrapper<string> configValue = rowContent[GetColumnNameIndex(_configValue)] as ValueTypeWrapper<string>;
            if (configGroupKeyName == null || configKeyName == null || configValue == null)
            {
                return;
            }
            Action<string, string> action;
            if (_configNameParseFunctionTable.TryGetValue(configGroupKeyName.Value, out action) && action != null)
            {
                action(configKeyName, configValue);
            }
        }

        protected override void OnTableParsed()
        {
        }

        private void OnParseServerType(string serverName, string serverType)
        {
            ServerName = serverName;
            SeverType = (RemoteConnetionType)Enum.Parse(typeof(RemoteConnetionType), serverType);
        }

        private void OnParseConnectServers(string serverName, string serverType)
        {
            RemoteConnetionType remoteServerType = (RemoteConnetionType)Enum.Parse(typeof(RemoteConnetionType), serverType);
            if (ConnectServersTable.ContainsKey(serverName))
            {
                DebugLog.LogError("ServerConfigReader.OnParseConnectServers() duplicate key occoured");
                return;
            }
            ConnectServersTable.Add(serverName, remoteServerType);
        }

        private void OnParseConnectDB(string dbName, string dbType)
        {
            DBCatagory connectToDbType = (DBCatagory)Enum.Parse(typeof(DBCatagory), dbType);
            if (ConnectDBTable.ContainsKey(dbName))
            {
                DebugLog.LogError("ServerConfigReader.OnParseConnectDB() duplicate key occoured");
                return;
            }
            ConnectDBTable.Add(dbName, connectToDbType);
        }

        private void OnParseRequiredSubsystem(string subsystemName, string param)
        {
            if (string.IsNullOrEmpty(subsystemName))
            {
                DebugLog.LogError("ServerConfigReader.OnParseRequiredSubsystem() invalid key occoured");
                return;
            }
            SubsystemList.Add(subsystemName);
        }

        private void OnParseRequiredTables(string tableName, string TablePath)
        {
            if (TableTable.ContainsKey(tableName))
            {
                DebugLog.LogError("ServerConfigReader.OnParseRequiredTables() duplicate key occoured" + tableName);
                return;
            }
            TableTable.Add(tableName, TablePath);
        }
    }
}