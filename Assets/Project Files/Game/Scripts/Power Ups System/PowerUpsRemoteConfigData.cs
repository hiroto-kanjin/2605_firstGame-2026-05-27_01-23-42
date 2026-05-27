using System.Collections.Generic;

namespace Watermelon
{
    [System.Serializable]
    public class PowerUpsRemoteConfigData : RemoteConfigData
    {
        public override string Key => "powerUps";
        public override bool PrettyPrint => true;

        public List<PowerUp> array = new List<PowerUp>();

        public PowerUp GetPowerUp(PUType puType)
        {
            if (array != null)
            {
                string name = puType.ToString();
                foreach (PowerUp powerUp in array)
                {
                    if (powerUp.name == name)
                    {
                        return powerUp;
                    }
                }
            }

            return null;
        }

        [System.Serializable]
        public class PowerUp
        {
            public string name;
            public int defaultCount;

            public string currency = "Coins";
            public int price = 100;
            public int purchaseCount = 1;
        }
    }
}

