using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.Common;
using System.Collections.Generic;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Games;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Model;
using log4net.Core;
using MySqlX.XDevAPI.Relational;

namespace KayKitMultiplayerServer.System.GameSystem.Subsystems
{
    public class GameLogicSystem : ServerSubsystemBase
    {
        // Game Table
        private Dictionary<GameType, IGameBuilder> _gameEngineerTable = new Dictionary<GameType, IGameBuilder>();
        private Dictionary<int, IGameLogic> _gameRoomIdGameTable = new Dictionary<int, IGameLogic>();

        // Online Game
        private Dictionary<GameType, OnlineGameBase> _onlineGameTable = new();
        public GameLogicSystem(IFiber systemFiber) : base(systemFiber)
        {
        }

        public void AddGame(GameType gameType, IGameBuilder gameBuilder)
        {
            _gameEngineerTable.Add(gameType, gameBuilder);
        }

        public IGameLogic CreateGame(GameRoom gameRoom)
        {
            bool succes = false;
            GameType gameType = (GameType) gameRoom.Info.RoomGameType;

            if (!_gameEngineerTable.TryGetValue(gameType, out IGameBuilder gameBuilder))
            {
                DebugLog.Log("game builder not found. game=" + gameType);
                return null;
            }

            IGameLogic gameLogic = gameBuilder.buildGameLogic(_systemFiber);
            if (gameLogic != null)
            {
                gameLogic.InitGame(gameRoom);
                _gameRoomIdGameTable.Add(gameRoom.Info.RoomId, gameLogic);
                return gameLogic;
            }

            DebugLog.Log("Table game builder build a null type class. game=" + gameType);
            return null;
        }
        public ErrorCode JoinGame(GamePlayerRoomInfo gamePlayerRoomInfo, out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo,
            out GameResultInfo gameResultInfo)
        {
            bool succes = false;
            ErrorCode errorCode = ErrorCode.GameHasNotReady;
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;

            if (_gameRoomIdGameTable.TryGetValue(gamePlayerRoomInfo.RoomId, out IGameLogic gameLogic))
            {
                errorCode = gameLogic.PlayerJoinGame(gamePlayerRoomInfo.AccountId, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }

            return errorCode;
        }

        public ErrorCode StartGame(int roomId, out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;
            ErrorCode errorCode = ErrorCode.GameHasNotReady;

            if (_gameRoomIdGameTable.TryGetValue(roomId, out IGameLogic gameLogic))
            {
                errorCode = gameLogic.StartGame(out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }

            return errorCode;
        }
        public ErrorCode PlayerLeaveGame(GamePlayer gamePlayer)
        {
            ErrorCode errorCode = ErrorCode.GameHasNotReady;

            if (gamePlayer.IsOnlineGamePlayer)
            {
                if (_onlineGameTable.TryGetValue(gamePlayer.ClientInfo.GameLocation, out OnlineGameBase onlineGame))
                {
                    errorCode = onlineGame.PlayerLeaveGame(gamePlayer.ClientInfo.AccountId);
                }
            }
            else
            {
                if (_gameRoomIdGameTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out IGameLogic gameLogic))
                {
                    errorCode = gameLogic.PlayerLeaveGame(gamePlayer.ClientInfo.AccountId);
                }
            }

            return errorCode;
        }
        public ErrorCode RemoveGame(int roomId)
        {
            ErrorCode errorCode = ErrorCode.GameHasNotReady;
            if (_gameRoomIdGameTable.TryGetValue(roomId, out IGameLogic gameLogic))
            {
                errorCode = gameLogic.RemoveGame();
                _gameRoomIdGameTable.Remove(roomId);
            }

            return errorCode;
        }
        public ErrorCode PlayerSyncGame(GamePlayer gamePlayer, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;
            ErrorCode errorCode = ErrorCode.GameRoomHasNotExist;

            if (!gamePlayer.IsOnlineGamePlayer)
            {
                if (_gameRoomIdGameTable.TryGetValue(gamePlayer.GamePlayerRoomInfo.RoomId, out IGameLogic gameLogic))
                {
                    errorCode = gameLogic.PlayerSyncGame(gamePlayer.ClientInfo.AccountId, playerMessage, gameMessage,
                        out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
                }
            }
            else
            {
                if (_onlineGameTable.TryGetValue(gamePlayer.ClientInfo.GameLocation, out OnlineGameBase onlineGame))
                {
                    errorCode = onlineGame.PlayerSyncGame(gamePlayer.ClientInfo.AccountId, playerMessage, gameMessage,
                        out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
                }
            }

            return errorCode;
        }
        public ErrorCode PlayerNetworkInput(int roomId, long accountId, PlayerNetworkInput playerNetworkInput)
        {
            ErrorCode errorCode = ErrorCode.GameRoomHasNotExist;

            if (_gameRoomIdGameTable.TryGetValue(roomId, out IGameLogic gameLogic))
            {
                gameLogic.PlayerNetworkInput(accountId, playerNetworkInput);
                errorCode = ErrorCode.Success;
            }

            return errorCode;
        }

        // Online Game
        public OnlineGameBase CreateOnlineGame(GameType gameType)
        {
            if (_onlineGameTable.TryGetValue(gameType, out OnlineGameBase onlineGame))
            {
                return onlineGame;
            }

            if (!_gameEngineerTable.TryGetValue(gameType, out IGameBuilder builder) || builder == null)
            {
                DebugLog.Log("Online game builder not found. game=" + gameType);
                return null;
            }

            OnlineGameBase onlineGameBase = builder.buildOnlineGame(_systemFiber);
            if (onlineGameBase != null)
            {
                onlineGameBase.InitGame(null);
                _onlineGameTable.Add(gameType, onlineGameBase);
                    
                return onlineGameBase;
            }

            DebugLog.Log("Online game builder build a null type class. game=" + gameType);
            return null;
        }
        public bool CheckOnlineGameServerIsStarted(GameType gameType)
        {
            return _onlineGameTable.ContainsKey(gameType);
        }

        public ErrorCode TryJoinOnlineGame(GamePlayer gamePlayer,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.GameRoomHasNotExist;
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;

            if (_onlineGameTable.TryGetValue(gamePlayer.ClientInfo.GameLocation, out OnlineGameBase onlineGame))
            {
                errorCode = onlineGame.PlayerJoinGame(gamePlayer.ClientInfo.AccountId, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);
            }
            return errorCode;
        }
    }
}