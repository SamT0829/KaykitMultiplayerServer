public enum ServerConnected
{
    ServerId,
    ServerName,
    RemoteServerType,
}
public enum ServerWelcomeRequest : int
{
    ServerId,
    ServerName,
    ConnectedServerType,
}
public enum ServerDisconnected : int
{
    ServerId,
    ServerName,
    RemoteServerType,
}
public enum ClientDisconnected
{
    ConnectionId
}


// Account Login
public enum Account2ProxyClientLoginRequest : int
{
    GameType, // int (mw)
    ClientConnectionId,  // int
    AccountId,  // long
}
public enum Proxy2AccountClientLoginRespond : int
{
    ErrorCode,  // ErrorCode
    AccountId,  // long
    LobbyId,    // int
    SessionId,  // int
    ClientConnectionId, // int
}
public enum Proxy2LobbyClientLoginRequest : int
{
    LocatedAccountServerId,  // int
    AccountId,  // long
    SessionId,  // int
    ClientConnectionId,  // int
    GameType, // int (mw)
}
public enum Lobby2ProxyClientLoginRespond : int
{
    LocatedAccountServerID,
    AccountID,
    ClientConnectionID,
}

// Lobby Login
public enum Lobby2FinanceQueryDataRequest
{
    AccountId,
    SessionId,
    GameType,
}
public enum Finance2LobbyQueryDataResond
{
    ErrorCode,
    GameType,
    AccountId,
    SessionId,
    Money,
    Diamond,
}
public enum Lobby2ProxyPlayerEnteredRequest
{
    AccountId,
    SessionId,
    LobbyConnectionId
}
public enum Proxy2LobbyPlayerEnteredRespond
{
    ErrorCode,
    AccountId,
    SessionId,
}
public enum Proxy2AccountPlayerEnteredRequest
{
    AccountId,
    SessionId,
    ConnectionId,
}
public enum Account2ProxyPlayerEnteredRespond
{
    AccountId,
    SessionId
}

// Lobby Player
public enum Lobby2GameLobbyPlayerJoinGameRequest
{
    GameType,
    AccountId,
    SessionId,
    GameLocation,
    LobbyConnectionId,
}

public enum Game2LobbyLobbyPlayerJoinGameRespond
{
    AccountId,
    SessionId
}
// Lobby Room
public enum Lobby2GameLobbyRoomEnteredRequest
{
    RoomData,
}
public enum Game2LobbyLobbyRoomEnteredRespond
{
    ErrorCode,
    RoomId,
}
public enum Lobby2GameLobbyPlayerPrepareEnterRequest
{
    AccountId,
    SessionId,
    GameLocation,
    LobbyConnectionId,
    RoomId,
}
public enum Game2LobbyLobbyPlayerPrepareEnterRespond
{
    AccountId,
    SessionId,
}


// Game Room
public enum Game2LobbyGameRoomOverRequest
{
    RoomId,
    GameRoomInfo,
}

public enum Lobby2GameGameRoomOverRespond
{
    AccountId,
    SessionId,
    LobbyConnectionId
}

// Kick
public enum Proxy2LobbyKickPlayerRequest : int
{
    AccountID,
    SessionID,
    ReplacedSessionID,
    AccountServerID,
    ClientConnectionID,
}
public enum Lobby2ProxyKickPlayerRespond : int
{
    AccountId,
    SessionId,
    ReplacedSessionId,
    AccountServerId,
    ClientConnectionId,
    KickSuccess, // bool
}
public enum Lobby2ProxyPlayerLeave : int
{
    AccountID,
    SessionID,
    LeaveDone, // bool
}
public enum Account2ProxyPlayerLeave : int
{
    AccountID,  // long
    SessionID,  // int
}
public enum Lobby2GamePlayerLeave
{
    AccountId,
    SessionId,
}
