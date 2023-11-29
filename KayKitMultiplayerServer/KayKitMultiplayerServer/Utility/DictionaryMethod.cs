using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KayKitMultiplayerServer.Utility
{
    public static class DictionaryMethod
    {
        public static bool RetrieveMessageItem<E, D>(this Dictionary<int, object> message, E index, out D value, bool isDebugLogForNotExistValue = true) where E : Enum
        {
            value = default;

            if (message.TryGetValue(index.GetHashCode(), out object msgValue))
            {
                try
                {
                    value = (D)msgValue;
                }
                catch
                {
                    //轉型失敗  
                    DebugLog.LogErrorFormat("Message {0} is different types at {1}, true type is {2}", typeof(E).Name, index, msgValue.GetType());
                    value = default;
                    return false;
                }

                return true;
            }

            if (isDebugLogForNotExistValue)
                DebugLog.LogErrorFormat("Message {0} is NotExistValue types at {1}", typeof(E).Name, index);

            value = default;
            return false;
        }
        public static bool RetrieveMessageItem<E, D>(this Dictionary<string, object> message, E index, out D value, bool isDebugLogForNotExistValue = true) where E : Enum
        {
            value = default;

            if (message.TryGetValue(index.ToString(), out object msgValue))
            {
                try
                {
                    value = (D)msgValue;
                }
                catch
                {
                    //轉型失敗  
                    DebugLog.LogErrorFormat("Message {0} is different types at {1}", typeof(E).Name, index);
                    value = default;
                    return false;
                }

                return true;
            }

            if (isDebugLogForNotExistValue)
                DebugLog.LogErrorFormat("Message {0} is NotExistValue types at {1}", typeof(E).Name, index);

            value = default;
            return false;
        }
        public static void AddMessageItem<E>(this Dictionary<int, object> message, E index, object item) where E : Enum
        {
            if (!message.ContainsKey(index.GetHashCode()))
            {
                message.Add(index.GetHashCode(), item);
            }
        }
    }
}