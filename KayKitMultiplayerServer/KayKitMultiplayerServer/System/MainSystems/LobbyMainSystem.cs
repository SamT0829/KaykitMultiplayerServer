using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.Network.ConfigReader;
using KayKitMultiplayerServer.System.Common.Subsystems;
using KayKitMultiplayerServer.System.LobbySystem.Model;
using KayKitMultiplayerServer.System.LobbySystem.Subsystems;
using KayKitMultiplayerServer.TableRelated;

namespace KayKitMultiplayerServer.System.MainSystems
{
    public class LobbyMainSystem : Common.MainSystem
    {
        private Dictionary<long, ClientInfo> _accountIdClientInfoIdTable = new Dictionary<long, ClientInfo>();

        // waiting Table
        private Dictionary<int, ClientInfo> _sessionIdClientInfoWaitingTable = new Dictionary<int, ClientInfo>();
        private Dictionary<int, ClientInfo> _sessionIdPlayerWaitFinanceTable = new Dictionary<int, ClientInfo>();

        // Leave table
        private List<ClientInfo> _leaveClientInfoTable = new List<ClientInfo>();

        public LobbyMainSystem(IFiber systemFiber) : base(systemFiber)
        {
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.ClientDisconnected, OnClientDisconnected);

            // Account Login
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Proxy2LobbyClientLoginRequest, OnProxy2LobbyClientLoginRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Proxy2LobbyKickPlayerRequest, OnProxy2LobbyKickPlayerRequest);

