using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using KayKitMultiplayerServer.Network.ConfigReader;
using KayKitMultiplayerServer.TableRelated;
using static Google.Protobuf.Reflection.FieldOptions.Types;
using KayKitMultiplayerServer.Network;

namespace KayKitMultiplayerServer.System.Common
{
    public abstract class MainSystem : ServerSubsystemBase
    {
        protected Dictionary<RemoteConnetionType,int> _connectedServerIdTable = new Dictionary<RemoteConnetionType, int>();


        protected MainSystem(IFiber systemFiber) : base(systemFiber)
        {
            SystemManager.Instance.RegisterMainSystem(this);

            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.ServerConnected, OnServerConnected);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.ServerWelcome, OnServerWelcome);

            Dictionary<string, RemoteConnetionType> connectServerTable =
                TableManager.Instance.GetTable<ServerConfigReader>().ConnectServersTable;
            Dictionary<string, RemoteConnetionType>.Enumerator iter = connectServerTable.GetEnumerator();
            while (iter.MoveNext())
            {
                int serverID = TableManager.Instance.GetTable<ServerListConfigReader>().GetServerID(iter.Current.Key);
                PhotonApplication.Instance.NetHandle.ServerPeerConnect(serverID);
            }
            iter.Dispose();
        }

        public int GetServerId(RemoteConnetionType serverType)
        {
            int serverId;
            if (_connectedServerIdTable.TryGetValue(serverType, out serverId))
            {
                return serverId;
            }

            return NetworkHandler.InvalidConnectionID;
        }

        public virtual void OnAllSubSystemPrepared() { }
        protected abstract void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType);

        private void OnServerConnected(int connectionID, Dictionary<int, object> msg)
        {
            int serverID = 0;
            string serverName = string.Empty;
            RemoteConnetionType serverType = RemoteConnetionType.Unknown;
            
            if (!RetrieveMessageItem(msg, ServerConnected.ServerId, out serverID)) return;
            if (!RetrieveMessageItem(msg, ServerConnected.ServerName, out serverName)) return;
            if (!RetrieveMessageItem(msg, ServerConnected.RemoteServerType, out serverType)) return;

            if (!_connectedServerIdTable.ContainsKey(serverType))
            {
                _connectedServerIdTable.Add(serverType, serverID);
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, ServerWelcomeRequest.ServerId, PhotonApplication.Instance.ServerInfo.ServerID);
            AddMessageItem(outMessage, ServerWelcomeRequest.ServerName, PhotonApplication.Instance.ServerInfo.ServerName);
            AddMessageItem(outMessage, ServerWelcomeRequest.ConnectedServerType, (int)PhotonApplication.Instance.ServerInfo.ServerType);

            PhotonApplication.Instance.NetHandle.Send(serverID, ServerHandlerMessage.ServerWelcome, outMessage);
        }

        private void OnServerWelcome(int connectionID, Dictionary<int, object> msg)
        {
            int serverID = 0;
            string serverName = string.Empty;
            RemoteConnetionType serverType = RemoteConnetionType.Unknown;

            if (!RetrieveMessageItem(msg, ServerWelcomeRequest.ServerId, out serverID)) return;
            if (!RetrieveMessageItem(msg, ServerWelcomeRequest.ServerName, out serverName)) return;
            if (!RetrieveMessageItem(msg, ServerWelcomeRequest.ConnectedServerType, out serverType)) return;

            if (!_connectedServerIdTable.ContainsKey(serverType))
            {
                _connectedServerIdTable.Add(serverType, serverID);
            }

            OnOutboundServerConnected(serverID, serverName, serverType);
        }
    }
}