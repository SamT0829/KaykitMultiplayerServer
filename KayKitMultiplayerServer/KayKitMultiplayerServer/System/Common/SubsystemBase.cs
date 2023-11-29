using System;
using System.Collections.Generic;

namespace KayKitMultiplayerServer.System.Common
{
    public abstract class SubsystemBase
    {
        private Dictionary<MessageType, Dictionary<int, Action<int, Dictionary<int, object>>>> _msgTypeToReceiver =
            new Dictionary<MessageType, Dictionary<int, Action<int, Dictionary<int, object>>>>();


        public abstract void ShutDown();

        protected static object RetrieveMessageItem(Dictionary<int, object> message, int index)
        {
            object returnValue;
            message.TryGetValue(index, out returnValue);
            return returnValue;
        }

        protected static bool RetrieveMessageItem<E, T>(Dictionary<int, object> message, E index, out T value, bool isDebugLogForNotExistValue = true) where E : Enum
        {
            value = default;
            if (message.TryGetValue(index.GetHashCode(), out object msgValue))
            {
                value = (T)msgValue;
                if (value != null)
                    return true;

                DebugLog.LogFormat("Message {0} is different types at {1}", typeof(E).Name, index);
                return false;
            }

            if (isDebugLogForNotExistValue)
                DebugLog.LogFormat("Message {0} has not value at {1}", typeof(E).Name, index);
            return false;
        }

        protected static void AddMessageItem(Dictionary<int, object> message, int index, object item)
        {
            if (!message.ContainsKey(index.GetHashCode()))
            {
                message.Add(index.GetHashCode(), item);
            }
        }

        protected static void AddMessageItem<E>(Dictionary<int, object> message, E index, object item) where E : Enum
        {
            if (!message.ContainsKey(index.GetHashCode()))
            {
                message.Add(index.GetHashCode(), item);
            }
        }

        public virtual void OnReceiveMessage(MessageType msgType, int messageId, int senderID, Dictionary<int, object> msg)
        {
            Dictionary<int, Action<int, Dictionary<int, object>>> functionTable;
            Action<int, Dictionary<int, object>> function;

            if (_msgTypeToReceiver.TryGetValue(msgType, out functionTable) && functionTable != null)
            {
                if (functionTable.TryGetValue(messageId, out function) && function != null)
                {
                    function(senderID, msg);
                }
            }
        }
        protected virtual void RegisterObservedMessage<E>(MessageType msgType, E messageHandle, Action<int, Dictionary<int, object>> listener) where E : Enum
        {
            Dictionary<int, Action<int, Dictionary<int, object>>> listenerMessage;
            if (!_msgTypeToReceiver.TryGetValue(msgType, out listenerMessage))
            {
                listenerMessage = new Dictionary<int, Action<int, Dictionary<int, object>>>();
                _msgTypeToReceiver.Add(msgType, listenerMessage);
            }

            if (!listenerMessage.ContainsKey(messageHandle.GetHashCode()))
            {
                listenerMessage.Add(messageHandle.GetHashCode(), listener);
            }
           
            SystemManager.Instance.AddMsgTypeListener(msgType, messageHandle,GetType());
        }
        protected virtual void UnregisterObservedMessage<E>(MessageType msgType, E messageHandle) where E : Enum
        {
            Dictionary<int, Action<int, Dictionary<int, object>>> listenerMessage;
            if (_msgTypeToReceiver.TryGetValue(msgType, out listenerMessage))
            {
                listenerMessage.Remove(messageHandle.GetHashCode());
            }

            if(listenerMessage.Count == 0)
                _msgTypeToReceiver.Remove(msgType);
            
            SystemManager.Instance.RemoveMsgTypeListener(msgType, messageHandle, GetType());
        }
    }
}