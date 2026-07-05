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

                if (!IsInRecipe(category, GetNumber(ball), ingredients))
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

        // hk追加：ボールのnumberを取り出す（進化は段階番号、特殊はインデックス）
        private int GetNumber(BallBehaviorHK ball)
        {
            if (ball.GetBallCategory() == BallCategory.Evolution)
                return BallBehaviorHK.GetEvolutionNumber(ball.GetBallType());
            return ball.GetBallIndex();
        }

        public CompletionRank GetRank(int score) // hk追加
        {
            if (score == 100) return CompletionRank.Perfect;
            if (score >= 80) return CompletionRank.Great;
            if (score >= 60) return CompletionRank.Good;
            if (score >= 40) return CompletionRank.Bad;
            return CompletionRank.Terrible;
        }
    }

    public enum CompletionRank // hk追加
    {
        Perfect,
        Great,
        Good,
        Bad,
        Terrible
    }
}