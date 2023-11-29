using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.System.GameSystem.Model;
using System;
using System.Collections.Generic;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using MySqlX.XDevAPI.Common;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using KayKitMultiplayerServer.System.LobbySystem.Model;

namespace KayKitMultiplayerServer.System.GameSystem.Subsystems
{
    public class GameRoomSystem : ServerSubsystemBase
    {
        public Dictionary<int, GameRoom> waittingStartRoomIdGameRoomTable = new Dictionary<int, GameRoom>();
        // Game lobbyRoom Table
        private Dictionary<int, GameRoom> _roomIdGameRoomTable = new Dictionary<int, GameRoom>();

        public GameRoomSystem(IFiber systemFiber) : base(systemFiber)
        {
            new BackgroundThread<int, GameRoom>(systemFiber, waittingStartRoomIdGameRoomTable, 30L, WaittingStartRoomBackgroundThreadUpdateAction);
        }

        public GameRoom GetGameRoomByGameRoomId(int gameRoomId)
        {
            _roomIdGameRoomTable.TryGetValue(gameRoomId, out GameRoom gameRoom);
            return gameRoom;
        }

        // Game Logic
        public ErrorCode InitGame(LobbyRoomInfo lobbyRoomInfo)
        {
            if (!waittingStartRoomIdGameRoomTable.ContainsKey(lobbyRoomInfo.RoomId))
            {
                if (!_roomIdGameRoomTable.ContainsKey(lobbyRoomInfo.RoomId))
                {
                    GameRoom gameRoom = new GameRoom();
                    gameRoom.InitGameRoom(lobbyRoomInfo);

                    IGameLogic gameLogic = SystemManager.Instance.GetSubsystem<GameLogicSystem>().CreateGame(gameRoom);
                    if (gameLogic == null)
                    {
                        // error Message
                        DebugLog.Log("GameLogicSystem failed CreateGame");
                        return ErrorCode.GameBuilderCantFound;
                    }

                    waittingStartRoomIdGameRoomTable.Add(gameRoom.Info.RoomId, gameRoom);
                    //CreateGameRoomDB(0, gameRoom, finishCallBack);
                    return ErrorCode.Success;
                }
            }

            return ErrorCode.GameRoomIdHasAlreadyExist;
        }
     
        public ErrorCode CheckGamePlayerAtGameRoom(int roomId, long accountId, out GamePlayerRoomInfo gamePlayerRoomInfo)
        {
            bool succes = false;
            gamePlayerRoomInfo = null;

            GameRoom gameRoom;
            if (waittingStartRoomIdGameRoomTable.TryGetValue(roomId, out gameRoom))
            {
                succes = gameRoom.CheckGameRoomInfoHasGamePlayerRoomInfo(accountId, out gamePlayerRoomInfo);
            }

            else if (_roomIdGameRoomTable.TryGetValue(roomId, out gameRoom))
            {
                succes = gameRoom.CheckGameRoomInfoHasGamePlayerRoomInfo(accountId, out gamePlayerRoomInfo);
            }

            return succes ? ErrorCode.Success : ErrorCode.GameRoomHasNotExist;
        }
        
