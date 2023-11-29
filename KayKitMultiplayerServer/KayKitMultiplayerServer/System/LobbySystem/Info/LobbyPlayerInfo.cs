using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using System.Collections.Generic;
using System;
using KayKitMultiplayerServer.System.LobbySystem.Model;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.LobbySystem.Info
{
    public class LobbyPlayerInfo
    {
        private enum LobbyPlayerData
        {
            AccountId,
            GameType,
            NickName,
            Status,
            TotalPlayCount,
            TotalLoseCount,
            TotalWinCount,
        }
       
        public long AccountId { get; set; }
        public GameType GameType { get; set; }
        public string NickName { get; set; }
        public LobbyPlayerStatus Status { get; set; }
        public int TotalPlayCount { get; set; }
        public int TotalLoseCount { get; set; }
        public int TotalWinCount { get; set; }

        // Db Function
        public ErrorCode ReadDB_InitLobbyPlayerInfo(Dictionary<string, object> reader)
        {
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_AccountId, out long accountId)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_NickName, out string nickName)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_LobbyPlayerStatus, out string playerStatus)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_GameType, out string gameTypeName)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_TotalPlayCount, out int totalPlayCount)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_TotalWinCount, out int totalWinCount)) return ErrorCode.PlayerUnableRetrieveData;
            if (!DBManager.GetDbParam(reader, DbStoreProcedureOutput.LobbyPlayerInfo_TotalLoseCount, out int totalLoseCount)) return ErrorCode.PlayerUnableRetrieveData;
            if (!Enum.TryParse(playerStatus, out LobbyPlayerStatus status)) return ErrorCode.PlayerUnableRetrieveData;
            if (!Enum.TryParse(gameTypeName, out GameType gameType)) return ErrorCode.PlayerUnableRetrieveData;

            AccountId = accountId;
            NickName = nickName;
            Status = status;
            GameType = gameType;
            TotalPlayCount = totalPlayCount;
            TotalWinCount = totalWinCount;
            TotalLoseCount = totalLoseCount;
            return ErrorCode.Success;
        }
        public void UpdateLobbyPlayerInfo_ToDb(IFiber fiber, Action<int> resultFunc)
        {
            string gameTypeName = Enum.ToObject(typeof(GameType), GameType).ToString();
            string playerStatus = Enum.ToObject(typeof(LobbyPlayerStatus), Status).ToString();

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_AccountId, AccountId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_GameType, gameTypeName)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_NickName, NickName)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_PlayerStatus, playerStatus)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_TotalPlayCount, TotalPlayCount)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_TotalWinCount, TotalWinCount)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.UpdateLobbyPlayerInfo_TotalLoseCount, TotalLoseCount)) return;

            //DebugLog.Log("[B lobby SP] params=[" + string.Join(";", parameters.Select(x => x.Key + "=" + x.Value)) + "]");
            DBManager.Instance.ExecuteNonQuery(DBCatagory.Lobby, DbStoreProcedureInput.UpdateLobbyPlayerInfo, parameters, fiber, resultFunc);
        }
        public string JsonSerializeObject()
        {
            return JsonConvert.SerializeObject(this);
        }
        public Dictionary<int, object> DictionarySerializeObject()
        {
            Dictionary<int, object> retv = new Dictionary<int, object>();
            retv.Add(LobbyPlayerData.AccountId.GetHashCode(), AccountId);
            retv.Add(LobbyPlayerData.GameType.GetHashCode(), GameType);
            retv.Add(LobbyPlayerData.NickName.GetHashCode(), NickName);
            retv.Add(LobbyPlayerData.Status.GetHashCode(), Status);
            retv.Add(LobbyPlayerData.TotalPlayCount.GetHashCode(), TotalPlayCount);
            retv.Add(LobbyPlayerData.TotalLoseCount.GetHashCode(), TotalLoseCount);
            retv.Add(LobbyPlayerData.TotalWinCount.GetHashCode(), TotalWinCount);

            return retv;
        }

        public List<object> SerializeObject()
        {
            List<object> retv = new List<object>();
            retv.Add(AccountId);                    //0
            retv.Add(NickName);                     //1
            retv.Add(Status);                       //2
            retv.Add(GameType);                     //3
            retv.Add(TotalPlayCount);               //4
            retv.Add(TotalLoseCount);               //5
            retv.Add(TotalWinCount);                //6

            return retv;
        }
        public void DeserializeObject(object[] retv)
        {
            AccountId = Convert.ToInt64(retv[0]);
            NickName = retv[1].ToString();
            Status = (LobbyPlayerStatus)Convert.ToInt32(retv[2]);
            GameType = (GameType)Convert.ToInt32(retv[3]);
            TotalPlayCount = Convert.ToInt32(retv[4]);
            TotalLoseCount = Convert.ToInt32(retv[5]);
            TotalWinCount = Convert.ToInt32(retv[6]);
        }
    }
}