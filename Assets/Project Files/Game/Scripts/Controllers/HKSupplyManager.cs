using UnityEngine;
using System.Collections.Generic;

namespace Watermelon.BubbleMerge
{
    public class HKSupplyManager : MonoBehaviour // hk追加
    {
        public static HKSupplyManager Instance { get; private set; }

        [SerializeField] private GameObject finisherPrefab;
        [SerializeField] private FinisherSupplyData finisherSupplyData; // hk追加
        [SerializeField] private BallData ballData; // hk追加：物理パラメータ用
        [SerializeField] private RecipeData recipeData; // hk追加：進化の枠・レシピ用
        [SerializeField] private Transform launcherPosition;
        [SerializeField] BubblesPhysicsData baseSquishPhysicsData; // hk追加：質量比較の基準となるBubblesPhysicsData（Unity画面でBubble Physics Data_baseを設定する）
        public BubblesPhysicsData BaseSquishPhysicsData => baseSquishPhysicsData; // hk追加

        private (BallCategory category, int number) currentBall;   // hk修正：branch＋ballType → category＋number
        private (BallCategory category, int number) nextBall;      // hk修正
        private (BallCategory category, int number) nextNextBall;  // hk修正

        private bool isFinisherActive = false;
        private bool isRecipeReady = false;
        private GameObject currentFinisher = null;
        private BubbleBehavior currentBallObject; // hk追加

        private void Awake()
        {
            Instance = this;
        }

        public void StartSupply()
        {
            currentBall = GetRandomBall();  // hk修正：③ではなく④自身が抽選する
            nextBall = GetRandomBall();
            nextNextBall = GetRandomBall();

            SpawnCurrentBall();
            LevelController.LevelBehavior.OnBubbleLaunched += OnBubbleLaunched;

            UpdateUI();
        }

        // hk追加：供給の抽選係（③GameLevelDataのballSupplyRatesを読み、category＋numberを返す）
        // 元々③にあったGetRandomBallを、データと処理を分けるため④へ移動したもの
        private (BallCategory category, int number) GetRandomBall()
        {
            GameLevelData level = HKGameManager.Instance.GetCurrentLevel();
            if (level == null || level.ballSupplyRates == null || level.ballSupplyRates.Count == 0)
            {
                Debug.LogError("HKSupplyManager: ballSupplyRatesが空です");
                return (BallCategory.Evolution, 0);
            }

            List<BallSupplyRate> rates = level.ballSupplyRates;

            // spawnRateを重みにした抽選
            float total = 0f;
            foreach (BallSupplyRate rate in rates)
            {
                total += rate.spawnRate;
            }

            if (total <= 0f)
            {
                // 全部0なら先頭を返す（設定ミスのフォールバック）
                return (rates[0].category, rates[0].number);
            }

            float pick = Random.Range(0f, total);
            float sum = 0f;
            foreach (BallSupplyRate rate in rates)
            {
                sum += rate.spawnRate;
                if (pick <= sum)
                {
                    return (rate.category, rate.number);
                }
            }

            // 浮動小数の誤差対策で末尾を返す
            return (rates[rates.Count - 1].category, rates[rates.Count - 1].number);
        }

        private void SpawnCurrentBall()
        {
            // hk修正：抽選が返したcategoryを見て分岐する（branch決め打ちを廃止）
            if (currentBall.category == BallCategory.Evolution)
            {
                // 進化ボール：現状はbranchをkinoko固定で生成する。
                // branchという概念は将来的に廃止予定。今はEvolutionBranchが見た目・サイズの入れ物として必要なため残す。
                // numberは進化の段階番号として、そのままstageIdに渡す（両方0始まりで一致）。
                currentBallObject = LevelController.LevelBehavior.SpawnBallHK(
                    Branch.kinoko,
                    currentBall.number,
                    launcherPosition.position
                );

                if (currentBallObject != null)
                {
                    currentBallObject.transform.SetParent(launcherPosition);
                }
            }
            else if (currentBall.category == BallCategory.Special)
            {
                // hk修正：特殊ボールを盤面に出す（SpawnSpecialBallHK経由でInit前にSetData）
                currentBallObject = LevelController.LevelBehavior.SpawnSpecialBallHK(
                    currentBall.number,
                    launcherPosition.position
                );

                if (currentBallObject != null)
                {
                    currentBallObject.transform.SetParent(launcherPosition);
                }
            }
            else
            {
                // 想定外のカテゴリ（お邪魔などが供給抽選に混ざった場合）
                Debug.LogWarning($"HKSupplyManager: 供給抽選に想定外のカテゴリ({currentBall.category})が来ました");
            }
        }

