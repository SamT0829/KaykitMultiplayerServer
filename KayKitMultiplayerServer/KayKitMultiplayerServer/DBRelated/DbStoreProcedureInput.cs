namespace KayKitMultiplayerServer.DBRelated
{
    public static class DbStoreProcedureInput
    {
        // Account
        public const string AccountRegister = "AccountRegister";
        public const string AccountRegister_GameType = "inGameType";
        public const string AccountRegister_GameId = "inGameId";
        public const string AccountRegister_GamePassword = "inGamePassword";
        public const string AccountRegister_RegisterTime = "inRegisterTime";

        public const string AccountLogin = "AccountLogin";
        public const string AccountLogin_GameType = "inGameType";
        public const string AccountLogin_GameId = "inGameId";
        public const string AccountLogin_Password = "inGamePassword";
        public const string AccountLogin_LoginTime = "inLoginTime";

        // Lobby
        public const string LobbyLogin = "LobbyLogin";
        public const string LobbyLogin_GameType = "inGameType";
        public const string LobbyLogin_AccountId = "inAccountId";
        public const string LobbyLogin_LoginTime = "inLoginTime";

        public const string LobbyPlayerInfoRegister = "LobbyPlayerInfoRegister";
        public const string LobbyPlayerInfoRegister_GameType = "inGameType";
        public const string LobbyPlayerInfoRegister_AccountId = "inAccountId";
        public const string LobbyPlayerInfoRegister_Nickname = "inNickname";

        public const string UpdateLobbyPlayerInfo = "UpdateLobbyPlayerInfo";
        public const string UpdateLobbyPlayerInfo_AccountId = "inAccountId";
        public const string UpdateLobbyPlayerInfo_GameType = "inGameType";
        public const string UpdateLobbyPlayerInfo_NickName = "inNickName";
        public const string UpdateLobbyPlayerInfo_PlayerStatus = "inPlayerStatus";
        public const string UpdateLobbyPlayerInfo_TotalPlayCount = "inTotalPlayCount";
        public const string UpdateLobbyPlayerInfo_TotalWinCount = "inTotalWinCount";
        public const string UpdateLobbyPlayerInfo_TotalLoseCount = "inTotalLoseCount";

        // Finance 
        public const string RetrieveCashData = "RetrieveCashData";
        public const string RetrieveCashData_GameType = "inGameType";
        public const string RetrieveCashData_AccoutID = "inAccountId";

        // Log System
        public const string LogPlayerErrorLog = "LogError";
        public const string LogPlayerErrorLog_AccountId = "inAccountId";
        public const string LogPlayerErrorLog_GameType = "inGameType";
        public const string LogPlayerErrorLog_ErrorLog = "inErrorCode";
        public const string LogPlayerErrorLog_LogTime = "inLogTime";
    }
}