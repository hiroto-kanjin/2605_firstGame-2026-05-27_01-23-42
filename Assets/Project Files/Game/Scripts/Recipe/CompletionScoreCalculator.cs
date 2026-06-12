using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CompletionScoreCalculator : MonoBehaviour // hk追加
    {
        public static CompletionScoreCalculator Instance { get; private set; }

        // 減点値（インスペクターで調整可能）
        [SerializeField] private int penaltyExtraEvolutionBall = 20;  // レシピ外の進化ボール
        [SerializeField] private int penaltyExtraSpecialBall = 15;    // レシピ外の特殊ボール
        [SerializeField] private int penaltyNuisanceBall = 5;         // お邪魔ボール

        private void Awake()
        {
            Instance = this;
        }

        // 完成度を計算して返す
        public int Calculate(List<BallBehaviorHK> ballsInPot, RecipeData recipe) // hk追加
        {
            int score = 100;

            foreach (BallBehaviorHK ball in ballsInPot)
            {
                BallCategory category = ball.GetBallCategory();
                BallType type = ball.GetBallType();

                // お邪魔ボールの場合
                if (category == BallCategory.Nuisance)
                {
                    score -= penaltyNuisanceBall;
                    continue;
                }

                // レシピに含まれているか確認する
                if (!IsInRecipe(ball.GetBranch(), type, recipe))
                {
                    // 進化ボールの場合
                    if (category == BallCategory.Evolution)
                    {
                        score -= penaltyExtraEvolutionBall;
                    }
                    // 特殊ボールの場合
                    else if (category == BallCategory.Special)
                    {
                        score -= penaltyExtraSpecialBall;
                    }
                }
            }

            return score;
        }

        // レシピに含まれている食材かどうか確認する
        private bool IsInRecipe(Branch branch, BallType type, RecipeData recipe) // hk追加
        {
            foreach (RecipeIngredient ingredient in recipe.requiredIngredients)
            {
                if (ingredient.branch == branch && ingredient.ballType == type)
                    return true;
            }
            return false;
        }

        // 完成度からランクを返す
        public CompletionRank GetRank(int score) // hk追加
        {
            if (score == 100) return CompletionRank.Perfect;
            if (score >= 80) return CompletionRank.Great;
            if (score >= 60) return CompletionRank.Good;
            if (score >= 40) return CompletionRank.Bad;
            return CompletionRank.Terrible;
        }
    }

    // 完成度ランクの定義
    public enum CompletionRank // hk追加
    {
        Perfect,   // 100点
        Great,     // 80〜99点
        Good,      // 60〜79点
        Bad,       // 40点以下
        Terrible   // マイナス（ゲテモノ料理）
    }
}