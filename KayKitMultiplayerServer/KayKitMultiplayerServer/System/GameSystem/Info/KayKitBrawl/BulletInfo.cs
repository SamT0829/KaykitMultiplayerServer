using System;
using System.Collections.Generic;

namespace KayKitMultiplayerServer.System.GameSystem.Info.KayKitBrawl
{
    public class BulletInfo
    {
        public long PlayerAccountId { get; set; }
        public int BulletId;
        public int BulletDamage { get; set; }

        public bool isAlive;
        private TimeSpan  bulletLifeTimeSpan;

        public void InitBullet(int bulletId, int bulletDamage, long playerAccountId, double bulletLifeTime)
        {
            isAlive = true;
            BulletId = bulletId;
            BulletDamage = bulletDamage;
            PlayerAccountId = playerAccountId;
            bulletLifeTimeSpan = TimeSpan.FromSeconds(bulletLifeTime);
        }

        public void Update(long timeelapsed)
        {
            bulletLifeTimeSpan -= TimeSpan.FromMilliseconds(timeelapsed);
            // Player Death Timer
            if (bulletLifeTimeSpan.TotalSeconds <= 0)
            {
                isAlive = false;
            }
        }

        public List<object> CreateSerializeObject()
        {
            List<object> retv = new List<object>();
            retv.Add(BulletId);
            retv.Add(isAlive);
            retv.Add(PlayerAccountId);

            return retv;
        }

        public void DeserializeObject(object[] retv)
        {
            PlayerAccountId = (long)retv[0];
            BulletDamage = (int)retv[1];
        }
    }
}