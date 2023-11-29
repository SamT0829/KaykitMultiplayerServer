using System.Collections.Generic;
using KayKitMultiplayerServer.Network.Interface;
using Newtonsoft.Json;
using Photon.SocketServer;
using PhotonHostRuntimeInterfaces;

namespace KayKitMultiplayerServer.Network.Client
{
    public class PhotonPeer : ClientPeer, IPeerOperator
    {
        private readonly IMessageHandler _networkHandler;
        private SendParameters _sendParameter = new SendParameters();

        public int PeerId { get; private set; }

        public PhotonPeer(InitRequest initRequest, int connectPeerId, IMessageHandler networkHandler) : base(initRequest)
        {
            _networkHandler = networkHandler;
            PeerId = connectPeerId;
            _networkHandler.OnConnect(PeerId, this);

            DebugLog.LogFormat("Peer connection with ID : {0}", PeerId);
        }

        protected override void OnOperationRequest(OperationRequest operationRequest, SendParameters sendParameters)
        {
            if (operationRequest.OperationCode == (byte)NetOperationCode.ClientServer)
            {
                operationRequest.Parameters[(byte)NetOperationType.MessageType] = MessageType.ClientHandlerMessage;
                operationRequest.Parameters[(byte)NetOperationType.SenderID] = PeerId;
                _networkHandler.OnMessageArrive(operationRequest.Parameters);
            }
        }

        protected override void OnDisconnect(DisconnectReason reasonCode, string reasonDetail)
        {
            _networkHandler.OnDisconnect(PeerId);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage[(int)ClientDisconnected.ConnectionId] = PeerId;
            _networkHandler.OnFakeMessageArrive(MessageType.ServerHandlerMessage, ServerHandlerMessage.ClientDisconnected, outMessage);

            DebugLog.LogFormat("Peer disconnect with ID : " + PeerId + ", IP : " + RemoteIP);
        }

        public void SendMessage(Dictionary<byte, object> message)
        {
            OperationResponse response = new OperationResponse((byte)NetOperationCode.ServerClient, message);
            message.Add((byte)NetOperationType.MessageType, MessageType.ClientHandlerMessage.GetHashCode());
            SendOperationResponse(response, _sendParameter);
        }
    }
}