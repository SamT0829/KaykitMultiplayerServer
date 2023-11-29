using System.Net;

namespace KayKitMultiplayerServer.Network.Server
{
    public class ServerPeerInfo
    {
        public int ServerID { get; private set; }
        public string ServerName { get; private set; }
        public RemoteConnetionType ServerType { get; private set; }
        public IPEndPoint ServerIp { get; private set; }

        public ServerPeerInfo(int serverId, string serverName, IPEndPoint ip, RemoteConnetionType serverType)
        {
            ServerID = serverId;
            ServerName = serverName;
            ServerIp = ip;
            ServerType = serverType;
        }
    }
}