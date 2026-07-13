using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKGameManager : MonoBehaviour // hk追加
    {
        public static HKGameManager Instance { get; private set; }

        [SerializeField] private float resultDelaySeconds = 1.5f; // hk追加：結果画面表示までの遅延秒数

        private GameLevelData currentLevel; // hk追加
        private int currentScore = 0; // hk追加
        private string currentRank; // hk修正：ランクをrankName（文字列）で持つ
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

            // hk修正：古いGameLevelDatabaseではなく、新しいLevelDatabase.GameLevelsから取得（棚を統合）
            GameLevelData[] gameLevels = LevelController.Database.GameLevels;
            int levelId = GameController.LevelID;

            if (gameLevels == null || levelId < 0 || levelId >= gameLevels.Length)
            {
                Debug.LogError($"HKGameManager: ゲームレベル{levelId}のデータがありません");
                currentLevel = null;
                return;
            }

            currentLevel = gameLevels[levelId];
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
            RecipeDisplayUI.Instance.SetupRecipe(currentLevel.recipeId); // hk修正：recipeIdで②から食材を組み立てる
           
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
                    Tween.DelayedCall(0.5f, () =>
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

            Debug.Log($"recipeReady={recipeReady} isRecipeReady={isRecipeReady} isFinisherActive={HKSupplyManager.Instance.IsFinisherActive()}");
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

        private bool CheckRecipe(List<BallBehaviorHK> ballsInPot) // hk修正：②の食材とcategory+numberで照合
        {
            var potContents = CountBalls(ballsInPot);

            List<RecipeSlotData> ingredients = HKSupplyManager.Instance.RecipeData.BuildSlotDataList(currentLevel.recipeId);

            foreach (RecipeSlotData ingredient in ingredients)
            {
                var key = (ingredient.category, ingredient.number);
                if (!potContents.ContainsKey(key) || potContents[key] < ingredient.count)
                    return false;
            }
            return true;
        }

        private Dictionary<(BallCategory, int), int> CountBalls(List<BallBehaviorHK> balls) // hk修正：category+numberで数える
        {
            var counts = new Dictionary<(BallCategory, int), int>();
            foreach (BallBehaviorHK ball in balls)
            {
                var key = (ball.GetBallCategory(), ball.GetNumber()); // hk修正：共通のGetNumber()を使う
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
            currentRank = CompletionScoreCalculator.Instance.GetRank(completionScore, currentLevel.recipeId); // hk修正：recipeIdを渡し、rankName（文字列）を受け取る

            HKSupplyManager.Instance.ClearFinisher();

            if (recipeMatched)
            {
                Debug.Log($"クリア！完成度：{currentScore}点　ランク：{currentRank}");
                Tween.DelayedCall(resultDelaySeconds, () => GameController.OnLevelCompleted());
            }
            else
            {
                Debug.Log($"ゲームオーバー　完成度：{currentScore}点");
                Tween.DelayedCall(resultDelaySeconds, () => GameController.OnLevelFailed());
            }
        }

        public bool IsFinalCountZero() => isFinalCountZero; // hk追加
        public GameLevelData GetCurrentLevel() => currentLevel; // hk追加
        public int GetCurrentScore() => currentScore; // hk追加
        public string GetCurrentRank() => currentRank; // hk修正：rankName（文字列）を返す
        public bool IsRecipeReady() => isRecipeReady; // hk追加
        public int GetShotsRemaining() => shotsRemaining; // hk追加
    }
}