using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.System.GameSystem.Model;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.GameSystem.Subsystems
{
    public class GamePlayerSystem : ServerSubsystemBase
    {
        // all LobbyPlayer Table
        private Dictionary<long, GamePlayer> _accountIdGamePlayerTable = new Dictionary<long, GamePlayer>();

        // Waiting Table
        private Dictionary<int, GamePlayer> _sessionIdWaitingEnterGamePlayerTable = new Dictionary<int, GamePlayer>();

        //緩存玩家離開
        private Dictionary<long, GamePlayer> _accountIdLeaverTable = new Dictionary<long, GamePlayer>();

        // GamePlayer network message event table
        private Dictionary<long, Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>> _gamePlayerAccountIdClientEventTable =
            new Dictionary<long, Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>>();
        public GamePlayerSystem(IFiber systemFiber) : base(systemFiber)
        {
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.GamePlayerMessage, OnGamePlayerMessage);
        }

        public void PlayerWaitingEnterGame(GamePlayer gamePlayer)
        {
            _sessionIdWaitingEnterGamePlayerTable[gamePlayer.ClientInfo.SessionId] = gamePlayer;
        }
        public GamePlayer PlayerEnterGame(int sessionId)
        {
            if (_sessionIdWaitingEnterGamePlayerTable.TryGetValue(sessionId, out GamePlayer gamePlayer))
            {
                AddPlayer(gamePlayer);
            }
            return gamePlayer;
        }
        public void AddPlayer(GamePlayer gamePlayer)
        {
            GamePlayer existGamePlayer = GetPlayerByAccountId(gamePlayer.ClientInfo.AccountId);

            if (existGamePlayer != null)
            {
                Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                AddMessageItem(outMessageToPrevious, KickPlayer.ErrorCode, ErrorCode.PlayerKickByDuplicatedLogin.GetHashCode());
                PhotonApplication.Instance.NetHandle.Send(existGamePlayer.ClientInfo.GameConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                PhotonApplication.Instance.NetHandle.Disconnect(existGamePlayer.ClientInfo.GameConnectionId);
                RemovePlayer(existGamePlayer);
            }

            _accountIdGamePlayerTable[gamePlayer.ClientInfo.AccountId] = gamePlayer;
            gamePlayer.RegesterMessageObserver();
        }
        public void RemovePlayer(GamePlayer gamePlayer)
        {
            _accountIdGamePlayerTable.Remove(gamePlayer.ClientInfo.AccountId);
            DebugLog.Log("Remove lobbyLobbyPlayerInfo: accountId=" + gamePlayer.ClientInfo.AccountId);
            _accountIdLeaverTable[gamePlayer.ClientInfo.AccountId] = gamePlayer;

            gamePlayer.UnregesterMessageObserver();
            gamePlayer.LeaveGameRoom();
        }
        public GamePlayer GetGamePlayerByConnectionId(int connectionId)
        {
            GamePlayer gamePlayer = _accountIdGamePlayerTable.Values.FirstOrDefault(x => x.ClientInfo.GameConnectionId == connectionId);
            return gamePlayer;
        }
        public GamePlayer GetPlayerByAccountId(long accountId)
        {
            GamePlayer gamePlayer;
            if (!_accountIdGamePlayerTable.TryGetValue(accountId, out gamePlayer))
            {
                //_accountIdLeaverTable.TryGetValue(accountId, out lobbyLobbyPlayerInfo);
            }
            return gamePlayer;
        }
        public void RemovePlayerByAccountId(long accountId)
        {
            GamePlayer gamePlayer = GetPlayerByAccountId(accountId);
            if (gamePlayer != null)
            {
                RemovePlayer(gamePlayer);
            }
        }

        public void RegisterPlayerObservedMessage(GamePlayer gamePlayer, ClientHandlerMessage msgType, Action<int, Dictionary<int, object>> listener)
        {
            Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>> msgActionTable;

            if (!_gamePlayerAccountIdClientEventTable.TryGetValue(gamePlayer.ClientInfo.AccountId, out msgActionTable) || msgActionTable == null)
            {
                msgActionTable = new Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>>();
                _gamePlayerAccountIdClientEventTable[gamePlayer.ClientInfo.AccountId] = msgActionTable;
            }
            msgActionTable[msgType] = listener;
        }

        public void UnregisterPlayerObservedMessage(GamePlayer gamePlayer, ClientHandlerMessage msgType)
        {
            Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>> msgActionTable;
            if (!_gamePlayerAccountIdClientEventTable.TryGetValue(gamePlayer.ClientInfo.AccountId, out msgActionTable) || msgActionTable == null)
            {
                return;
            }
            msgActionTable.Remove(msgType);
        }
        private void OnGamePlayerMessage(int connectionId, Dictionary<int, object> msg)
        {
            ClientHandlerMessage msgType;
            Dictionary<int, object> message;

            if (!RetrieveMessageItem(msg, PlayerMessage.HandlerMessageType, out msgType)) return;
            if (!RetrieveMessageItem(msg, PlayerMessage.HandlerMessageData, out message)) return;

            GamePlayer gamePlayer = GetGamePlayerByConnectionId(connectionId);
            if (gamePlayer == null)
            {
                return;
            }

            Dictionary<ClientHandlerMessage, Action<int, Dictionary<int, object>>> msgActionTable;
            if (_gamePlayerAccountIdClientEventTable.TryGetValue(gamePlayer.ClientInfo.AccountId, out msgActionTable) && msgActionTable != null)
            {
                Action<int, Dictionary<int, object>> func;
                if (msgActionTable.TryGetValue(msgType, out func) && func != null)
                {
                    func(connectionId, message);
                }
            }
        }
    }
}