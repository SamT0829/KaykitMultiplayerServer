using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;

namespace KayKitMultiplayerServer.System.MainSystems
{
    public class FinanceMainSystem : Common.MainSystem
    {
        public FinanceMainSystem(IFiber systemFiber) : base(systemFiber)
        {
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2FinanceQueryDataRequest, OnLobby2FinanceQueryDataRequest);
        }

        protected override void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType)
        {
        }

        private void OnLobby2FinanceQueryDataRequest(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            GameType gameType;

            if (!RetrieveMessageItem(msg, Lobby2FinanceQueryDataRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2FinanceQueryDataRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2FinanceQueryDataRequest.GameType, out gameType)) return;

            string gameTypeName = Enum.ToObject(typeof(GameType), gameType).ToString();

            //Get LobbyPlayer Finance Data
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.RetrieveCashData_GameType, gameTypeName)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.RetrieveCashData_AccoutID, accountId)) return;
            DBManager.Instance.ExecuteReader(DBCatagory.Finance, DbStoreProcedureInput.RetrieveCashData,
                parameters, _systemFiber, (reader) =>
                    OnDbCallback_RetrieveLobbyPlayerCashData(reader, connectionId, accountId, sessionId, gameType));
        }

        private void OnDbCallback_RetrieveLobbyPlayerCashData(List<Dictionary<string, object>> reader, int connectionId, long accountId, int sessionId, GameType gameType)
        {
            long money = 0L;
            long diamond = 0L;
            ErrorCode err = ErrorCode.Success;

            if (reader.Count <= 0)
            {
                DebugLog.Log("[A account SP] reader count=0");
                err = ErrorCode.FirmUnableRetrieveCashData;
            }
            else
            {
                Dictionary<string, object> needReader = reader[0];
                DebugLog.Log("[A account SP] reader[0]=[" + string.Join(";", reader[0].Select(x => x.Key + "=" + x.Value)) + "]");
                if (!DBManager.GetDbParam(needReader, DbStoreProcedureOutput.RetrieveLobbyPlayerCashData_Money, out money)) { err = ErrorCode.FirmUnableRetrieveCashData; }
                if (!DBManager.GetDbParam(needReader, DbStoreProcedureOutput.RetrieveLobbyPlayerCashData_Diamond, out diamond)) { err = ErrorCode.FirmUnableRetrieveCashData; }
            }

            On2LobbyQueryMoneyRespond(connectionId, err, accountId, sessionId, gameType, money, diamond);
        }

        private void On2LobbyQueryMoneyRespond(int connectionId, ErrorCode errorCode, long accountId, int sessionId, GameType gameType,
            long money, long diamond)
        {
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.ErrorCode, errorCode);
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.GameType, gameType);
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.AccountId, accountId);
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.SessionId, sessionId);
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.Money, money);
            AddMessageItem(outMessage, Finance2LobbyQueryDataResond.Diamond, diamond);

            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Finance2LobbyQueryDataResond, outMessage);
        }
    }
}