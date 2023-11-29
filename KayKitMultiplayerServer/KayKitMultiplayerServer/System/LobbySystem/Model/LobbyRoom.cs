using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KayKitMultiplayerServer.Utility;

namespace KayKitMultiplayerServer.System.LobbySystem.Model
{
    public class LobbyRoom
    {
        public LobbyRoomInfo LobbyRoomInfo = new LobbyRoomInfo();

        private Dictionary<long, LobbyPlayer> accountIdLobbyPlayersTable = new Dictionary<long, LobbyPlayer>();
        private Queue<List<object>> roomChatMsg = new Queue<List<object>>();

        private bool startGame;

        public bool LobbyRoomHasPlayer()
        {
            return accountIdLobbyPlayersTable.Count > 0;
        }
        public void CreateLobbyRoom(int roomType, int roomId, string roomName, string roomPassword, int maxPlayer, LobbyPlayer hostLobbyLobbyPlayer)
        {
            Team team = Team.None;
            if (roomType >= GameType.KayKitTeamBrawl.GetHashCode())
            {
                team = Team.RedTeam;
            }

            hostLobbyLobbyPlayer.JoinLobbyRoom(roomId, team);
            LobbyRoomInfo.InitData(roomType, roomId, roomName, roomPassword, maxPlayer, hostLobbyLobbyPlayer.LobbyPlayerRoomInfo);
            accountIdLobbyPlayersTable.Add(hostLobbyLobbyPlayer.LobbyPlayerInfo.AccountId, hostLobbyLobbyPlayer);
            hostLobbyLobbyPlayer.RegesterHostLobbyRoomMessageObserver();
        }
        public bool HasRoomChatMessage()
        {
            return roomChatMsg.Count > 0;
        }
        public void AddRoomMessage(LobbyRoomMessage roomMessage, string name, string chatMessage)
        {
            List<object> chatData = new List<object>();
            chatData.Add(roomMessage);
            chatData.Add(name);
            chatData.Add(chatMessage);
            roomChatMsg.Enqueue(chatData);
        }
        public bool CheckAllPlayerReady()
        {
            foreach (var lobbyPlayer in accountIdLobbyPlayersTable.Values)
            {
                if (!lobbyPlayer.LobbyPlayerRoomInfo.isReady)
                    return false;
            }

            return true;
        }
        public List<object> GetRoomChatMessage()
        {
            List<object> chatMessage = null;

            if (HasRoomChatMessage())
                chatMessage = roomChatMsg.Dequeue();

            return chatMessage;
        }
        public void LobbyRoomPlayerAction(Action<LobbyPlayer> lobbyPlayerAction)
        {
            Parallel.ForEach(accountIdLobbyPlayersTable.Values, lobbyPlayerAction);
        }
        public ErrorCode JoinLobbyRoom(LobbyPlayer lobbyPlayer, string roomPassword)
        {
            ErrorCode err = ErrorCode.Success;
            if (LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Start)
                return ErrorCode.LobbyRoomHasInGame;

            if (LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Game)
                return ErrorCode.LobbyRoomHasInGame;

            if (accountIdLobbyPlayersTable.Count > LobbyRoomInfo.MaxPlayer)
                return ErrorCode.LobbyRoomPlayerHasFull;

            if (LobbyRoomInfo.RoomPassword != string.Empty && LobbyRoomInfo.RoomPassword != roomPassword)
                return ErrorCode.LobbyRoomPasswordIsWrong;

            Team team = Team.None;
            if (LobbyRoomInfo.RoomType >= GameType.KayKitTeamBrawl.GetHashCode())
                team = LobbyRoomInfo.GetTeam();

            lobbyPlayer.JoinLobbyRoom(LobbyRoomInfo.RoomId, team);
            LobbyRoomInfo.AddLobbyPlayerRoomInfo(lobbyPlayer.LobbyPlayerRoomInfo);
            accountIdLobbyPlayersTable.Add(lobbyPlayer.LobbyPlayerInfo.AccountId, lobbyPlayer);

            // Send Message
            AddRoomMessage(LobbyRoomMessage.InfoMessage, lobbyPlayer.LobbyPlayerInfo.NickName, "has join lobbyRoom game");
            return err;
        }

        public void ReconnectedPlayerLobbyRoom(LobbyPlayer lobbyPlayer)
        {
            //lobbyPlayer.JoinLobbyRoom(LobbyRoomInfo.RoomId, lobbyPlayer.LobbyPlayerRoomInfo.Team);
            accountIdLobbyPlayersTable.Add(lobbyPlayer.LobbyPlayerInfo.AccountId, lobbyPlayer);
        }

