using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CompletionScoreCalculator : MonoBehaviour // hk追加
    {
        public static CompletionScoreCalculator Instance { get; private set; }

        [SerializeField] private int penaltyExtraEvolutionBall = 20;
        [SerializeField] private int penaltyExtraSpecialBall = 15;
        [SerializeField] private int penaltyNuisanceBall = 5;

        private void Awake()
        {
            Instance = this;
        }

        // hk修正：レシピIDから②の食材（進化＋特殊）を取り、鍋と照合して採点する
        public int Calculate(List<BallBehaviorHK> ballsInPot, int recipeId)
        {
            int score = 100;

            List<RecipeSlotData> ingredients = HKSupplyManager.Instance.RecipeData.BuildSlotDataList(recipeId);

            foreach (BallBehaviorHK ball in ballsInPot)
            {
                BallCategory category = ball.GetBallCategory();

                if (category == BallCategory.Nuisance)
                {
                    score -= penaltyNuisanceBall;
                    continue;
                }

                if (!IsInRecipe(category, ball.GetNumber(), ingredients)) // hk修正：共通のGetNumber()を使う
                {
                    if (category == BallCategory.Evolution)
                        score -= penaltyExtraEvolutionBall;
                    else if (category == BallCategory.Special)
                        score -= penaltyExtraSpecialBall;
                }
            }

            return score;
        }

        // hk修正：category＋numberのセットでレシピに含まれるか判定
        private bool IsInRecipe(BallCategory category, int number, List<RecipeSlotData> ingredients)
        {
            foreach (RecipeSlotData ingredient in ingredients)
            {
                if (ingredient.category == category && ingredient.number == number)
                    return true;
            }
            return false;
        }

        // hk修正：ランク判定をレシピデータ（completionStages）ベースに変更。
        // 点数とrecipeIdを受け取り、そのレシピのrankName＋minScoreを高い下限順に照合してランク名を返す。
        // 数字はコードに持たず、すべてデータ側（レシピ）に置く。レベル（レシピ）ごとに下限を変えられる。
        public string GetRank(int score, int recipeId) // hk修正：戻り値をstring（rankName）に、引数にrecipeIdを追加
        {
            RecipeEntry entry = HKSupplyManager.Instance.RecipeData.GetRecipeById(recipeId);
            if (entry == null || entry.completionStages == null || entry.completionStages.Count == 0)
                return ""; // データが無ければ空（呼び出し側で扱う）

            // minScoreの高い順に並べ替えて、点数が下限以上になる最初のランクを返す
            List<CompletionStage> stages = new List<CompletionStage>(entry.completionStages);
            stages.Sort((a, b) => b.minScore.CompareTo(a.minScore));

            foreach (CompletionStage stage in stages)
            {
                if (score >= stage.minScore)
                    return stage.rankName;
            }

            // どの下限にも届かなければ、一番低いランク（並べ替え後の末尾）を返す
            return stages[stages.Count - 1].rankName;
        }
    }
}