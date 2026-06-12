using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "BallSupplyData", menuName = "HK/BallSupplyData")]
    public class BallSupplyData : ScriptableObject // hk追加
    {
        [SerializeField] private List<BallSupplyEntry> entries;

        // 出現率に基づいてランダムにボールを返す（Branch + BallType）
        public (Branch branch, BallType ballType) GetRandomBall() // hk追加
        {
            float total = 0f;
            foreach (BallSupplyEntry entry in entries)
                total += entry.spawnRate;

            float random = Random.Range(0f, total);
            float cumulative = 0f;

            foreach (BallSupplyEntry entry in entries)
            {
                cumulative += entry.spawnRate;
                if (random <= cumulative)
                    return (entry.branch, entry.ballType);
            }

            return (entries[0].branch, entries[0].ballType);
        }
    }

    [System.Serializable]
    public class BallSupplyEntry // hk追加
    {
        public Branch branch;       // hk追加
        public BallType ballType;
        [Range(0f, 100f)]
        public float spawnRate;
    }
}