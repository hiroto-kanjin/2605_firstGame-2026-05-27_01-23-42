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

        // hk追加：RecipeDataではなくList<RecipeIngredient>を受け取る
        public int Calculate(List<BallBehaviorHK> ballsInPot, List<RecipeIngredient> ingredients)
        {
            int score = 100;

            foreach (BallBehaviorHK ball in ballsInPot)
            {
                BallCategory category = ball.GetBallCategory();
                BallType type = ball.GetBallType();

                if (category == BallCategory.Nuisance)
                {
                    score -= penaltyNuisanceBall;
                    continue;
                }

                if (!IsInRecipe(ball.GetBranch(), type, ingredients))
                {
                    if (category == BallCategory.Evolution)
                    {
                        score -= penaltyExtraEvolutionBall;
                    }
                    else if (category == BallCategory.Special)
                    {
                        score -= penaltyExtraSpecialBall;
                    }
                }
            }

            return score;
        }

        private bool IsInRecipe(Branch branch, BallType type, List<RecipeIngredient> ingredients) // hk追加
        {
            foreach (RecipeIngredient ingredient in ingredients)
            {
                if (ingredient.branch == branch && ingredient.ballType == type)
                    return true;
            }
            return false;
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