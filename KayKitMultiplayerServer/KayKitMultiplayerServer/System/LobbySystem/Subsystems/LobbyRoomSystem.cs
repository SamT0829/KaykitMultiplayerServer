using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.System.GameSystem.Model;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using KayKitMultiplayerServer.System.LobbySystem.Model;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.Utility;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;

namespace KayKitMultiplayerServer.System.LobbySystem.Subsystems
{
    public class LobbyRoomSystem : ServerSubsystemBase
    {
        private const int PAGE_ROOM_DATA = 0;

        private static int _testRoomId = -1;


        private static int _roomId = 0;
        public int RoomId
        {
            get
            {
                _roomId++;
                var roomId = _roomId;
                return roomId;
            }
        }

        // Game lobbyRoom Table
        private Dictionary<int, LobbyRoom> _roomIdLobbyRoomTable = new Dictionary<int, LobbyRoom>();
        public LobbyRoomSystem(IFiber systemFiber) : base(systemFiber)
        {
            new BackgroundThread<int, LobbyRoom>(systemFiber, _roomIdLobbyRoomTable, 10L, LobbyRoomBackgroundThreadUpdateAction);

            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Game2LobbyLobbyRoomEnteredRespond, OnGame2LobbyLobbyRoomEnteredRespond);
            RegisterObservedMessage(MessageType.ServerHandlerMessage, ServerHandlerMessage.Game2LobbyGameRoomOverRequest, OnGame2LobbyGameRoomOverRequest);

        }

        public LobbyRoom GetLobbyRoomByRoomName(string roomName)
        {
            LobbyRoom lobbyRoom = _roomIdLobbyRoomTable.Values.FirstOrDefault(x => x.LobbyRoomInfo.RoomName == roomName);
            return lobbyRoom;
        }
        public LobbyRoom GetLobbyRoomByRoomId(int roomid)
        {
            _roomIdLobbyRoomTable.TryGetValue(roomid, out LobbyRoom lobbyRoom);
            return lobbyRoom;
        }
        public List<object> GetRoomListData()
        {
            var roomData = _roomIdLobbyRoomTable.Values.ToList();
            int maxSendDataCount = 10;
            int totalPage = roomData.Count / maxSendDataCount;
            int startPage = totalPage > 0 ? PAGE_ROOM_DATA * maxSendDataCount : 0;
            int finishPage = roomData.Count > startPage + maxSendDataCount ? startPage + maxSendDataCount : roomData.Count;

            List<object> roomString = new List<object>();
            for (int i = startPage; i < finishPage; i++)
            {
                roomString.Add(roomData[i].LobbyRoomInfo.SerializeObject());
            }

            return roomString;
        }
        public ErrorCode CreateLobbyRoom(int roomType, string roomName, int maxPlayer, LobbyPlayer lobbyPlayerHost, out LobbyRoom lobbyRoom, string roomPassword = "")
        {
            ErrorCode err = ErrorCode.LobbyRoomNameHasAlreadyBeenUsed;

            lobbyRoom = GetLobbyRoomByRoomName(roomName);
            if (lobbyRoom == null)
            {
                var roomId = RoomId;
                if (!_roomIdLobbyRoomTable.TryGetValue(roomId, out lobbyRoom))
                {
                    lobbyRoom = new LobbyRoom();
                    lobbyRoom.CreateLobbyRoom(roomType, roomId, roomName, roomPassword, maxPlayer, lobbyPlayerHost);
                    _roomIdLobbyRoomTable.Add(roomId, lobbyRoom);
                    err = ErrorCode.Success;
                }
            }

            return err;
        }
        public void TestGame(int roomType, LobbyPlayer lobbyPlayer, out LobbyRoom lobbyRoom)
        {
            var roomId = _testRoomId;

            if (!_roomIdLobbyRoomTable.TryGetValue(roomId, out lobbyRoom))
            {
                lobbyRoom = new LobbyRoom();
                lobbyRoom.CreateLobbyRoom(roomType, roomId, "TestGame", string.Empty, 4, lobbyPlayer);
                _roomIdLobbyRoomTable.Add(roomId, lobbyRoom);
            }
            else
            {
                lobbyRoom.JoinLobbyRoom(lobbyPlayer, string.Empty);
            }
        }
        public ErrorCode JoinLobbyRoom(int roomId, LobbyPlayer lobbyPlayer, out LobbyRoom lobbyRoom, string roomPassword = "")
        {
            if (_roomIdLobbyRoomTable.TryGetValue(roomId, out lobbyRoom))
            {
                return lobbyRoom.JoinLobbyRoom(lobbyPlayer, roomPassword);
            }

            return ErrorCode.LobbyRoomDoesNotExist;
        }
        public bool LeaveLobbyRoom(LobbyPlayer lobbyPlayer)
        {
            LobbyRoom lobbyRoom;
            if (_roomIdLobbyRoomTable.TryGetValue(lobbyPlayer.LobbyPlayerRoomInfo.RoomId, out lobbyRoom))
            {
                lobbyRoom.LeaveLobbyRoomFromAccountId(lobbyPlayer.LobbyPlayerInfo.AccountId);

                if (!lobbyRoom.LobbyRoomHasPlayer())
                {
                    _roomIdLobbyRoomTable.Remove(lobbyPlayer.LobbyPlayerRoomInfo.RoomId);
                }

                return true;
            }
            return false;
        }
        public ErrorCode StartGame(LobbyPlayer lobbyPlayer)
        {
            ErrorCode errorCode = ErrorCode.LobbyRoomDoesNotExist;
            LobbyRoom lobbyRoom = GetLobbyRoomByRoomId(lobbyPlayer.LobbyPlayerRoomInfo.RoomId);
            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);
            
