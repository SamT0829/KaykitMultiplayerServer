using System.Collections.Generic;

namespace KayKitMultiplayerServer.Network.Interface
{
    public interface IMessageHandler
    {
        void OnMessageArrive(Dictionary<byte, object> inData);

        void OnFakeMessageArrive<E>(MessageType msgType, E messageHandler, Dictionary<int, object> inData);

        void OnConnect(int connectionId, IPeerOperator peer);

        void OnDisconnect(int connectionId);
    }
}