using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.System.GameSystem.Subsystems;
using System.Collections.Generic;
using KayKitMultiplayerServer.Utility;
using System.Linq;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using Newtonsoft.Json;

namespace KayKitMultiplayerServer.System.GameSystem.Model
{
    public class GamePlayer
    {
        public ClientInfo ClientInfo;
        public GamePlayerRoomInfo GamePlayerRoomInfo;

        public bool IsOnlineGamePlayer = false;
        public void InitGameRoomPlayer(ClientInfo clientInfo, GamePlayerRoomInfo gamePlayerRoomInfo)
        {
            ClientInfo = clientInfo;
            GamePlayerRoomInfo = gamePlayerRoomInfo;
        }
        public void RegesterMessageObserver()
        {
            GamePlayerSystem gamePlayerSystem = SystemManager.Instance.GetSubsystem<GamePlayerSystem>();

            gamePlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.GamePlayerSyncRequest, OnGamePlayerSyncRequest);
            gamePlayerSystem.RegisterPlayerObservedMessage(this, ClientHandlerMessage.GamePlayerNetworkInputRequest, OnGamePlayerNetworkInputRequest);
        }
        public void UnregesterMessageObserver()
        {
            GamePlayerSystem gamePlayerSystem = SystemManager.Instance.GetSubsystem<GamePlayerSystem>();

            gamePlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.GamePlayerSyncRequest);
            gamePlayerSystem.UnregisterPlayerObservedMessage(this, ClientHandlerMessage.GamePlayerNetworkInputRequest);
        }


        public bool IsInTeamGame()
        {
            return GamePlayerRoomInfo.Team != Team.None;
        }

        public bool LeaveGameRoom()
        {
            GameStaticInfo gameStaticInfo;
            GameDynamicInfo gameDynamicInfo;
            GameResultInfo gameResultInfo;


            SystemManager.Instance.GetSubsystem<GameLogicSystem>().PlayerLeaveGame(this);

            if (!IsOnlineGamePlayer)
            {
                return SystemManager.Instance.GetSubsystem<GameRoomSystem>().OnPlayerLeaveGameRoom(this);
            }

            return true;
        }
        
        private void OnGamePlayerSyncRequest(int connectionId, Dictionary<int, object> msg)
        {
            ErrorCode errorCode;
            Dictionary<int, object> playerMessage;
            Dictionary<int, object> gameMessage;

            if (msg.RetrieveMessageItem(GamePlayerSyncRequest.PlayerSyncMessage, out playerMessage)) { }
            if (msg.RetrieveMessageItem(GamePlayerSyncRequest.GameMessage, out gameMessage, false)) { }

            //DebugLog.Log(JsonConvert.SerializeObject(gameMessage));
            GameStaticInfo gameStaticInfo;
            GameDynamicInfo gameDynamicInfo;
            GameResultInfo gameResultInfo;
            errorCode = SystemManager.Instance.GetSubsystem<GameLogicSystem>().PlayerSyncGame(this,
                playerMessage, gameMessage, out gameStaticInfo, out gameDynamicInfo, out gameResultInfo);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(GamePlayerSyncRespond.ErrorCode, errorCode.GetHashCode());
            if (errorCode == ErrorCode.Success)
            {
                if(gameStaticInfo != null)
                    outMessage.AddMessageItem(GamePlayerSyncRespond.GameStaticInfo, gameStaticInfo.SerializeObject());
                if(gameDynamicInfo != null)
                    outMessage.AddMessageItem(GamePlayerSyncRespond.GameDynamicInfo, gameDynamicInfo.SerializeObject());
                if (gameResultInfo != null)
                    outMessage.AddMessageItem(GamePlayerSyncRespond.GameResultInfo, gameResultInfo.SerializeObject());
            }

            if (!IsOnlineGamePlayer)
            {
                var gameRoom = SystemManager.Instance.GetSubsystem<GameRoomSystem>().GetGameRoomByGameRoomId(GamePlayerRoomInfo.RoomId);
                if (gameRoom != null)
                    gameRoom.SendMessageToAllPlayer(ClientHandlerMessage.GamePlayerSyncRespond, outMessage);
                else
                    DebugLog.LogError("OnGamePlayerSyncRequest cant find gameRoom");
            }
            else
            {
                PhotonApplication.Instance.NetHandle.Send(ClientInfo.GameConnectionId, ClientHandlerMessage.GamePlayerSyncRespond, outMessage);
            }
        }
        private void OnGamePlayerNetworkInputRequest(int connectionId, Dictionary<int, object> msg)
        {
            float xVelocity;
            float yVelocity;
            float xRotation;
            float yRotation;
            Dictionary<int, bool> buttons;
            Dictionary<NetworkInputButtons, bool> networkButtons = null;

            //float gunRotationZ;
            float[] gunAimDirection;

            if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.xVelocity, out xVelocity)) { }
            if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.yVelocity, out yVelocity)) { }
            if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.xRotation, out xRotation)) { }
            if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.yRotation, out yRotation)) { }

            if (msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.NetworkButon, out buttons))
            {
                networkButtons = buttons.ToDictionary(x => (NetworkInputButtons)x.Key, x => x.Value);
            }

            ////if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.GunRotationZ, out gunRotationZ)) { }
            if (!msg.RetrieveMessageItem(GamePlayerNetworkInputRequest.GunAimDirection, out gunAimDirection)) { }

            PlayerNetworkInput playerNetworkInput = new PlayerNetworkInput(xVelocity, yVelocity,
                xRotation, yRotation, networkButtons, gunAimDirection);

            SystemManager.Instance.GetSubsystem<GameLogicSystem>().PlayerNetworkInput(GamePlayerRoomInfo.RoomId, GamePlayerRoomInfo.AccountId, playerNetworkInput);

            Dictionary<int, object> outMessage = new Dictionary<int, object>();
            outMessage.AddMessageItem(GamePlayerNetworkInputRespond.AccountId, GamePlayerRoomInfo.AccountId);
            outMessage.AddMessageItem(GamePlayerNetworkInputRespond.PlayerNetworkInput, playerNetworkInput.CreateSerializedObject());
            PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.GamePlayerNetworkInputRespond, outMessage);
        }
    }
}