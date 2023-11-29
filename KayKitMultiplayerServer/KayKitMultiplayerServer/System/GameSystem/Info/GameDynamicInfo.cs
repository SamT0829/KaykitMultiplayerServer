using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using KayKitMultiplayerServer.Utility;
using System.Linq;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public class GameDynamicInfo
    {
        public long ServerNowTime { get; set; }
        private enum GameDynamicInfoKey
        {
            GameDynamicData,
        }

        private Dictionary<int, object> gameDynamicDataTable = new Dictionary<int, object>();

        public void AddDynamicData<TEnum>(TEnum gameDynamicDataKey, object data) where TEnum : Enum
        {
            if (!gameDynamicDataTable.TryGetValue(gameDynamicDataKey.GetHashCode(), out object gameDynamicDataValue))
            {
                gameDynamicDataTable.AddMessageItem(gameDynamicDataKey, data);
            }
            else
            {
                gameDynamicDataTable[gameDynamicDataKey.GetHashCode()] = data;
            }
        }

        public bool RetrieveDynamicData<TEnum, D>(TEnum gameDynamicDataKey, out D gameDynamicDataValue, bool isDebugLogForNotExistValue = true) where TEnum : Enum
        {
            if (gameDynamicDataTable.RetrieveMessageItem(gameDynamicDataKey, out gameDynamicDataValue, isDebugLogForNotExistValue))
            {
                return true;
            }

            gameDynamicDataValue = default;
            return false;
        }

        //public Dictionary<string, object> SerializeObject()
        //{
        //    Dictionary<string, object> data = new Dictionary<string, object>();
        //    var gameDynamicData = gameDynamicDataTable.ToDictionary(x => x.Key, x => x.Value);
        //    data.Add(GameDynamicInfoKey.GameDynamicData.ToString(), gameDynamicData);

        //    return data;
        //}
        public Dictionary<int, object> SerializeObject()
        {
            Dictionary<int, object> data = new Dictionary<int, object>();
            var gameDynamicData = gameDynamicDataTable.ToDictionary(x => x.Key, x => x.Value);
            data.Add(GameDynamicInfoKey.GameDynamicData.GetHashCode(), gameDynamicData);

            return data;
        }

        public void DeserializeObject(Dictionary<int, object> data)
        {
            if (!data.RetrieveMessageItem(GameDynamicInfoKey.GameDynamicData, out Dictionary<int, object> gameDynamicData))
            {
                gameDynamicDataTable.Add(((int)GameDynamicInfoKey.GameDynamicData), gameDynamicData);
            }
        }
    }
}