public enum AccountConnectRespond
{

}

public enum AccountRegisterRequest
{
    GameType,
    GameId,
    Password,
}
public enum AccountRegisterRespond
{
    ErrorCode,
    Succes,
}

// Account Login
public enum AccountLoginRequest
{
    GameType,
    GameId,
    Password,
}
public enum AccountLoginRespond
{
    ErrorCode,
    SessionId,
    LobbyServerIP,
    LobbyServerPort,
}

// Lobby Login
public enum LobbyLoginRequest
{
    SessionId,
}
public enum LobbyConnectedRespond : int
{
    ErrorCode,  // int (ErrorCode)
    IsPlayerInfoDataNotExist,
    LobbyPlayerInfo,  
    LobbyGameInfo, 
    ServerTime,
}
public enum LobbyPlayerRegisterRequest
{
    SessionId,
    GameType,
    Nickname,
}
public enum LobbyPlayerRegisterRespond
{
    ErrorCode,
    NickNameAlreadyUsed,

}
public enum LobbyPlayerPrepareEnterRequest
{
    AccountId,
}
public enum LobbyPlayerPrepareEnterRespond
{
    ErrorCode,
    GameServerIP,
    GameServerPort
}
public enum GameConnectedRequest
{
    SessionId
}
public enum GameConnectedRespond
{
    ErrorCode,
    GameStaticInfo,
    GameDynamicInfo,
    GameResultInfo,
}
public enum LobbyPlayerRoomEntered
{
    ErrorCode,
    GameScene,
}
public enum GameRoomStart
{
    ErrorCode,
    GameStaticInfo,
    GameDynamicInfo,
    GameResultInfo,
}
public enum KickPlayer
{
    ErrorCode,
}

// Background Thread
public enum LobbyPlayerBackgroundThread
{
    LobbyRoomListData,
}
public enum LobbyRoomBackgroundThread
{
    LobbyRoomData,
    LobbyRoomMessage,
}
// Lobby Player Message
public enum PlayerMessage
{
    HandlerMessageType,
    HandlerMessageData,
}
public enum LobbyPlayerCreateLobbyRoomRequest
{
    RoomType,
    RoomName,
    RoomPassword,
    MaxPlayer,
}
public enum LobbyPlayerCreateLobbyRoomRespond
{
    ErrorCode,
    RoomData,
}

public enum LobbyPlayerJoinLobbyRoomRequest
{
    RoomId,
    RoomType,
    RoomName,
    RoomPassword,
    PlayerData,
}
public enum LobbyPlayerJoinLobbyRoomRespond
{
    ErrorCode,
    RoomData,
}
public enum LobbyPlayerChatLobbyRoomRequest
{
    ChatMsg,
}
public enum LobbyPlayerLeaveLobbyRoomRequest
{
}
public enum LobbyPlayerLeaveLobbyRoomRespond
{
    Succes,
}

public enum LobbyPlayerJoinGameRequest
{
    GameType,
}
public enum LobbyPlayerJoinGameRespond
{
    ErrorCode,
    GameServerIP,
    GameServerPort,
}
// Game
public enum GamePlayerSyncRequest
{
    PlayerSyncMessage,
    GameMessage,
}
public enum GamePlayerSyncRespond
{
    ErrorCode,
    GameStaticInfo,
    GameDynamicInfo,
    GameResultInfo,
}
public enum LobbyPlayerTestGameRequest
{
    GameType,
}
//GamePlayer
public enum GamePlayerNetworkInputRequest
{
    xVelocity,
    yVelocity,
    xRotation,
    yRotation,
    NetworkButon,

    // Weapon
    GunRotationZ,
    GunAimDirection,
}
public enum GamePlayerNetworkInputRespond
{
    AccountId,
    PlayerNetworkInput,
}

// GameRoom
public enum GameRoomOver
{
    LobbyRoomInfo,
}