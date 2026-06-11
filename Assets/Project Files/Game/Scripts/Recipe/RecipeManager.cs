using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class RecipeManager : MonoBehaviour // hk追加
    {
        public static RecipeManager Instance { get; private set; }

        // 現在のレシピデータ（インスペクターで設定）
        [SerializeField] private RecipeData currentRecipe;

        private void Awake()
        {
            Instance = this;
        }

        // CookingAreaManagerから呼ばれる照合メソッド
        public void Judge(List<BallBehaviorHK> ballsInPot) // hk追加
        {
            // 鍋の中身を集計する
            Dictionary<BallType, int> potContents = CountBalls(ballsInPot);

            // レシピと照合する
            bool recipeMatched = CheckRecipe(potContents);

            // 完成度を計算する
            int completionScore = CompletionScoreCalculator.Instance.Calculate(ballsInPot, currentRecipe);

            // HKGameManagerに結果を伝える
            HKGameManager.Instance.OnJudgementResult(recipeMatched, completionScore);
        }

        // 鍋の中身がレシピを満たしているか確認する（フィニッシャーなしで呼べる版）
        public bool CheckRecipeReady(List<BallBehaviorHK> ballsInPot) // hk追加
        {
            Dictionary<BallType, int> potContents = CountBalls(ballsInPot);
            return CheckRecipe(potContents);
        }


        // 鍋の中のボールを種類ごとに数える
        private Dictionary<BallType, int> CountBalls(List<BallBehaviorHK> balls) // hk追加
        {
            Dictionary<BallType, int> counts = new Dictionary<BallType, int>();
            foreach (BallBehaviorHK ball in balls)
            {
                BallType type = ball.GetBallType();
                if (counts.ContainsKey(type))
                    counts[type]++;
                else
                    counts[type] = 1;
            }
            return counts;
        }

        // レシピの条件を満たしているか確認する
        private bool CheckRecipe(Dictionary<BallType, int> potContents) // hk追加
        {
            foreach (RecipeIngredient ingredient in currentRecipe.requiredIngredients)
            {
                if (!potContents.ContainsKey(ingredient.ingredientType) ||
                    potContents[ingredient.ingredientType] < ingredient.requiredCount)
                {
                    return false;
                }
            }
            return true;
        }

        // 現在のレシピを外から参照できるようにする
        public RecipeData GetCurrentRecipe() // hk追加
        {
            return currentRecipe;
        }

        // レシピを外から設定できるようにする（ステージ開始時に使う）
        public void SetRecipe(RecipeData recipe) // hk追加
        {
            currentRecipe = recipe;
        }
    }
}