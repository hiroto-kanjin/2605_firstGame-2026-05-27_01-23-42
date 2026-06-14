using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "BallData", menuName = "HK/BallData")]
    public class BallData : ScriptableObject // hk追加
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
        // hk追加：BranchとBallTypeに合うEntryを探す
        public BallSupplyEntry GetEntry(Branch branch, BallType ballType)
        {
            foreach (var entry in entries)
            {
                if (entry.branch == branch && entry.ballType == ballType)
                    return entry;
            }
            return null;
        }
    }

    [System.Serializable]
    public class BallSupplyEntry // hk追加
    {
        public Branch branch;       // hk追加
        public BallType ballType;
        [Range(0f, 100f)]
        public float spawnRate;

        [Header("Physics")] // hk追加
        public float mass = 1f;          // hk追加
        public float linearDamping = 0.4f; // hk追加
        [Range(0f, 1f)]
        public float bounciness = 0.3f;  // hk追加
        public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f); // hk追加：速度0〜1で追加分10→0.5
    }
}