using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.Network.ConfigReader;
using KayKitMultiplayerServer.System.AccountSystem.Subsystems;
using KayKitMultiplayerServer.TableRelated;

namespace KayKitMultiplayerServer.System.MainSystems
{
    public class AccountMainSystem : Common.MainSystem
    {
        private const int waitConnectionSessionId = -1;
        private Dictionary<long, ClientInfo> _accountIdConnectionId = new();

        public AccountMainSystem(IFiber systemFiber) : base(systemFiber)
        {
            // Disconnect
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.ClientDisconnected, OnClientDisconnected);

            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Proxy2AccountClientLoginRespond, OnProxy2AccountClientLoginRespond);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Proxy2AccountPlayerEnteredRequest, OnProxy2AccountPlayerEnteredRequest);

            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.AccountConnectedRequest, OnAccountConnectRequest);
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.AccountRegisterRequest, OnAccountRegisterRequest);
        }

        protected override void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType)
        {
        }

        public void SendClientLoginRequestToProxyServer(int connectionId, long accountId, GameType gameType)
        {
            ClientInfo previousClientInfo;
            if (_accountIdConnectionId.TryGetValue(accountId, out previousClientInfo))
            {
                // kick previous account
                if (previousClientInfo.AccountConnectionId != connectionId)
                {
                    Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                    AddMessageItem(outMessageToPrevious, KickPlayer.ErrorCode, ErrorCode.AccountKickByDuplicatedLogin);
                    PhotonApplication.Instance.NetHandle.Send(previousClientInfo.AccountConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                    PhotonApplication.Instance.NetHandle.Disconnect(previousClientInfo.AccountConnectionId);
                }

                _accountIdConnectionId.Remove(accountId);
            }

            ClientInfo clientInfo = new ClientInfo(accountId, waitConnectionSessionId, gameType);
            clientInfo.SetAccountConnectionId(connectionId);
            _accountIdConnectionId.Add(accountId, clientInfo);

            var proxyServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Proxy);
            // Proxy Server Not Ready
            if (proxyServerId == -1)
            {
                DebugLog.LogErrorFormat("cant find proxy serverId");
                return;
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Account2ProxyClientLoginRequest.GameType, gameType);
            AddMessageItem(outMessage, Account2ProxyClientLoginRequest.ClientConnectionId, connectionId);
            AddMessageItem(outMessage, Account2ProxyClientLoginRequest.AccountId, accountId);
            PhotonApplication.Instance.NetHandle.Send(proxyServerId, ServerHandlerMessage.Account2ProxyClientLoginRequest, outMessage);
            DebugLog.LogErrorFormat("send Account2ProxyClientLoginRequest" +proxyServerId);
        }

        // Disconnect
        private void OnClientDisconnected(int connectionId, Dictionary<int, object> msg)
        {
            object tmp = RetrieveMessageItem(msg, (int)ClientDisconnected.ConnectionId);
            if (tmp == null || !(tmp is int))
            {
                return;
            }
            int clientConnectionId = (int)tmp;

            ClientInfo clientInfo = _accountIdConnectionId.Values.FirstOrDefault(x => x.AccountConnectionId == clientConnectionId);

            if (clientInfo != null)
            {
                SystemManager.Instance.GetSubsystem<LoginSystem>().AccomplishLogin(clientInfo.AccountId);
                if (clientInfo.SessionId != waitConnectionSessionId)
                {
                    var proxyServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Proxy);

                    Dictionary<int, object> outMessage = new Dictionary<int, object>();
                    AddMessageItem(outMessage, Account2ProxyPlayerLeave.AccountID, clientInfo.AccountId);
                    AddMessageItem(outMessage, Account2ProxyPlayerLeave.SessionID, clientInfo.SessionId);
                    PhotonApplication.Instance.NetHandle.Send(proxyServerId, ServerHandlerMessage.Account2ProxyPlayerLeave, outMessage);
                }

                _accountIdConnectionId.Remove(clientInfo.AccountId);
            }
        }
        // Login
        private void OnProxy2AccountClientLoginRespond(int connectionId, Dictionary<int, object> msg)
        {
            ErrorCode err;
            long accountId;
            int lobbyId;
            int sessionId;
            int fromLobbyClientConnectionId;

            if (!RetrieveMessageItem(msg, Proxy2AccountClientLoginRespond.ErrorCode, out err)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountClientLoginRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountClientLoginRespond.LobbyId, out lobbyId)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountClientLoginRespond.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountClientLoginRespond.ClientConnectionId, out fromLobbyClientConnectionId)) return;

            ClientInfo clientInfo;
            if (!_accountIdConnectionId.TryGetValue(accountId, out clientInfo))
            {
                DebugLog.Log("Client login: SessionId not exist....");
                Dictionary<int, object> outLoadLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outLoadLeaveMessage, Account2ProxyPlayerLeave.AccountID, accountId);
                AddMessageItem(outLoadLeaveMessage, Account2ProxyPlayerLeave.SessionID, sessionId);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Account2ProxyPlayerLeave, outLoadLeaveMessage);
                return;
            }

            if (fromLobbyClientConnectionId != clientInfo.AccountConnectionId)
            {
                DebugLog.Log("Client login: A newer client has connected already ....");
                Dictionary<int, object> outLoadLeaveMessage = new Dictionary<int, object>();
                AddMessageItem(outLoadLeaveMessage, Account2ProxyPlayerLeave.AccountID, accountId);
                AddMessageItem(outLoadLeaveMessage, Account2ProxyPlayerLeave.SessionID, sessionId);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Account2ProxyPlayerLeave, outLoadLeaveMessage);
                return;
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            if (err != ErrorCode.Success)
            {
                AddMessageItem(outMessage, AccountLoginRespond.ErrorCode, (int)err);
                AddMessageItem(outMessage, AccountLoginRespond.SessionId, sessionId);
                AddMessageItem(outMessage, AccountLoginRespond.LobbyServerIP, string.Empty);
                AddMessageItem(outMessage, AccountLoginRespond.LobbyServerPort, 0);
                PhotonApplication.Instance.NetHandle.Send(clientInfo.AccountConnectionId, ClientHandlerMessage.AccountLoginRespond, outMessage);

                SystemManager.Instance.GetSubsystem<LoginSystem>().AccomplishLogin(accountId);
                _accountIdConnectionId.Remove(accountId);
                PhotonApplication.Instance.NetHandle.Disconnect(clientInfo.AccountConnectionId);

                // login error log
                //LogSystem system = ServerSystemManager.Instance.GetSubsystem<LogSystem>();
                //system.LogError(accountId, GameType.None, err);
                return;
            }

            string lobbyIP = TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterIpAddress(lobbyId);
            string lobbyPort = (clientInfo.AccountConnectionId > 0) ? TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterPort(lobbyId)
                : TableManager.Instance.GetTable<ServerListConfigReader>().GetWebPort(lobbyId);

            int lobbyPortNumber;
            if (!int.TryParse(lobbyPort, out lobbyPortNumber))
            {
                DebugLog.LogError("Client login: error server port " + lobbyPort);
            }

            clientInfo.SetSessionId(sessionId);

            AddMessageItem(outMessage, (int)AccountLoginRespond.ErrorCode, (int)err);  // ErrorCode.Success
            AddMessageItem(outMessage, (int)AccountLoginRespond.SessionId, sessionId);
            AddMessageItem(outMessage, (int)AccountLoginRespond.LobbyServerIP, lobbyIP);
            AddMessageItem(outMessage, (int)AccountLoginRespond.LobbyServerPort, lobbyPortNumber);
            PhotonApplication.Instance.NetHandle.Send(clientInfo.AccountConnectionId, ClientHandlerMessage.AccountLoginRespond, outMessage);
        }
        // Lobby Login
        private void OnProxy2AccountPlayerEnteredRequest(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            int clientConnectionId;

            if (!RetrieveMessageItem(msg, Proxy2AccountPlayerEnteredRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountPlayerEnteredRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Proxy2AccountPlayerEnteredRequest.ConnectionId, out clientConnectionId)) return;

            // close the connection between client and Account system
            ClientInfo clientInfo;
            if (_accountIdConnectionId.TryGetValue(accountId, out clientInfo))
            {
                if (clientInfo.AccountConnectionId == clientConnectionId)
                {
                    SystemManager.Instance.GetSubsystem<LoginSystem>().AccomplishLogin(accountId);
                    _accountIdConnectionId.Remove(accountId);
                    PhotonApplication.Instance.NetHandle.Disconnect(clientConnectionId);
                }
                else
                {
                    DebugLog.LogErrorFormat("_accountIdConnectionId AccountConnectionId != clientConnectionId");
                    return;
                }
            }
            else
            {
                DebugLog.LogErrorFormat("_accountIdConnectionId not exist for accountId = {0} ", accountId);
                return;
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Account2ProxyPlayerEnteredRespond.AccountId, accountId);
            AddMessageItem(outMessage, Account2ProxyPlayerEnteredRespond.SessionId, sessionId);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Account2ProxyPlayerEnteredRespond, outMessage);
        }
        private void OnAccountConnectRequest(int connectionId, Dictionary<int, object> msg)
        {
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.AccountConnectedRespond, outMessage);
        }
        private void OnAccountRegisterRequest(int connectionId, Dictionary<int, object> msg)
        {
            GameType gameType;
            string gameId = string.Empty;
            string password;
            DateTime registerTime = DateTime.Now;


            if (!RetrieveMessageItem(msg, AccountRegisterRequest.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, AccountRegisterRequest.GameId, out gameId)) return;
            if (!RetrieveMessageItem(msg, AccountRegisterRequest.Password, out password)) return;

            string gameTypeName = Enum.ToObject(typeof(GameType), gameType).ToString();
            // account login 
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountRegister_GameType, gameTypeName)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountRegister_GameId, gameId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountRegister_GamePassword, password)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountRegister_RegisterTime, registerTime)) return;

            DBManager.Instance.ExecuteReader(DBCatagory.Account, DbStoreProcedureInput.AccountRegister,
                parameters, _systemFiber, (reader) => OnDbCallback_AccountRegister(connectionId, reader, gameType)
            );
        }

        private void OnDbCallback_AccountRegister(int connectionId, List<Dictionary<string, object>> reader, GameType gameType)
        {
            ErrorCode err = ErrorCode.Success;
            int outSuccess = 0;
            bool isSuccess = false;

            if (reader.Count <= 0)
            {
                DebugLog.Log("[A account SP] reader count=0");
                err = ErrorCode.AccountUnableRetrieveData;
            }
            else
            {
                Dictionary<string, object> needReader = reader[0];
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.AccountLogin_Success, out outSuccess))
                { isSuccess = outSuccess == 1 ? true : false; };
            }

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, AccountRegisterRespond.ErrorCode, (int)err);
            AddMessageItem(outMessage, AccountRegisterRespond.Succes, isSuccess);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.AccountRegisterRespond, outMessage);
        }
    }
}