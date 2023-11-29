using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using KayKitMultiplayerServer.System.GameSystem.Model;
using MySqlX.XDevAPI.Common;
using Photon.SocketServer;
using static Google.Protobuf.Reflection.FieldOptions.Types;
using MySqlX.XDevAPI;
using UnityEngine;
using KayKitMultiplayerServer.TableRelated.Application;
using KayKitMultiplayerServer.TableRelated;
using System.Linq;
using System.Numerics;
using KayKitMultiplayerServer.System.GameSystem.Games;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.MainSystems
{
    public class GameMainSystem : Common.MainSystem
    {
        // Waiting table
        private Dictionary<int, ClientInfo> _sessionIdCientInfoWaitingEnterTable = new Dictionary<int, ClientInfo>();

        private Dictionary<long, ClientInfo> _accountIdClientInfoIdTable = new Dictionary<long, ClientInfo>();
        // Leave table
        private List<ClientInfo> _leaveClientInfoTable = new List<ClientInfo>();

        public GameMainSystem(IFiber systemFiber) : base(systemFiber)
        {
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.ClientDisconnected, OnClientDisconnected);

            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2GameLobbyRoomEnteredRequest, OnLobby2GameLobbyRoomEnteredRequest);

            // Room Game
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2GameLobbyPlayerPrepareEnterRequest, OnLobby2GameLobbyPlayerPrepareEnterRequest);
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.GameConnectedRequest, OnGameConnectedRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2GameGameRoomOverRespond, OnLobby2GameGameRoomOverRespond);

            // Online Game
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2GameLobbyPlayerJoinGameRequest, OnLobby2GameLobbyPlayerJoinGameRequest);
        }

        public override void OnAllSubSystemPrepared()
        {
            SystemManager.Instance.GetSubsystem<GameLogicSystem>().AddGame(GameType.KayKitBrawl, new KayKitBrawlBuilder());
            SystemManager.Instance.GetSubsystem<GameLogicSystem>().AddGame(GameType.KayKitTeamBrawl, new KayKitTeamBrawlBuilder());
            SystemManager.Instance.GetSubsystem<GameLogicSystem>().AddGame(GameType.KayKitCoinBrawl, new KayKitCoinBrawlBuilder());


            // Online Game
            SystemManager.Instance.GetSubsystem<GameLogicSystem>().AddGame(GameType.DragonBoard, new DragonBoardGameBuilder());
            SystemManager.Instance.GetSubsystem<GameLogicSystem>().CreateOnlineGame(GameType.DragonBoard);


        }
        protected override void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType)
        {
        }

        private bool GameLeaveByClientOnlineInfo(ClientInfo clientInfo)
        {
            if (clientInfo != null)
            {
                SystemManager.Instance.GetSubsystem<GamePlayerSystem>().RemovePlayerByAccountId(clientInfo.AccountId);
                _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);
                _sessionIdCientInfoWaitingEnterTable.Remove(clientInfo.SessionId);
                DebugLog.Log("GameLeaveByClientOnlineInfo");

                return true;
            }
            return false;
        }

        #region Network Message Callback
        // Disconnected
        private void OnClientDisconnected(int connectionId, Dictionary<int, object> msg)
        {
            if (!RetrieveMessageItem(msg, ClientDisconnected.ConnectionId, out int clientConnectionId)) return;

            ClientInfo clientInfo = _leaveClientInfoTable.FirstOrDefault(x => x.GameConnectionId == clientConnectionId);
            if (GameLeaveByClientOnlineInfo(clientInfo))
            {
                DebugLog.LogFormat("OnClientDisconnected Success Disconnected from {0}", clientConnectionId);
                return;
            }

            clientInfo = _accountIdClientInfoIdTable.Values.FirstOrDefault(x => x.GameConnectionId == clientConnectionId);
            if (GameLeaveByClientOnlineInfo(clientInfo))
            {
                DebugLog.LogFormat("OnClientDisconnected Success from table Disconnected from {0}", clientConnectionId);
                return;
            }

            DebugLog.LogFormat("OnClientDisconnected Failed Disconnected from {0}", clientConnectionId);
        }
        private void OnLobby2GameLobbyRoomEnteredRequest(int connectionId, Dictionary<int, object> msg)
        {
            Dictionary<int, object> roomData;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyRoomEnteredRequest.RoomData, out roomData)) return;

            LobbyRoomInfo lobbyRoomInfo = new LobbyRoomInfo();
            lobbyRoomInfo.DeserializeObject(roomData);

            ErrorCode errorCode = SystemManager.Instance.GetSubsystem<GameRoomSystem>().InitGame(lobbyRoomInfo);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Game2LobbyLobbyRoomEnteredRespond.ErrorCode, errorCode);
            AddMessageItem(outMessage, Game2LobbyLobbyRoomEnteredRespond.RoomId, lobbyRoomInfo.RoomId);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Game2LobbyLobbyRoomEnteredRespond, outMessage);
        }
        private void OnLobby2GameLobbyPlayerPrepareEnterRequest(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            GameType gameLocation;
            int lobbyConnectionId;
            int roomId;
           
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerPrepareEnterRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerPrepareEnterRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerPrepareEnterRequest.GameLocation, out gameLocation)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerPrepareEnterRequest.LobbyConnectionId, out lobbyConnectionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerPrepareEnterRequest.RoomId, out roomId)) return;

            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(accountId, out previousClientInfo))
            {
                // 重複登入 or Game Server 已經有玩家登入
                // kick previous client 
                PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.GameConnectionId);

                DebugLog.Log("Game LobbyPlayer Prepare: Game get duplicated accountId from GameServer (_accountIdClientInfoIdTable), weird error.");
                _accountIdClientInfoIdTable.Remove(previousClientInfo.AccountId);
                _leaveClientInfoTable.Add(previousClientInfo);

            }

            if (_sessionIdCientInfoWaitingEnterTable.ContainsKey(sessionId))
            {
                DebugLog.Log("Game LobbyPlayer Prepare: Game get duplicated sessionId from GameServer(_sessionIdCientInfoWaitingEnterTable), weird error.");
                return;
            }

            var clientInfo = new ClientInfo(accountId, sessionId, gameLocation);
            clientInfo.SetClientLocated(RemoteConnetionType.Game);
            clientInfo.SetLobbyConnectionId(lobbyConnectionId);

            GamePlayerRoomInfo gamePlayerRoomInfo;
            ErrorCode err = SystemManager.Instance.GetSubsystem<GameRoomSystem>().CheckGamePlayerAtGameRoom(roomId, accountId, out gamePlayerRoomInfo);

            if (err == ErrorCode.Success)
            {
                GamePlayer gamePlayer = new GamePlayer();
                gamePlayer.InitGameRoomPlayer(clientInfo, gamePlayerRoomInfo);
                SystemManager.Instance.GetSubsystem<GamePlayerSystem>().PlayerWaitingEnterGame(gamePlayer);
                _sessionIdCientInfoWaitingEnterTable[sessionId] = clientInfo;

                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, (int)Game2LobbyLobbyPlayerPrepareEnterRespond.AccountId, accountId);
                AddMessageItem(outMessage, (int)Game2LobbyLobbyPlayerPrepareEnterRespond.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Game2LobbyLobbyPlayerPrepareEnterRespond, outMessage);
            }
            else
            {
                //Dictionary<int, object> outMessage = new Dictionary<int, object>();
                //AddMessageItem(outMessage, (int)Game2LobbyPlayerPrepareEnterRespond.AccountId, accountId);
                //AddMessageItem(outMessage, (int)Game2LobbyPlayerPrepareEnterRespond.SessionId, sessionId);
                //AddMessageItem(outMessage, (int)Game2LobbyPlayerPrepareEnterRespond.LobbyConnectionId, lobbyConnectionId);
                //ServerApplication.Instance.NetHandle.Send(connectionId, MsgType.NetMsg_Game2LobbyPlayerPrepareRespond, outMessage);
                DebugLog.LogFormat("OnClientDisconnected Failed Can't Get GamePlayerInfo from GameRoomSystem, at {0} accountId {1} roomId", accountId, roomId);
            }
        }
        private void OnGameConnectedRequest(int connectionId, Dictionary<int, object> msg)
        {
            int sessionId;
            if(!RetrieveMessageItem(msg, GameConnectedRequest.SessionId, out sessionId)) return;

            ClientInfo clientInfo;
            if (!_sessionIdCientInfoWaitingEnterTable.TryGetValue(sessionId, out clientInfo))
            {
                DebugLog.LogErrorFormat("Can't find sessionId for {0} from _sessionIdGamePlayerPrepareTable", sessionId);
                PhotonApplication.Instance.NetHandle.Disconnect(connectionId);
                return;
            }

            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(clientInfo.AccountId, out previousClientInfo))
            {
                _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);
                _leaveClientInfoTable.Add(previousClientInfo);
                PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.GameConnectionId);
            }

            _sessionIdCientInfoWaitingEnterTable.Remove(sessionId);
            _accountIdClientInfoIdTable[clientInfo.AccountId] = clientInfo;
            clientInfo.SetGameConnectionId(connectionId);

            GamePlayer gamePlayer = SystemManager.Instance.GetSubsystem<GamePlayerSystem>().PlayerEnterGame(sessionId);
            GameStaticInfo gameStaticInfo;
            GameDynamicInfo gameDynamicInfo;
            GameResultInfo gameResultInfo;
            ErrorCode err;

            if (gamePlayer.IsOnlineGamePlayer)
            {
              
                err = SystemManager.Instance.GetSubsystem<GameLogicSystem>().TryJoinOnlineGame(gamePlayer,
                    out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }
            else
            {
                err = SystemManager.Instance.GetSubsystem<GameRoomSystem>().TryJoinGame(gamePlayer,
                    out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }

            if (err == ErrorCode.Success)
            {
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, GameConnectedRespond.ErrorCode, err.GetHashCode());
                if (gameStaticInfo != null)
                    AddMessageItem(outMessage, GameConnectedRespond.GameStaticInfo, gameStaticInfo.SerializeObject());
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.GameConnectedRespond, outMessage);
                DebugLog.LogWarningFormat("NetMsg_GameEnteredRespond , {0}", err);
            }
            else
            {
                DebugLog.LogWarningFormat("OnGameEnteredRequest Add player failed , {0}", err);
            }
        }
        private void OnLobby2GameGameRoomOverRespond(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnLobby2GameGameRoomOverRespond" + JsonConvert.SerializeObject(msg));

            long accountId;
            int sessionId;
            int lobbyConnectionId;

            if (!RetrieveMessageItem(msg, Lobby2GameGameRoomOverRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameGameRoomOverRespond.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameGameRoomOverRespond.LobbyConnectionId, out lobbyConnectionId)) return;

            ClientInfo clientInfo;
            if (!_accountIdClientInfoIdTable.TryGetValue(accountId, out clientInfo))
            {
                DebugLog.Log("Game _accountIdClientInfoIdTable: AccountId not exist....");
                return;
            }

            if (clientInfo.SessionId != sessionId || clientInfo.LobbyConnectionId != lobbyConnectionId)
            {
                DebugLog.Log("Game _accountIdClientInfoIdTable: A newer client has connected already ....");
                PhotonApplication.Instance.NetHandle.Disconnect(clientInfo.GameConnectionId);
                return;
            }
        }

        private void OnLobby2GameLobbyPlayerJoinGameRequest(int connectionId, Dictionary<int, object> msg)
        {
            int gameType;
            long accountId;
            int sessionId;
            GameType gameLocation;
            int lobbyConnectionId;

            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerJoinGameRequest.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerJoinGameRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerJoinGameRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerJoinGameRequest.GameLocation, out gameLocation)) return;
            if (!RetrieveMessageItem(msg, Lobby2GameLobbyPlayerJoinGameRequest.LobbyConnectionId, out lobbyConnectionId)) return;

            bool serverStarted = SystemManager.Instance.GetSubsystem<GameLogicSystem>().CheckOnlineGameServerIsStarted((GameType)gameType);
            if (!serverStarted)
            {

            }


            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(accountId, out previousClientInfo))
            {
                // 重複登入 or Game Server 已經有玩家登入
                // kick previous client 
                PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.GameConnectionId);

                DebugLog.Log("Game LobbyPlayer Prepare: Game get duplicated accountId from GameServer (_accountIdClientInfoIdTable), weird error.");
                _accountIdClientInfoIdTable.Remove(previousClientInfo.AccountId);
                _leaveClientInfoTable.Add(previousClientInfo);
            }

            if (_sessionIdCientInfoWaitingEnterTable.ContainsKey(sessionId))
            {
                DebugLog.Log("Game LobbyPlayer Prepare: Game get duplicated sessionId from GameServer(_sessionIdCientInfoWaitingEnterTable), weird error.");
                return;
            }

            var clientInfo = new ClientInfo(accountId, sessionId, (GameType)gameType);
            clientInfo.SetClientLocated(RemoteConnetionType.Game);
            clientInfo.SetLobbyConnectionId(lobbyConnectionId);
         
            GamePlayer gamePlayer = new GamePlayer();
            gamePlayer.InitGameRoomPlayer(clientInfo, null);
            gamePlayer.IsOnlineGamePlayer = true;
            SystemManager.Instance.GetSubsystem<GamePlayerSystem>().PlayerWaitingEnterGame(gamePlayer);
            _sessionIdCientInfoWaitingEnterTable[sessionId] = clientInfo;

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, (int)Game2LobbyLobbyPlayerJoinGameRespond.AccountId, accountId);
            AddMessageItem(outMessage, (int)Game2LobbyLobbyPlayerJoinGameRespond.SessionId, sessionId);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Game2LobbyLobbyPlayerJoinGameRespond, outMessage);
        }
        #endregion
    }
}