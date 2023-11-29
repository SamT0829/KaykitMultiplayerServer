namespace KayKitMultiplayerServer.Network.Server
{
    public class ServerInfo
    {
        public int ServerID { get; private set; }
        public string ServerName { get; private set; }
        public RemoteConnetionType ServerType { get; private set; }
        public int ServerInnerPort { get; private set; }
        public int ClientOuterPort { get; private set; }
        public int ClientWebPort { get; private set; }

        public ServerInfo(int serverId, string serverName, RemoteConnetionType serverType, int serverInnerPort, int clientOuterPort, int clientWebPort)
        {
            ServerID = serverId;
            ServerName = serverName;
            ServerType = serverType;
            ServerInnerPort = serverInnerPort;
            ClientOuterPort = clientOuterPort;
            ClientWebPort = clientWebPort;

            DebugLog.LogFormat(
                "Initialize server config serverId : {0} serverName : {1} server type : {2} serverInnerPort : {3}, clientOuterPort : {4}",
                serverId, serverName, serverType, serverInnerPort, clientOuterPort);
        }
    }
}