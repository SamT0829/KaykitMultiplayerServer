using System;
using KayKitMultiplayerServer.Utility;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public class GameStaticInfo
    {
        protected enum GameStaticInfoKey
        {
            GameStaticData,
        }
       
        public Dictionary<int, object> gameStaticDataTable = new();

        public void AddStaticData<TEnum>(TEnum gameStaticDataKey, object data) where TEnum : Enum
        {
            if (!gameStaticDataTable.TryGetValue(gameStaticDataKey.GetHashCode(), out object gameDynamicDataValue))
            {
                gameStaticDataTable.AddMessageItem(gameStaticDataKey, data);
            }
            else
            {
                gameStaticDataTable[gameStaticDataKey.GetHashCode()] = data;
            }
        }

        public bool RetrieveStaticData<TEnum, D>(TEnum gameStaticDataKey, out D gameStaticDataValue, bool isDebugLogForNotExistValue = true) where TEnum : Enum
        {
            if (gameStaticDataTable.RetrieveMessageItem(gameStaticDataKey, out gameStaticDataValue, isDebugLogForNotExistValue))
            {
                return true;
            }

            gameStaticDataValue = default;
            return false;
        }

        public Dictionary<int, object> SerializeObject()
        {
            Dictionary<int, object> data = new Dictionary<int, object>();
            data.Add(GameStaticInfoKey.GameStaticData.GetHashCode(), gameStaticDataTable);

            return data;
        }

        public void DeserializeObject(Dictionary<int, object> data)
        {
            if (!data.RetrieveMessageItem(GameStaticInfoKey.GameStaticData, out Dictionary<int, object> gameDynamicData))
            {
                gameStaticDataTable.AddMessageItem(GameStaticInfoKey.GameStaticData, gameDynamicData);
            }
        }
    }
}