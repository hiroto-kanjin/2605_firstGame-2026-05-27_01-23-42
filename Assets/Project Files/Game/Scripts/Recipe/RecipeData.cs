using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：レシピ（完成料理）のデータベース。完成料理を複数持つ
    [CreateAssetMenu(fileName = "RecipeData", menuName = "HK/RecipeData")]
    public class RecipeData : ScriptableObject
    {
        [SerializeField] private List<RecipeEntry> recipes = new List<RecipeEntry>();

        // 名前で完成料理を取り出す
        public RecipeEntry GetRecipe(string recipeName)
        {
            foreach (var r in recipes)
            {
                if (r.recipeName == recipeName)
                    return r;
            }
            return null;
        }
    }

    // hk追加：完成料理1つぶん
    [System.Serializable]
    public class RecipeEntry
    {
        public string recipeName;                 // 完成料理の名前

        [Header("進化の枠（最後のnumberはイレギュラー素材。レシピに使わない）")]
        public List<int> evolutionChain = new List<int>(); // numberを順に並べる

        [Header("通常レシピ（何をいくつ要求するか）")]
        public List<RequiredItem> requiredList = new List<RequiredItem>();

        [Header("特殊ボールのレシピ（この料理に固定で紐づく。手で追加）")]
        public List<RequiredItem> specialList = new List<RequiredItem>();

        [Header("通常の完成データ（点数で段階分け）")]
        public List<CompletionStage> completionStages = new List<CompletionStage>();

        [Header("裏メニュー")]
        public bool hasSecret;                     // 裏メニュー有り/無し
        public int secretNuisanceCount;            // 裏：お邪魔の必要数
        public int secretIrregularCount;           // 裏：イレギュラー素材の必要数
        public Sprite secretImage;                 // 裏の完成絵（1枚）
    }

    // hk追加：レシピが要求する材料1つ（numberを何個）
    [System.Serializable]
    public class RequiredItem
    {
        public int number;   // ①BallDataの進化ボールの番号
        public int count;    // 必要な個数
    }

    // hk追加：完成データの1段階（名前・点数下限・絵）
    [System.Serializable]
    public class CompletionStage
    {
        public string rankName;   // 段階の名前（パーフェクト/グレート/グッド/バッド）
        public int minScore;      // この点数以上ならこの絵（下限）
        public Sprite image;      // 完成料理の絵
    }
}