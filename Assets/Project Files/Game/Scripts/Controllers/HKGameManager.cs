using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKGameManager : MonoBehaviour // hk追加
    {
        public static HKGameManager Instance { get; private set; }

        // 現在の完成度スコア
        private int currentScore = 0; // hk追加

        // 現在の完成度ランク
        private CompletionRank currentRank; // hk追加

        // レシピが成立しているかどうか
        private bool isRecipeReady = false; // hk追加

        private void Awake()
        {
            Instance = this;
        }

        // ゲーム開始時の初期化
        public void StartGame(RecipeData recipe) // hk追加
        {
            Debug.Log("StartGame called"); // hk追加：デバッグ用
            currentScore = 0;
            isRecipeReady = false;

            // hk追加：HKSupplyManagerの状態をリセットする
            HKSupplyManager.Instance.ResetState();

            RecipeManager.Instance.SetRecipe(recipe);
            CookingAreaManager.Instance.ResetPot();

            // hk追加：供給システムを開始する
            HKSupplyManager.Instance.StartSupply();
        }

        // 鍋の中身が変わるたびにRecipeManagerから呼ばれる
        public void OnPotContentsChanged() // hk追加
        {
            Debug.Log("OnPotContentsChanged called, recipeReady will be checked"); // hk追加：デバッグ用
            // 鍋の中身を取得する
            var ballsInPot = CookingAreaManager.Instance.GetBallsInPot();

            // レシピが成立しているか確認する
            bool recipeReady = RecipeManager.Instance.CheckRecipeReady(ballsInPot);

            if (recipeReady && !isRecipeReady)
            {
                // レシピ成立 → フィニッシャーを出現させる
                isRecipeReady = true;
                HKSupplyManager.Instance.OnRecipeCompleted();

                // 鍋が光るエフェクト（今後実装）
                Debug.Log("レシピ成立！鍋が光ります");
            }
            else if (!recipeReady && isRecipeReady)
            {
                // レシピが崩れた（鍋からボールが出た場合）
                isRecipeReady = false;
                Debug.Log("レシピが崩れました");
            }
        }

        // フィニッシャーが鍋に入った時にRecipeManagerから呼ばれる
        public void OnJudgementResult(bool recipeMatched, int completionScore) // hk追加
        {
            currentScore = completionScore;
            currentRank = CompletionScoreCalculator.Instance.GetRank(completionScore);

            // hk追加：判定後はフィニッシャーを破棄する
            HKSupplyManager.Instance.ClearFinisher();

            if (recipeMatched)
            {
                // クリア
                Debug.Log($"クリア！完成度：{currentScore}点　ランク：{currentRank}");
                GameController.OnLevelCompleted();
            }
            else
            {
                // ゲームオーバー
                Debug.Log($"ゲームオーバー　完成度：{currentScore}点");
                GameController.OnLevelFailed();
            }
        }

        // 現在の完成度スコアを返す
        public int GetCurrentScore() // hk追加
        {
            return currentScore;
        }

        // 現在の完成度ランクを返す
        public CompletionRank GetCurrentRank() // hk追加
        {
            return currentRank;
        }

        // レシピが成立しているかどうかを返す
        public bool IsRecipeReady() // hk追加
        {
            return isRecipeReady;
        }
    }
}