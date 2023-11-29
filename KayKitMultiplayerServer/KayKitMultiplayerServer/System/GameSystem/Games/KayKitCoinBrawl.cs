using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Info.KayKitBrawl;
using KayKitMultiplayerServer.TableRelated.Application;
using KayKitMultiplayerServer.TableRelated;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Random = System.Random;
using System.Linq;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using KayKitMultiplayerServer.Utility;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public class KayKitCoinBrawl : KayKitBase
    {
        private enum KayKitCoinBrawlState
        {
            None,
            Prepare,
            Game,
            Bonus,
            Finish,
            Ending,
        }
        private enum KayKitCoinBrawlDynamicInfo
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
        private readonly Stopwatch _gameTimer = new();
        private readonly RoutineBackgroundThread<KayKitCoinBrawlState> _routine;
        private KayKitCoinBrawlState _gameState = KayKitCoinBrawlState.None;

        private Dictionary<int, CoinInfo> coinSpawner = new();
        private readonly Random _random = new();

        public KayKitCoinBrawl(IFiber fiber) : base(fiber)
        {
            // Init Game Table
            _playerPositionTable = TableManager.Instance.GetTable<PlayerPositionTable>("DefaultFolder");

            // Setting routine
            _routine = new RoutineBackgroundThread<KayKitCoinBrawlState>(fiber, KayKitCoinBrawlState.Prepare, 10L);
            _routine.AddState(KayKitCoinBrawlState.Prepare, Convert.ToInt64(PrepareTimer.TotalMilliseconds), PrepareBegin, PrepareUpdate, PrepareFinish);
            _routine.AddState(KayKitCoinBrawlState.Game, Convert.ToInt64(GameTimer.TotalMilliseconds), GameBegin, GameUpdate, GameFinish);
            _routine.AddState(KayKitCoinBrawlState.Finish, Convert.ToInt64(FinishTimer.TotalMilliseconds), FinishStart, null, FinishFinish);
            _routine.AddState(KayKitCoinBrawlState.Ending, Convert.ToInt64(FinishTimer.TotalMilliseconds), () => _routine.Stop(), null, null);

            _routine.AddTrasitionState(KayKitCoinBrawlState.Prepare, KayKitCoinBrawlState.Game);
            _routine.AddTrasitionState(KayKitCoinBrawlState.Game, KayKitCoinBrawlState.Finish);
            _routine.AddTrasitionState(KayKitCoinBrawlState.Finish, KayKitCoinBrawlState.Ending);

            // Game Event
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerTakeCoin, OnPlayerTakeCoin);
            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerGetBullet, OnPlayerGetBullet);
        }

        protected override bool OnInitGame()
        {
            bool succeed = true;
            _gameRoom.ParallerGamePlayerAction(info =>
            {
                int index = _gameRoom.GetPlayerIndexFromAccoundID(info.AccountId);

                if (index < 0)
                {
                    succeed = false;
                    return;
                }

                // Set player position
                var position = _playerPositionTable.GetPlayerPosition(GameType.KayKitBrawl, index);
                info.InitGameData(position);
            });

            return succeed;
        }
        protected override void OnStartGame()
        {
            _routine.Run();
        }
        protected override void OnRemoveGame()
        {
            _routine.Stop();
            Dispose();
        }

        protected override void SetGameStaticData(out GameStaticInfo gameStaticInfo)
        {
            gameStaticInfo = _gameStaticInfo;
        }
        protected override void SetGameDynamicData(out GameDynamicInfo gameDynamicInfo)
        {
            // Game Dynamic Data - GameTimer
            Dictionary<string, double> gameTimer = new Dictionary<string, double>();
            gameTimer.Add("PrepareTimer" ,PrepareTimer.TotalSeconds);
            gameTimer.Add("GameTimer" ,GameTimer.TotalSeconds);
            gameTimer.Add("BonusTimer", FinishTimer.TotalSeconds);

            // Game Dynamic Data - GameCoin
            Dictionary<int, object> coinData = coinSpawner.ToDictionary(x => x.Key, x => (object)x.Value.CreateSerializeObject());

            _gameDynamicInfo.AddDynamicData(KayKitCoinBrawlDynamicInfo.GameTimer, gameTimer);
            _gameDynamicInfo.AddDynamicData(KayKitCoinBrawlDynamicInfo.GameCoinInfo, coinData);

            gameDynamicInfo = _gameDynamicInfo;
        }
        protected override void SetGameResultData(out GameResultInfo gameResultInfo)
        {
            gameResultInfo = null;
            switch (_gameState)
            {
                case KayKitCoinBrawlState.Finish:
                    gameResultInfo = _gameResultInfo;
                    break;
            }
        }

        protected override void OnPlayerPosition(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            if (_gameState == KayKitCoinBrawlState.Finish)
                return;

            float[] playerPosition = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerPosition = playerPosition.ToVector3();
        }
        protected override void OnPlayerLocalEulerAngles(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            if (_gameState == KayKitCoinBrawlState.Finish)
                return;

            float[] playerLocalScale = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerLocalEulerAngles = playerLocalScale.ToVector3();
        }

        // State Action //
        private void PrepareBegin()
        {
            _gameState = KayKitCoinBrawlState.Prepare;
        }
        private bool PrepareUpdate(long timeelapsed)
        {
            PrepareTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (PrepareTimer.TotalMilliseconds < 0)
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
            _gameState = KayKitCoinBrawlState.Game;

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
            _gameState = KayKitCoinBrawlState.Finish;
            _gameResultInfo.winnerInfo = CheckWinnerPlayer();
        }
        private void FinishStart()
        {
        }
        private void FinishFinish()
        {
            SystemManager.Instance.GetSubsystem<GameRoomSystem>().GameOver(_gameRoom);
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

        public List<GamePlayerRoomInfo> CheckWinnerPlayer()
        {
            var winPlayer = _gameRoom.Info.GamePlayerRoomInfos.Where(
                p => p.CointCount == _gameRoom.Info.GamePlayerRoomInfos.Max(m => m.CointCount));

            return winPlayer.ToList();
        }
    }
}