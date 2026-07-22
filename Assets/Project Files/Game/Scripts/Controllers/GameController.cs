using UnityEngine;
using Watermelon.BubbleMerge;
using Watermelon.Map;

namespace Watermelon
{
    public class GameController : MonoBehaviour
    {
        private static GameController instance;

        [DrawReference]
        [SerializeField] GameData gameData;

        [Space]
        [SerializeField] UIController uiController;

        private static ParticlesController particlesController;
        private static FloatingTextController floatingTextController;
        private static LevelController levelController;
        private static TrajectoryController trajectoryController;
        private static TutorialController tutorialController;
        private static MapBehavior mapBehavior;
        private static PUController powerUpController;

        public static event SimpleCallback OnLevelChanged;

        public static SimpleStringSave LevelSaveId { get; private set; } // hk修正：番号ではなく固有IDで保存する

        public static int LevelID // hk修正：外部互換のため、固有IDから配列の位置(番号)を逆算して返す
        {
            get
            {
                int index = LevelController.Database.GetIndexByGameLevelId(LevelSaveId.Value);
                return index >= 0 ? index : 0;
            }
        }

        public static GameData Data => instance.gameData;

        private void Awake()
        {
            instance = this;

            LevelSaveId = SaveController.GetSaveObject<SimpleStringSave>("levelSaveId"); // hk修正：キー名も変更（旧データと衝突させないため）

            CacheComponent(out particlesController);
            CacheComponent(out floatingTextController);
            CacheComponent(out levelController);
            CacheComponent(out trajectoryController);
            CacheComponent(out tutorialController);
            CacheComponent(out mapBehavior);
            CacheComponent(out powerUpController);

            uiController.Init();

            particlesController.Init();
            floatingTextController.Init();
            trajectoryController.Init();
            powerUpController.Init();
            powerUpController.InitBehaviors();
            tutorialController.Init();

            uiController.InitPages();
        }

        private void Start()
        {
            UIController.ShowPage<UIMainMenu>();

            levelController.Init();
            MapLevelAbstractBehavior.OnLevelClicked += OnLevelClickedCallback;
            mapBehavior.Show();

            GameLoading.MarkAsReadyToHide();

            if (LevelAutoRun.CheckIfNeedToAutoRunLevel())
            {
                OnLevelStart(LevelAutoRun.GetLevelIndex());
            }
        }

        private void OnLevelClickedCallback(int value)
        {
            if (LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
            {
                UIController.GetPage<UIMainMenu>().ShowLevelPopup(value);
            }
            else
            {
                UIAddLivesPanel.Show((lifeRecieved) =>
                {
                    if (lifeRecieved)
                    {
                        UIController.GetPage<UIMainMenu>().ShowLevelPopup(value);
                    }
                });
            }
        }

        public static void OnLevelStart(int levelId) // hk修正：外部（マップ等）からは今まで通り番号を受け取り、ここで固有IDに変換する
        {
            LivesSystem.LockLife();

            GameLevelData targetLevel = LevelController.Database.GameLevels[levelId];
            LevelSaveId.Value = targetLevel.gameLevelId;
            OnLevelChanged?.Invoke();

            HKGameManager.Instance.SetCurrentLevel(LevelSaveId.Value);
            LevelController.LoadLevel(LevelSaveId.Value);
            HKGameManager.Instance.StartGame();

            mapBehavior.Hide();

            UIController.HidePage<UIMainMenu>();
            UIController.ShowPage<UIGame>();
        }

        public static void OnLevelCompleted() // hk修正：固有IDベースで次のレベルへ進む
        {
            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIComplete>();

            int currentIndex = LevelController.Database.GetIndexByGameLevelId(LevelSaveId.Value);
            int nextIndex = currentIndex + 1;

            if (nextIndex < LevelController.Database.GameLevels.Length)
            {
                LevelSaveId.Value = LevelController.Database.GameLevels[nextIndex].gameLevelId;
            }

            if (nextIndex > LevelController.MaxLevelReached)
                LevelController.MaxLevelReached = nextIndex;
        }

        public static void NextLevel()
        {
            SaveController.MarkAsSaveIsRequired();

            HKGameManager.Instance.SetCurrentLevel(LevelSaveId.Value);
            LevelController.LoadLevel(LevelSaveId.Value);

            OnLevelChanged?.Invoke();

            HKGameManager.Instance.StartGame();

            AdsManager.ShowInterstitial(null);
        }

        public static void OnLevelFailed()
        {
            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIGameOver>();
        }

        public static void ReplayLevel()
        {
            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();

            HKGameManager.Instance.SetCurrentLevel(LevelSaveId.Value);
            LevelController.LoadLevel(LevelSaveId.Value);

            HKGameManager.Instance.StartGame();

            AdsManager.ShowInterstitial(null);
        }

        public static void CloseLevel()
        {
            LevelController.CloseLevel();
            mapBehavior.Show();
            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIMainMenu>();

            AdsManager.ShowInterstitial(null);
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;
                return true;
            }

            Debug.LogError(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;
            return false;
        }
        #endregion

        #region Dev
        public static void OnLevelManuallyChanged()
        {
            OnLevelChanged?.Invoke();
        }
        #endregion
    }
}