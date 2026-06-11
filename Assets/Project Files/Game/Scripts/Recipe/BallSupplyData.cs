using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "BallSupplyData", menuName = "HK/BallSupplyData")] // hk追加
    public class BallSupplyData : ScriptableObject // hk追加
    {
        [SerializeField] private List<BallSupplyEntry> entries;

        // 出現率に基づいてランダムにボールの種類を返す
        public BallType GetRandomBallType() // hk追加
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
                    return entry.ballType;
            }

            // フォールバック（念のため）
            return entries[0].ballType;
        }
    }

    [System.Serializable]
    public class BallSupplyEntry // hk追加
    {
        public BallType ballType;   // ボールの種類
        [Range(0f, 100f)]
        public float spawnRate;     // 出現率（0〜100）
    }
}