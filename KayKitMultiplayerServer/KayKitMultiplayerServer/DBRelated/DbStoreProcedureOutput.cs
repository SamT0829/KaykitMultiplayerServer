namespace KayKitMultiplayerServer.DBRelated
{
    public static class DbStoreProcedureOutput
    {
        // Account
        public const string AccountLogin_AccountId = "accountId";
        public const string AccountLogin_Success = "outSuccess";
        public const string AccountLogin_AccountNotExist = "outAccountNotExist";
        public const string AccountLogin_OauthExpired = "outOauthExpired";


        // Lobby
        public const string LobbyLogin_Success = "outSuccess";
        public const string LobbyLogin_PlayerInfoNotExist = "outPlayerInfoNotExist";
        public const string LobbyLogin_AccountId = "accountId";

        public const string LobbyPlayerInfoRegister_Success = "outSuccess";
        public const string LobbyPlayerInfoRegister_NickNameAlreadyUsed = "outNickNameAlreadyUsed";

        // Lobby - LobbyPlayerInfo
        public const string LobbyPlayerInfo_AccountId = "accountId";
        public const string LobbyPlayerInfo_NickName = "nickname";
        public const string LobbyPlayerInfo_LobbyPlayerStatus = "playerStatus";
        public const string LobbyPlayerInfo_GameType = "gameType";
        public const string LobbyPlayerInfo_TotalPlayCount = "totalPlayCount";
        public const string LobbyPlayerInfo_TotalWinCount = "totalWinCount";
        public const string LobbyPlayerInfo_TotalLoseCount = "totalLoseCount";

        // Finance
        public const string RetrieveLobbyPlayerCashData_Money = "accountId";
        public const string RetrieveLobbyPlayerCashData_Diamond = "accountId";
    }
}