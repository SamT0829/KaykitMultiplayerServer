using KayKitMultiplayerServer.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayKitMultiplayerServer.TableRelated.Application
{
    public class PlayerPositionTable : TableBase
    {
        private const string s_id = "UID";
        private const string s_gameType = "GameType";
        private const string s_position = "Position";

        private Dictionary<GameType, Dictionary<int, Vector3>> _gameTypePlayerPositionTable =
            new Dictionary<GameType, Dictionary<int, Vector3>>();


        public Vector3 GetPlayerPosition(GameType gameType, int UID)
        {
            Vector3 position = Vector3.zero;
            if (_gameTypePlayerPositionTable.TryGetValue(gameType, out Dictionary<int, Vector3> positionTable))
            {
                if (positionTable.TryGetValue(UID, out position))
                    return position;
            }

            DebugLog.LogErrorFormat("Can't Get start position from {0} game type ,{1} uid", gameType, UID);
            return position;
        }


        protected override void OnRowParsed(List<object> rowContent)
        {
            int uid = rowContent[GetColumnNameIndex(s_id)] as ValueTypeWrapper<int>;
            string gameTypeName = rowContent[GetColumnNameIndex(s_gameType)] as ValueTypeWrapper<string>;
            GameType gameType = (GameType)Enum.Parse(typeof(GameType), gameTypeName);
            string positionString = rowContent[GetColumnNameIndex(s_position)] as ValueTypeWrapper<string>;
            Vector3 startPosition = positionString.ToVector3();
          

            if (!_gameTypePlayerPositionTable.TryGetValue(gameType, out Dictionary<int, Vector3> startPositionTable))
            {
                startPositionTable = new Dictionary<int, Vector3>();
                _gameTypePlayerPositionTable[gameType] = startPositionTable;
            }

            if (!startPositionTable.TryGetValue(uid, out Vector3 vectorPosition))
            {
                vectorPosition = startPosition;
                startPositionTable[uid] = vectorPosition;
            }
        }

        protected override void OnTableParsed()
        {
        }
    }
}