using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKSupplyManager : MonoBehaviour // hk追加
    {
        public static HKSupplyManager Instance { get; private set; }

        [SerializeField] private GameObject finisherPrefab;
        [SerializeField] private FinisherSupplyData finisherSupplyData; // hk追加
        [SerializeField] private BallData supplyData;
        [SerializeField] private Transform launcherPosition;

        private (Branch branch, BallType ballType) currentBall;   // hk追加
        private (Branch branch, BallType ballType) nextBall;      // hk追加
        private (Branch branch, BallType ballType) nextNextBall;  // hk追加

        private bool isFinisherActive = false; // hk追加
        private bool isRecipeReady = false; // hk追加：レシピ成立フラグ
        private GameObject currentFinisher = null;
        private BubbleBehavior currentBallObject; // hk追加

        private void Awake()
        {
            Instance = this;
        }

        public void StartSupply()
        {
            currentBall = supplyData.GetRandomBall();
            nextBall = supplyData.GetRandomBall();
            nextNextBall = supplyData.GetRandomBall();

            SpawnCurrentBall();
            LevelController.LevelBehavior.OnBubbleLaunched += OnBubbleLaunched;

            UpdateUI();
        }

        private void SpawnCurrentBall() // hk追加：通常ボールをランチャーに生成する
        {
            currentBallObject = LevelController.LevelBehavior.SpawnBallHK(
                currentBall.branch,
                GetStageIdFromBallType(currentBall.ballType),
                launcherPosition.position
            );

            if (currentBallObject != null)
            {
                currentBallObject.transform.SetParent(launcherPosition);
            }
        }

        private void SpawnFinisher() // hk追加：フィニッシャーをランチャーに生成する
        {
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

            // hk追加：フィニッシャーがランチャーに出た時点でカウントダウン開始を通知する
            HKGameManager.Instance.OnFinisherSpawned();

            Debug.Log("フィニッシャー出現！");
        }

        private void ShiftToNext() // hk追加：次のボールまたはフィニッシャーを0.5秒後に生成する
        {
            if (isRecipeReady)
            {
                Invoke(nameof(SpawnFinisher), 0.5f);
            }
            else
            {
                currentBall = nextBall;
                nextBall = nextNextBall;
                nextNextBall = supplyData.GetRandomBall();
                Invoke(nameof(SpawnCurrentBall), 0.5f);
            }
            UpdateUI();
        }

        private void OnBubbleLaunched(BubbleBehavior launchedBubble)
        {
            Debug.Log("OnBubbleLaunched called. isFinisherActive=" + isFinisherActive + " bubble=" + launchedBubble.name);

            HKGameManager.Instance.OnShotFired(); // hk追加

            if (isFinisherActive)
            {
                // hk追加：フィニッシャーが発射されたら何も生成しない
                launchedBubble.transform.SetParent(null);
                return;
            }

            if (launchedBubble == currentBallObject)
            {
                // hk追加：ランチャーのボールが発射された場合
                launchedBubble.transform.SetParent(null);
                launchedBubble.EnablePhysics();
                currentBallObject = null;
                ShiftToNext();
            }
            else
            {
                // hk追加：盤面のボールが発射された場合、ランチャーのボールを破棄して次へ
                if (currentBallObject != null)
                {
                    currentBallObject.gameObject.SetActive(false);
                    currentBallObject = null;
                }
                ShiftToNext();
            }
        }

        public void OnRecipeCompleted() // hk追加：レシピ成立時に呼ばれる
        {
            isRecipeReady = true;
            CancelInvoke(nameof(SpawnCurrentBall)); // hk追加：通常ボールの生成をキャンセル
            TrajectoryController.EndDrag(); // hk追加：ドラッグを強制解除する

            // hk追加：ランチャーにボールがなければ0.5秒後にフィニッシャーを生成する
            if (currentBallObject == null)
            {
                Invoke(nameof(SpawnFinisher), 0.5f);
            }
            // hk追加：ランチャーにボールがあれば発射後にShiftToNextでフィニッシャーを生成する
        }

        public void OnFinisherEnteredPot() // hk追加
        {
            isFinisherActive = false;
            currentFinisher = null;
        }

        public void ClearFinisher() // hk追加
        {
            if (currentFinisher != null)
            {
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

        public void ResetState()
        {
            isFinisherActive = false;
            isRecipeReady = false;

            CancelInvoke(nameof(SpawnCurrentBall));
            CancelInvoke(nameof(SpawnFinisher));

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