using System.Collections.Generic;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public class GameResultInfo
    {
        public Team WinnerTeam = Team.None;
        public List<GamePlayerRoomInfo> winnerInfo = new();

        public List<object> SerializeObject()
        {
            List<object> retv = new List<object>();

            List<object> playerData = new List<object>();
            winnerInfo.ForEach(playerInfo => playerData.Add(playerInfo.SerializedObject()));
            retv.Add(playerData);

            return retv;
        }
    }
}