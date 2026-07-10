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
        public int recipeId; // hk追加：このレベルで使う完成料理のID（②RecipeDataを指す）

        [Header("盤面デザイン")]
        public Level levelDesign; // hk追加：このゲームレベルで使う盤面デザイン（Level.asset）への参照

        [Header("背景")]
        public GameObject levelBackgroundPrefab; // hk修正：背景プレハブを直接指定（Type二択を廃止、CSV指定対応）

        [Header("フィニッシャー")]
        public int finisherShotLimit = 3; // hk追加：レベル固有。②にはないのでここに残す

        [Header("ボール供給")]
        public List<BallSupplyRate> ballSupplyRates = new List<BallSupplyRate>(); // hk追加：ゲームレベルごとの供給確率

        [Header("お邪魔ボール出現イベント")] // hk追加
        public List<NuisanceSpawnEvent> nuisanceSpawnEvents = new List<NuisanceSpawnEvent>(); // hk追加

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
        public BallCategory category; // hk追加：進化 or 特殊
        public int number;            // hk追加：素材番号（0001など）
        [Range(0f, 100f)]
        public float spawnRate;
    }
}