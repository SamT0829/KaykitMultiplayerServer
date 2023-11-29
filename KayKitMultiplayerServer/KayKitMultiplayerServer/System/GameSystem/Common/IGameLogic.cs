using System.Collections.Generic;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Model;
using MySqlX.XDevAPI.Common;

namespace KayKitMultiplayerServer.System.GameSystem.Common
{
    public interface IGameLogic
    {
        bool InitGame(GameRoom gameRoom);
        ErrorCode StartGame(out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo);
        ErrorCode PlayerJoinGame(long accountId, 
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo);
        ErrorCode PlayerLeaveGame(long accountId);
        ErrorCode PlayerSyncGame(long accountId, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo);
        ErrorCode PlayerNetworkInput(long accountId, PlayerNetworkInput playerNetworkInput);

        ErrorCode RemoveGame();
    }
}