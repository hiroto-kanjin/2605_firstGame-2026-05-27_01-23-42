using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKGameManager : MonoBehaviour // hk追加
    {
        public static HKGameManager Instance { get; private set; }

        [SerializeField] private GameLevelDatabase gameLevelDatabase; // hk追加

        private GameLevelData currentLevel; // hk追加
        private int currentScore = 0; // hk追加
        private CompletionRank currentRank; // hk追加
        private bool isRecipeReady = false; // hk追加
        private bool isGameEnded = false; // hk追加
        private int shotsRemaining = 0; // hk追加
        private bool isFinalCountZero = false; // hk追加

        private void Awake()
        {
            Instance = this;
        }

        public void StartGame() // hk追加
        {
            Debug.Log("StartGame called");

            currentLevel = gameLevelDatabase.GetLevel(GameController.LevelID);
            if (currentLevel == null) return;

            currentScore = 0;
            isRecipeReady = false;
            isGameEnded = false;
            isFinalCountZero = false;
            shotsRemaining = currentLevel.turnsLimit;

            HKSupplyManager.Instance.ResetState();
            CookingAreaManager.Instance.ResetPot();
            HKSupplyManager.Instance.StartSupply();

            UIController.GetPage<Watermelon.UIGame>().ResetCountUI();
            UIController.GetPage<Watermelon.UIGame>().UpdateShotsRemaining(shotsRemaining);
            RecipeDisplayUI.Instance.SetupRecipe(currentLevel.requiredIngredients); // hk追加：レシピUIを初期化する
            LevelController.LevelBehavior.SpawnNuisanceBallsFromLevelHK(); // hk追加：お邪魔ボールを配置する
        }

        public void OnShotFired() // hk追加
        {
            if (isGameEnded) return;

            shotsRemaining--;
            Debug.Log("残りショット数: " + shotsRemaining);

            if (isRecipeReady)
            {
                UIController.GetPage<Watermelon.UIGame>().UpdateFinalCountdown(shotsRemaining);

                if (shotsRemaining <= 0)
                {
                    isFinalCountZero = true;
                    Tween.DelayedCall(0.5f, () => // hk追加：0.5秒後にゲームオーバー判定
                    {
                        if (!isGameEnded)
                        {
                            Debug.Log("ファイナルカウント切れ。ゲームオーバー");
                            OnJudgementResult(false, 0);
                        }
                    });
                }
            }
            else
            {
                UIController.GetPage<Watermelon.UIGame>().UpdateShotsRemaining(shotsRemaining);

                if (shotsRemaining <= 0)
                {
                    Debug.Log("ショット数上限に達しました。ゲームオーバー");
                    LevelController.LevelFail();
                }
            }
        }

        public void OnFinisherSpawned() // hk追加：フィニッシャーがランチャーに出た時に呼ばれる
        {
            isRecipeReady = true;
            shotsRemaining = currentLevel.finisherShotLimit;
            isFinalCountZero = false;
            Debug.Log("フィニッシャー出現！残りショット数: " + shotsRemaining);

            UIController.GetPage<Watermelon.UIGame>().SwitchToFinalCountdown(shotsRemaining);
        }

        public void OnPotContentsChanged() // hk追加
        {
            if (isGameEnded) return;

            var ballsInPot = CookingAreaManager.Instance.GetBallsInPot();
            bool recipeReady = CheckRecipe(ballsInPot);

            Debug.Log($"recipeReady={recipeReady} isRecipeReady={isRecipeReady} isFinisherActive={HKSupplyManager.Instance.IsFinisherActive()}"); // hk追加：デバッグ用
            // hk追加：フィニッシャーが既に出ている場合はレシピ成立を無視する
            if (recipeReady && !isRecipeReady && !HKSupplyManager.Instance.IsFinisherActive())
            {
                HKSupplyManager.Instance.OnRecipeCompleted();
                Debug.Log("レシピ成立！");
            }
            else if (!recipeReady && isRecipeReady && !HKSupplyManager.Instance.IsFinisherActive())
            {
                isRecipeReady = false;
                Debug.Log("レシピが崩れました");
            }
        }

        private bool CheckRecipe(List<BallBehaviorHK> ballsInPot) // hk追加
        {
            var potContents = CountBalls(ballsInPot);

            foreach (RecipeIngredient ingredient in currentLevel.requiredIngredients)
            {
                var key = (ingredient.branch, ingredient.ballType);
                if (!potContents.ContainsKey(key) || potContents[key] < ingredient.requiredCount)
                    return false;
            }
            return true;
        }

        private Dictionary<(Branch, BallType), int> CountBalls(List<BallBehaviorHK> balls) // hk追加
        {
            var counts = new Dictionary<(Branch, BallType), int>();
            foreach (BallBehaviorHK ball in balls)
            {
                var key = (ball.GetBranch(), ball.GetBallType());
                if (counts.ContainsKey(key))
                    counts[key]++;
                else
                    counts[key] = 1;
            }
            return counts;
        }

        public void OnJudgementResult(bool recipeMatched, int completionScore) // hk追加
        {
            if (isGameEnded) return;

            isGameEnded = true;
            isRecipeReady = false;

            currentScore = completionScore;
            currentRank = CompletionScoreCalculator.Instance.GetRank(completionScore);

            HKSupplyManager.Instance.ClearFinisher();

            if (recipeMatched)
            {
                Debug.Log($"クリア！完成度：{currentScore}点　ランク：{currentRank}");
                GameController.OnLevelCompleted();
            }
            else
            {
                Debug.Log($"ゲームオーバー　完成度：{currentScore}点");
                GameController.OnLevelFailed();
            }
        }

        public bool IsFinalCountZero() => isFinalCountZero; // hk追加
        public GameLevelData GetCurrentLevel() => currentLevel; // hk追加
        public int GetCurrentScore() => currentScore; // hk追加
        public CompletionRank GetCurrentRank() => currentRank; // hk追加
        public bool IsRecipeReady() => isRecipeReady; // hk追加
        public int GetShotsRemaining() => shotsRemaining; // hk追加
    }
}