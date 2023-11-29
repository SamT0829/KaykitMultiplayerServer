namespace KayKitMultiplayerServer.System.LobbySystem.Info
{
    public enum Team
    {
        None,
        RedTeam,
        BlueTeam,
    }

    public class LobbyPlayerRoomInfo
    {
        public int RoomId { get; set; }
        public long AccountId { get; set; }
        public string NickName { get; set; }
        public GameType GameType { get; set; }
        public string PlayerDataStatus { get; set; }

        // Player in LobbyRoom Setting
        public bool isReady { get; set; }
        public Team Team { get; set; }

        public LobbyPlayerRoomInfo()
        {
            RoomId = -1;
            isReady = false;
        }

        public void InitData(LobbyPlayerInfo lobbyPlayerInfo, int roomId, Team team)
        {
            AccountId = lobbyPlayerInfo.AccountId;
            NickName = lobbyPlayerInfo.NickName;
            GameType = lobbyPlayerInfo.GameType;
            RoomId = roomId;
            Team = team;
            PlayerDataStatus = "";
        }

        public void SetLobbyPlayerReady()
        {
            isReady = !isReady;
        }

        public void ResetInfo()
        {
            RoomId = -1;
            isReady = false;
        }
    }
}