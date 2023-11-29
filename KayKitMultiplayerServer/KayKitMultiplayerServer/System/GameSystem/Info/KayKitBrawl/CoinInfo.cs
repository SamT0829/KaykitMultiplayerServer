using System.Collections.Generic;
using KayKitMultiplayerServer.Utility;
using UnityEngine;

namespace KayKitMultiplayerServer.System.GameSystem.Info.KayKitBrawl
{
    public class CoinInfo
    {
        public int CoinID;
        public bool active;
        public Vector3 Position;
        public string PlayerName = string.Empty;

        public CoinInfo()
        {
            active = false;
            Position = Vector3.zero;
        }

        public void InitCoin(int coinId, Vector3 position)
        {
            CoinID = coinId;
            active = true;
            Position = position;
        }

        public void PlayerGetCoin(string playerName)
        {
            active = false;
            PlayerName = playerName;
        }

        public void RemoveCoin(Vector3 position)
        {
            active = true;
            Position = position;
            PlayerName = string.Empty;
        }

        public List<object> CreateSerializeObject()
        {
            List<object> retv = new List<object>();
            retv.Add(CoinID);
            retv.Add(active);
            retv.Add(Position.ToFloatArray());

            return retv;
        }
    }
}