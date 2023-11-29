using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.LobbySystem.Info
{
    public class LobbyPlayerGameInfo
    {
        public long Money { get; set; }
        public long Diamond { get; set; }

        public void InitLobbyGameInfo(long money, long diamond)
        {
            Money = money;
            Diamond = diamond;
        }
        public string JsonSerializeObject()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}