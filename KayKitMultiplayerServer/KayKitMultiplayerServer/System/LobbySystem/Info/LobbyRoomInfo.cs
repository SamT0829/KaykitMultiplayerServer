using KayKitMultiplayerServer.System.LobbySystem.Model;
using System.Collections.Generic;
using System.Linq;
using KayKitMultiplayerServer.Utility;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.LobbySystem.Info
{
    public class LobbyRoomInfo
    {
        private enum LobbyRoomInfoMessageType
        {
            RoomType,
            RoomId,
            RoomName,
            RoomPassword,
            MaxPlayer,
            HostPlayer,
            LobbyRoomStatus,
            LobbyPlayerRoomInfos,
        }

        public int RoomType { get; private set; }
        public int RoomId { get; private set; }
        public string RoomName { get; private set; }
        public string RoomPassword { get; private set; }
        public int MaxPlayer { get; private set; }
        public string HostPlayer { get; private set; }
        public LobbyRoomState LobbyRoomState = LobbyRoomState.Idle;
        private List<LobbyPlayerRoomInfo> lobbyPlayerRoomInfosList = new List<LobbyPlayerRoomInfo>();
        public List<LobbyPlayerRoomInfo> LobbyPlayerRoomInfos { get { return lobbyPlayerRoomInfosList; } }

        public void InitData(int roomType, int roomId, string roomName, string roomPassword, int maxPlayer, LobbyPlayerRoomInfo lobbyPlayerRoomInfo)
        {
            RoomType = roomType;
            RoomId = roomId;
            RoomName = roomName;
            RoomPassword = roomPassword;
            MaxPlayer = maxPlayer;
            HostPlayer = lobbyPlayerRoomInfo.NickName;
            AddLobbyPlayerRoomInfo(lobbyPlayerRoomInfo);
        }

        public void AddLobbyPlayerRoomInfo(LobbyPlayerRoomInfo lobbyPlayerRoomInfo)
        {
            lobbyPlayerRoomInfosList.Add(lobbyPlayerRoomInfo);
        }
        public void RemoveLobbyPlayerRoomInfo(LobbyPlayerRoomInfo lobbyPlayerRoomInfo)
        {
            lobbyPlayerRoomInfosList.Remove(lobbyPlayerRoomInfo);

            // Change host player
            if (HostPlayer == lobbyPlayerRoomInfo.NickName)
                if (lobbyPlayerRoomInfosList.Count > 0)
                    ChangeHostPlayer(lobbyPlayerRoomInfosList.First());
        }
        public void ChangeLobbyRoomName(string name)
        {
            RoomName = name;
        }

        public void ChangeLobbyRoomPassword(string password)
        {
            RoomPassword = password;
        }

        public void ChangeMaxPlayer(int maxPlayer)
        {
            MaxPlayer = maxPlayer;
        }

        public void ChangeHostPlayer(LobbyPlayerRoomInfo lobbyPlayerRoomInfo)
        {
            HostPlayer = lobbyPlayerRoomInfo.NickName;
        }

        public Team GetTeam()
        {
            var redTeamCount = lobbyPlayerRoomInfosList.Where(p => p.Team == Team.RedTeam).Count();
            var blueTeamCount = lobbyPlayerRoomInfosList.Where(p => p.Team == Team.BlueTeam).Count();

            if (redTeamCount >= blueTeamCount)
                return Team.BlueTeam;
            else
                return Team.RedTeam;
        }
        
        public Dictionary<int, object> SerializeObject()
        {
            Dictionary<int, object> message = new Dictionary<int, object>();
            message.AddMessageItem(LobbyRoomInfoMessageType.RoomType, RoomType);
            message.AddMessageItem(LobbyRoomInfoMessageType.RoomId, RoomId);
            message.AddMessageItem(LobbyRoomInfoMessageType.RoomName, RoomName);
            message.AddMessageItem(LobbyRoomInfoMessageType.RoomPassword, RoomPassword);
            message.AddMessageItem(LobbyRoomInfoMessageType.MaxPlayer, MaxPlayer);
            message.AddMessageItem(LobbyRoomInfoMessageType.HostPlayer, HostPlayer);
            message.AddMessageItem(LobbyRoomInfoMessageType.LobbyRoomStatus, LobbyRoomState);
            message.AddMessageItem(LobbyRoomInfoMessageType.LobbyPlayerRoomInfos, JsonConvert.SerializeObject(lobbyPlayerRoomInfosList));

            return message;
        }

        public void DeserializeObject(Dictionary<int, object> roomData)
        {
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.RoomType, out int roomType)) { RoomType = roomType; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.RoomId, out int roomId)) { RoomId = roomId; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.RoomName, out string roomName)) { RoomName = roomName; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.RoomPassword, out string roomPassword)) { RoomPassword = roomPassword; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.MaxPlayer, out int maxPlayer)) { MaxPlayer = maxPlayer; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.HostPlayer, out string hostPlayer)) { HostPlayer = hostPlayer; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.LobbyRoomStatus, out LobbyRoomState lobbyRoomStatus)) { LobbyRoomState = lobbyRoomStatus; }
            if (roomData.RetrieveMessageItem(LobbyRoomInfoMessageType.LobbyPlayerRoomInfos, out string lobbyPlayerRoomInfos))
            { lobbyPlayerRoomInfosList = JsonConvert.DeserializeObject<List<LobbyPlayerRoomInfo>>(lobbyPlayerRoomInfos); }
        }
    }
}