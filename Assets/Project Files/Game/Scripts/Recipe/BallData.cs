using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "BallData", menuName = "HK/BallData")]
    public class BallData : ScriptableObject // hk追加
    {
        [Header("進化ボール (Branch + BallType)")]
        [SerializeField] private List<EvolutionBallEntry> evolutionEntries = new List<EvolutionBallEntry>();

        [Header("特殊ボール")]
        [SerializeField] private List<SpecialBallEntry> specialEntries = new List<SpecialBallEntry>();

        [Header("お邪魔ボール")]
        [SerializeField] private List<NuisanceBallEntry> nuisanceEntries = new List<NuisanceBallEntry>();

        public EvolutionBallEntry GetEntry(Branch branch, BallType ballType)
        {
            foreach (var entry in evolutionEntries)
            {
                if (entry.branch == branch && entry.ballType == ballType)
                    return entry;
            }
            return null;
        }

        public SpecialBallEntry GetSpecialEntry(int index)
        {
            if (index < specialEntries.Count)
                return specialEntries[index];
            return null;
        }

        public NuisanceBallEntry GetNuisanceEntry(int index)
        {
            if (index < nuisanceEntries.Count)
                return nuisanceEntries[index];
            return null;
        }
    }

    [System.Serializable]
    public class EvolutionBallEntry // hk追加：進化ボール
    {
        public string entryName; // 追加：インスペクター上で見分けるための名前

        public Branch branch;
        public BallType ballType;

        [Header("Physics")]
        public float mass = 1f;
        public float linearDamping = 25f;
        [Range(0f, 1f)]
        public float bounciness = 0.3f;
        public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f);
    }

    [System.Serializable]
    public class SpecialBallEntry // hk追加：特殊ボール
    {
        public string entryName;
        public Sprite icon;
        public float size = 1f; // hk追加：表示サイズ（EvolutionStage.sizeと同じ基準）

        [Header("Physics")]
        public float mass = 1f;
        public float linearDamping = 25f;
        [Range(0f, 1f)]
        public float bounciness = 0.3f;
        public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f);
    }

    [System.Serializable]
    public class NuisanceBallEntry // hk追加：お邪魔ボール
    {
        public string entryName;
        public Sprite icon;
        public float size = 1f; // hk追加：表示サイズ（EvolutionStage.sizeと同じ基準）

        [Header("Physics")]
        public float mass = 1f;
        public float linearDamping = 25f;
        [Range(0f, 1f)]
        public float bounciness = 0f;
        public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f);
    }
}