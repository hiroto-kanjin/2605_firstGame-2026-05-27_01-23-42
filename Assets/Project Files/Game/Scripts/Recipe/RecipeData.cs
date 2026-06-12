using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "RecipeData", menuName = "HK/RecipeData")]
    public class RecipeData : ScriptableObject
    {
        public List<RecipeIngredient> requiredIngredients;
    }

    [System.Serializable]
    public class RecipeIngredient // hk追加
    {
        public BallCategory category;   // hk追加
        public Branch branch;            // hk追加：Evolutionの場合のみ使用
        public BallType ballType;
        public int requiredCount;
    }
}