        public void LeaveLobbyRoomFromAccountId(long accountId)
        {
            if (accountIdLobbyPlayersTable.TryGetValue(accountId, out LobbyPlayer lobbyPlayer))
            {
                if (LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Idle || LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Start)
                {
                    LobbyRoomInfo.RemoveLobbyPlayerRoomInfo(lobbyPlayer.LobbyPlayerRoomInfo);
                    accountIdLobbyPlayersTable.Remove(accountId);

                    if (LobbyRoomHasPlayer())
                        AddRoomMessage(LobbyRoomMessage.InfoMessage, lobbyPlayer.LobbyPlayerInfo.NickName, "has leave lobbyRoom game");

                    if (LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Start)
                        startGame = false;
                }
            }
        }
        public ErrorCode StartLobbyRoom(LobbyPlayer lobbyPlayer, int gameServerId)
        {
            ErrorCode errorCode = ErrorCode.Success;

            if (LobbyRoomInfo.HostPlayer != lobbyPlayer.LobbyPlayerRoomInfo.NickName)
            {
                AddRoomMessage(LobbyRoomMessage.WarningMessage, "Game", "Start Game Failed, Host player is " + LobbyRoomInfo.HostPlayer);
                return errorCode;
            }

            if (LobbyRoomInfo.LobbyRoomState != LobbyRoomState.Idle)
            {
                AddRoomMessage(LobbyRoomMessage.WarningMessage, "Game", "Start Game Failed, Game has already to Start");
                return errorCode;
            }

            if (!CheckAllPlayerReady())
            {
                AddRoomMessage(LobbyRoomMessage.WarningMessage, "Game", "Start Game Failed, Player has not ready");
                return errorCode;
            }

            LobbyRoomInfo.LobbyRoomState = LobbyRoomState.Start;
            startGame = true;
            IFiber task = new PoolFiber();
            task.Start();
            task.Schedule(() =>
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                int sec = 0;

                while (timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    if (timer.Elapsed > TimeSpan.FromSeconds(sec)) // run every 1000ms - 1s
                    {
                        AddRoomMessage(LobbyRoomMessage.InfoMessage, "Game", "has about to start for " + (TimeSpan.FromSeconds(3).Seconds - timer.Elapsed.Seconds) + " sec");
                        sec++;
                    }

                    if (!startGame)
                    {
                        AddRoomMessage(LobbyRoomMessage.WarningMessage, "Game", "Start Game Failed");
                        LobbyRoomInfo.LobbyRoomState = LobbyRoomState.Idle;
                        task.Dispose();
                        return;
                    }
                }

                //Send Message to Game Server
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                outMessage.AddMessageItem(Lobby2GameLobbyRoomEnteredRequest.RoomData, LobbyRoomInfo.SerializeObject());
                PhotonApplication.Instance.NetHandle.Send(gameServerId, ServerHandlerMessage.Lobby2GameLobbyRoomEnteredRequest, outMessage);

                task.Dispose();
            }, 0);

            return errorCode;
        }
        public ErrorCode ChangeTeam(LobbyPlayer lobbyPlayer)
        { 
            ErrorCode errorCode = ErrorCode.Success;
            if(lobbyPlayer.LobbyPlayerRoomInfo.Team == Team.BlueTeam)
            {
               lobbyPlayer.LobbyPlayerRoomInfo.Team = Team.RedTeam;
            }
            else if (lobbyPlayer.LobbyPlayerRoomInfo.Team == Team.RedTeam)
            {
              lobbyPlayer.LobbyPlayerRoomInfo.Team = Team.BlueTeam;
            }

            return errorCode;
        }
        public bool StartGame()
        {
            bool succes = false;

            if (LobbyRoomInfo.LobbyRoomState == LobbyRoomState.Start)
            {
                LobbyRoomInfo.LobbyRoomState = LobbyRoomState.Game;

                foreach (var lobbyPlayer in accountIdLobbyPlayersTable.Values)
                {
                    //lobbyPlayer.LobbyPlayerInfo.UpdateLobbyPlayerInfo_ToDb(null, null);
                    lobbyPlayer.LobbyPlayerRoomInfo.isReady = false;
                    lobbyPlayer.LobbyPlayerInfo.Status = LobbyPlayerStatus.Game;
                }
                succes = true;
            }

            return succes;
        }
    }
}