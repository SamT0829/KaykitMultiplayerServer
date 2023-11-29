using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Info.KayKitBrawl;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using KayKitMultiplayerServer.Utility;
using KayKitMultiplayerServer.Utility.BackgroundThreads;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public class KayKitTeamBrawl : KayKitBase
    {
        private enum KayKitTeamBrawlState
        {
            None,
            Prepare,    
            Game,
            Finish,
            Ending,
            Exit,
        }
        private enum KayKitTeamBrawlStaticInfo
        {
            GameRound = 100,
            GameScore,
        }
        private enum KayKitTeamBrawlDynamicInfo
        {
            GameState = 100,
            GameStateData,

            GameTimer,
        }
        private enum KayKitTeamBrawlGameData
        {
            GameMessage,
        }
        private enum KayKitTeamBrawlFinishData
        {
            TeamWinner,
        }

        // Timer
        private TimeSpan _prepareTimer = TimeSpan.FromSeconds(3);
        private TimeSpan _gameTimer = TimeSpan.FromSeconds(30);
        private TimeSpan _finishTimer = TimeSpan.FromSeconds(2);
        private TimeSpan _endingTimer = TimeSpan.FromSeconds(1);

        // Round
        private int _nowGameRound;
        private int _totalGameRound;
        private int _redTeamScore;
        private int _blueTeamScore;

        // Game Message
        private Queue<string> gameMessage = new();

        private Team winnerTeam;

        private readonly Stopwatch _timer = new();
        private readonly RoutineBackgroundThread<KayKitTeamBrawlState> _routine;
        private KayKitTeamBrawlState _gameState;

        public KayKitTeamBrawl(IFiber fiber) : base(fiber)
        {
            // Setting routine
            _routine = new RoutineBackgroundThread<KayKitTeamBrawlState>(fiber, KayKitTeamBrawlState.Prepare, 10L);
            _routine.AddState(KayKitTeamBrawlState.Prepare, Convert.ToInt64(_prepareTimer.TotalMilliseconds), PrepareBegin, PrepareUpdate, PrepareEnd);
            _routine.AddState(KayKitTeamBrawlState.Game, Convert.ToInt64(_gameTimer.TotalMilliseconds), GameBegin, GameUpdate, GameEnd);
            _routine.AddState(KayKitTeamBrawlState.Finish, Convert.ToInt64(_finishTimer.TotalMilliseconds), FinishBegin, null, FinishEnd);
            _routine.AddState(KayKitTeamBrawlState.Ending, Convert.ToInt64(_endingTimer.TotalMilliseconds), EndingBegin, null, EndingEnd);
            _routine.AddState(KayKitTeamBrawlState.Exit, 0L, () => _routine.Stop(), null, null);

            // Routine State
            _routine.AddTrasitionState(KayKitTeamBrawlState.Prepare, KayKitTeamBrawlState.Game);
            _routine.AddTrasitionState(KayKitTeamBrawlState.Game, KayKitTeamBrawlState.Finish);
            _routine.AddTrasitionState(KayKitTeamBrawlState.Finish, KayKitTeamBrawlState.Prepare);
            _routine.AddTrasitionState(KayKitTeamBrawlState.Ending, KayKitTeamBrawlState.Exit);

            RegisterGamePlayerAction(PlayerSyncNetworkMessage.PlayerGetBullet, OnPlayerGetBullet);
        }

        protected override bool OnInitGame()
        {
            bool succeed = true;
            _nowGameRound = 1;
            _totalGameRound = 1;
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
            Dispose();
        }
        // Game Static Data
        protected override void SetGameStaticData(out GameStaticInfo gameStaticInfo)
        {
            // Game Static Data - GameScore
            List<object> gameScoreData = new();
            gameScoreData.Add(_redTeamScore);
            gameScoreData.Add(_blueTeamScore);

            _gameStaticInfo.AddStaticData(KayKitTeamBrawlStaticInfo.GameRound, _nowGameRound);
            _gameStaticInfo.AddStaticData(KayKitTeamBrawlStaticInfo.GameScore, gameScoreData);

            gameStaticInfo = _gameStaticInfo;
        }
        // Game Dynamic Data
        protected override void SetGameDynamicData(out GameDynamicInfo gameDynamicInfo)
        {
            base.SetGameDynamicData(out _gameDynamicInfo);
            // Game Dynamic Data - GameTimer
            List<object> gameTimerData = new();
            gameTimerData.Add(_prepareTimer.TotalSeconds);
            gameTimerData.Add(_gameTimer.TotalSeconds);
            gameTimerData.Add(_finishTimer.TotalSeconds);

            _gameDynamicInfo.AddDynamicData(KayKitTeamBrawlDynamicInfo.GameTimer, gameTimerData);

            string message;
            Dictionary<int, object> gameStateData = new();
            switch (_gameState)
            {
                case KayKitTeamBrawlState.Game:
                    if (gameMessage.Count <= 0)
                        break;
                    message = gameMessage.Dequeue();
                    gameStateData.Add(KayKitTeamBrawlGameData.GameMessage.GetHashCode(), message);
                    break;
                case KayKitTeamBrawlState.Finish:
                    gameStateData.Add(KayKitTeamBrawlFinishData.TeamWinner.GetHashCode(), winnerTeam.ToString());
                    break;
                case KayKitTeamBrawlState.Ending:
                    break;
            }

            _gameDynamicInfo.AddDynamicData(KayKitTeamBrawlDynamicInfo.GameState, _gameState.GetHashCode());
            _gameDynamicInfo.AddDynamicData(KayKitTeamBrawlDynamicInfo.GameStateData, gameStateData);

            gameDynamicInfo = _gameDynamicInfo;
        }
        // Game Result Data
        protected override void SetGameResultData(out GameResultInfo gameResultInfo)
        {
            gameResultInfo = null;
            switch (_gameState)
            {
                case KayKitTeamBrawlState.Ending:
                    gameResultInfo = _gameResultInfo;
                    break;
            }
        }
        protected override void OnPlayerPosition(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            if(_gameState == KayKitTeamBrawlState.Finish)
                return;

            float[] playerPosition = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerPosition = playerPosition.ToVector3();
        }
        protected override void OnPlayerLocalEulerAngles(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is float[]))
                return;

            if (_gameState == KayKitTeamBrawlState.Finish)
                return;

            float[] playerLocalScale = (float[])playerMesageValue;
            gamePlayerRoomInfo.PlayerLocalEulerAngles = playerLocalScale.ToVector3();
        }

        // State Action //
        private void PrepareBegin()
        {
            _gameState = KayKitTeamBrawlState.Prepare;
            winnerTeam = Team.None;
        }
        private bool PrepareUpdate(long timeelapsed)
        {
            _prepareTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (_prepareTimer.TotalMilliseconds < 0)
                _prepareTimer = TimeSpan.Zero;

            return true;
        }
        private void PrepareEnd()
        {
            DebugLog.Log("PrepareFinish");

            if (_nowGameRound == _totalGameRound)
            {
                _routine.AddTrasitionState(KayKitTeamBrawlState.Game, KayKitTeamBrawlState.Ending);
            }
        }
        private void GameBegin()
        {
            DebugLog.Log("GameBegin");
            _gameState = KayKitTeamBrawlState.Game;
            ResetAllGamePlayerInfo();

            _timer.Start();

            _gameTimer = TimeSpan.FromSeconds(30);
        }
        private bool GameUpdate(long timeelapsed)
        {
            _gameTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (_gameTimer.TotalMilliseconds < 0)
                _gameTimer = TimeSpan.Zero;

            //if (_gameRoom.GetRoomGamePlayerRoomInfos().Count > 0)
            //{
            //    foreach (var gamePlayerInfo in _gameRoom.GetRoomGamePlayerRoomInfos())
            //    {
            //        UpdatePlayerInfo(gamePlayerInfo, timeelapsed);
            //    }
            //}

            GameLogic();
            return true;
        }
        private void GameEnd()
        {
            DebugLog.Log("GameFinish");
            _timer.Stop();
        }
        private void FinishBegin()
        {
            _gameState = KayKitTeamBrawlState.Finish;
            NextGameRound();
        }
        private void FinishEnd()
        {
            DebugLog.LogErrorFormat("FinishEnd");
        }
        private void EndingBegin()
        {
            GameWinnerResult();
            _gameState = KayKitTeamBrawlState.Ending;
            DebugLog.LogErrorFormat("Ending");
        }
        private void EndingEnd()
        {
            DebugLog.LogErrorFormat("EndingEnd");

            SystemManager.Instance.GetSubsystem<GameRoomSystem>().GameOver(_gameRoom);
        }
        private void OnPlayerGetBullet(GamePlayerRoomInfo gamePlayerRoomInfo, object playerMesageValue)
        {
            if (!(playerMesageValue is object[]))
            {
                DebugLog.LogErrorFormat(this.GetType().Name + " OnPlayerGetBullet playerMesageValue true type is " +
                                        playerMesageValue.GetType());
                return;
            }

            BulletInfo bulletInfo = new BulletInfo();
            bulletInfo.DeserializeObject((object[])playerMesageValue);

            if (!_gameRoom.IsGamePlayerOnSameTeam(gamePlayerRoomInfo.AccountId, bulletInfo.PlayerAccountId))
            {
                // Player Death
                if (gamePlayerRoomInfo.PlayerHealth > 0 && !gamePlayerRoomInfo.IsPlayerDie)
                {
                    gamePlayerRoomInfo.PlayerHealth -= bulletInfo.BulletDamage;

                    if (gamePlayerRoomInfo.PlayerHealth <= 0)
                    {
                        gamePlayerRoomInfo.IsPlayerDie = true;
                        gamePlayerRoomInfo.DeathCount++;

                        if (_gameRoom.GetGamePlayerRoomInfo(bulletInfo.PlayerAccountId, out GamePlayerRoomInfo bulletPlayerInfo))
                        {
                            string message = bulletPlayerInfo.NickName + " Kill " + gamePlayerRoomInfo.NickName;
                            gameMessage.Enqueue(message);
                            DebugLog.Log(message);
                        }
                    }
                }
            }
        }
        private void NextGameRound()
        {
            _nowGameRound++;
        }
        private void ResetAllGamePlayerInfo()
        {
            _gameRoom.ParallerGamePlayerAction(info =>
            {
                int index = _gameRoom.GetPlayerIndexFromAccoundID(info.AccountId);

                info.IsPlayerDie = false;
                // Set player position
                var position = _playerPositionTable.GetPlayerPosition((GameType)_gameRoom.Info.RoomGameType, index);
                info.ResetGameData(position);
            });
        }
        private void GameLogic()
        { 
            var redTeamPlayers =  _gameRoom.GetRoomGamePlayerRoomInfos().Where(info => info.Team == Team.RedTeam).ToList();
            var blueTeamPlayers = _gameRoom.GetRoomGamePlayerRoomInfos().Where(info => info.Team == Team.BlueTeam).ToList();

            bool redTeamDefeat = redTeamPlayers.All(teamPlayer => teamPlayer.IsPlayerDie);
            bool blueTeamDefeat = blueTeamPlayers.All(teamPlayer => teamPlayer.IsPlayerDie);

            if (redTeamDefeat && redTeamPlayers.Count > 0)
            {
                _blueTeamScore++;
                winnerTeam = Team.BlueTeam;
                _routine.NextState();
            }

            else if (blueTeamDefeat && blueTeamPlayers.Count > 0)
            {
                _redTeamScore++;
                winnerTeam = Team.RedTeam;
                _routine.NextState();
            }

            else if (_gameTimer == TimeSpan.Zero)
            { 
                var redTeamDieCount = redTeamPlayers.Where(redPlayer => redPlayer.IsPlayerDie).Count();
                var blueTeamDieCount = blueTeamPlayers.Where(bluePlayer => bluePlayer.IsPlayerDie).Count();

                if (redTeamDieCount > blueTeamDieCount)
                    _blueTeamScore++;
                else if (redTeamDieCount < blueTeamDieCount)
                    _redTeamScore++;
            }
        }
        public void GameWinnerResult()
        {
            Team winnerTeam = Team.None;
            if (_redTeamScore > _blueTeamScore) winnerTeam = Team.RedTeam;
            else if (_redTeamScore < _blueTeamScore) winnerTeam = Team.BlueTeam;

            var winPlayer = _gameRoom.Info.GamePlayerRoomInfos.Where(
                p => p.Team == winnerTeam).ToList();

            var losePlayer = _gameRoom.Info.GamePlayerRoomInfos.Where(
                p => p.Team != winnerTeam).ToList();

            _gameResultInfo.WinnerTeam = winnerTeam;
            _gameResultInfo.winnerInfo = winPlayer;

            var winPlayerAccountId = winPlayer.Select(p => p.AccountId).ToList();
            var losePlayerAccountId = losePlayer.Select(p => p.AccountId).ToList();

            _gameRoom.SetGameResult(winnerTeam, winPlayerAccountId, losePlayerAccountId);
        }
    }
}