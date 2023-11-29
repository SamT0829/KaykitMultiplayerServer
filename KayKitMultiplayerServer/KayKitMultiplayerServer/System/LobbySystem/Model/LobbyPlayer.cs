using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using KayKitMultiplayerServer.System.LobbySystem.Subsystems;
using System.Collections.Generic;
using KayKitMultiplayerServer.System.Common.Subsystems;
using KayKitMultiplayerServer.Utility;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.LobbySystem.Model
{
    public class LobbyPlayer
    {
        public ClientInfo ClientInfo { get; private set; }
        public LobbyPlayerInfo LobbyPlayerInfo = new LobbyPlayerInfo();
        public LobbyPlayerGameInfo LobbyPlayerGameInfo = new LobbyPlayerGameInfo();
        public LobbyPlayerRoomInfo LobbyPlayerRoomInfo = new LobbyPlayerRoomInfo();

        public IFiber LobbyPlayerSystemFiber;

        public LobbyPlayer(ClientInfo clientInfo, IFiber lobbyPlayerSystemFiber)
        {
            ClientInfo = clientInfo;
            LobbyPlayerSystemFiber = lobbyPlayerSystemFiber;
        }

        public void RegesterMessageObserver()
        {
            LobbyPlayerSystem lobbyPlayerSystem = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>();

            // Lobby LobbyRoom
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRequest, OnLobbyPlayerCreateLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerJoinLobbyRoomRequest, OnLobbyPlayerJoinLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerLeaveLobbyRoomRequest, OnLobbyPlayerLeaveLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerReadyLobbyRoomRequest, OnLobbyPlayerReadyLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerStartLobbyRoomRequest, OnLobbyPlayerStartLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerChangeTeamLobbyRoomRequest, OnLobbyPlayerChangeTeamLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerChatLobbyRoomRequest, OnLobbyPlayerChatLobbyRoomRequest);
            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerTestGameRequest, OnLobbyPlayerTestGameRequest);

            lobbyPlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerJoinGameRequest, OnLobbyPlayerJoinGameRequest);

        }
        public void RegesterHostLobbyRoomMessageObserver()
        {
            LobbyPlayerSystem lobbyPlayerSystem = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>();

        }
        public void UnregesterMessageObserver()
        {
            LobbyPlayerSystem lobbyPlayerSystem = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>();

            // Lobby LobbyRoom
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerJoinLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerLeaveLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerReadyLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerStartLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerChangeTeamLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerChatLobbyRoomRequest);
            lobbyPlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.LobbyPlayerTestGameRequest);


        }
        public void UnregesterHostLobbyRoomMessageObserver()
        {
            LobbyPlayerSystem lobbyPlayerSystem = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>();

        }

        // Lobby LobbyRoom Function
        public void JoinLobbyRoom(int roomId, Team team)
        {
            LobbyPlayerInfo.Status = LobbyPlayerStatus.LobbyRoom;
            LobbyPlayerRoomInfo.InitData(LobbyPlayerInfo, roomId, team);
        }
        public bool LeaveLobbyRoom()
        {
            var succes = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().LeaveLobbyRoom(this);
            if (succes)
            {
                switch (LobbyPlayerInfo.Status)
                {
                    case LobbyPlayerStatus.None:
                    case LobbyPlayerStatus.Lobby:
                        break;
                    case LobbyPlayerStatus.LobbyRoom:
                        LobbyPlayerInfo.Status = LobbyPlayerStatus.Lobby;
                        LobbyPlayerRoomInfo.ResetInfo();
                        //LobbyPlayerInfo.UpdateLobbyPlayerInfo_ToDb(null, null);
                        break;
                    //case LobbyPlayerStatus.Game:
                    //    var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);
                    //    Dictionary<int, object> outGameMessage = new Dictionary<int, object>();
                    //    outGameMessage.AddMessageItem(Lobby2GamePlayerLeaveRequest.AccountId, ClientOnlineInfo.AccountId);
                    //    outGameMessage.AddMessageItem(Lobby2GamePlayerLeaveRequest.SessionId, ClientOnlineInfo.SessionId);
                    //    PhotonApplication.Instance.NetHandle.Send(gameServerId, MsgType.NetMsg_Lobby2GamePlayerLeaveRequest, outGameMessage);
                    //    break;
                }
            }

            return succes;
        }
        // Network Function
        private void OnLobbyPlayerCreateLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnLobbyPlayerCreateLobbyRoomRequest" + JsonConvert.SerializeObject(msg));

            int roomType;
            string roomName;
            string roomPassword;
            int maxPlayer;
            Dictionary<int, object> outMessage;

            if (!msg.RetrieveMessageItem(LobbyPlayerCreateLobbyRoomRequest.RoomType, out roomType)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerCreateLobbyRoomRequest.RoomName, out roomName)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerCreateLobbyRoomRequest.RoomPassword, out roomPassword)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerCreateLobbyRoomRequest.MaxPlayer, out maxPlayer)) return;

            if (roomType == GameType.None.GetHashCode())
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerCreateLobbyRoomRespond.ErrorCode, ErrorCode.LobbyRoomCreateFailed);
                PhotonApplication.Instance.NetHandle.Send(ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRespond, outMessage);
                return;
            }

            if (LobbyPlayerInfo.Status == LobbyPlayerStatus.LobbyRoom)
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem( LobbyPlayerCreateLobbyRoomRespond.ErrorCode, ErrorCode.PlayerStatusIsInRoom);
                PhotonApplication.Instance.NetHandle.Send(ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRespond, outMessage);
                return;
            }

            LobbyRoom lobbyRoom;
            ErrorCode err = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().CreateLobbyRoom(roomType, roomName, maxPlayer, this, out lobbyRoom, roomPassword);

            if (err == ErrorCode.Success)   
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerCreateLobbyRoomRespond.ErrorCode, err);
                outMessage.AddMessageItem(LobbyPlayerCreateLobbyRoomRespond.RoomData, lobbyRoom.LobbyRoomInfo.SerializeObject());
                PhotonApplication.Instance.NetHandle.Send(ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRespond, outMessage);
            }
            else
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerCreateLobbyRoomRespond.ErrorCode, err);
                PhotonApplication.Instance.NetHandle.Send(ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerCreateLobbyRoomRespond, outMessage);

                LogSystem logSystem = SystemManager.Instance.GetSubsystem<LogSystem>();
                logSystem.LogError(ClientInfo.AccountId, ClientInfo.GameLocation, err);
            }
        }
        private void OnLobbyPlayerJoinLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            int roomId;
            int roomType;
            string roomName;
            string roomPassword;
            Dictionary<int, object> outMessage;

            if (!msg.RetrieveMessageItem(LobbyPlayerJoinLobbyRoomRequest.RoomId, out roomId)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerJoinLobbyRoomRequest.RoomType, out roomType)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerJoinLobbyRoomRequest.RoomName, out roomName)) return;
            if (!msg.RetrieveMessageItem(LobbyPlayerJoinLobbyRoomRequest.RoomPassword, out roomPassword)) return;

            if (LobbyPlayerInfo.Status == LobbyPlayerStatus.LobbyRoom)
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, ErrorCode.PlayerStatusIsInRoom);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerJoinLobbyRoomRespond, outMessage);
                return;
            }

            LobbyRoom lobbyRoom;
            ErrorCode err = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().JoinLobbyRoom(roomId, this, out lobbyRoom, roomPassword);

            if (err == ErrorCode.Success)
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
                if (lobbyRoom != null)
                    outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.RoomData, lobbyRoom.LobbyRoomInfo.SerializeObject());

                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerJoinLobbyRoomRespond, outMessage);
            }
            else
            {
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerJoinLobbyRoomRespond, outMessage);

                LogSystem logSystem = SystemManager.Instance.GetSubsystem<LogSystem>();
                logSystem.LogError(ClientInfo.AccountId, ClientInfo.GameLocation, err);
            }
        }
        private void OnLobbyPlayerLeaveLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnLeaveGameRequest");

            if (LobbyPlayerInfo.Status != LobbyPlayerStatus.LobbyRoom)
            {
                // Error
                DebugLog.Log("LobbyPlayer Status is Not at Room");
                return;
            }

            var success = LeaveLobbyRoom();

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(LobbyPlayerLeaveLobbyRoomRespond.Succes, success);
            PhotonApplication.Instance.NetHandle.Send(ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyPlayerLeaveLobbyRoomRespond, outMessage);
        }
        private void OnLobbyPlayerReadyLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnLobbyPlayerReadyLobbyRoomRequest");

            var lobbyRoom = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().GetLobbyRoomByRoomId(LobbyPlayerRoomInfo.RoomId);
            if (lobbyRoom != null)
            {
                if (lobbyRoom.LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Idle)
                {
                    LobbyPlayerRoomInfo.SetLobbyPlayerReady();
                }
            }
        }
        private void OnLobbyPlayerStartLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            Dictionary<int, object> outMessage;
            ErrorCode err;

            if (LobbyPlayerInfo.Status != LobbyPlayerStatus.LobbyRoom)
            {
                // Error
                DebugLog.LogError("LobbyPlayer Status is Not at Room");
                err = ErrorCode.PlayerStatusIsnotInRoom;
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerStartLobbyRoomRespond, outMessage);
                return;
            }

            err = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().StartGame(this);
            outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerStartLobbyRoomRespond, outMessage);
        }
        private void OnLobbyPlayerChangeTeamLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            Dictionary<int, object> outMessage;
            ErrorCode err;

            if (LobbyPlayerInfo.Status != LobbyPlayerStatus.LobbyRoom)
            {
                // Error
                DebugLog.LogError("LobbyPlayer Status is Not at Room");
                err = ErrorCode.PlayerStatusIsnotInRoom;
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerChangeTeamLobbyRoomRespond, outMessage);
                return;
            }

            if (LobbyPlayerRoomInfo.isReady)
            {
                err = ErrorCode.LobbyPlayerRoomIsReady;
                outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerChangeTeamLobbyRoomRespond, outMessage);
                return;
            }

            err = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().ChangeTeam(this);
            outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(LobbyPlayerJoinLobbyRoomRespond.ErrorCode, err);
            PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.LobbyPlayerChangeTeamLobbyRoomRespond, outMessage);
        }
        private void OnLobbyPlayerChatLobbyRoomRequest(int connectionId, Dictionary<int, object> msg)
        {
            string chatMsg;
            if (!msg.RetrieveMessageItem(LobbyPlayerChatLobbyRoomRequest.ChatMsg, out chatMsg)) return;
            LobbyRoom lobbyRoom = SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().GetLobbyRoomByRoomId(LobbyPlayerRoomInfo.RoomId);

            if (LobbyPlayerInfo.Status != LobbyPlayerStatus.LobbyRoom || lobbyRoom == null)
            {
                // Error
                DebugLog.Log("Lobby Player Status isn't at LobbyRoom");
                return;
            }

            lobbyRoom.AddRoomMessage(LobbyRoomMessage.PlayerMessage, LobbyPlayerInfo.NickName, chatMsg);
        }
        private void OnLobbyPlayerTestGameRequest(int connectionId, Dictionary<int, object> msg)
        {
            int roomType;
            if (!msg.RetrieveMessageItem(LobbyPlayerCreateLobbyRoomRequest.RoomType, out roomType)) return;

            LobbyRoom lobbyRoom;
            SystemManager.Instance.GetSubsystem<LobbyRoomSystem>().TestGame(roomType, this, out lobbyRoom);

            //Send Message to Game Server
            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(Lobby2GameLobbyRoomEnteredRequest.RoomData, lobbyRoom.LobbyRoomInfo.SerializeObject());
            PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GameLobbyRoomEnteredRequest, outMessage);
        }
        private void OnLobbyPlayerJoinGameRequest(int connectionId, Dictionary<int, object> msg)
        {
            int gameType;
            if (!msg.RetrieveMessageItem(LobbyPlayerJoinGameRequest.GameType, out gameType)) return;

            ErrorCode errorCode = ErrorCode.Success;
            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);
            if (gameServerId < 0) errorCode = ErrorCode.ServerNotReady;

            //Send Message to Game Server
            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(Lobby2GameLobbyPlayerJoinGameRequest.GameType, gameType);
            outMessage.AddMessageItem(Lobby2GameLobbyPlayerJoinGameRequest.AccountId, ClientInfo.AccountId);
            outMessage.AddMessageItem(Lobby2GameLobbyPlayerJoinGameRequest.SessionId, ClientInfo.SessionId);
            outMessage.AddMessageItem(Lobby2GameLobbyPlayerJoinGameRequest.GameLocation, ClientInfo.GameLocation);
            outMessage.AddMessageItem(Lobby2GameLobbyPlayerJoinGameRequest.LobbyConnectionId, ClientInfo.LobbyConnectionId);
            PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GameLobbyPlayerJoinGameRequest, outMessage);
        }
    }
}