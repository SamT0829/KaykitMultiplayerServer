public enum LobbyPlayerStatus
{
    None,
    Lobby,                 // initial status
    LobbyRoom,
    Game,
}

public enum LobbyRoomState
{
    None,
    Idle,
    Start,
    Game,
}

public enum LobbyRoomMessage
{
    InfoMessage,
    WarningMessage,
    PlayerMessage,
}

public enum GamePlayerState
{
    WaitJoin,
    JoinFinish,
    StartGame,
}

public enum GameRoomState
{
    WaitingEnterRoom,
    EnterRoomFinish,
    GameStart,
}
public enum PlayerSyncNetworkMessage
{
    PlayerAccountId,
    PlayerPosition,
    PlayerLocalEulerAngles,
    PlayerTakeCoin,
    PlayerGetBullet,
}
