namespace KayKitMultiplayerServer.Network.Client
{
    public class ClientInfo
    {
        public long AccountId { get; private set; }
        public int SessionId { get; private set; }
        public GameType GameLocation { get; private set; }

        public RemoteConnetionType ClientCurrentLocated { get; private set; }
        public int AccountConnectionId { get; private set; }
        public int LobbyConnectionId { get; private set; }
        public int GameConnectionId { get; private set; }

        public ClientInfo(long accountId, int sessionId, GameType gameLocation)
        {
            AccountId = accountId;
            SessionId = sessionId;
            GameLocation = gameLocation;
            ClientCurrentLocated = RemoteConnetionType.Unknown;
        }

        public void SetSessionId(int sessionId)
        {
            SessionId = sessionId;
        }
        public void SetClientLocated(RemoteConnetionType remoteConnetionType)
        {
            ClientCurrentLocated = remoteConnetionType;
        }
        public void SetAccountConnectionId(int accountConnectionId)
        {
            AccountConnectionId = accountConnectionId;
        }
        public void SetLobbyConnectionId(int lobbyConnectionId)
        {
            LobbyConnectionId = lobbyConnectionId;
        }
        public void SetGameConnectionId(int gameConnectionId)
        {
            GameConnectionId = gameConnectionId;
        }
    }
}