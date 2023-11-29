using System;
using System.Collections.Generic;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.TableRelated;
using NotImplementedException = System.NotImplementedException;

namespace KayKitMultiplayerServer.Network.ConfigReader
{
    public class DBConfigReader : TableBase
    {
        private const string _dbType = "DbType";
        private const string _dbName = "DbName";
        private const string _ipAddress = "IpAddress";
        private const string _port = "Port";
        private const string _hostName = "HostName";
        private const string _password = "Password";

        protected override void OnRowParsed(List<object> rowContent)
        {
        }

        protected override void OnTableParsed()
        {
        }

        public DBCatagory GetDbType(string dbUniqueName)
        {
            string dbTypeName = GetValue<string, string>(dbUniqueName, _dbType);
            DBCatagory dbType = (DBCatagory)Enum.Parse(typeof(DBCatagory), dbTypeName);
            return dbType;
        }

        public string GetDbName(string dbUniqueName)
        {
            return GetValue<string, string>(dbUniqueName, _dbName);
        }

        public string GetIpAddress(string dbUniqueName)
        {
            return GetValue<string, string>(dbUniqueName, _ipAddress);
        }

        public string GetPort(string dbUniqueName)
        {
            return GetValue<string, string>(dbUniqueName, _port);
        }

        public string GetHostName(string dbUniqueName)
        {
            return GetValue<string, string>(dbUniqueName, _hostName);
        }

        public string GetPassword(string dbUniqueName)
        {
            return GetValue<string, string>(dbUniqueName, _password);
        }
    }
}