using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using System.Collections.Generic;
using System.Linq;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using KayKitMultiplayerServer.Utility;
using System.Threading.Tasks;
using System;
using KayKitMultiplayerServer.System.LobbySystem.Model;

namespace KayKitMultiplayerServer.System.GameSystem.Model
{
    public class GameRoom
    {
        public GameRoomInfo Info = new GameRoomInfo();
        private Dictionary<long, GamePlayer> accountIdGamePlayerTable = new Dictionary<long, GamePlayer>();

        // team player table
        private Dictionary<Team, GamePlayer> teamGamePlayerTable = new();

        // Prepare Game
        public void InitGameRoom(LobbyRoomInfo lobbyRoomInfo)
        {
            Info.InitGameRoomInfo(lobbyRoomInfo);
        }
        public bool CheckGameRoomInfoHasGamePlayerRoomInfo(long accountId, out GamePlayerRoomInfo gamePlayerRoomInfo)
        {
            gamePlayerRoomInfo = Info.GamePlayerRoomInfos.FirstOrDefault(info => info.AccountId == accountId);

            return gamePlayerRoomInfo != null;
        }
        public bool CheckAllPlayerHasJoinRoom()
        {
            foreach (var gamePlayerRoomInfo in Info.GamePlayerRoomInfos)
            {
                if (gamePlayerRoomInfo.GamePlayerState != GamePlayerState.JoinFinish)
                    return false;
            }

            return true;
        }

        public void ParallerGamePlayerAction(Action<GamePlayerRoomInfo> playerInitAction)
        {
            Parallel.ForEach(Info.GamePlayerRoomInfos, playerInitAction.Invoke);
        }
        public void PlayerJoinGameRoom(long accountId, GamePlayer gamePlayer)
        {
            if (accountIdGamePlayerTable.TryGetValue(accountId, out GamePlayer existGamePlayer))
            {
                Dictionary<int, object> outMessageToPrevious = new Dictionary<int, object>();
                outMessageToPrevious.AddMessageItem(KickPlayer.ErrorCode, ErrorCode.PlayerKickByDuplicatedLogin.GetHashCode());
                PhotonApplication.Instance.NetHandle.Send(existGamePlayer.ClientInfo.GameConnectionId, ClientHandlerMessage.KickPlayer, outMessageToPrevious);
                PhotonApplication.Instance.NetHandle.Disconnect(existGamePlayer.ClientInfo.GameConnectionId);
                SystemManager.Instance.GetSubsystem<GamePlayerSystem>().RemovePlayer(existGamePlayer);
                accountIdGamePlayerTable.Remove(accountId);

                if (existGamePlayer.IsInTeamGame())
                {
                    teamGamePlayerTable.Remove(existGamePlayer.GamePlayerRoomInfo.Team);
                }
            }

            accountIdGamePlayerTable.Add(accountId, gamePlayer);
            
            if (gamePlayer.IsInTeamGame())
            {
                teamGamePlayerTable.Add(gamePlayer.GamePlayerRoomInfo.Team, gamePlayer);
            }

            gamePlayer.GamePlayerRoomInfo.GamePlayerState = GamePlayerState.JoinFinish;
        }

