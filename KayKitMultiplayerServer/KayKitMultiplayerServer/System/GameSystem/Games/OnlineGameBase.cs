using System;
using System.Collections.Generic;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Model;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using log4net.Core;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public abstract class OnlineGameBase : IGameLogic
    {
        protected Dictionary<long, GamePlayer> _gameTablePlayerTable = new();
        protected GameStaticInfo _gameStaticInfo = new();
        protected GameDynamicInfo _gameDynamicInfo = new();
        protected GameResultInfo _gameResultInfo = new();

        public bool InitGame(GameRoom gameRoom)
        {
            bool succes = true;

            succes = OnInitGame();

            return succes;
        }

        public ErrorCode StartGame(out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo,
            out GameResultInfo gameResultInfo)
        {
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;

            OnStartGame();

            return ErrorCode.OnlineGameNotNeedStartGame;
        }

        public ErrorCode PlayerJoinGame(long accountId, out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo,
            out GameResultInfo gameResultInfo)
        {
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;

            if (_gameTablePlayerTable.ContainsKey(accountId))
                return ErrorCode.GamePlayerHasInGame;


            _gameTablePlayerTable.Add(accountId, SystemManager.Instance.GetSubsystem<GamePlayerSystem>().GetPlayerByAccountId(accountId));
            OnPlayerJoinGame(accountId);
            var errorCode = PlayerSyncGame(accountId, null, null, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);

            gameStaticInfo = _gameStaticInfo;

            return errorCode;
        }
        public ErrorCode PlayerLeaveGame(long accountId)
        {
            var errorCode = ErrorCode.Success;
            if (_gameTablePlayerTable.ContainsKey(accountId))
                _gameTablePlayerTable.Remove(accountId);

            errorCode = OnPlayerLeaveGame(accountId);

            return errorCode;
        }

        protected abstract bool OnInitGame();
        protected abstract void OnStartGame();
        protected abstract void OnPlayerJoinGame(long accountId);
        public abstract ErrorCode PlayerSyncGame(long accountId, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo);
        public abstract ErrorCode PlayerNetworkInput(long accountId, PlayerNetworkInput playerNetworkInput);

        public abstract ErrorCode OnPlayerLeaveGame(long accountId);

        public abstract ErrorCode RemoveGame();
        // Game Static Data
        protected abstract void SetGameStaticData(out GameStaticInfo gameStaticInfo);
        // Game Dynamic Data
        protected abstract void SetGameDynamicData(out GameDynamicInfo gameDynamicInfo);
    }
}