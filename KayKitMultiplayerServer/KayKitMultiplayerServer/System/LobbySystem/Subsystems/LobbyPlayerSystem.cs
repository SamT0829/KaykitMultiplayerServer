using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.System.LobbySystem.Model;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using static Google.Protobuf.Reflection.FieldOptions.Types;

namespace KayKitMultiplayerServer.System.LobbySystem.Subsystems
{
    public class LobbyPlayerSystem : ServerSubsystemBase
    {
        // all LobbyPlayer Table
        private Dictionary<long, LobbyPlayer> _accountIdLobbyPlayerTable = new Dictionary<long, LobbyPlayer>();

        // Waiting Table
        private Dictionary<int, LobbyPlayer> _sessionIdWaitingEnterLobbyPlayerTable = new Dictionary<int, LobbyPlayer>();

        // lobbyPlayer network message event table
        private Dictionary<long, Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>> _accountIdLobbyPlayerHanlderTable =
            new Dictionary<long, Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>>();

        public LobbyPlayerSystem(IFiber systemFiber) : base(systemFiber)
        {
            new BackgroundThread<long, LobbyPlayer>(systemFiber, _accountIdLobbyPlayerTable, 30L, LobbyPlayerBackgroundThreadUpdateAction);

            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Proxy2LobbyPlayerEnteredRespond, OnProxy2LobbyPlayerEnteredRespond);

            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.LobbyPlayerMessage, OnLobbyPlayerMessage);
        }
        public LobbyPlayer GetPlayerByConnectionId(int connectionId)
        {
            LobbyPlayer lobbyPlayer = _accountIdLobbyPlayerTable.Values.FirstOrDefault(x => x.ClientInfo.LobbyConnectionId == connectionId);
            return lobbyPlayer;
        }
        public LobbyPlayer GetPlayerByAccountId(long accountId)
        {
            LobbyPlayer lobbyPlayer;
            if (!_accountIdLobbyPlayerTable.TryGetValue(accountId, out lobbyPlayer))
            {
                //_accountIdLeaverTable.TryGetValue(accountId, out lobbyLobbyPlayerInfo);
            }
            return lobbyPlayer;
        }
        public LobbyPlayer GetWaitingEnterPlayerBySessionId(int sessionId)
        {
            LobbyPlayer lobbyPlayer;
            if (!_sessionIdWaitingEnterLobbyPlayerTable.TryGetValue(sessionId, out lobbyPlayer))
            {
                //_accountIdLeaverTable.TryGetValue(accountId, out lobbyLobbyPlayerInfo);
            }
            return lobbyPlayer;
        }
        public void PlayerWaitingEnterLobby(LobbyPlayer lobbyPlayer)
        {
            _sessionIdWaitingEnterLobbyPlayerTable[lobbyPlayer.ClientInfo.SessionId] = lobbyPlayer;
        }
        public ErrorCode RemoveExistPlayer(LobbyPlayer lobbyPlayer)
        {
            ErrorCode err = ErrorCode.Success;
            LobbyPlayer existLobbyPlayer = GetPlayerByAccountId(lobbyPlayer.ClientInfo.AccountId);

            if (existLobbyPlayer != null)
            {
                Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                AddMessageItem(outMessageToPrevious, (int)KickPlayer.ErrorCode, (int)ErrorCode.PlayerKickByDuplicatedLogin);
                PhotonApplication.Instance.NetHandle.Send(existLobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                PhotonApplication.Instance.NetHandle.Disconnect(existLobbyPlayer.ClientInfo.LobbyConnectionId);

                RemovePlayer(existLobbyPlayer);
            }

            return err;
        }
   
        public void RegisterPlayerObservedMessage(LobbyPlayer lobbyPlayer, ClientHandlerMessage msgType, Action<int, Dictionary<int, object>> listener)
        {
            Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>> clientHandleTable;

            if (!_accountIdLobbyPlayerHanlderTable.TryGetValue(lobbyPlayer.ClientInfo.AccountId, out clientHandleTable) || clientHandleTable == null)
            {
                clientHandleTable = new Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>();
                _accountIdLobbyPlayerHanlderTable[lobbyPlayer.ClientInfo.AccountId] = clientHandleTable;
            }
            clientHandleTable[msgType] = listener;
        }
        public void UnregisterPlayerObservedMessage(LobbyPlayer lobbyPlayer, ClientHandlerMessage msgType)
        {
            Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>> clientHandleTable;
            if (!_accountIdLobbyPlayerHanlderTable.TryGetValue(lobbyPlayer.ClientInfo.AccountId, out clientHandleTable) || clientHandleTable == null)
            {
                return;
            }
            clientHandleTable.Remove(msgType);
        }
        private void AddPlayer(LobbyPlayer lobbyPlayer)
        {
            LobbyPlayer existLobbyPlayer = GetPlayerByAccountId(lobbyPlayer.ClientInfo.AccountId);

            if (existLobbyPlayer != null)
            {
                Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                AddMessageItem(outMessageToPrevious, KickPlayer.ErrorCode.GetHashCode(), ErrorCode.PlayerKickByDuplicatedLogin.GetHashCode());
                PhotonApplication.Instance.NetHandle.Send(existLobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                PhotonApplication.Instance.NetHandle.Disconnect(existLobbyPlayer.ClientInfo.LobbyConnectionId);
                RemovePlayer(existLobbyPlayer);
            }

            _accountIdLobbyPlayerTable[lobbyPlayer.ClientInfo.AccountId] = lobbyPlayer;
            lobbyPlayer.RegesterMessageObserver();
        }
        public void RemovePlayerByAccountId(long accountId)
        {
            LobbyPlayer lobbyPlayer = GetPlayerByAccountId(accountId);
            if (lobbyPlayer != null)
            {
                RemovePlayer(lobbyPlayer);
            }
        }
        public void RemovePlayer(LobbyPlayer lobbyPlayer)
        {
            _accountIdLobbyPlayerTable.Remove(lobbyPlayer.ClientInfo.AccountId);

            DebugLog.Log("Remove LobbyPlayerInfo: accountId=" + lobbyPlayer.ClientInfo.AccountId);

            lobbyPlayer.UnregesterMessageObserver();
            lobbyPlayer.LeaveLobbyRoom();
        }
        // Thread Update
        private void LobbyPlayerBackgroundThreadUpdateAction(LobbyPlayer lobbyPlayer)
        {
            Dictionary<int, object> outMessage;

            switch (lobbyPlayer.LobbyPlayerInfo.Status)
            {
                case LobbyPlayerStatus.Lobby:
                    List<object> roomListData = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().GetRoomListData();
                    outMessage = new Dictionary<int, object>();
                    AddMessageItem(outMessage, LobbyPlayerBackgroundThread.LobbyRoomListData.GetHashCode(), roomListData);
                    PhotonApplication.Instance.NetHandle.Send(lobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerBackgroundThread, outMessage);
                    break;
            }
        }

        // Network Event
        private void OnProxy2LobbyPlayerEnteredRespond(int connectionId, Dictionary<int, object> msg)
        {
            ErrorCode err;
            long accountId;
            int sessionId;

            if (!RetrieveMessageItem(msg, Proxy2LobbyPlayerEnteredRespond.ErrorCode, out err)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyPlayerEnteredRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyPlayerEnteredRespond.SessionId, out sessionId)) return;

            LobbyPlayer lobbyPlayer;
            if (!_sessionIdWaitingEnterLobbyPlayerTable.TryGetValue(sessionId, out lobbyPlayer) || lobbyPlayer == null)
            {
                DebugLog.Log("Client login: PlayerSystem lost temp lobbyLobbyPlayerInfo: " + sessionId);
                return;
            }
            _sessionIdWaitingEnterLobbyPlayerTable.Remove(sessionId);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            if (err == ErrorCode.PlayerKickByDuplicatedLogin)
            {
                AddMessageItem(outMessage, LobbyConnectedRespond.ErrorCode, (int)err);
                AddMessageItem(outMessage, LobbyConnectedRespond.ServerTime, DateTime.Now.Ticks);
                PhotonApplication.Instance.NetHandle.Send(lobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyConnectedRespond, outMessage);
                PhotonApplication.Instance.NetHandle.Disconnect(lobbyPlayer.ClientInfo.LobbyConnectionId);
                return;
            }

            AddPlayer(lobbyPlayer);

            // Recconnect game
            //if (lobbyPlayer.LobbyPlayerInfo.Status == LobbyPlayerStatus.Game)
            //{
            //    Dictionary<string, object> parameter = new Dictionary<string, object>();
            //    DBManager.AddDbParam(parameter, DbStoreProcedureInput.LobbyRetrieveLobbyPlayerInfo_AccountId, lobbyPlayer.LobbyPlayerInfo.AccountId);
            //    DBManager.Instance.ExecuteReader(DBCatagory.Lobby, DbStoreProcedureInput.LobbyRetrieveLobbyPlayerInfo,
            //        parameter, _systemFiber, reader => OnDbCallback_RetrieveLobbyPlayerInfo(reader, lobbyPlayer));
            //}

            lobbyPlayer.LobbyPlayerInfo.Status = LobbyPlayerStatus.Lobby;

            AddMessageItem(outMessage, LobbyConnectedRespond.ErrorCode, (int)err);
            AddMessageItem(outMessage, LobbyConnectedRespond.LobbyPlayerInfo, lobbyPlayer.LobbyPlayerInfo.JsonSerializeObject());
            AddMessageItem(outMessage, LobbyConnectedRespond.LobbyGameInfo, lobbyPlayer.LobbyPlayerGameInfo.JsonSerializeObject());
            AddMessageItem(outMessage, LobbyConnectedRespond.ServerTime, DateTime.Now.Ticks);
            PhotonApplication.Instance.NetHandle.Send(lobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyConnectedRespond, outMessage);
        }
        private void OnLobbyPlayerMessage(int connectionId, Dictionary<int, object> msg)
        {
            ClientHandlerMessage msgType;
            Dictionary<int, object> message;

            if (!RetrieveMessageItem(msg, PlayerMessage.HandlerMessageType, out msgType)) return;
            if (!RetrieveMessageItem(msg, PlayerMessage.HandlerMessageData, out message)) return;

            LobbyPlayer lobbyPlayer = GetPlayerByConnectionId(connectionId);
            if (lobbyPlayer == null)
            {
                DebugLog.LogErrorFormat("Can't Get Player By Connection Id : {0}", connectionId);
                return;
            }

            var handlerActionTable = new Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>();
            if (_accountIdLobbyPlayerHanlderTable.TryGetValue(lobbyPlayer.ClientInfo.AccountId, out handlerActionTable) && handlerActionTable != null)
            {
                Action<int, Dictionary<int, object>> func;
                if (handlerActionTable.TryGetValue(msgType, out func) && func != null)
                {
                    func(connectionId, message);
                }
            }
        }
    }
}