            if (gameServerId < 0)
            {
                return ErrorCode.ServerNotReady;
            }

            if (lobbyRoom != null)
                errorCode = lobbyRoom.StartLobbyRoom(lobbyPlayer, gameServerId);

            return errorCode;
        }

        public ErrorCode ChangeTeam(LobbyPlayer lobbyPlayer)
        {
            ErrorCode errorCode = ErrorCode.LobbyRoomDoesNotExist;
            LobbyRoom lobbyRoom = GetLobbyRoomByRoomId(lobbyPlayer.LobbyPlayerRoomInfo.RoomId);
           
            if (lobbyRoom != null)
                errorCode = lobbyRoom.ChangeTeam(lobbyPlayer);

            return errorCode;
        }

        // Thread Update
        private void LobbyRoomBackgroundThreadUpdateAction(LobbyRoom lobbyRoom)
        {
            if (lobbyRoom.LobbyRoomHasPlayer())
            {
                switch (lobbyRoom.LobbyRoomInfo.LobbyRoomState)
                {
                    case LobbyRoomState.Idle:
                    case LobbyRoomState.Start:
                        List<object> lobbyRoomChatMessage = lobbyRoom.GetRoomChatMessage();
                        lobbyRoom.LobbyRoomPlayerAction((lobbyPlayer) => SendRoomBackgroundThreadMessage(lobbyPlayer, lobbyRoom, lobbyRoomChatMessage));
                        break;
                }
            }
        }
        private void SendRoomBackgroundThreadMessage(LobbyPlayer lobbyPlayer, LobbyRoom lobbyRoom, List<object> lobbyRoomChatMessage)
        {
            Dictionary<int, object> outMessage = new Dictionary<int, object>();

            if (lobbyPlayer.LobbyPlayerInfo.Status == LobbyPlayerStatus.LobbyRoom)
            {
                AddMessageItem(outMessage, LobbyRoomBackgroundThread.LobbyRoomData, lobbyRoom.LobbyRoomInfo.SerializeObject());
                if (lobbyRoomChatMessage != null)
                    AddMessageItem(outMessage, LobbyRoomBackgroundThread.LobbyRoomMessage, JsonConvert.SerializeObject(lobbyRoomChatMessage));
                PhotonApplication.Instance.NetHandle.Send(lobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyRoomBackgroundThread, outMessage);
            }
        }
        private void OnGame2LobbyLobbyRoomEnteredRespond(int connectionId, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnGame2LobbyRoomEnteredRespond" + JsonConvert.SerializeObject(msg));
            if (!RetrieveMessageItem(msg, Game2LobbyLobbyRoomEnteredRespond.ErrorCode, out ErrorCode errorCode)) return;
            if (!RetrieveMessageItem(msg, Game2LobbyLobbyRoomEnteredRespond.RoomId, out int roomId)) return;

            if (_roomIdLobbyRoomTable.TryGetValue(roomId, out LobbyRoom lobbyRoom))
            {
                lobbyRoom.LobbyRoomPlayerAction(lobbyPlayer =>
                {
                    Dictionary<int, object> outMessage = new Dictionary<int, object>();
                    if (lobbyPlayer.LobbyPlayerInfo.Status == LobbyPlayerStatus.LobbyRoom)
                    {
                        AddMessageItem(outMessage, LobbyPlayerRoomEntered.ErrorCode, errorCode);
                        AddMessageItem(outMessage, LobbyPlayerRoomEntered.GameScene, lobbyRoom.LobbyRoomInfo.RoomType);
                        PhotonApplication.Instance.NetHandle.Send(lobbyPlayer.ClientInfo.LobbyConnectionId, ClientHandlerMessage.LobbyRoomEnterGameRoom, outMessage);
                    }
                });
                lobbyRoom.StartGame();
            }
            else
            {
                DebugLog.LogErrorFormat("Cant found Room for {0} roomId", roomId);
            }
        }
        private void OnGame2LobbyGameRoomOverRequest(int connectionID, Dictionary<int, object> msg)
        {
            DebugLog.Log("OnGame2LobbyGameOverRequest" + JsonConvert.SerializeObject(msg));
            if (!RetrieveMessageItem(msg, Game2LobbyGameRoomOverRequest.RoomId, out int roomId)) return;
            if (!RetrieveMessageItem(msg, Game2LobbyGameRoomOverRequest.GameRoomInfo, out Dictionary<int, object> gameRoomData)) return;

            if (_roomIdLobbyRoomTable.TryGetValue(roomId, out LobbyRoom lobbyRoom))
            {
                lobbyRoom.LobbyRoomInfo.LobbyRoomState = LobbyRoomState.Idle;

                List<LobbyPlayerInfo> removePlayerInfoList = new List<LobbyPlayerInfo>();
                lobbyRoom.LobbyRoomPlayerAction(lobbyPlayer =>
                {
                    var playerAccountId = lobbyPlayer.LobbyPlayerRoomInfo.AccountId;

                    GameRoomInfo gameRoomInfo = new();
                    gameRoomInfo.DeserializeObject(gameRoomData);
                    if (gameRoomInfo.WinnerTeam == lobbyPlayer.LobbyPlayerRoomInfo.Team)
                        lobbyPlayer.LobbyPlayerInfo.TotalWinCount++;
                    else if(gameRoomInfo.WinnerTeam != lobbyPlayer.LobbyPlayerRoomInfo.Team && gameRoomInfo.WinnerTeam != Team.None)
                        lobbyPlayer.LobbyPlayerInfo.TotalLoseCount++;

                    //var win = gameRoomInfo.WinnerPlayerAccountId.Where(accountId => accountId == playerAccountId);
                    //if (win.Count() > 0) lobbyPlayer.LobbyPlayerInfo.TotalWinCount++;

                    //var lose = gameRoomInfo.LosePlayerAccountId.Where(accountId => accountId == playerAccountId);
                    //if (lose.Count() > 0) lobbyPlayer.LobbyPlayerInfo.TotalLoseCount++;

                    var player = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>().GetPlayerByAccountId(playerAccountId);
                    if (player != null)
                    {
                        Dictionary<int, object> outMessage = new Dictionary<int, object>();
                        lobbyPlayer = player;
                        lobbyPlayer.LobbyPlayerInfo.Status = LobbyPlayerStatus.LobbyRoom;
                        lobbyPlayer.LobbyPlayerInfo.UpdateLobbyPlayerInfo_ToDb(_systemFiber, count =>
                        {
                            var gameServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Game);
                            AddMessageItem(outMessage, Lobby2GameGameRoomOverRespond.AccountId, player.ClientInfo.AccountId);
                            AddMessageItem(outMessage, Lobby2GameGameRoomOverRespond.SessionId, player.ClientInfo.SessionId);
                            AddMessageItem(outMessage, Lobby2GameGameRoomOverRespond.LobbyConnectionId, player.ClientInfo.LobbyConnectionId);
                            PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GameGameRoomOverRespond, outMessage);
                        });
                    }
                    else
                    {
                        removePlayerInfoList.Add(lobbyPlayer.LobbyPlayerInfo);
                        DebugLog.Log("removeList add " + lobbyPlayer.LobbyPlayerInfo.AccountId);
                    }
                });

                removePlayerInfoList.ForEach(removePlayerInfo =>
                {
                    removePlayerInfo.Status = LobbyPlayerStatus.Lobby;
                    removePlayerInfo.UpdateLobbyPlayerInfo_ToDb(_systemFiber, null);
                    lobbyRoom.LeaveLobbyRoomFromAccountId(removePlayerInfo.AccountId);

                    if (!lobbyRoom.LobbyRoomHasPlayer())
                    {
                        _roomIdLobbyRoomTable.Remove(roomId);
                    }

                    DebugLog.Log("OnGame2LobbyGameOverRequest cant find Lobby player by accounId " + removePlayerInfo.AccountId);
                });

                lobbyRoom.LobbyRoomPlayerAction(lobbyPlayer =>
                {
                    var player = SystemManager.Instance.GetSubsystem<LobbyPlayerSystem>().GetPlayerByAccountId(lobbyPlayer.LobbyPlayerInfo.AccountId);
                    if (player != null)
                    {
                        DebugLog.Log("Send GameOver Respond GoToLobby gameisOver" + player.ClientInfo.LobbyConnectionId);
                        Dictionary<int, object> outClientMessage = new Dictionary<int, object>();
                        AddMessageItem(outClientMessage, GameRoomOver.LobbyRoomInfo, lobbyRoom.LobbyRoomInfo.SerializeObject());
                        PhotonApplication.Instance.NetHandle.Send(player.ClientInfo.LobbyConnectionId, ClientHandlerMessage.GameRoomOver, outClientMessage);
                    }
                });
            }
        }
    }
}