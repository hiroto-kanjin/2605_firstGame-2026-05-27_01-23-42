using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKSupplyManager : MonoBehaviour // hk追加
    {
        public static HKSupplyManager Instance { get; private set; }

        [SerializeField] private GameObject finisherPrefab;
        [SerializeField] private FinisherSupplyData finisherSupplyData; // hk追加
        [SerializeField] private BallData supplyData;
        [SerializeField] private Transform assistantPosition;

        // 現在・ネクスト・ネクストネクストのボール情報（Branch + BallType）
        private (Branch branch, BallType ballType) currentBall;   // hk追加
        private (Branch branch, BallType ballType) nextBall;      // hk追加
        private (Branch branch, BallType ballType) nextNextBall;  // hk追加

        private bool isFinisherActive = false;
        private GameObject currentFinisher = null;
        private BubbleBehavior currentBallObject; // hk追加
        private int finisherShotCount; // hk追加：フィニッシャー出現後の残り発射回数

        private void Awake()
        {
            Instance = this;
        }

        // 変更後
        // hk追加：レベル開始時にHKGameManagerから呼ばれる
        public void StartSupply()
        {
            currentBall = supplyData.GetRandomBall();
            nextBall = supplyData.GetRandomBall();
            nextNextBall = supplyData.GetRandomBall();

            SpawnCurrentBall();
            LevelController.LevelBehavior.OnBubbleLaunched += OnBubbleLaunched; // hk追加

            UpdateUI();
        }

        // hk追加：現在のボールを助手悪魔の位置に生成する
        private void SpawnCurrentBall()
        {
            currentBallObject = LevelController.LevelBehavior.SpawnBallHK(
                currentBall.branch,
                GetStageIdFromBallType(currentBall.ballType),
                assistantPosition.position
            );

            // hk追加：補充されたボールは他のボールと干渉しない
            if (currentBallObject != null)
            {
                currentBallObject.DisablePhysics();
            }
        }

        private void OnBubbleLaunched(BubbleBehavior launchedBubble)
        {
            Debug.Log("OnBubbleLaunched called. isFinisherActive=" + isFinisherActive + " bubble=" + launchedBubble.name); // hk追加：デバッグ用

            if (launchedBubble == currentBallObject)
            {
                // hk追加：弾かれたボールは干渉可能にする
                launchedBubble.EnablePhysics();

                OnAssistantBallLaunched();
            }
            else
            {
                OnFieldBallLaunched();
            }

            // hk追加：フィニッシャー出現中は、弾くたびに残り回数を減らす
            if (isFinisherActive)
            {
                finisherShotCount--;
                Debug.Log("残り発射回数: " + finisherShotCount); // hk追加：デバッグ用

                if (finisherShotCount <= 0)
                {
                    // hk追加：回数を使い切った時点でまだフィニッシャーが盤面にあればゲームオーバー
                    if (isFinisherActive)
                    {
                        HKGameManager.Instance.OnJudgementResult(false, 0);
                    }
                }
            }
        }

        // 助手悪魔のボールが弾かれた時
        public void OnAssistantBallLaunched()
        {
            ShiftQueue(false); // 弾いたボールは消さない
        }

        public void OnFieldBallLaunched()
        {
            ShiftQueue(true); // 助手悪魔のボールを消す
        }

        private void ShiftQueue(bool discardCurrentBall)
        {
            if (discardCurrentBall && currentBallObject != null)
            {
                OnAssistantBallDiscarded(currentBallObject);
            }

            currentBall = nextBall;
            nextBall = nextNextBall;
            nextNextBall = supplyData.GetRandomBall();

            // hk追加：フィニッシャー出現中は補充しない
            if (!isFinisherActive)
            {
                SpawnCurrentBall();
            }

            UpdateUI();
        }

        // hk追加：助手悪魔が保持していたボールが不要になった時に呼ばれる（捨てるアニメーションは今後実装）
        private void OnAssistantBallDiscarded(BubbleBehavior ball)
        {
            // hk追加：アニメーション実装までの仮処理として非表示にする   ☆彡
            ball.gameObject.SetActive(false);
        }

        // hk追加：BallTypeをstageIdに変換する（企画1始まり→プログラム0始まり）
        private int GetStageIdFromBallType(BallType ballType)
        {
            switch (ballType)
            {
                case BallType.EvolutionBall_01: return 0;
                case BallType.EvolutionBall_02: return 1;
                case BallType.EvolutionBall_03: return 2;
                case BallType.EvolutionBall_04: return 3;
                case BallType.EvolutionBall_05: return 4;
                case BallType.EvolutionBall_06: return 5;
                case BallType.EvolutionBall_07: return 6;
                case BallType.EvolutionBall_08: return 7;
                case BallType.EvolutionBall_09: return 8;
                case BallType.EvolutionBall_10: return 9;
                case BallType.EvolutionBall_11: return 10;
                default: return 0;
            }
        }

        // レシピ成立時にRecipeManagerから呼ばれる
        public void OnRecipeCompleted() // hk追加
        {
            if (isFinisherActive) return;

            currentFinisher = Instantiate(
                finisherPrefab,
                assistantPosition.position,
                Quaternion.identity
            );

            FinisherBall.FinisherType type = FinisherBall.FinisherType.Fire;
            Texture icon = finisherSupplyData.GetIcon(type);
            currentFinisher.GetComponent<FinisherBall>().SetData(type, icon);

            // hk追加：フィニッシャーをbubblesリストに追加してActivateMinDragの対象にする
            BubbleBehavior finisherBubble = currentFinisher.GetComponent<BubbleBehavior>();
            if (finisherBubble != null)
            {
                LevelController.LevelBehavior.AddBubble(finisherBubble);
            }

            if (currentBallObject != null) currentBallObject.gameObject.SetActive(false);
            isFinisherActive = true;

            finisherShotCount = RecipeManager.Instance.GetCurrentRecipe().finisherShotLimit;
            Debug.Log("フィニッシャー出現！残り発射回数: " + finisherShotCount);
        }

        // フィニッシャーが鍋に入った時にCookingAreaManagerから呼ばれる
        public void OnFinisherEnteredPot() // hk追加
        {
            isFinisherActive = false;
            currentFinisher = null;
        }
        // hk追加：フィニッシャーを破棄する（クリア・ゲームオーバー時に呼ぶ）
        public void ClearFinisher() // hk追加
        {
            if (currentFinisher != null)
            {
                // hk追加：bubblesリストからフィニッシャーを削除してからDestroy
                BubbleBehavior finisherBubble = currentFinisher.GetComponent<BubbleBehavior>();
                if (finisherBubble != null)
                {
                    LevelController.LevelBehavior.RemoveBubble(finisherBubble);
                }

                Destroy(currentFinisher);
                currentFinisher = null;
            }
            isFinisherActive = false;
        }
        // hk追加：レベル開始時に状態をリセットする
        public void ResetState()
        {
            isFinisherActive = false;
            finisherShotCount = 0;

            if (currentFinisher != null)
            {
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


        public (Branch, BallType) GetCurrentBall() => currentBall; // hk追加
        public (Branch, BallType) GetNextBall() => nextBall;       // hk追加
        public (Branch, BallType) GetNextNextBall() => nextNextBall; // hk追加
        public bool IsFinisherActive() => isFinisherActive; // hk追加
        public BallData SupplyData => supplyData; // hk追加
        public FinisherSupplyData FinisherData => finisherSupplyData; // hk追加
        private void UpdateUI() // hk追加
        {
            // 今後実装
        }
    }
}