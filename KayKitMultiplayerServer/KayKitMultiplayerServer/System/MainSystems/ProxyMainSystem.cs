using System.Collections.Generic;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.Network.Client;

namespace KayKitMultiplayerServer.System.MainSystems
{
    public class ProxyMainSystem : Common.MainSystem
    {
        private Dictionary<long, ClientInfo> _accountIdClientOnlineInfoTable = new Dictionary<long, ClientInfo>();

        private int _currentSessionID = 1;
        private int NewSessionID
        {
            get
            {
                int newSessionID = _currentSessionID;
                ++_currentSessionID;
                return newSessionID;
            }
        }

        public ProxyMainSystem(IFiber systemFiber) : base(systemFiber)
        {
            // Account Login 
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Account2ProxyClientLoginRequest, OnAccount2ProxyClientLoginRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2ProxyClientLoginRespond, OnLobby2ProxyClientLoginRespond);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Account2ProxyPlayerLeave, OnAccount2ProxyPlayerLeave);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2ProxyPlayerLeave, OnLobby2ProxyPlayerLeave);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2ProxyKickPlayerRespond, OnLobby2ProxyKickPlayerRespond);

            // Lobby Login
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Lobby2ProxyPlayerEnteredRequest, OnLobby2ProxyPlayerEnteredRequest);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Account2ProxyPlayerEnteredRespond, OnAccount2ProxyPlayerEnteredRespond);
        }

        protected override void OnOutboundServerConnected(int serverId, string serverName, RemoteConnetionType serverType)
        {
            
        }

        private void SendClientLoginRequestToLobbyServer(int lobbyServerId, int accountServerId, long accountId, int sessionId, int clientConnectionId, GameType gameType)
        {
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            AddMessageItem(outMessage, Proxy2LobbyClientLoginRequest.LocatedAccountServerId, accountServerId);
            AddMessageItem(outMessage, Proxy2LobbyClientLoginRequest.AccountId, accountId);
            AddMessageItem(outMessage, Proxy2LobbyClientLoginRequest.SessionId, sessionId);
            AddMessageItem(outMessage, Proxy2LobbyClientLoginRequest.ClientConnectionId, clientConnectionId);
            AddMessageItem(outMessage, Proxy2LobbyClientLoginRequest.GameType, gameType);
            PhotonApplication.Instance.NetHandle.Send(lobbyServerId, ServerHandlerMessage.Proxy2LobbyClientLoginRequest, outMessage);
        }
        
        // Account Login 
        private void OnAccount2ProxyClientLoginRequest(int connectionId, Dictionary<int, object> msg)
        {
            GameType gameType;
            int clientConnectionId;
            long accountId;

            if (!RetrieveMessageItem(msg, Account2ProxyClientLoginRequest.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, Account2ProxyClientLoginRequest.ClientConnectionId, out clientConnectionId)) return;
            if (!RetrieveMessageItem(msg, Account2ProxyClientLoginRequest.AccountId, out accountId)) return;

            int sessionId = NewSessionID;
            var lobbyId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Lobby);

            // Lobby Server Not Ready
            if (lobbyId == -1)
            {
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.ErrorCode, ErrorCode.ServerNotReady);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.AccountId, accountId);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.SessionId, sessionId);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.LobbyId, 0);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.ClientConnectionId, clientConnectionId);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Proxy2AccountClientLoginRespond, outMessage);
                return;
            }

            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                // Kick lobby lobbyLobbyPlayerInfo
                Dictionary<int, object> outToLobbyMessage = new Dictionary<int, object>();
                AddMessageItem(outToLobbyMessage, Proxy2LobbyKickPlayerRequest.AccountID, clientInfo.AccountId);
                AddMessageItem(outToLobbyMessage, Proxy2LobbyKickPlayerRequest.SessionID, sessionId);
                AddMessageItem(outToLobbyMessage, Proxy2LobbyKickPlayerRequest.ReplacedSessionID, clientInfo.SessionId);
                AddMessageItem(outToLobbyMessage, Proxy2LobbyKickPlayerRequest.AccountServerID, connectionId);
                AddMessageItem(outToLobbyMessage, Proxy2LobbyKickPlayerRequest.ClientConnectionID, clientConnectionId);
                PhotonApplication.Instance.NetHandle.Send(lobbyId, ServerHandlerMessage.Proxy2LobbyKickPlayerRequest, outToLobbyMessage);

                _accountIdClientOnlineInfoTable.Remove(accountId);
            }
            else
            {
                SendClientLoginRequestToLobbyServer(lobbyId, connectionId, accountId, sessionId, clientConnectionId, gameType);
            }

            // setup client information
            clientInfo = new ClientInfo(accountId, sessionId, gameType);
            clientInfo.SetAccountConnectionId(clientConnectionId);
            clientInfo.SetClientLocated(RemoteConnetionType.Account);
            _accountIdClientOnlineInfoTable.Add(accountId, clientInfo);
        }
        private void OnLobby2ProxyClientLoginRespond(int connectionId, Dictionary<int, object> msg)
        {
            int accountServerId;
            long accountId;
            int clientConnectionID;

            if (!RetrieveMessageItem(msg, Lobby2ProxyClientLoginRespond.LocatedAccountServerID, out accountServerId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyClientLoginRespond.AccountID, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyClientLoginRespond.ClientConnectionID, out clientConnectionID)) return;

            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.ErrorCode, ErrorCode.Success);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.AccountId, accountId);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.SessionId, clientInfo.SessionId);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.LobbyId, connectionId);
                AddMessageItem(outMessage, Proxy2AccountClientLoginRespond.ClientConnectionId, clientConnectionID);
                PhotonApplication.Instance.NetHandle.Send(accountServerId, ServerHandlerMessage.Proxy2AccountClientLoginRespond, outMessage);
            }
        }
        private void OnAccount2ProxyPlayerLeave(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            if (!RetrieveMessageItem(msg, Account2ProxyPlayerLeave.AccountID, out accountId)) return;
            if (!RetrieveMessageItem(msg, Account2ProxyPlayerLeave.SessionID, out sessionId)) return;

            ClientInfo clientInfoOriginal;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfoOriginal))
            {
                if (clientInfoOriginal.SessionId == sessionId)
                {
                    _accountIdClientOnlineInfoTable.Remove(accountId);
                    DebugLog.Log("Account LobbyPlayer Leave, Account Id = " + accountId.ToString());
                }
            }
        }
        private void OnLobby2ProxyPlayerLeave(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            bool leaveDone;

            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerLeave.AccountID, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerLeave.SessionID, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerLeave.LeaveDone, out leaveDone)) return;

            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                if (clientInfo.SessionId == sessionId)
                {
                    _accountIdClientOnlineInfoTable.Remove(accountId);
                }
            }
        }
        private void OnLobby2ProxyKickPlayerRespond(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionID;
            int replacedSessionID;
            int accountServerID;
            int clientConnectionID;
            bool didKick;

            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.SessionId, out sessionID)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.ReplacedSessionId, out replacedSessionID)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.AccountServerId, out accountServerID)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.ClientConnectionId, out clientConnectionID)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyKickPlayerRespond.KickSuccess, out didKick)) return;

            // re-login if accountId exist (new login)
            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                if (clientInfo.SessionId == sessionID)
                {
                    SendClientLoginRequestToLobbyServer(
                        connectionId, accountServerID, accountId, sessionID, clientConnectionID, clientInfo.GameLocation);
                }
            }
        }

        // Lobby Login
        private void OnLobby2ProxyPlayerEnteredRequest(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            int lobbyConnectionId;

            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerEnteredRequest.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerEnteredRequest.SessionId, out sessionId)) return;
            if (!RetrieveMessageItem(msg, Lobby2ProxyPlayerEnteredRequest.LobbyConnectionId, out lobbyConnectionId)) return;

            var accountServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Account);

            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                if (clientInfo.SessionId == sessionId)
                {
                    clientInfo.SetLobbyConnectionId(lobbyConnectionId);

                    Dictionary<int, object> outMessage = new Dictionary<int, object>();
                    AddMessageItem(outMessage, Proxy2AccountPlayerEnteredRequest.AccountId, clientInfo.AccountId);
                    AddMessageItem(outMessage, Proxy2AccountPlayerEnteredRequest.SessionId, clientInfo.SessionId);
                    AddMessageItem(outMessage, Proxy2AccountPlayerEnteredRequest.ConnectionId, clientInfo.AccountConnectionId);
                    PhotonApplication.Instance.NetHandle.Send(accountServerId, ServerHandlerMessage.Proxy2AccountPlayerEnteredRequest, outMessage);
                    return;
                }
            }

            // current sessionId doesn't exist
            Dictionary<int, object> outToLobbyMessage = new Dictionary<int, object>();
            AddMessageItem(outToLobbyMessage, Proxy2LobbyPlayerEnteredRespond.ErrorCode, ErrorCode.PlayerKickByDuplicatedLogin);
            AddMessageItem(outToLobbyMessage, Proxy2LobbyPlayerEnteredRespond.AccountId, accountId);
            AddMessageItem(outToLobbyMessage, Proxy2LobbyPlayerEnteredRespond.SessionId, sessionId);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ServerHandlerMessage.Proxy2LobbyPlayerEnteredRespond, outToLobbyMessage);
        }
        private void OnAccount2ProxyPlayerEnteredRespond(int connectionId, Dictionary<int, object> msg)
        {
            long accountId;
            int sessionId;
            ErrorCode err = ErrorCode.PlayerKickByDuplicatedLogin;

            if (!RetrieveMessageItem(msg, Account2ProxyPlayerEnteredRespond.AccountId, out accountId)) return;
            if (!RetrieveMessageItem(msg, Account2ProxyPlayerEnteredRespond.SessionId, out sessionId)) return;

            var lobbyId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Lobby);


            ClientInfo clientInfo;
            if (_accountIdClientOnlineInfoTable.TryGetValue(accountId, out clientInfo))
            {
                if (clientInfo.SessionId == sessionId)
                {
                    err = ErrorCode.Success;
                    clientInfo.SetClientLocated(RemoteConnetionType.Lobby);
                }
                Dictionary<int, object> outToLobbyMessage = new Dictionary<int, object>();
                AddMessageItem(outToLobbyMessage, (int)Proxy2LobbyPlayerEnteredRespond.ErrorCode, err);
                AddMessageItem(outToLobbyMessage, (int)Proxy2LobbyPlayerEnteredRespond.AccountId, accountId);
                AddMessageItem(outToLobbyMessage, (int)Proxy2LobbyPlayerEnteredRespond.SessionId, sessionId);
                PhotonApplication.Instance.NetHandle.Send(lobbyId, ServerHandlerMessage.Proxy2LobbyPlayerEnteredRespond, outToLobbyMessage);
            }
            else
            {
                // TODO: 
                DebugLog.Log("Client login: ProxySystem can't find ClientOnlineInfo of accountId: " + accountId + " After close Account:Client connection.");
            }
        }
    }
}