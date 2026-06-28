using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKSupplyManager : MonoBehaviour // hk追加
    {
        public static HKSupplyManager Instance { get; private set; }

        [SerializeField] private GameObject finisherPrefab;
        [SerializeField] private FinisherSupplyData finisherSupplyData; // hk追加
        [SerializeField] private BallData ballData; // hk追加：物理パラメータ用
        [SerializeField] private Transform launcherPosition;

        private (Branch branch, BallType ballType) currentBall;   // hk追加
        private (Branch branch, BallType ballType) nextBall;      // hk追加
        private (Branch branch, BallType ballType) nextNextBall;  // hk追加

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
            var stage = HKGameManager.Instance.GetCurrentLevel();
            currentBall = stage.GetRandomBall(); // hk追加：GameLevelDataから供給確率を読む
            nextBall = stage.GetRandomBall();
            nextNextBall = stage.GetRandomBall();

            SpawnCurrentBall();
            LevelController.LevelBehavior.OnBubbleLaunched += OnBubbleLaunched;

            UpdateUI();
        }

        private void SpawnCurrentBall()
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
                var stage = HKGameManager.Instance.GetCurrentLevel(); // hk追加：GameLevelDataから供給確率を読む
                currentBall = nextBall;
                nextBall = nextNextBall;
                nextNextBall = stage.GetRandomBall();
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

        public void ClearFinisher()
        {
            if (currentFinisher != null)
            {
                FinisherBall finisherBall = currentFinisher.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    foreach (var ball in finisherBall.GetAttachedBalls())
                    {
                        if (ball != null)
                        {
                            ball.transform.SetParent(LevelController.LevelBehavior.transform); // hk追加：プールの親に戻す
                            BubbleBehavior bubble = ball.GetComponent<BubbleBehavior>();
                            if (bubble != null)
                            {
                                LevelController.LevelBehavior.RemoveBubble(bubble);
                                bubble.gameObject.SetActive(false);
                            }
                        }
                    }
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

        public void ResetState()
        {
            isFinisherActive = false;
            isRecipeReady = false;

            CancelInvoke(nameof(SpawnCurrentBall));
            CancelInvoke(nameof(SpawnFinisher));
            // hk追加：くっついたボールのSpringJoint2Dを削除する
            if (currentFinisher != null)
            {
                FinisherBall finisherBall = currentFinisher.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    foreach (var ball in finisherBall.GetAttachedBalls())
                    {
                        if (ball != null)
                        {
                            SpringJoint2D joint = ball.GetComponent<SpringJoint2D>();
                            if (joint != null) Destroy(joint);
                            ball.bodyType = RigidbodyType2D.Dynamic;
                            ball.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
                            Collider2D col = ball.GetComponent<Collider2D>();
                            if (col != null) col.enabled = true;
                        }
                    }
                }
            }
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
        public BallData SupplyData => ballData; // hk追加：物理パラメータ用
        public FinisherSupplyData FinisherData => finisherSupplyData; // hk追加
        public void UpdateUI() { } // hk追加：今後実装
    }
}