public enum MessageType
{
    ClientHandlerMessage,
    ServerHandlerMessage,
}

public enum ArrayIndicator : int
{
    MessageType = 19999,
    MessageData = 29999,
}

// 
public enum ClientHandlerMessage
{
    // Account
    AccountConnectedRequest,
    AccountConnectedRespond,

    AccountRegisterRequest,
    AccountRegisterRespond,

    AccountLoginRequest,
    AccountLoginRespond,

    // Lobby
    LobbyConnectedRequest,
    LobbyConnectedRespond,

    LobbyPlayerRegisterRequest,
    LobbyPlayerRegisterRespond,

    // Game
    LobbyPlayerPrepareEnterRequest,
    LobbyPlayerPrepareEnterRespond,

    GameConnectedRequest,
    GameConnectedRespond,

    GameJoinGameRequest,
    GameJoinGameRespond,

    GameRoomStart,
    GameRoomOver,
    KickPlayer,

    // BackgroundThread
    LobbyPlayerBackgroundThread,
    LobbyRoomBackgroundThread,

    // Lobby Player Message
    LobbyPlayerMessage = 100000,
    LobbyPlayerMessageBegin,

    LobbyPlayerCreateLobbyRoomRequest,
    LobbyPlayerCreateLobbyRoomRespond,

    LobbyPlayerJoinLobbyRoomRequest,
    LobbyPlayerJoinLobbyRoomRespond,

    LobbyPlayerReadyLobbyRoomRequest,
    LobbyPlayerReadyLobbyRoomRespond,

    LobbyPlayerChangeTeamLobbyRoomRequest,
    LobbyPlayerChangeTeamLobbyRoomRespond,

    LobbyPlayerLeaveLobbyRoomRequest,
    LobbyPlayerLeaveLobbyRoomRespond,

    LobbyPlayerStartLobbyRoomRequest,
    LobbyPlayerStartLobbyRoomRespond,

    LobbyPlayerChatLobbyRoomRequest,

    LobbyPlayerTestGameRequest,
    LobbyPlayerTestGameRespond,

    LobbyRoomEnterGameRoom,

    LobbyPlayerGoToShopRequest,

    LobbyPlayerGoToMailRequest,




    LobbyPlayerJoinGameRequest,
    LobbyPlayerJoinGameRespond,

    LobbyPlayerMessageEnd = 200000,

    // GamePlayer //
    GamePlayerMessage = 200001,
    GamePlayerMessageBegin,

    GamePlayerSyncRequest,
    GamePlayerSyncRespond,

    GamePlayerNetworkInputRequest,
    GamePlayerNetworkInputRespond,

    GamePlayerMessageEnd = 300000,
}

public enum ServerHandlerMessage
{
    // server //
    ServerConnected,
    ServerDisconnected,
    ClientConnected,
    ClientDisconnected,
    ServerWelcome,

    // Account Login
    Account2ProxyClientLoginRequest,
    Proxy2AccountClientLoginRespond,

    Proxy2LobbyClientLoginRequest,
    Lobby2ProxyClientLoginRespond,

    Proxy2LobbyKickPlayerRequest,
    Lobby2ProxyKickPlayerRespond,

    // Lobby Login
    Lobby2FinanceQueryDataRequest,
    Finance2LobbyQueryDataResond,

    Lobby2ProxyPlayerEnteredRequest,
    Proxy2LobbyPlayerEnteredRespond,

    Proxy2AccountPlayerEnteredRequest,
    Account2ProxyPlayerEnteredRespond,

    // Lobby Player
    Lobby2GameLobbyPlayerJoinGameRequest,
    Game2LobbyLobbyPlayerJoinGameRespond,

    // Lobby Room
    Lobby2GameLobbyRoomEnteredRequest,
    Game2LobbyLobbyRoomEnteredRespond,

    // Game Enter
    Lobby2GameLobbyPlayerPrepareEnterRequest,
    Game2LobbyLobbyPlayerPrepareEnterRespond,

    Game2LobbyGameRoomOverRequest,
    Lobby2GameGameRoomOverRespond,

    // Player Leave
    Account2ProxyPlayerLeave,
    Lobby2ProxyPlayerLeave,
    Lobby2GamePlayerLeave,
}

public enum NetOperationCode : byte
{
    BeforeLogin = 1,
    ClientServer = 2,
    ServerClient = 3,
    ServerServer = 4,
}

public enum NetOperationType : byte
{
    MessageType,
    MessageID,
    SenderID,
    RemoteType,
    Data,
    SelfDefinedType,
}

public enum RemoteConnetionType : int
{
    Unknown,
    Client,
    Account,
    Proxy,
    Lobby,
    Game,
    Finance,
    ThirdParty,
}

public enum GameType
{
    None,
    KaykitGame,
    KayKitBrawl,
    KayKitCoinBrawl,

    DragonBoard,

    // Team Game
    KayKitTeamBrawl = 100,
}