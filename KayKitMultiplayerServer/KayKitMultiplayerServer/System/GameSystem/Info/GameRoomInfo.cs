using KayKitMultiplayerServer.System.LobbySystem.Info;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using KayKitMultiplayerServer.Utility;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public class GameRoomInfo
    {
        private enum GameRoomInfoMessageType
        {
            RoomType,
            RoomId,
            RoomName,
            GameRoomState,
            GamePlayerRoomInfos,
            GameWinnerTeam,
            GameWinnerPlayerRoomInfos,
            GameLosePlayerRoomInfos,
        }

        public int RoomGameType { get; private set; }
        public int RoomId { get; private set; }
        public string RoomName { get; private set; }
        public GameRoomState GameRoomState { get; set; }

        public List<GamePlayerRoomInfo> GamePlayerRoomInfos = new List<GamePlayerRoomInfo>();

        // Game Result
        public Team WinnerTeam = Team.None;
        public List<long> WinnerPlayerAccountId = new();
        public List<long> LosePlayerAccountId = new();

        public void InitGameRoomInfo(LobbyRoomInfo lobbyRoomInfo)
        {
            RoomId = lobbyRoomInfo.RoomId;
            RoomName = lobbyRoomInfo.RoomName;
            RoomGameType = lobbyRoomInfo.RoomType;
            //GameRoomState = GameRoomState.WaitingEnter;

            foreach (var lobbyPlayerRoomInfo in lobbyRoomInfo.LobbyPlayerRoomInfos)
            {
                GamePlayerRoomInfo gamePlayerInfo = new GamePlayerRoomInfo();
                gamePlayerInfo.InitData(lobbyPlayerRoomInfo);
                GamePlayerRoomInfos.Add(gamePlayerInfo);
            }
        }

        public Dictionary<int, object> SerializeObject()
        {
            Dictionary<int, object> message = new Dictionary<int, object>();
            message.AddMessageItem(GameRoomInfoMessageType.RoomType, RoomGameType);
            message.AddMessageItem(GameRoomInfoMessageType.RoomId, RoomId);
            message.AddMessageItem(GameRoomInfoMessageType.RoomName, RoomName);
            //message.AddMessageItem(GameRoomInfoMessageType.GameRoomState, GameRoomState);
            //message.AddMessageItem(GameRoomInfoMessageType.GamePlayerRoomInfos, JsonConvert.SerializeObject(GamePlayerRoomInfos));
            message.AddMessageItem(GameRoomInfoMessageType.GameWinnerTeam, WinnerTeam);
            message.AddMessageItem(GameRoomInfoMessageType.GameWinnerPlayerRoomInfos, WinnerPlayerAccountId.ToArray());
            message.AddMessageItem(GameRoomInfoMessageType.GameLosePlayerRoomInfos, LosePlayerAccountId.ToArray());

            return message;
        }

        public void DeserializeObject(Dictionary<int, object> message)
        {
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.RoomType, out int roomType)) { RoomGameType = roomType; }
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.RoomId, out int roomId)) { RoomId = roomId; }
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.RoomName, out string roomName)) { RoomName = roomName; }
            //if (message.RetrieveMessageItem(GameRoomInfoMessageType.GameRoomState, out int maxPlayer)) { GameRoomState = maxPlayer; }
            //if (message.RetrieveMessageItem(GameRoomInfoMessageType.GamePlayerRoomInfos, out string hostPlayer)) { HostPlayer = hostPlayer; }
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.GameWinnerTeam, out Team winnerTeam)) { WinnerTeam = winnerTeam; }
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.GameWinnerPlayerRoomInfos, out long[] winnerPlayerAccountId))
            { WinnerPlayerAccountId = winnerPlayerAccountId.ToList(); }
            if (message.RetrieveMessageItem(GameRoomInfoMessageType.GameLosePlayerRoomInfos, out long[] losePlayerAccountId))
            { LosePlayerAccountId = losePlayerAccountId.ToList(); }
        }

        public List<object> SerilizeGamePlayerRoomInfoObject()
        {
            List<object> retv = new();
            GamePlayerRoomInfos.ForEach(gamePlayerRoomInfo =>
            {
                retv.Add(gamePlayerRoomInfo.SerializedObject());
            });

            return retv;
        }
    }
}