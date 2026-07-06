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
        // hk追加：IDで完成料理を取り出す
        public RecipeEntry GetRecipeById(int recipeId)
        {
            foreach (var r in recipes)
            {
                if (r.recipeId == recipeId)
                    return r;
            }
            return null;
        }
        // hk追加：CSV書き出し用に、料理リスト全体を読み取り専用で返す
        public List<RecipeEntry> GetRecipeListForExport()
        {
            return recipes;
        }
        // hk追加：完成料理名で「進化の枠の段階数」を返す（マージ判定用）
        public int GetEvolutionChainLength(string recipeName)
        {
            RecipeEntry entry = GetRecipe(recipeName);
            if (entry == null) return 0;
            return entry.evolutionChain.Count;
        }

        // hk追加：レシピIDから「食材1件」のリストを組み立てる（進化＋特殊をcategory付きでまとめる）
        // UI表示・採点・鍋照合が、これ1つを共通で使う
        public List<RecipeSlotData> BuildSlotDataList(int recipeId)
        {
            List<RecipeSlotData> result = new List<RecipeSlotData>();
            RecipeEntry entry = GetRecipeById(recipeId);
            if (entry == null) return result;

            // 通常レシピ＝進化ボール
            foreach (RequiredItem item in entry.requiredList)
            {
                if (item.count <= 0) continue; // 0は使わない
                result.Add(new RecipeSlotData(BallCategory.Evolution, item.number, item.count));
            }

            // 特殊ボールのレシピ
            foreach (RequiredItem item in entry.specialList)
            {
                if (item.count <= 0) continue;
                result.Add(new RecipeSlotData(BallCategory.Special, item.number, item.count));
            }

            return result;
        }
    }

    // hk追加：食材1件（category＋number＋count）。UI・採点・照合で共通に使う入れ物
    public class RecipeSlotData
    {
        public BallCategory category; // 進化 or 特殊
        public int number;            // 番号
        public int count;             // 必要数

        public RecipeSlotData(BallCategory category, int number, int count)
        {
            this.category = category;
            this.number = number;
            this.count = count;
        }
    }

    // hk追加：完成料理1つぶん
    [System.Serializable]
    public class RecipeEntry
    {
        public int recipeId;                      // 完成料理のID（重複しない番号。③やCSVから指すキー）
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
        public GameObject secretPrefab;            // 裏（Anomaly）の演出プレハブ（絵もエフェクトも中に仕込む）
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
        public GameObject prefab; // 完成演出のプレハブ（絵もエフェクトも中に仕込む）
    }
}