            // Lobby Login
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.LobbyConnectedRequest, OnLobbyLoginRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Finance2LobbyQueryDataResond, OnFinance2LobbyQueryDataResond);
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.LobbyPlayerRegisterRequest, OnLobbyPlayerRegisterRequest);

            // Lobby PrepareEnter
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.LobbyPlayerPrepareEnterRequest, OnLobbyPlayerPrepareEnterRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Game2LobbyLobbyPlayerPrepareEnterRespond, OnGame2LobbyLobbyPlayerPrepareEnterRespond);


            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Game2LobbyLobbyPlayerJoinGameRespond, OnGame2LobbyLobbyPlayerJoinGameRespond);
        }

        protected override void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType)
        {
        }
        private bool LobbyLeaveByClientOnlineInfo(ClientInfo clientInfo)
        {
            if (clientInfo != null)
            {
                SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>().RemovePlayerByAccountId(clientInfo.AccountId);
                bool leaveDone = _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);

                var proxyServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Proxy);
                // inform Proxy system
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, (int)Lobby2ProxyPlayerLeave.AccountID, clientInfo.AccountId);
                AddMessageItem(outMessage, (int)Lobby2ProxyPlayerLeave.SessionID, clientInfo.SessionId);
                AddMessageItem(outMessage, (int)Lobby2ProxyPlayerLeave.LeaveDone, leaveDone);
                PhotonApplication.Instance.NetHandle.Send(proxyServerId, ServerHandlerMessage.Lobby2ProxyPlayerLeave, outMessage);

                //_sessionIdPlayerWaitFinanceTable.Remove(clientInfo.SessionId);

                return true;

            }
            return false;
        }
        // Others //
        private bool CheckAnotherPlayerLogined(ClientInfo clientInfo)
        {
            // check whether another one just in login process
            ClientInfo clientInfoInGame;
            if( _accountIdClientInfoIdTable.TryGetValue(clientInfo.AccountId, out clientInfoInGame))
            {
                if (clientInfoInGame.SessionId != clientInfo.SessionId)
                {
                    PhotonApplication.Instance.NetHandle.Disconnect(clientInfoInGame.LobbyConnectionId);
                    return true;
                }
            }
          
            return false;
        }
        // Network
        private void OnClientDisconnected(int connectionId, Dictionary<int, object> msg)
        {
            int clientConnectionId;
            if (!RetrieveMessageItem(msg, ClientDisconnected.ConnectionId, out clientConnectionId)) return;

            ClientInfo onlineInfo = _leaveClientInfoTable.FirstOrDefault(x => x.LobbyConnectionId == clientConnectionId);
            if (LobbyLeaveByClientOnlineInfo(onlineInfo))
            {
                DebugLog.LogFormat("OnClientDisconnected Success Disconnected from {0}", clientConnectionId);
                return;
            }

            onlineInfo = _accountIdClientInfoIdTable.Values.FirstOrDefault(x => x.LobbyConnectionId == clientConnectionId);
            if (LobbyLeaveByClientOnlineInfo(onlineInfo))
            {
                DebugLog.LogFormat("OnClientDisconnected Success Disconnected from {0}", clientConnectionId);
                return;
            }

            DebugLog.LogFormat("OnClientDisconnected Failed Disconnected from {0}", clientConnectionId);
        }
        private void OnProxy2LobbyClientLoginRequest(int connectionId, Dictionary<int, object> msg)
        {
            int accountServerId;
            long accountId;
            int sessionId;
            int clientConnectionId;
            GameType gameType;

            if (!RetrieveMessageItem(msg, Proxy2LobbyClientLoginRequest.LocatedAccountServerId, out accountServerId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyClientLoginRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyClientLoginRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyClientLoginRequest.ClientConnectionId, out clientConnectionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyClientLoginRequest.GameType, out gameType)) return;

            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(accountId, out previousClientInfo))
            {
                // kick previous client 
                PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.LobbyConnectionId);

                Dictionary<int, object> outLoadMessage = new Dictionary<int, object>();
                AddMessageItem(outLoadMessage, (int)Lobby2ProxyPlayerLeave.AccountID, accountId);
                AddMessageItem(outLoadMessage, (int)Lobby2ProxyPlayerLeave.SessionID, previousClientInfo.SessionId);
                AddMessageItem(outLoadMessage, (int)Lobby2ProxyPlayerLeave.LeaveDone, true);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Lobby2ProxyPlayerLeave, outLoadMessage);

                _accountIdClientInfoIdTable.Remove(previousClientInfo.AccountId);
                _leaveClientInfoTable.Add(previousClientInfo);
            }

            if (_sessionIdClientInfoWaitingTable.ContainsKey(sessionId))
            {
                DebugLog.Log("Client login: Lobby get duplicated sessionId from ProxyServer, weird error.");
                return;
            }

            var clientOnlineInfo = new ClientInfo(accountId, sessionId, gameType);
            clientOnlineInfo.SetAccountConnectionId(clientConnectionId);
            clientOnlineInfo.SetClientLocated(RemoteConnetionType.Lobby);
            _sessionIdClientInfoWaitingTable.Add(sessionId, clientOnlineInfo);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, (int)Lobby2ProxyClientLoginRespond.LocatedAccountServerID, accountServerId);
            AddMessageItem(outMessage, (int)Lobby2ProxyClientLoginRespond.AccountID, accountId);
            AddMessageItem(outMessage, (int)Lobby2ProxyClientLoginRespond.ClientConnectionID, clientConnectionId);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Lobby2ProxyClientLoginRespond, outMessage);
        }
        private void OnProxy2LobbyKickPlayerRequest(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            int replacedSessionId;
            int accountServerId;
            int clientConnectionId;

            if (!RetrieveMessageItem(msg, Proxy2LobbyKickPlayerRequest.AccountID, out accountId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyKickPlayerRequest.SessionID, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyKickPlayerRequest.ReplacedSessionID, out replacedSessionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyKickPlayerRequest.AccountServerID, out accountServerId)) return;
            if (!RetrieveMessageItem(msg, Proxy2LobbyKickPlayerRequest.ClientConnectionID, out clientConnectionId)) return;

            _sessionIdClientInfoWaitingTable.Remove(replacedSessionId);

            bool kickDone = false;
            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(accountId, out previousClientInfo))
            {
                if (sessionId == previousClientInfo.SessionId)
                {
                    Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                    AddMessageItem(outMessageToPrevious, (int)KickPlayer.ErrorCode, (int)ErrorCode.AccountKickByDuplicatedLogin);
                    PhotonApplication.Instance.NetHandle.Send(previousClientInfo.LobbyConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                    PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.LobbyConnectionId);

                    kickDone = true;
                    _accountIdClientInfoIdTable.Remove(accountId);
                    //_sessionIdPlayerWaitFinanceTable.Remove(sessionId);
                    _leaveClientInfoTable.Add(previousClientInfo);
                }
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.AccountId, accountId);
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.SessionId, sessionId);
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.ReplacedSessionId, replacedSessionId);
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.AccountServerId, accountServerId);
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.ClientConnectionId, clientConnectionId);
            AddMessageItem(outMessage, (int)Lobby2ProxyKickPlayerRespond.KickSuccess, kickDone);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Lobby2ProxyKickPlayerRespond, outMessage);
        }
        private void OnLobbyLoginRequest(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.LogErrorFormat("OnLobbyLoginRequest");
            int sessionId;
            if (!RetrieveMessageItem(msg, LobbyLoginRequest.SessionId, out sessionId)) return;

            ClientInfo clientInfo;
            if (!_sessionIdClientInfoWaitingTable.TryGetValue(sessionId, out clientInfo) || clientInfo == null)
            {
                PhotonApplication.Instance.NetHandle.Disconnect(connectionId);
                DebugLog.LogErrorFormat("Can't find sessionId for {0} from _sessionIdClientInfoWaitingTable", sessionId);
                return;
            }

            ReaderDB_LobbyLogin(connectionId, clientInfo, _systemFiber);
        }
        private void OnFinance2LobbyQueryDataResond(int connectionId, Dictionary<int, object> msg)
        {
            ErrorCode err;
            long accountId;
            int sessionId;
            GameType gameType;
            long money;
            long diamond;

            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.ErrorCode, out err)) return;
            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.Money, out money)) return;
            if (!RetrieveMessageItem(msg, Finance2LobbyQueryDataResond.Diamond, out diamond)) return;

            ClientInfo clientInfo;
            if (!_sessionIdPlayerWaitFinanceTable.TryGetValue(sessionId, out clientInfo) || clientInfo == null)
            {
                // 客戶端登錄：當進程從財務返回時，大廳找不到玩家
                DebugLog.Log("Client login: Lobby can't find lobbyLobbyPlayerInfo when process being back from Finance.");
                return;
            }
            _sessionIdPlayerWaitFinanceTable.Remove(sessionId);

            if (CheckAnotherPlayerLogined(clientInfo))
            {
                // 客戶端登錄：當進程從財務返回時，另一個玩家登錄
                DebugLog.Log("Client login: Another lobbyLobbyPlayerInfo logined when process being back from Finance.");
                return;
            }

            if (err != ErrorCode.Success)
            {
                Dictionary<int, object> outClientMessage = new Dictionary<int, object>();
                AddMessageItem(outClientMessage, LobbyConnectedRespond.ErrorCode, (int)err);
                PhotonApplication.Instance.NetHandle.Send(clientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyConnectedRespond, outClientMessage);

                // login error log
                LogSystem system = SystemManager.Instance.GetSubsystem<LogSystem>();
                system.LogError(accountId, GameType.None, err);
                return;
            }

            
            var lobbyPlayer = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>().GetWaitingEnterPlayerBySessionId(clientInfo.SessionId);
            if (lobbyPlayer == null)
            {
                // 客戶端登錄：當進程從財務返回時，另一個玩家登錄
                DebugLog.Log("Client login: Another player is logout when process being back from Finance.");
                return;
            }

            lobbyPlayer.LobbyPlayerGameInfo.InitLobbyGameInfo(money, diamond);
            //lobbyPlayer.PrepareEnterGameType = gameType;

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Lobby2ProxyPlayerEnteredRequest.AccountId, clientInfo.AccountId);
            AddMessageItem(outMessage, Lobby2ProxyPlayerEnteredRequest.SessionId, clientInfo.SessionId);
            AddMessageItem(outMessage, Lobby2ProxyPlayerEnteredRequest.LobbyConnectionId, clientInfo.LobbyConnectionId);
            PhotonApplication.Instance.NetHandle.Send(GetServerId(RemoteConnetionType.Proxy),
                ServerHandlerMessage.Lobby2ProxyPlayerEnteredRequest, outMessage);
        }
        private void OnLobbyPlayerRegisterRequest(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.LogErrorFormat("OnLobbyPlayerRegisterRequest");

            int sessionId;
            GameType gameType;
            string playerNickname;

            if (!RetrieveMessageItem(msg, LobbyPlayerRegisterRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, LobbyPlayerRegisterRequest.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, LobbyPlayerRegisterRequest.Nickname, out playerNickname)) return;

            ClientInfo clientInfo;
            if (!_sessionIdClientInfoWaitingTable.TryGetValue(sessionId, out clientInfo))
            {
                PhotonApplication.Instance.NetHandle.Disconnect(connectionId);
                DebugLog.LogErrorFormat("Can't find sessionId for {0} from _sessionIdClientInfoWaitingTable", sessionId);
                return;
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyPlayerInfoRegister_GameType, gameType.ToString())) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyPlayerInfoRegister_AccountId, clientInfo.AccountId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyPlayerInfoRegister_Nickname, playerNickname)) return;

            DBManager.Instance.ExecuteReader(DBCatagory.Lobby, DbStoreProcedureInput.LobbyPlayerInfoRegister, parameters, _systemFiber,
                (reader) => OnDbCallback_LobbyPlayerInfoRegister(connectionId, reader, clientInfo));
        }
        private void OnLobbyPlayerPrepareEnterRequest(int connectionId, Dictionary<int, object> msg)
        {
            if (!RetrieveMessageItem(msg, LobbyPlayerPrepareEnterRequest.AccountId, out long accountId)) return;

            var gameServerId = GetServerId(RemoteConnetionType.Game);

            // Game Server Not Ready
            if (gameServerId == -1)
            {
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, LobbyPlayerPrepareEnterRespond.ErrorCode, ErrorCode.ServerNotReady);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerPrepareEnterRespond, outMessage);
                return;
            }

            ClientInfo clientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(accountId, out clientInfo))
            {
                var player = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>().GetPlayerByAccountId(accountId);

                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, Lobby2GameLobbyPlayerPrepareEnterRequest.AccountId, clientInfo.AccountId);
                AddMessageItem(outMessage, Lobby2GameLobbyPlayerPrepareEnterRequest.SessionId, clientInfo.SessionId);
                AddMessageItem(outMessage, Lobby2GameLobbyPlayerPrepareEnterRequest.GameLocation, clientInfo.GameLocation);
                AddMessageItem(outMessage, Lobby2GameLobbyPlayerPrepareEnterRequest.LobbyConnectionId, clientInfo.LobbyConnectionId);
                AddMessageItem(outMessage, Lobby2GameLobbyPlayerPrepareEnterRequest.RoomId, player.LobbyPlayerRoomInfo.RoomId);
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GameLobbyPlayerPrepareEnterRequest, outMessage);
            }
            else
            {
                DebugLog.LogFormat("_accountIdClientInfoIdTable cant find accountId : {0}, OnLobbyPlayerPrepareEnterRequest Send failed"
                , accountId);
            }
        }
        private void OnGame2LobbyLobbyPlayerPrepareEnterRespond(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;

            if (!RetrieveMessageItem(msg, Game2LobbyLobbyPlayerPrepareEnterRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Game2LobbyLobbyPlayerPrepareEnterRespond.SessionId, out sessionId)) return;

            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);

            ClientInfo clientInfo;
            if (!_accountIdClientInfoIdTable.TryGetValue(accountId, out clientInfo))
            {

                DebugLog.Log("Lobby _accountIdClientInfoIdTable: AccountId not exist....");
                Dictionary<int, object> outGameLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.AccountId, accountId);
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GamePlayerLeave, outGameLeaveMessage);
                return;
            }

            if (clientInfo.SessionId != sessionId)
            {
                DebugLog.Log("Lobby _accountIdClientInfoIdTable: A newer client has connected already ....");
                Dictionary<int, object> outGameLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.AccountId, accountId);
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GamePlayerLeave, outGameLeaveMessage);
                return;
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();

            string gameIP = TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterIpAddress(gameServerId);
            string gamePort = (clientInfo.LobbyConnectionId > 0) ? TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterPort(gameServerId)
                : TableManager.Instance.GetTable<ServerListConfigReader>().GetWebPort(gameServerId);

            int gamePortNumber;
            if (!int.TryParse(gamePort, out gamePortNumber))
            {
                DebugLog.LogError("Client Game Entered: error server port " + gamePort);
            }

            AddMessageItem(outMessage, LobbyPlayerPrepareEnterRespond.ErrorCode, ErrorCode.Success);  // ErrorCode.Success
            AddMessageItem(outMessage, LobbyPlayerPrepareEnterRespond.GameServerIP, gameIP);
            AddMessageItem(outMessage, LobbyPlayerPrepareEnterRespond.GameServerPort, gamePortNumber);
            PhotonApplication.Instance.NetHandle.Send(clientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerPrepareEnterRespond, outMessage);
        }

        private void OnGame2LobbyLobbyPlayerJoinGameRespond(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;

            if (!RetrieveMessageItem(msg, Game2LobbyLobbyPlayerPrepareEnterRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Game2LobbyLobbyPlayerPrepareEnterRespond.SessionId, out sessionId)) return;

            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);

            ClientInfo clientInfo;
            if (!_accountIdClientInfoIdTable.TryGetValue(accountId, out clientInfo))
            {

                DebugLog.Log("Lobby _accountIdClientInfoIdTable: AccountId not exist....");
                Dictionary<int, object> outGameLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.AccountId, accountId);
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GamePlayerLeave, outGameLeaveMessage);
                return;
            }

            if (clientInfo.SessionId != sessionId)
            {
                DebugLog.Log("Lobby _accountIdClientInfoIdTable: A newer client has connected already ....");
                Dictionary<int, object> outGameLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.AccountId, accountId);
                AddMessageItem(outGameLeaveMessage, Lobby2GamePlayerLeave.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GamePlayerLeave, outGameLeaveMessage);
                return;
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();

            string gameIP = TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterIpAddress(gameServerId);
            string gamePort = (clientInfo.LobbyConnectionId > 0) ? TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterPort(gameServerId)
                : TableManager.Instance.GetTable<ServerListConfigReader>().GetWebPort(gameServerId);

            int gamePortNumber;
            if (!int.TryParse(gamePort, out gamePortNumber))
            {
                DebugLog.LogError("Client Game Entered: error server port " + gamePort);
            }

            AddMessageItem(outMessage, LobbyPlayerJoinGameRespond.ErrorCode, ErrorCode.Success.GetHashCode());  // ErrorCode.Success
            AddMessageItem(outMessage, LobbyPlayerJoinGameRespond.GameServerIP, gameIP);
            AddMessageItem(outMessage, LobbyPlayerJoinGameRespond.GameServerPort, gamePortNumber);
            PhotonApplication.Instance.NetHandle.Send(clientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerJoinGameRespond, outMessage);
        }

        // DB
        public void ReaderDB_LobbyLogin(int connectionId, ClientInfo clientInfo, IFiber fiber)
        {
            string gameTypeName = Enum.ToObject(typeof(GameType), clientInfo.GameLocation).ToString();

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyLogin_AccountId, clientInfo.AccountId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyLogin_LoginTime, DateTime.Now)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LobbyLogin_GameType, gameTypeName)) return;

            DBManager.Instance.ExecuteReader(DBCatagory.Lobby, DbStoreProcedureInput.LobbyLogin, parameters, fiber, 
                (reader) => OnDbCallback_LobbyLogin(connectionId, reader, clientInfo));
        }
        private void OnDbCallback_LobbyLogin(int connectionId, List<Dictionary<string, object>> reader, ClientInfo clientInfo)
        {
            ErrorCode err = ErrorCode.Success;
            Dictionary<int, object> outMessage;
            bool isSuccess = false;
            bool isAccountNotExist = false;
            LobbyPlayer lobbyPlayer = null;


            if (reader.Count <= 0)
            {
                //DebugLog.Log("[A lobby SP] reader count=0");
                err = ErrorCode.LobbyDBUnableRetrieveData;
            }
            else
            {
                int outSuccess = 0;
                int outAccountNotExist = 0;

                Dictionary<string, object> needReader = reader[0];
                DebugLog.Log("[A lobby SP] reader[0]=[" + string.Join(";", reader[0].Select(x => x.Key + "=" + x.Value)) + "]");
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.LobbyLogin_Success, out outSuccess))
                { isSuccess = outSuccess == 1 ? true : false; };
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.LobbyLogin_PlayerInfoNotExist, out outAccountNotExist))
                { isAccountNotExist = outAccountNotExist == 1 ? true : false; };

                if (isSuccess)
                {
                    lobbyPlayer = new LobbyPlayer(clientInfo, _systemFiber);
                    err = lobbyPlayer.LobbyPlayerInfo.ReadDB_InitLobbyPlayerInfo(needReader);
                }
                else if (isAccountNotExist)
                {
                    outMessage = new Dictionary<int, object>();
                    AddMessageItem(outMessage, LobbyConnectedRespond.ErrorCode, (int)err);
                    AddMessageItem(outMessage, LobbyConnectedRespond.IsPlayerInfoDataNotExist, isAccountNotExist);
                    PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyConnectedRespond, outMessage);
                    return;
                }
            }

            if (err != ErrorCode.Success)
            {
                outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, LobbyConnectedRespond.ErrorCode, (int)err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyConnectedRespond, outMessage);

                PhotonApplication.Instance.NetHandle.Disconnect(clientInfo.LobbyConnectionId);
                _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);
                _leaveClientInfoTable.Add(clientInfo);

                //// login error log
                LogSystem logSystem = SystemManager.Instance.GetSubsystem<LogSystem>();
                logSystem.LogError(clientInfo.AccountId, clientInfo.GameLocation, err);
                return;
            }

            ClientInfo previousClientInfo;
            if (_accountIdClientInfoIdTable.TryGetValue(clientInfo.AccountId, out previousClientInfo))
            {
                PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.LobbyConnectionId);
                _accountIdClientInfoIdTable.Remove(previousClientInfo.AccountId);
                _leaveClientInfoTable.Add(previousClientInfo);
            }

            _sessionIdClientInfoWaitingTable.Remove(clientInfo.SessionId);
            _accountIdClientInfoIdTable[clientInfo.AccountId] = clientInfo;
            clientInfo.SetLobbyConnectionId(connectionId);

            // check lobbyLobbyPlayerInfo exist
            LobbyPlayerSystem lobbyPlayerSystem = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>();
            err = lobbyPlayerSystem.RemoveExistPlayer(lobbyPlayer);
            if (err != ErrorCode.Success)
            {
                outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, LobbyConnectedRespond.ErrorCode, (int)err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyConnectedRespond, outMessage);

                PhotonApplication.Instance.NetHandle.Disconnect(clientInfo.LobbyConnectionId);
                _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);
                _leaveClientInfoTable.Add(clientInfo);

                DebugLog.Log("ClientLogin: RemoveExistPlayer. err=" + err);

                // login error log
                LogSystem logSystem = SystemManager.Instance.GetSubsystem<LogSystem>();
                logSystem.LogError(clientInfo.AccountId, GameType.None, err);
                return;
            }

            lobbyPlayerSystem.PlayerWaitingEnterLobby(lobbyPlayer);

            // Get Lobby Player Data
            Dictionary<int, object> outMessageToFinance = new Dictionary<int, object>();
            AddMessageItem(outMessageToFinance, (int)Lobby2FinanceQueryDataRequest.AccountId, clientInfo.AccountId);
            AddMessageItem(outMessageToFinance, (int)Lobby2FinanceQueryDataRequest.SessionId, clientInfo.SessionId);
            AddMessageItem(outMessageToFinance, (int)Lobby2FinanceQueryDataRequest.GameType, (int)clientInfo.GameLocation);
            PhotonApplication.Instance.NetHandle.Send(GetServerId(RemoteConnetionType.Finance),
                ServerHandlerMessage.Lobby2FinanceQueryDataRequest, outMessageToFinance);

            _sessionIdPlayerWaitFinanceTable.Add(clientInfo.SessionId, lobbyPlayer.ClientInfo);
        }
        private void OnDbCallback_LobbyPlayerInfoRegister(int connectionId, List<Dictionary<string, object>> reader, ClientInfo clientInfo)
        {
            ErrorCode err = ErrorCode.Success;

            if (reader.Count <= 0)
            {
                //DebugLog.Log("[A lobby SP] reader count=0");
                err = ErrorCode.LobbyDBUnableRetrieveData;
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, LobbyPlayerRegisterRespond.ErrorCode, (int)err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerRegisterRespond, outMessage);
                PhotonApplication.Instance.NetHandle.Disconnect(connectionId);
              
                _accountIdClientInfoIdTable.Remove(clientInfo.AccountId);
                _leaveClientInfoTable.Add(clientInfo);

                //// login error log
                LogSystem logSystem = SystemManager.Instance.GetSubsystem<LogSystem>();
                logSystem.LogError(clientInfo.AccountId, clientInfo.GameLocation, err);
            }
            else
            {
                int outSuccess = 0;
                int outNickNameAlreadyUsed = 0;
                bool isSuccess = false;
                bool isNickNameAlreadyUsed = false;

                Dictionary<string, object> needReader = reader[0];
                DebugLog.Log("[A lobby SP] reader[0]=[" + string.Join(";", reader[0].Select(x => x.Key + "=" + x.Value)) + "]");
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.LobbyPlayerInfoRegister_Success, out outSuccess))
                    isSuccess = outSuccess == 1;
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.LobbyPlayerInfoRegister_NickNameAlreadyUsed, out outNickNameAlreadyUsed))
                    isNickNameAlreadyUsed = outNickNameAlreadyUsed == 1;

                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, LobbyPlayerRegisterRespond.ErrorCode, (int)err);
                AddMessageItem(outMessage, LobbyPlayerRegisterRespond.NickNameAlreadyUsed, isNickNameAlreadyUsed);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerRegisterRespond, outMessage);
            }
        }
    }
}