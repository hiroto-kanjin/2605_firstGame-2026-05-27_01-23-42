using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class GameLevelData // hk追加：1ゲームレベル分のデータ
    {
        [Header("基本設定")]
        public string levelName;
        public int turnsLimit = 20;
        public int coinsReward = 20;

        [Header("レシピ")]
        public List<RecipeIngredient> requiredIngredients = new List<RecipeIngredient>();
        public int finisherShotLimit = 3;

        [Header("ボール供給")]
        public List<BallSupplyRate> ballSupplyRates = new List<BallSupplyRate>(); // hk追加：ゲームレベルごとの供給確率

        [Header("お邪魔ボール出現イベント")] // hk追加
        public List<NuisanceSpawnEvent> nuisanceSpawnEvents = new List<NuisanceSpawnEvent>(); // hk追加

        [Header("料理結果")]
        public List<CompletionImage> completionImages = new List<CompletionImage>();

        // hk追加：供給確率に基づいてランダムにボールを返す
        public (Branch branch, BallType ballType) GetRandomBall()
        {
            float total = 0f;
            foreach (BallSupplyRate rate in ballSupplyRates)
                total += rate.spawnRate;

            float random = Random.Range(0f, total);
            float cumulative = 0f;

            foreach (BallSupplyRate rate in ballSupplyRates)
            {
                cumulative += rate.spawnRate;
                if (random <= cumulative)
                    return (rate.branch, rate.ballType);
            }

            return (ballSupplyRates[0].branch, ballSupplyRates[0].ballType);
        }

        // hk追加：指定ショット数に対応する出現イベントをすべて返す
        public List<NuisanceSpawnEvent> GetEventsForShot(int shotNumber)
        {
            List<NuisanceSpawnEvent> result = new List<NuisanceSpawnEvent>();
            foreach (NuisanceSpawnEvent e in nuisanceSpawnEvents)
            {
                if (e.triggerShot == shotNumber)
                    result.Add(e);
            }
            return result;
        }
    }

    [System.Serializable]
    public class NuisanceSpawnEvent // hk追加：お邪魔ボール出現イベント1件
    {
        public int triggerShot;             // 何ショット目に出るか（0=即時）
        public int count;                   // 何個出るか
        public NuisanceBallType ballType;   // どの種類のお邪魔ボールか
    }

    [System.Serializable]
    public class BallSupplyRate // hk追加：ゲームレベルごとのボール供給確率
    {
        public Branch branch;
        public BallType ballType;
        [Range(0f, 100f)]
        public float spawnRate;
    }

    [System.Serializable]
    public class CompletionImage // hk追加：料理結果の絵（完成度別）
    {
        public CompletionRank rank;
        public Sprite image;
    }

    [System.Serializable]
    public class RecipeIngredient // hk追加：レシピ食材
    {
        public BallCategory category;
        public Branch branch;
        public BallType ballType;
        public int requiredCount = 1;
    }
}