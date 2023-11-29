using System.Collections.Generic;
using KayKitMultiplayerServer.Network.ConfigReader;
using System.Net;
using KayKitMultiplayerServer.Network.Interface;
using KayKitMultiplayerServer.TableRelated;
using Photon.SocketServer;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;

namespace KayKitMultiplayerServer.Network.Server
{
    public class PhotonInboundPeer : InboundS2SPeer, IPeerOperator
    {
        private IMessageHandler _networkHandler;
        private ServerPeerInfo _serverPeerInfo;

        public int ConnectionID { get; private set; }
        private bool _isActived = false;


        public PhotonInboundPeer(InitRequest initRequest, IMessageHandler networkHandler) : base(initRequest)
        {
            _networkHandler = networkHandler;
            ConnectionID = NetworkHandler.InvalidConnectionID;
        }
        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            object messageIDObj;
            if (operationRequest.Parameters.TryGetValue((byte)NetOperationType.MessageID, out messageIDObj))
            {
                ServerHandlerMessage msgType = (ServerHandlerMessage)messageIDObj;
                if (msgType == ServerHandlerMessage.ServerWelcome)
                {
                    object senderIDObj;
                    if (operationRequest.Parameters.TryGetValue((byte)NetOperationType.SenderID, out senderIDObj))
                    {
                        ConnectionID = (int)senderIDObj;

                        string innerRemoteIpAddress = TableManager.Instance.GetTable<ServerListConfigReader>().GetInnerIpAddress(ConnectionID);
                        if (RemoteIP == innerRemoteIpAddress)
                        {
                            _networkHandler.OnConnect(ConnectionID, this);
                            _isActived = true;
                        }
                        else
                        {
                            _serverPeerInfo = new ServerPeerInfo(ConnectionID, string.Empty,
                                new IPEndPoint(RemoteIPAddress, RemotePort), RemoteConnetionType.Unknown);
                            Disconnect();
                            return;
                        }
                    }

                    object tmp;
                    Dictionary<int, object> msg;
                    if (!operationRequest.Parameters.TryGetValue((byte)NetOperationType.Data, out tmp))
                        return;

                    msg = (Dictionary<int, object>)tmp;
                    string serverName = string.Empty;
                    RemoteConnetionType serverType;
                    int serverId;

                    if (!msg.TryGetValue((int)ServerConnected.ServerId, out tmp))
                    {
                        return;
                    }
                    serverId = (int)tmp;

                    if (!msg.TryGetValue((int)ServerConnected.ServerName, out tmp))
                    {
                        return;
                    }
                    serverName = (string)tmp;

                    if (!msg.TryGetValue((int)ServerConnected.RemoteServerType, out tmp))
                    {
                        return;
                    }
                    serverType = (RemoteConnetionType)tmp;

                    IPEndPoint ipEnd = new IPEndPoint(RemoteIPAddress, RemotePort);
                    _serverPeerInfo = new ServerPeerInfo(serverId, serverName, ipEnd, serverType);

                    if (_isActived)
                        DebugLog.LogFormat("Inbound connected from server {0}", serverName);
                }
                else if (!_isActived)
                {
                    return;
                }
            }

            _networkHandler.OnMessageArrive(operationRequest.Parameters);
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            if (!_isActived)
                return;

            _networkHandler.OnDisconnect(ConnectionId);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage[(int)ServerDisconnected.ServerId] = _serverPeerInfo.ServerID;
            outMessage[(int)ServerDisconnected.ServerName] = _serverPeerInfo.ServerName;
            outMessage[(int)ServerDisconnected.RemoteServerType] = (int)_serverPeerInfo.ServerType;
            PhotonApplication.Instance.NetHandle.OnFakeMessageArrive(MessageType.ServerHandlerMessage, ServerHandlerMessage.ServerDisconnected, outMessage);

            Dispose();
        }

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
        }

        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
            
        }

        public void SendMessage(Dictionary<byte, object> message)
        {
            OperationRequest request = new OperationRequest((byte)NetOperationCode.ServerServer, message);
            message.Add((byte)NetOperationType.MessageType, MessageType.ServerHandlerMessage);
            SendOperationRequest(request, new SendParameters());
        }
    }
}