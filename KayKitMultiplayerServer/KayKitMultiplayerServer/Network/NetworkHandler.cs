using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Threading;
using KayKitMultiplayerServer.Network.ConfigReader;
using KayKitMultiplayerServer.Network.Interface;
using KayKitMultiplayerServer.Network.Server;
using KayKitMultiplayerServer.System;
using KayKitMultiplayerServer.TableRelated;
using Newtonsoft.Json;
using Photon.SocketServer;

namespace KayKitMultiplayerServer.Network
{
    public class NetworkHandler : IMessageHandler
    {
        public const int InvalidConnectionID = -1;

        private readonly string _sdByte = "#b";
        private readonly string _sdShort = "#s";
        private readonly string _sdInt = "#i";
        private readonly string _sdLong = "#l";
        private readonly string _sdFloat = "#f";
        private readonly string _sdVector3 = "#v3";


        private ReaderWriterLock _lock = new ReaderWriterLock();
        private Dictionary<int, IPeerOperator> _peerTable = new Dictionary<int, IPeerOperator>();

        public bool ServerPeerConnect(int connectionId)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(TableManager.Instance.GetTable<ServerListConfigReader>().GetInnerIpAddress(connectionId), out ipAddress)
                || ipAddress == null)
            {
                DebugLog.LogError("NetworkHandler.Connect invalid server ip");
                return false;
            }
            int port;
            if (!int.TryParse(TableManager.Instance.GetTable<ServerListConfigReader>().GetInnerPort(connectionId), out port))
            {
                DebugLog.LogError("NetworkHandler.Connect invalid format of server port = " + port);
                return false;
            }

            IPEndPoint ip = new IPEndPoint(ipAddress, port);

            ServerPeerInfo serverPeerInfo = new ServerPeerInfo(
                connectionId,
                TableManager.Instance.GetTable<ServerListConfigReader>().GetServerName(connectionId),
                ip,
                TableManager.Instance.GetTable<ServerListConfigReader>().GetServerType(connectionId));
            PhotonOutboundPeer peer = new PhotonOutboundPeer(ApplicationBase.Instance, this, serverPeerInfo);
            peer.DoConnect();