        public void PlayerLeaveGameRoom(long accountId)
        {
            if (accountIdGamePlayerTable.TryGetValue(accountId, out GamePlayer gamePlayer))
            { 
                accountIdGamePlayerTable.Remove(accountId);
                if (gamePlayer.GamePlayerRoomInfo.Team != Team.None)
                    teamGamePlayerTable.Remove(gamePlayer.GamePlayerRoomInfo.Team);
            }
        }
        public bool GamePlayerCountInGameRoom()
        {
            return accountIdGamePlayerTable.Count > 0;
        }
        public bool IsHaveGameRoomPlayer()
        {
            return accountIdGamePlayerTable.Count > 0;
        }
        public bool IsGamePlayerOnSameTeam(GamePlayerRoomInfo gamePlayerRoomInfo1, GamePlayerRoomInfo gamePlayerRoomInfo2)
        {
            return gamePlayerRoomInfo1.Team == gamePlayerRoomInfo2.Team;
        }
        public bool IsGamePlayerOnSameTeam(long gamePlayerAccountId1, long gamePlayerAccountId2)
        {
            GamePlayerRoomInfo gamePlayerRoomInfo1 = Info.GamePlayerRoomInfos.FirstOrDefault(info => info.AccountId == gamePlayerAccountId1);
            GamePlayerRoomInfo gamePlayerRoomInfo2 = Info.GamePlayerRoomInfos.FirstOrDefault(info => info.AccountId == gamePlayerAccountId2);
            return gamePlayerRoomInfo1 != null && gamePlayerRoomInfo2 != null && gamePlayerRoomInfo1.Team == gamePlayerRoomInfo2.Team;
        }
        public List<GamePlayerRoomInfo> GetRoomGamePlayerRoomInfos()
        {
            List<GamePlayerRoomInfo> gamePlayerRoomInfos = new List<GamePlayerRoomInfo>();
            if (IsHaveGameRoomPlayer())
            {
                foreach (var gamePlayer in accountIdGamePlayerTable.Values)
                {
                    gamePlayerRoomInfos.Add(gamePlayer.GamePlayerRoomInfo);
                }
            }

            return gamePlayerRoomInfos;
        }
        public bool GetGamePlayerRoomInfo(long accountId, out GamePlayerRoomInfo gamePlayerRoomInfo)
        {
            if (accountIdGamePlayerTable.TryGetValue(accountId, out GamePlayer gamePlayer))
            {
                gamePlayerRoomInfo = gamePlayer.GamePlayerRoomInfo;
                return true;
            }

            gamePlayerRoomInfo = null;
            return false;
        }
        public int GetPlayerIndexFromAccoundID(long accountId)
        {
            int index = -1;
            if (CheckGameRoomInfoHasGamePlayerRoomInfo(accountId, out GamePlayerRoomInfo gamePlayerRoomInfo))
            {
                index = Info.GamePlayerRoomInfos.IndexOf(gamePlayerRoomInfo);
            }

            if (index < 0)
            {
                DebugLog.LogErrorFormat("GetPlayerIndexFromAccoundID cant found index from {0} accoundId", gamePlayerRoomInfo.AccountId);
            }

            return index;
        }
        public void SendMessageToAllPlayer(ClientHandlerMessage msgType, Dictionary<int, object> outMessage)
        {
            Parallel.ForEach(accountIdGamePlayerTable.Values, gamePlayer =>
            {
                if (gamePlayer != null) PhotonApplication.Instance.NetHandle.Send(gamePlayer.ClientInfo.GameConnectionId, msgType, outMessage);
            });
        }

        // Game
        public void StartGame(ErrorCode errorCode, GameStaticInfo gameStaticInfo, GameDynamicInfo gameDynamicInfo, GameResultInfo gameResultInfo)
        {
            Parallel.ForEach(accountIdGamePlayerTable.Values, gamePlayerInfo =>
            {
                gamePlayerInfo.GamePlayerRoomInfo.GamePlayerState = GamePlayerState.StartGame;
                
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(GameRoomStart.ErrorCode, errorCode);
                if (gameStaticInfo != null)
                    outMessage.AddMessageItem(GameRoomStart.GameStaticInfo, gameStaticInfo.SerializeObject());
                if (gameDynamicInfo != null)
                    outMessage.AddMessageItem(GameRoomStart.GameDynamicInfo, gameDynamicInfo.SerializeObject());
                if (gameResultInfo != null)
                    outMessage.AddMessageItem(GameRoomStart.GameResultInfo, gameResultInfo.SerializeObject());
                var gamePlayer = SystemManager.Instance.GetSubsystem<GamePlayerSystem>().GetPlayerByAccountId(gamePlayerInfo.GamePlayerRoomInfo.AccountId);
                if (gamePlayer != null) PhotonApplication.Instance.NetHandle.Send(gamePlayer.ClientInfo.GameConnectionId, ClientHandlerMessage.GameRoomStart, outMessage);
            });
        }
        public List<GamePlayerRoomInfo> CheckWinnerPlayer()
        {
           var winPlayer = Info.GamePlayerRoomInfos.Where(
               p => p.CointCount == Info.GamePlayerRoomInfos.Max(m => m.CointCount));

           return winPlayer.ToList();
        }

        public void SetGameResult(Team winnerTeam, List<long> winnerPlayerAccountId, List<long> losePlayerAccountId)
        {
            Info.WinnerTeam = winnerTeam;
            Info.WinnerPlayerAccountId = winnerPlayerAccountId;
            Info.LosePlayerAccountId = losePlayerAccountId;
        }
    }
}