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

        public static SimpleIntSave LevelSave { get; private set; }
        public static int LevelID => LevelSave.Value;

        public static GameData Data => instance.gameData;

        private void Awake()
        {
            instance = this;

            LevelSave = SaveController.GetSaveObject<SimpleIntSave>("levelSave");

            // Cache components
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
            // Display default page
            UIController.ShowPage<UIMainMenu>();

            levelController.Init();
            MapLevelAbstractBehavior.OnLevelClicked += OnLevelClickedCallback;
            mapBehavior.Show();

            // Move this method to the point when the game is fully loaded
            GameLoading.MarkAsReadyToHide();

            if(LevelAutoRun.CheckIfNeedToAutoRunLevel())
            {
                OnLevelStart(LevelAutoRun.GetLevelIndex());
            }
        }

        private void OnLevelClickedCallback(int value)
        {
            if(LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
            {
                UIController.GetPage<UIMainMenu>().ShowLevelPopup(value);
            }
            else
            {
                UIAddLivesPanel.Show((lifeRecieved) =>
                {   
                    if(lifeRecieved)
                    {
                        UIController.GetPage<UIMainMenu>().ShowLevelPopup(value);
                    }
                });
            }
        }

        public static void OnLevelStart(int levelId)
        {
            LivesSystem.LockLife();

            LevelSave.Value = levelId;
            OnLevelChanged?.Invoke();
            LevelController.LoadLevel(levelId);

            mapBehavior.Hide();

            UIController.HidePage<UIMainMenu>();
            UIController.ShowPage<UIGame>();
        }

        public static void OnLevelCompleted()
        {
            UIController.HidePage<UIGame>();
            UIController.ShowPage<UIComplete>();

            LevelSave.Value++;

            if (LevelSave.Value > LevelController.MaxLevelReached)
                LevelController.MaxLevelReached = LevelSave.Value;
        }

        public static void NextLevel()
        {
            SaveController.MarkAsSaveIsRequired();
            LevelController.LoadLevel(LevelSave.Value);

            OnLevelChanged?.Invoke();

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
            LevelController.LoadLevel(LevelSave.Value);

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