            return true;
        }

        public void Send<E>(int connectionId, E messageId, Dictionary<int, object> message)
            where E : Enum
        {
            IPeerOperator peer;
            _lock.AcquireReaderLock(1000);
            if (!_peerTable.TryGetValue(connectionId, out peer))
            {
                _lock.ReleaseReaderLock();
                return;
            }
            _lock.ReleaseReaderLock();

            if (peer != null)
            {
                peer.SendMessage(WrapMessage(messageId, message));
            }
        }
        public void Disconnect(int connectionID)
        {
            IPeerOperator peer;
            _lock.AcquireReaderLock(1000);
            if (!_peerTable.TryGetValue(connectionID, out peer))
            {
                _lock.ReleaseReaderLock();
                return;
            }
            _lock.ReleaseReaderLock();

            if (peer != null)
            {
                peer.Disconnect();
            }
        }

        #region IMessageHandler
        public void OnConnect(int connectionId, IPeerOperator peer)
        {
            _lock.AcquireWriterLock(1000);
            _peerTable.Add(connectionId, peer);
            _lock.ReleaseWriterLock();
        }
        public void OnDisconnect(int connectionId)
        {
            _lock.AcquireWriterLock(1000);
            _peerTable.Remove(connectionId);
            _lock.ReleaseWriterLock();
        }
        public void OnMessageArrive(Dictionary<byte, object> inData)
        {
            MessageType msgType;
            int messageId;
            int senderID;
            RemoteConnetionType connetionType;
            Dictionary<int, object> data;

            // 自定義類型訊息
            bool selfDefined = false;
            if (RetrivieMessageData(inData, NetOperationType.SelfDefinedType, out selfDefined))
            {
                if (selfDefined)
                {
                    RedefineSelfType(inData);
                }
            };

            if (!RetrivieMessageData(inData, NetOperationType.MessageType, out msgType)) return;
            if (!RetrivieMessageData(inData, NetOperationType.MessageID, out messageId)) return;
            if (!RetrivieMessageData(inData, NetOperationType.SenderID, out senderID)) return;
            if (!RetrivieMessageData(inData, NetOperationType.RemoteType, out connetionType)) return;
            if (!RetrivieMessageData(inData, NetOperationType.Data, out data)) return;

            SystemManager.Instance.DispatchMessage(msgType, messageId, senderID, data);
        }
        public void OnFakeMessageArrive<E>(MessageType msgType, E messageHandler, Dictionary<int, object> inData)
        {
            SystemManager.Instance.DispatchMessage(msgType,messageHandler.GetHashCode(),
                PhotonApplication.Instance.ServerInfo.ServerID, inData);
        }
        #endregion

        private bool RetrivieMessageData<E, D>(Dictionary<byte, object> message, E index, out D value)  where E : Enum
        {
            value = default;

            if (message.TryGetValue((byte)index.GetHashCode(), out object msgValue))
            {
                try
                {
                    value = (D)msgValue;
                }
                catch
                {
                    //轉型失敗  
                    DebugLog.LogFormat("Message {0} is different types at {1} true type is {2}", typeof(E).Name, index, msgValue.GetType());
                    value = default;
                    return false;
                }

                return true;
            }

            value = default;
            return false;
        }
        private Dictionary<byte, object> WrapMessage<E>(E msgType, Dictionary<int, object> message) where E : Enum
        {
            Dictionary<byte, object> retValue;
            if (typeof(E) == typeof(ClientHandlerMessage))
            {
                if (msgType.GetHashCode() > ClientHandlerMessage.LobbyPlayerMessageBegin.GetHashCode() && 
                    msgType.GetHashCode() < ClientHandlerMessage.LobbyPlayerMessageEnd.GetHashCode())
                {
                    Dictionary<int, object> playerMessage = new Dictionary<int, object>();
                    playerMessage[(int)PlayerMessage.HandlerMessageType] = msgType.GetHashCode();
                    playerMessage[(int)PlayerMessage.HandlerMessageData] = message;

                    retValue = new Dictionary<byte, object>();
                    retValue[(byte)NetOperationType.MessageID] = ClientHandlerMessage.LobbyPlayerMessage.GetHashCode();
                    retValue[(byte)NetOperationType.SenderID] = PhotonApplication.Instance.ServerInfo.ServerID;
                    retValue[(byte)NetOperationType.RemoteType] = (int)PhotonApplication.Instance.ServerInfo.ServerType;
                    retValue[(byte)NetOperationType.Data] = playerMessage;

                    return retValue;
                }
                if (msgType.GetHashCode() > ClientHandlerMessage.GamePlayerMessageBegin.GetHashCode() &&
                    msgType.GetHashCode() < ClientHandlerMessage.GamePlayerMessageEnd.GetHashCode())
                {
                    Dictionary<int, object> playerMessage = new Dictionary<int, object>();
                    playerMessage[(int)PlayerMessage.HandlerMessageType] = msgType.GetHashCode();
                    playerMessage[(int)PlayerMessage.HandlerMessageData] = message;

                    retValue = new Dictionary<byte, object>();
                    retValue[(byte)NetOperationType.MessageID] = ClientHandlerMessage.GamePlayerMessage.GetHashCode();
                    retValue[(byte)NetOperationType.SenderID] = PhotonApplication.Instance.ServerInfo.ServerID;
                    retValue[(byte)NetOperationType.RemoteType] = (int)PhotonApplication.Instance.ServerInfo.ServerType;
                    retValue[(byte)NetOperationType.Data] = playerMessage;

                    return retValue;
                }
            }

            retValue = new Dictionary<byte, object>();
            retValue[(byte)NetOperationType.MessageID] = msgType.GetHashCode();
            retValue[(byte)NetOperationType.SenderID] = PhotonApplication.Instance.ServerInfo.ServerID;
            retValue[(byte)NetOperationType.RemoteType] = (int)PhotonApplication.Instance.ServerInfo.ServerType;
            retValue[(byte)NetOperationType.Data] = message;

            return retValue;
        }
        private void RedefineSelfType(IDictionary message)
        {
            IDictionary modifiedTable = new Dictionary<object, object>();
            var iter = message.Keys.GetEnumerator();
            while (iter.MoveNext())
            {
                string temp = message[iter.Current] as string;
                if (!string.IsNullOrEmpty(temp))
                {
                    if (temp.StartsWith(_sdByte))
                    {
                        modifiedTable[iter.Current] = byte.Parse(temp.Substring(2));
                    }
                    else if (temp.StartsWith(_sdShort))
                    {
                        modifiedTable[iter.Current] = short.Parse(temp.Substring(2));
                    }
                    else if (temp.StartsWith(_sdInt))
                    {
                        modifiedTable[iter.Current] = int.Parse(temp.Substring(2));
                    }
                    else if (temp.StartsWith(_sdLong))
                    {
                        modifiedTable[iter.Current] = long.Parse(temp.Substring(2));
                    }
                    else if (temp.StartsWith(_sdFloat))
                    {
                        modifiedTable[iter.Current] = float.Parse(temp.Substring(2));
                    }
                    else if (temp.StartsWith(_sdVector3))
                    {
                        List<float> vectorData = new List<float>();

                        var data = temp.Substring(3);
                        var dataArray = data.Substring(1, data.Length - 2).Split(',');
                        Array.ForEach(dataArray, s => vectorData.Add(float.Parse(s)));

                        Vector3 vector3 = new Vector3(vectorData[0], vectorData[1], vectorData[2]);
                        modifiedTable[iter.Current] = vector3;
                    }
                }

                object[] subTable = message[iter.Current] as object[];
                if (subTable != null)
                {
                    Dictionary<int, object> regeneratedSubTable = new Dictionary<int, object>();
                    for (int i = 0; i < subTable.Length - 1; i += 2)
                    {
                        if (subTable[i] == null || !(subTable[i] is double))
                        {
                            return;
                        }
                        double key = (double)subTable[i];
                        regeneratedSubTable.Add((int)key, subTable[i + 1]);
                    }
                    if (regeneratedSubTable.ContainsKey((int)ArrayIndicator.MessageType) &&
                        regeneratedSubTable.ContainsKey((int)ArrayIndicator.MessageData))
                    {
                        modifiedTable[iter.Current] = regeneratedSubTable[(int)ArrayIndicator.MessageData];
                    }
                    else
                    {
                        RedefineSelfType(regeneratedSubTable);
                        modifiedTable[iter.Current] = regeneratedSubTable;
                    }

                }

                Dictionary<int, object> samTable = message[iter.Current] as Dictionary<int, object>;
                if (samTable != null)
                {
                    if (samTable.ContainsKey((int)ArrayIndicator.MessageType) &&
                        samTable.ContainsKey((int)ArrayIndicator.MessageData))
                    {
                        modifiedTable[iter.Current] = samTable[(int)ArrayIndicator.MessageData];
                    }
                    else
                    {
                        RedefineSelfType(samTable);
                        modifiedTable[iter.Current] = samTable;
                    }

                }
            }

            var iter2 = modifiedTable.Keys.GetEnumerator();
            while (iter2.MoveNext())
            {
                message[iter2.Current] = modifiedTable[iter2.Current];
            }
        }
    }
}