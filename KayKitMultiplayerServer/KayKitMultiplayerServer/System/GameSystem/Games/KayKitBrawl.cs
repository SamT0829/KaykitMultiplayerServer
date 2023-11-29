using System;
using System.Collections.Generic;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Common;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Info.KayKitBrawl;
using KayKitMultiplayerServer.System.GameSystem.Model;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using KayKitMultiplayerServer.Utility;
using UnityEngine;
using System.Diagnostics;
using Random = System.Random;
using System.Linq;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using KayKitMultiplayerServer.TableRelated;
using KayKitMultiplayerServer.TableRelated.Application;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public class KayKitBrawl : IGameLogic
    {
        private enum KayKitBrawlGameState
        {
            None,
            Prepare,
            Game,
            Bonus,
            Finish,
            Ending,
        }
        private enum KayKitBrawlDynamicInfo
        {
            GameTimer,
            GameCoinInfo,
        }

        private int _coinId;
        private int CoinId
        {
            get
            {
                var _coinID = _coinId;
                _coinId++;
                return _coinID;
            }
        }

        // Timer
        private TimeSpan PrepareTimer = TimeSpan.FromSeconds(3);
        private TimeSpan GameTimer = TimeSpan.FromSeconds(60);
        private TimeSpan FinishTimer = TimeSpan.FromSeconds(5);

        // Game Setting
        private KayKitBrawlGameState gameState = KayKitBrawlGameState.None;
        private readonly Random _random = new();

        private GameRoom _gameRoom;
        private GameStaticInfo _gameStaticInfo = new();
        private GameDynamicInfo _gameDynamicInfo = new();
        private GameResultInfo _gameResultInfo = new();

        private RoutineBackgroundThread<KayKitBrawlGameState> routine;

        // Game Material
        private Dictionary<int, CoinInfo> coinSpawner = new();
        private Stopwatch _gameTimer = new();

        // Game Table
        private PlayerPositionTable _playerPositionTable;

        private Dictionary<PlayerSyncNetworkMessage, Action<GamePlayerRoomInfo, object>> gamePlayerRoomActionTable = new();

        public KayKitBrawl(IFiber fiber)
        {
            // Init Game Table
            _playerPositionTable = TableManager.Instance.GetTable<PlayerPositionTable>("DefaultFolder");

            // Setting routine
            routine = new RoutineBackgroundThread<KayKitBrawlGameState>(fiber, KayKitBrawlGameState.Prepare, 10L);
            routine.AddState(KayKitBrawlGameState.Prepare, Convert.ToInt64(PrepareTimer.TotalMilliseconds), PrepareBegin, PrepareUpdate, PrepareFinish);
            routine.AddState(KayKitBrawlGameState.Game, Convert.ToInt64(GameTimer.TotalMilliseconds), GameBegin, GameUpdate, GameFinish);
            routine.AddState(KayKitBrawlGameState.Finish, Convert.ToInt64(FinishTimer.TotalMilliseconds), FinishStart, null, FinishFinish);
            routine.AddState(KayKitBrawlGameState.Ending, Convert.ToInt64(FinishTimer.TotalMilliseconds), () => routine.Stop(), null, null);

            routine.AddTrasitionState(KayKitBrawlGameState.Prepare, KayKitBrawlGameState.Game);
            routine.AddTrasitionState(KayKitBrawlGameState.Game, KayKitBrawlGameState.Finish);
            routine.AddTrasitionState(KayKitBrawlGameState.Finish, KayKitBrawlGameState.Ending);

            // Game Event
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerPosition, OnPlayerPosition);
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerLocalEulerAngles, OnPlayerLocalEulerAngles);
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerTakeCoin, OnPlayerTakeCoin);
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerGetBullet, OnPlayerGetBullet);
        }

        public bool InitGame(GameRoom gameRoom)
        {
            bool succes = true;
            _gameRoom = gameRoom;

            _gameRoom.ParallerGamePlayerAction(info =>
            {
                int index = gameRoom.GetPlayerIndexFromAccoundID(info.AccountId);

                if (index < 0)
                {
                    succes = false;
                    return;
                }

                // Set player position
                var position = _playerPositionTable.GetPlayerPosition(GameType.KayKitBrawl, index);
                info.InitGameData(position);
            });


            return succes;
        }
        public ErrorCode StartGame(out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;
            gameStaticInfo = _gameStaticInfo;
            routine.Run();

            gameDynamicInfo = null;
            gameResultInfo = null;

            return errorCode;
        }
        public ErrorCode PlayerJoinGame(long accountId, out GameStaticInfo gameStaticInfo,
            out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;
            gameStaticInfo = null;
            gameDynamicInfo = null;
            gameResultInfo = null;

            if (_gameRoom.Info.GameRoomState == GameRoomState.GameStart)
                errorCode = PlayerSyncGame(accountId, null, null, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);      //RecconectGameData

            return errorCode;
        }
        public ErrorCode PlayerLeaveGame(long accountId)
        {
            ErrorCode errorCode = ErrorCode.Success;
          
            return errorCode;
        }
        public ErrorCode RemoveGame()
        {
            ErrorCode errorCode = ErrorCode.Success;
            return errorCode;
        }
        public ErrorCode PlayerNetworkInput(long accountId, PlayerNetworkInput playerNetworkInput)
        {
            ErrorCode errorCode = ErrorCode.Success;

            if (_gameRoom.GetGamePlayerRoomInfo(accountId, out GamePlayerRoomInfo gamePlayerRoomInfo))
            {
                gamePlayerRoomInfo.IsPlayerShoot = playerNetworkInput.GetNetworkButtonInputData(NetworkInputButtons.FIRE);
                gamePlayerRoomInfo.gunAimDirection = playerNetworkInput.gunAimDirection;
            }

            return errorCode;
        }

        public ErrorCode PlayerSyncGame(long accountId, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            ErrorCode errorCode = ErrorCode.Success;
            gameDynamicInfo = _gameDynamicInfo;
            gameStaticInfo = _gameStaticInfo;
            gameResultInfo = null;

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


            // Game Dynamic Data
            Dictionary<string, double> gameTimer = new Dictionary<string, double>();
            gameTimer.Add("PrepareTimer" ,PrepareTimer.TotalSeconds);
            gameTimer.Add("GameTimer" ,GameTimer.TotalSeconds);
            gameTimer.Add("BonusTimer", FinishTimer.TotalSeconds);

            Dictionary<int, object> coinData = coinSpawner.ToDictionary(x => x.Key, x => (object)x.Value.CreateSerializeObject());

            _gameDynamicInfo.AddDynamicData(KayKitBrawlDynamicInfo.GameTimer, gameTimer);
            _gameDynamicInfo.AddDynamicData(KayKitBrawlDynamicInfo.GameCoinInfo, coinData);

            switch (gameState)
            {
                case KayKitBrawlGameState.Finish:
                    gameResultInfo = _gameResultInfo;
                    gameState = KayKitBrawlGameState.Ending;
                    break;
            }

            return errorCode;
        }
       
        // State Action //
        private void PrepareBegin()
        {
            gameState = KayKitBrawlGameState.Prepare;
        }
        private bool PrepareUpdate(long timeelapsed)
        {
            PrepareTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if(PrepareTimer.TotalMilliseconds < 0)
                PrepareTimer = TimeSpan.Zero;

            return true;
        }
        private void PrepareFinish()
        {
            DebugLog.Log("PrepareFinish");
        }
        private void GameBegin()
        {
            DebugLog.Log("GameBegin");
            _gameTimer.Start();
            gameState = KayKitBrawlGameState.Game;

        }
        private bool GameUpdate(long timeelapsed)
        {
            GameTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (GameTimer.TotalMilliseconds < 0)
                GameTimer = TimeSpan.Zero;

            // Coin Spawner
            if (_gameTimer.Elapsed > TimeSpan.FromSeconds(3 * _coinId)) // run every 1000ms - 1s
            {
                var position = new Vector3((float)_random.Next(-150, 150) / 100, 0, (float)_random.Next(-150, 150) / 100);
                CoinSpawner(CoinId, position);
            }

            if (_gameRoom.GetRoomGamePlayerRoomInfos().Count > 0)
            {
                foreach (var gamePlayerInfo in _gameRoom.GetRoomGamePlayerRoomInfos())
                {
                    UpdatePlayerInfo(gamePlayerInfo, timeelapsed);
                }
            }

            //if (timer.Elapsed > TimeSpan.FromSeconds(1 * monsterID)) // run every 1000ms - 1s
            //{
            //    var position = new Vector3((float)random.Next(-150, 150) / 100, (float)random.Next(-150, 150) / 100);
            //    MonsterSpawner(MonsterID, position, 1);
            //}

            //if (bulletSpawner.Count > 0)
            //{
            //    var bulletInfoList = bulletSpawner.Values.ToList();
            //    bulletInfoList.ForEach(bulletInfo =>
            //    {
            //        bulletInfo.Update(timeelapsed);
            //        if (!bulletInfo.alive)
            //            bulletSpawner.Remove(bulletID);
            //    });
            //}

            //if (_gameRoom.GetGameRoomAllGamePlayerInfo().Count > 0)
            //{
            //    foreach (var gamePlayerInfo in GameRoom.GetGameRoomAllGamePlayerInfo())
            //    {
            //        gamePlayerInfo.UpdatePlayerInfo(timeelapsed);
            //        UpdatePlayerInfo(gamePlayerInfo, timeelapsed);
            //    }
            //}

            return true;
        }
        private void GameFinish()
        {
            DebugLog.Log("GameFinish");
            _gameTimer.Stop();
            gameState = KayKitBrawlGameState.Finish;
            _gameResultInfo.winnerInfo = _gameRoom.CheckWinnerPlayer();
        }
        private void FinishStart()
        {
        }
        private void FinishFinish()
        {
            SystemManager.Instance.GetSubsystem<GameRoomSystem>().GameOver(_gameRoom);
        }
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
        private void RegisterGamePlayerAction(PlayerSyncNetworkMessage playerSyncNetworkMessage,
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

        private void UpdatePlayerInfo(GamePlayerRoomInfo gamePlayerRoomInfo, long timeElapsed)
        {
            // Player Death
            if (gamePlayerRoomInfo.IsPlayerDie)
            {
                gamePlayerRoomInfo.DeathTimer -= TimeSpan.FromMilliseconds(timeElapsed);

                if (gamePlayerRoomInfo.DeathTimer <= TimeSpan.Zero)
                {
                    gamePlayerRoomInfo.IsPlayerDie = false;
                    gamePlayerRoomInfo.PlayerPosition = Vector3.zero;
                    gamePlayerRoomInfo.PlayerHealth = gamePlayerRoomInfo.PlayerMaxHealth;
                }
            }
        }

        // Player Event Function
        //private void UpdatePlayerInfo(GamePlayerRoomInfo gamePlayerInfo, long timeElapsed)
        //{
        //    // Player Death Timer
        //    if (gamePlayerInfo.IsPlayerDie)
        //    {
        //        gamePlayerInfo.DeathTimer -= TimeSpan.FromMilliseconds(timeElapsed);

        //        if (gamePlayerInfo.DeathTimer <= TimeSpan.Zero)
        //        {
        //            gamePlayerInfo.IsPlayerDie = false;
        //            gamePlayerInfo.PlayerPosition = Vector3.zero;
        //            gamePlayerInfo.PlayerHealth = gamePlayerInfo.PlayerMaxHealth;
        //        }
        //    }
        //}
        private void OnPlayerPosition(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            float[] playerPosition = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerPosition = playerPosition.ToVector3();
        }
        private void OnPlayerLocalEulerAngles(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            float[] playerLocalScale = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerLocalEulerAngles = playerLocalScale.ToVector3();
        }
        private void OnPlayerTakeCoin(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is int))
                return;

            int coinId = (int)playerMesageValue;
            if (coinSpawner.TryGetValue((int)coinId, out CoinInfo coin))
            {
                if (!coin.active)
                    return;

                gamePlayerRoomInfo.CointCount++;
                coin.PlayerGetCoin(gamePlayerRoomInfo.NickName);
            }
        }
        private void OnPlayerGetBullet(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is int))
                return;

            int bulletDamage = Convert.ToInt32(playerMesageValue);

            // Player Death
            if (gamePlayerRoomInfo.PlayerHealth > 0)
            {
                gamePlayerRoomInfo.PlayerHealth -= bulletDamage;

                if (gamePlayerRoomInfo.PlayerHealth <= 0)
                {
                    gamePlayerRoomInfo.IsPlayerDie = true;
                    gamePlayerRoomInfo.DeathCount++;
                    gamePlayerRoomInfo.CointCount = 0;
                    gamePlayerRoomInfo.DeathTimer = TimeSpan.FromSeconds(3);

                    // Remove player Coin
                    var removeCoin = coinSpawner.Values.Where(coin => coin.PlayerName == gamePlayerRoomInfo.NickName);
                    foreach (var coin in removeCoin)
                    {
                        var position = new Vector2(gamePlayerRoomInfo.PlayerPosition.x + _random.Next(-1, 1) * 0.5f,
                            gamePlayerRoomInfo.PlayerPosition.y + _random.Next(-1, 1) * 0.5f);
                        coin.RemoveCoin(position);
                    }
                }
            }
        }

        // Game Spawner
        private CoinInfo CoinSpawner(int coinId, Vector3 position)
        {
            CoinInfo coinInfo = new CoinInfo();
            coinInfo.InitCoin(coinId, position);
            coinSpawner.Add(coinInfo.CoinID, coinInfo);

            return coinInfo;
        }
    }
}