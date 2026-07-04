using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：種類ごとの性質を1つぶん入れる箱
    [System.Serializable]
    public class BallProperty
    {
        public BallCategory category;   // どの種類か（進化/特殊/お邪魔）
        public bool canMerge;           // マージするか
        public bool joinsRecipe;        // レシピ判定に入るか
        public bool hasSpecialBehavior; // 特殊な振る舞い（アニメ・UI・エフェクト）があるか
    }

    // hk追加：3種類ぶんの性質をまとめて持つ入れ物
    [CreateAssetMenu(fileName = "BallProperties", menuName = "HK/BallProperties")]
    public class BallProperties : ScriptableObject
    {
        [SerializeField] private BallProperty[] properties;

        // hk追加：種類を渡すと、その種類の性質を返す
        public BallProperty Get(BallCategory category)
        {
            foreach (var p in properties)
            {
                if (p.category == category)
                    return p;
            }
            return null;
        }
    }
}