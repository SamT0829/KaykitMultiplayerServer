using System.Collections.Generic;
using KayKitMultiplayerServer.Network.Interface;
using Photon.SocketServer;
using Photon.SocketServer.ServerToServer;
using PhotonHostRuntimeInterfaces;
using static Google.Protobuf.Reflection.FieldOptions.Types;
using NotImplementedException = System.NotImplementedException;

namespace KayKitMultiplayerServer.Network.Server
{
    public class PhotonOutboundPeer : OutboundS2SPeer, IPeerOperator
    {
        private IMessageHandler _networkHandler;
        private ServerPeerInfo _serverPeerInfo;

        public int ConnectionID { get; private set; }

        public PhotonOutboundPeer(ApplicationBase application, IMessageHandler networkHandler, ServerPeerInfo serverPeerInfo) : base(application)
        {
            ConnectionID = serverPeerInfo.ServerID;
            _networkHandler = networkHandler;
            _serverPeerInfo = serverPeerInfo;
        }

        public void DoConnect()
        {
            DebugLog.LogWarning("Try connect to " + _serverPeerInfo.ServerIp + _serverPeerInfo.ServerName);
            ConnectTcp(_serverPeerInfo.ServerIp, _serverPeerInfo.ServerName);                                                                             
        }

        public void SendMessage(Dictionary<byte, object> message)
        {
            OperationRequest request = new OperationRequest((byte)NetOperationCode.ServerServer, message);
            message.Add((byte)NetOperationType.MessageType, MessageType.ServerHandlerMessage);
            var sendResult = SendOperationRequest(request, new SendParameters());
            if (sendResult != SendResult.Ok)
            {
                DebugLog.Log("MessageError : " + sendResult.ToString() + " : " + message[(byte)NetOperationType.MessageID].ToString());
            }
        }

        protected override void OnConnectionEstablished(object responseObject)
        {
            DebugLog.LogWarning("Outbound : " + _serverPeerInfo.ServerName + " connected.");
            _networkHandler.OnConnect(ConnectionID, this);
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage[(int)ServerConnected.ServerId] = _serverPeerInfo.ServerID;
            outMessage[(int)ServerConnected.ServerName] = _serverPeerInfo.ServerName;
            outMessage[(int)ServerConnected.RemoteServerType] = (int)_serverPeerInfo.ServerType;
            PhotonApplication.Instance.NetHandle.OnFakeMessageArrive(MessageType.ServerHandlerMessage, ServerHandlerMessage.ServerConnected, outMessage);
        }

        protected override void OnConnectionFailed(int errorCode, string errorMessage)
        {
            DebugLog.LogWarning("Outbound : " + _serverPeerInfo.ServerName + " OnConnectionFailed.");
        }

        protected override void OnEvent(IEventData eventData, SendParameters sendParameters)
        {
            DebugLog.LogWarning("Outbound : " + _serverPeerInfo.ServerName + " OnDisconnect.");
        }
        protected override void OnOperationResponse(OperationResponse operationResponse, SendParameters sendParameters)
        {
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            _networkHandler.OnMessageArrive(operationRequest.Parameters);
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            DebugLog.LogWarning(_serverPeerInfo.ServerName + " disconnected.");
            _networkHandler.OnDisconnect(ConnectionID);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage[(int)ServerDisconnected.ServerId] = _serverPeerInfo.ServerID;
            outMessage[(int)ServerDisconnected.ServerName] = _serverPeerInfo.ServerName;
            outMessage[(int)ServerDisconnected.RemoteServerType] = (int)_serverPeerInfo.ServerType;
            PhotonApplication.Instance.NetHandle.OnFakeMessageArrive(MessageType.ServerHandlerMessage, ServerHandlerMessage.ServerDisconnected, outMessage);

            RequestFiber.Schedule(DoConnect, 5000L);
        }
    }
}