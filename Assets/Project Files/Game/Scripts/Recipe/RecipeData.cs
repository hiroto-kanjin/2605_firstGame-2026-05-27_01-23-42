using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "RecipeData", menuName = "HK/RecipeData")] // hk追加
    public class RecipeData : ScriptableObject // hk追加
    {
        // レシピに必要な食材リスト
        public List<RecipeIngredient> requiredIngredients;
    }

    [System.Serializable]
    public class RecipeIngredient // hk追加
    {
        public BallType ingredientType; // 食材の種類
        public int requiredCount;       // 必要個数
    }
}