using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Model;
using KayKitMultiplayerServer.TableRelated.Application;
using KayKitMultiplayerServer.TableRelated;
using System.Collections.Generic;
using System;
using KayKitMultiplayerServer.Utility;
using UnityEngine;
using System.Data.Common;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public abstract class KayKitBase : IGameLogic, IDisposable
    {
        protected enum KayKitBaseDynamicInfo
        {
            GamePlayerRoomInfo,
        }
        private bool disposedValue = false;
        protected GameRoom _gameRoom;
        protected GameStaticInfo _gameStaticInfo = new();
        protected GameDynamicInfo _gameDynamicInfo = new();
        protected GameResultInfo _gameResultInfo = new();

        protected PlayerPositionTable _playerPositionTable;
        private Dictionary<PlayerSyncNetworkMessage, Action<GamePlayerRoomInfo, object>> gamePlayerRoomActionTable = new();
        public KayKitBase(IFiber fiber)
        {
            _playerPositionTable = TableManager.Instance.GetTable<PlayerPositionTable>("DefaultFolder");

            // Game Event
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerPosition, OnPlayerPosition);
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerLocalEulerAngles, OnPlayerLocalEulerAngles);
        }

        #region Game Logic
        public bool InitGame(GameRoom gameRoom)
        {
            bool succes = true;
            _gameRoom = gameRoom;
            _gameRoom.Info.GameRoomState = GameRoomState.WaitingEnterRoom;

            succes = OnInitGame();

            ResetAllPlayerPosition();


            return succes;
        }
        public ErrorCode StartGame(out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;
            _gameRoom.Info.GameRoomState = GameRoomState.GameStart;

            gameStaticInfo = _gameStaticInfo;

            OnStartGame();

            _gameDynamicInfo.AddDynamicData(KayKitBaseDynamicInfo.GamePlayerRoomInfo, _gameRoom.Info.SerilizeGamePlayerRoomInfoObject());

            gameDynamicInfo = _gameDynamicInfo;
            gameResultInfo = null;

            return errorCode;
        }
        public ErrorCode PlayerJoinGame(long accountId, out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo,
            out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;
            gameStaticInfo = _gameStaticInfo;
            gameDynamicInfo = null;
            gameResultInfo = null;

            if (_gameRoom.Info.GameRoomState == GameRoomState.GameStart)
                errorCode = PlayerSyncGame(accountId, null, null, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);      //RecconectGameData

            return ErrorCode.Success;
        }

        public ErrorCode PlayerLeaveGame(long accountId)
        {
            ErrorCode errorCode = ErrorCode.Success;

            return ErrorCode.Success;
        }
      
        public ErrorCode RemoveGame()
        {
            ErrorCode errorCode = ErrorCode.Success;
            OnRemoveGame();
            return errorCode;
        }
        public void Dispose()
        {
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }
    
        public ErrorCode PlayerSyncGame(long accountId, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;

            // Sync Game player Info
            if (playerMessage != null)
            {
                if (playerMessage.RetrieveMessageItem(PlayerSyncNetworkMessage.PlayerAccountId, out long playerAccountId))
                {
                    if (accountId == playerAccountId)
                    {
                        if (_gameRoom.GetGamePlayerRoomInfo(accountId, out GamePlayerRoomInfo gamePlayerRoomInfoInfo))
                        {
                            CallGamePlayerAction(playerMessage, gamePlayerRoomInfoInfo);
                        }
                    }
                }
            }

            // Game Static Data
            SetGameStaticData(out gameStaticInfo);

            // Game Dynamic Data
            SetGameDynamicData(out gameDynamicInfo);

            // Game Result Data
            SetGameResultData(out gameResultInfo);


            return ErrorCode.Success;
        }
        public ErrorCode PlayerNetworkInput(long accountId, PlayerNetworkInput playerNetworkInput)
        {
            if (_gameRoom.GetGamePlayerRoomInfo(accountId, out GamePlayerRoomInfo gamePlayerRoomInfo))
            {
                gamePlayerRoomInfo.IsPlayerShoot = playerNetworkInput.GetNetworkButtonInputData(NetworkInputButtons.FIRE);
                gamePlayerRoomInfo.gunAimDirection = playerNetworkInput.gunAimDirection;
            }

            return ErrorCode.Success;
        }
        #endregion

        protected void RegisterGamePlayerAction(PlayerSyncNetworkMessage playerSyncNetworkMessage,
            Action<GamePlayerRoomInfo, object> callAction)
        {
            Action<GamePlayerRoomInfo, object> gamePlayerAction;
            if (!gamePlayerRoomActionTable.TryGetValue(playerSyncNetworkMessage, out gamePlayerAction))
            {
                gamePlayerRoomActionTable.Add(playerSyncNetworkMessage, callAction);
            }
            else
            {
                gamePlayerRoomActionTable[playerSyncNetworkMessage] = callAction;
            }
        }
        protected void ResetAllPlayerPosition()
        {
            _gameRoom.ParallerGamePlayerAction(info =>
            {
                int index = _gameRoom.GetPlayerIndexFromAccoundID(info.AccountId);

                // Set player position
                var position = _playerPositionTable.GetPlayerPosition((GameType)_gameRoom.Info.RoomGameType, index);
                info.InitGameData(position);
            });
        }

        protected abstract bool OnInitGame();
        protected abstract void OnStartGame();
        protected abstract void OnRemoveGame();
        // Game Static Data
        protected abstract void SetGameStaticData(out GameStaticInfo gameStaticInfo);
        // Game Dynamic Data
        protected virtual void SetGameDynamicData(out GameDynamicInfo gameDynamicInfo)
        {
            _gameDynamicInfo.AddDynamicData(KayKitBaseDynamicInfo.GamePlayerRoomInfo, _gameRoom.Info.SerilizeGamePlayerRoomInfoObject());
            gameDynamicInfo = _gameDynamicInfo;
        }

        // Game Result Data
        protected abstract void SetGameResultData(out GameResultInfo gameResultInfo);
     
        protected abstract void OnPlayerPosition(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue);
        protected abstract void OnPlayerLocalEulerAngles(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue);
        private void CallGamePlayerAction(Dictionary<int, object> playerMessage, GamePlayerRoomInfo gamePlayerRoomInfo)
        {
            foreach (var message in playerMessage)
            {
                Action<GamePlayerRoomInfo, object> gamePlayerAction;
                if (gamePlayerRoomActionTable.TryGetValue((PlayerSyncNetworkMessage)message.Key, out gamePlayerAction))
                {
                    gamePlayerAction.Invoke(gamePlayerRoomInfo, message.Value);
                }
            }
        }
    }
}