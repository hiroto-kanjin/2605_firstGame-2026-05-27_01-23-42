using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class StageData // hk追加：1レベル分のゲームデータ
    {
        [Header("基本設定")]
        public string stageName; // hk追加：ステージ名（識別用）
        public int turnsLimit = 20; // hk追加：ショット数上限
        public int coinsReward = 20; // hk追加：クリア報酬コイン数

        [Header("レシピ")]
        public List<RecipeIngredient> requiredIngredients = new List<RecipeIngredient>(); // hk追加：レシピ食材リスト
        public int finisherShotLimit = 3; // hk追加：フィニッシャーショット数

        [Header("ボール供給")]
        public List<BallSupplyRate> ballSupplyRates = new List<BallSupplyRate>(); // hk追加：ボール供給確率

        [Header("料理結果")]
        public List<CompletionImage> completionImages = new List<CompletionImage>(); // hk追加：料理結果の絵（完成度別）
    }

    [System.Serializable]
    public class BallSupplyRate // hk追加：ボール供給確率
    {
        public Branch branch;
        public BallType ballType;
        [Range(0f, 100f)]
        public float spawnRate;
    }

    [System.Serializable]
    public class CompletionImage // hk追加：料理結果の絵（完成度別）
    {
        public CompletionRank rank; // hk追加：このランクの時に表示する絵
        public Sprite image; // hk追加：料理結果の絵
    }

    [System.Serializable]
    public class RecipeIngredient // hk追加：レシピ食材
    {
        public BallCategory category;
        public Branch branch;
        public BallType ballType;
        public int requiredCount = 1; // hk追加：デフォルト1
    }
}