        public ErrorCode TryJoinGame(GamePlayer gamePlayer,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.GameRoomHasNotExist;
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;
            GameRoom gameRoom;

            if (waittingStartRoomIdGameRoomTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out gameRoom))
            {
                errorCode = SystemManager.Instance.GetSubsystem<GameLogicSystem>().JoinGame(gamePlayer.GamePlayerRoomInfo, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }
            // reconnect Game
            else if (_roomIdGameRoomTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out gameRoom))
            {
                errorCode = SystemManager.Instance.GetSubsystem<GameLogicSystem>().JoinGame(gamePlayer.GamePlayerRoomInfo, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }

            if (errorCode == ErrorCode.Success)
            {
                gameRoom?.PlayerJoinGameRoom(gamePlayer.GamePlayerRoomInfo.AccountId, gamePlayer);
            }

            return errorCode;
        }
        public void GameOver(GameRoom gameRoom)
        {
            if (_roomIdGameRoomTable.TryGetValue(gameRoom.Info.RoomId, out GameRoom _gameRoom))
            {
                SystemManager.Instance.GetSubsystem<GameLogicSystem>().RemoveGame(gameRoom.Info.RoomId);

                var lobbyServerId = SystemManager.Instance.MainSystem.GetServerId(RemoteConnetionType.Lobby);
                if (lobbyServerId < 0)
                    DebugLog.LogError("lobbyServerId is not found");

                //Send to lobby message
                DebugLog.Log("GameOver Send Message to LobbyMainSystem and goto Lobby");
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, Game2LobbyGameRoomOverRequest.RoomId, gameRoom.Info.RoomId);
                AddMessageItem(outMessage, Game2LobbyGameRoomOverRequest.GameRoomInfo, gameRoom.Info.SerializeObject());
                PhotonApplication.Instance.NetHandle.Send(lobbyServerId, ServerHandlerMessage.Game2LobbyGameRoomOverRequest, outMessage);
                _roomIdGameRoomTable.Remove(gameRoom.Info.RoomId);
            }
        }

        public bool OnPlayerLeaveGameRoom(GamePlayer gamePlayer)
        {
            GameRoom gameRoom;
            if (waittingStartRoomIdGameRoomTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out gameRoom))
            {
                gameRoom.PlayerLeaveGameRoom(gamePlayer.GamePlayerRoomInfo.AccountId);

                if (!gameRoom.GamePlayerCountInGameRoom())
                {
                    waittingStartRoomIdGameRoomTable.Remove(gamePlayer.GamePlayerRoomInfo.RoomId);
                    SystemManager.Instance.GetSubsystem<GameLogicSystem>().RemoveGame(gamePlayer.GamePlayerRoomInfo.RoomId);
                }

                return true;
            }

            if (_roomIdGameRoomTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out gameRoom))
            {
                gameRoom.PlayerLeaveGameRoom(gamePlayer.GamePlayerRoomInfo.AccountId);

                if (!gameRoom.GamePlayerCountInGameRoom())
                {
                    GameOver(gameRoom);
                }

                return true;
            }

            return false;
        }
        private void WaittingStartRoomBackgroundThreadUpdateAction(GameRoom gameRoom)
        {
            switch (gameRoom.Info.GameRoomState)
            {
                case GameRoomState.WaitingEnterRoom:
                    if (gameRoom.CheckAllPlayerHasJoinRoom())
                        gameRoom.Info.GameRoomState = GameRoomState.EnterRoomFinish;
                    break;

                case GameRoomState.EnterRoomFinish:
                    if (waittingStartRoomIdGameRoomTable.TryGetValue(gameRoom.Info.RoomId, out GameRoom _gameRoom))
                    {
                        GameStaticInfo gameStaticInfo;
                        GameDynamicInfo gameDynamicInfo;
                        GameResultInfo gameResultInfo;

                        ErrorCode error = SystemManager.Instance.GetSubsystem<GameLogicSystem>().StartGame(_gameRoom.Info.RoomId,
                            out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);

                        if (error == ErrorCode.Success)
                        {
                            gameRoom.StartGame(error, gameStaticInfo, gameDynamicInfo, gameResultInfo);
                            _roomIdGameRoomTable.Add(gameRoom.Info.RoomId, _gameRoom);

                            //Action<bool> finishUpdateGameRoomDb = (succes) =>
                            //{
                            //    gameRoom.StartGame(error, gameStaticInfo, gameDynamicInfo, gameResultInfo);
                            //    _roomIdGameRoomTable.Add(gameRoom.Info.RoomId, _gameRoom);
                            //};

                            //CreateGameRoomDB(1, gameRoom, finishUpdateGameRoomDb);
                        }
                    }
                    waittingStartRoomIdGameRoomTable.Remove(gameRoom.Info.RoomId);
                    break;
            }
        }
    }
}