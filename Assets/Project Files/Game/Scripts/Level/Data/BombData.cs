using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class BombData
    {
        [SerializeField] int movesToSpawn;
        public int MovesToSpawn => movesToSpawn;
    }
}
