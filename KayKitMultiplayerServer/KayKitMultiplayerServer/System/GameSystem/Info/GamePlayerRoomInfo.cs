using System;
using KayKitMultiplayerServer.System.LobbySystem.Info;
using System.Collections.Generic;
using KayKitMultiplayerServer.Utility;
using UnityEngine;

namespace KayKitMultiplayerServer.System.GameSystem.Info
{
    public class GamePlayerRoomInfo
    {
        public long AccountId { get; set; }
        public string NickName { get; set; }
        public int RoomId { get; set; }
        public GameType GameType { get; set; }
        public Team Team { get; set; }
        public GamePlayerState GamePlayerState { get; set; }

        // Game Player Data
        public Vector3 PlayerPosition { get; set; }
        public Vector3 PlayerLocalEulerAngles { get; set; }
        public Vector3 gunAimDirection;

        public int PlayerHealth { get; set; }
        public int PlayerMaxHealth { get; set; }

        // Game Player bool
        public bool IsPlayerDie { get; set; }
        public bool IsPlayerShoot { get; set; }

        public TimeSpan DeathTimer;

        // GameData 
        public int KillCount { get; set; }
        public int DeathCount { get; set; }
        public int CointCount { get; set; }

        public GamePlayerRoomInfo()
        {
            CointCount = 0;
            PlayerMaxHealth = 10;
            PlayerHealth = PlayerMaxHealth;
        }

        public void InitData(LobbyPlayerRoomInfo lobbyPlayerRoomInfoInfo)
        {
            AccountId = lobbyPlayerRoomInfoInfo.AccountId;
            NickName = lobbyPlayerRoomInfoInfo.NickName;
            RoomId = lobbyPlayerRoomInfoInfo.RoomId;
            GameType = lobbyPlayerRoomInfoInfo.GameType;
            Team = lobbyPlayerRoomInfoInfo.Team;
        }

        public void InitGameData(Vector3 playerPosition)
        {
            PlayerPosition = playerPosition;
        }

        public void ResetGameData(Vector3 playerPosition)
        {
            PlayerHealth = PlayerMaxHealth;
            DeathTimer = TimeSpan.Zero;
            PlayerPosition = playerPosition;
        }
        public List<object> SerializedObject()
        {
            List<object> retv = new List<object>();
            retv.Add(AccountId);                                    //0
            retv.Add(NickName);                                     //1
            retv.Add(GameType);                                     //2
            retv.Add(Team);                                         //3

            // Game Player Data
            retv.Add(PlayerPosition.ToFloatArray());            //4 
            retv.Add(PlayerLocalEulerAngles.ToFloatArray());    //5
            retv.Add(gunAimDirection.ToFloatArray());           //6
            retv.Add(PlayerHealth);                             //7
            retv.Add(PlayerMaxHealth);                          //8

            // Game Player bool
            retv.Add(IsPlayerDie);                                   //9    
            retv.Add(IsPlayerShoot);                                 //10    

            // Game Data
            retv.Add(KillCount);            //11
            retv.Add(DeathCount);           //12
            retv.Add(CointCount);           //13

            return retv;
        }

        public void DeserializeObject(object[] retv)
        {
            AccountId = Convert.ToInt64(retv[0]);
            NickName = retv[1].ToString();
            GameType = (GameType)Convert.ToInt32(retv[2]);
            Team = (Team)Convert.ToInt32(retv[3]);

            // Game Player Data
            PlayerPosition = ((float[])retv[4]).ToVector3();
            PlayerLocalEulerAngles = ((float[])retv[5]).ToVector3();
            gunAimDirection = ((float[])retv[6]).ToVector3();
            PlayerHealth = Convert.ToInt32(retv[7]);
            PlayerMaxHealth = Convert.ToInt32(retv[8]);

            // Game Player bool
            IsPlayerDie = Convert.ToBoolean(retv[9]);
            IsPlayerShoot = Convert.ToBoolean(retv[10]);

            // Game Data
            KillCount = Convert.ToInt32(retv[11]);
            DeathCount = Convert.ToInt32(retv[12]);
            CointCount = Convert.ToInt32(retv[13]);
        }
    }
}