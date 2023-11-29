using System.Collections.Generic;

namespace KayKitMultiplayerServer.Network.Interface
{
    public interface IPeerOperator
    {
        void SendMessage(Dictionary<byte, object> message);
        void Disconnect();
    }
}