using System;
using System.Collections.Generic;
using KayKitMultiplayerServer.TableRelated;
using KayKitMultiplayerServer.Utility;
using NotImplementedException = System.NotImplementedException;

namespace KayKitMultiplayerServer.Network.ConfigReader
{
    public class ServerListConfigReader : TableBase
    {
        private const string _serverName = "ServerName";
        private const string _serverType = "ServerType";
        private const string _serverID = "ServerID";
        private const string _innerIpAddress = "InnerIpAddress";
        private const string _outerIpAddress = "OuterIpAddress";
        private const string _innerPort = "InnerPort";
        private const string _outerPort = "OuterPort";
        private const string _webPort = "WebPort";

        private Dictionary<int, string> _serverIdToName = new Dictionary<int, string>();

        protected override void OnRowParsed(List<object> rowContent)
        {
            string serverName = rowContent[GetColumnNameIndex(_serverName)] as ValueTypeWrapper<string>;
            int serverId = rowContent[GetColumnNameIndex(_serverID)] as ValueTypeWrapper<int>;

            _serverIdToName.Add(serverId, serverName);
        }

        protected override void OnTableParsed()
        {
        }

        public RemoteConnetionType GetServerType(string serverName)
        {
            string serverTypeName = GetValue<string, string>(serverName, _serverType);
            RemoteConnetionType severType = (RemoteConnetionType)Enum.Parse(typeof(RemoteConnetionType), serverTypeName);
            return severType;
        }

        public RemoteConnetionType GetServerType(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetServerType(serverName);
        }

        public string GetServerName(int serverID)
        {
            string outValue;
            _serverIdToName.TryGetValue(serverID, out outValue);
            return outValue;
        }

        public int GetServerID(string serverName)
        {
            return GetValue<string, int>(serverName, _serverID);
        }

        /// <summary>
        /// 獲取內部 IP 地址
        /// </summary>
        /// <param name="serverID">服務器ID</param>
        /// <returns></returns>
        public string GetInnerIpAddress(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetValue<string, string>(serverName, _innerIpAddress);
        }

        public string GetInnerIpAddress(string serverName)
        {
            return GetValue<string, string>(serverName, _innerIpAddress);
        }

        public string GetInnerPort(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetValue<string, string>(serverName, _innerPort);
        }

        public string GetInnerPort(string serverName)
        {
            return GetValue<string, string>(serverName, _innerPort);
        }

        public string GetOuterIpAddress(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetValue<string, string>(serverName, _outerIpAddress);
        }

        public string GetOuterIpAddress(string serverName)
        {
            return GetValue<string, string>(serverName, _outerIpAddress);
        }

        public string GetOuterPort(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetValue<string, string>(serverName, _outerPort);
        }

        public string GetOuterPort(string serverName)
        {
            return GetValue<string, string>(serverName, _outerPort);
        }

        public string GetWebPort(int serverID)
        {
            string serverName = GetServerName(serverID);
            return GetValue<string, string>(serverName, _webPort);
        }

        public string GetWebPort(string serverName)
        {
            return GetValue<string, string>(serverName, _webPort);
        }
    }
}