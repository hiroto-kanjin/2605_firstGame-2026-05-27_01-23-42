using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "FinisherSupplyData", menuName = "HK/FinisherSupplyData")] // hk追加
    public class FinisherSupplyData : ScriptableObject // hk追加
    {
        [SerializeField] private List<FinisherIconEntry> entries;

        // FinisherTypeに対応するアイコンを取得する
        public Texture GetIcon(FinisherBall.FinisherType type) // hk追加
        {
            foreach (FinisherIconEntry entry in entries)
            {
                if (entry.finisherType == type)
                    return entry.icon;
            }
            return null;
        }
        public FinisherIconEntry GetEntry(FinisherBall.FinisherType type) // hk追加
        {
            foreach (FinisherIconEntry entry in entries)
            {
                if (entry.finisherType == type)
                    return entry;
            }
            return null;
        }
    }

    [System.Serializable]
    public class FinisherIconEntry // hk追加
    {
        public FinisherBall.FinisherType finisherType;
        public Texture icon;

        [Header("Physics")] // hk追加
        public float mass = 1f;            // hk追加
        public float linearDamping = 25f;  // hk追加
        [Range(0f, 1f)]
        public float bounciness = 0.3f;    // hk追加
        public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f); // hk追加
    }
}