        private void SpawnFinisher()
        {
            if (currentFinisher != null) return; // hk追加：既にフィニッシャーがいる場合はスキップ

            isRecipeReady = false;
            isFinisherActive = true;

            currentFinisher = Instantiate(
                finisherPrefab,
                launcherPosition.position,
                Quaternion.identity
            );

            currentFinisher.transform.SetParent(launcherPosition);

            FinisherBall.FinisherType type = FinisherBall.FinisherType.Fire;
            Texture icon = finisherSupplyData.GetIcon(type);
            currentFinisher.GetComponent<FinisherBall>().SetData(type, icon);

            BubbleBehavior finisherBubble = currentFinisher.GetComponent<BubbleBehavior>();
            if (finisherBubble != null)
            {
                LevelController.LevelBehavior.AddBubble(finisherBubble);
            }

            HKGameManager.Instance.OnFinisherSpawned();

            Debug.Log("フィニッシャー出現！");
        }

        private void ShiftToNext()
        {
            if (isRecipeReady)
            {
                Invoke(nameof(SpawnFinisher), 0.5f);
            }
            else
            {
                currentBall = nextBall;
                nextBall = nextNextBall;
                nextNextBall = GetRandomBall();  // hk修正：④自身が抽選する
                Invoke(nameof(SpawnCurrentBall), 0.5f);
            }
            UpdateUI();
        }

        private void OnBubbleLaunched(BubbleBehavior launchedBubble)
        {
            Debug.Log("OnBubbleLaunched called. isFinisherActive=" + isFinisherActive + " bubble=" + launchedBubble.name);

            HKGameManager.Instance.OnShotFired();

            if (isFinisherActive)
            {
                launchedBubble.transform.SetParent(null);
                return;
            }

            if (launchedBubble == currentBallObject)
            {
                launchedBubble.transform.SetParent(null);
                launchedBubble.EnablePhysics();
                currentBallObject = null;
                ShiftToNext();
            }
            else
            {
                if (currentBallObject != null)
                {
                    currentBallObject.gameObject.SetActive(false);
                    currentBallObject = null;
                }
                ShiftToNext();
            }
        }

        public void OnRecipeCompleted()
        {
            isRecipeReady = true;
            CancelInvoke(nameof(SpawnCurrentBall));
            TrajectoryController.EndDrag();

            if (currentBallObject != null)
            {
                currentBallObject.gameObject.SetActive(false);
                currentBallObject = null;
            }
            Invoke(nameof(SpawnFinisher), 0.5f);
        }

        public void OnFinisherEnteredPot()
        {
            isFinisherActive = false;
            currentFinisher = null;
        }

        // hk追加：くっついたボールの後処理をFinisherBallに一元化して呼ぶだけにする
        public void ClearFinisher()
        {
            if (currentFinisher != null)
            {
                FinisherBall finisherBall = currentFinisher.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    finisherBall.DetachAllBalls(LevelController.LevelBehavior.transform);
                }

                BubbleBehavior finisherBubble = currentFinisher.GetComponent<BubbleBehavior>();
                if (finisherBubble != null)
                {
                    LevelController.LevelBehavior.RemoveBubble(finisherBubble);
                }

                Destroy(currentFinisher);
                currentFinisher = null;
            }
            isFinisherActive = false;
            isRecipeReady = false;
        }

        // hk追加：くっついたボールの後処理をFinisherBallに一元化して呼ぶだけにする
        public void ResetState()
        {
            isFinisherActive = false;
            isRecipeReady = false;

            CancelInvoke(nameof(SpawnCurrentBall));
            CancelInvoke(nameof(SpawnFinisher));

            if (currentFinisher != null)
            {
                FinisherBall finisherBall = currentFinisher.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    finisherBall.DetachAllBalls(LevelController.LevelBehavior.transform);
                }

                Destroy(currentFinisher);
                currentFinisher = null;
            }

            if (currentBallObject != null)
            {
                currentBallObject.gameObject.SetActive(false);
                currentBallObject = null;
            }

            if (LevelController.LevelBehavior != null)
            {
                LevelController.LevelBehavior.OnBubbleLaunched -= OnBubbleLaunched;
            }
        }

        

        public (BallCategory, int) GetCurrentBall() => currentBall;   // hk修正：戻り値の型をcategory＋numberに
        public (BallCategory, int) GetNextBall() => nextBall;         // hk修正
        public (BallCategory, int) GetNextNextBall() => nextNextBall; // hk修正
        public bool IsFinisherActive() => isFinisherActive; // hk追加
        public BallData SupplyData => ballData; // hk追加：物理パラメータ用
        public RecipeData RecipeData => recipeData; // hk追加：進化の枠・レシピ用
        public FinisherSupplyData FinisherData => finisherSupplyData; // hk追加
        public void UpdateUI() { } // hk追加：今後実装
    }
}