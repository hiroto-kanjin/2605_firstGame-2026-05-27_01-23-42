// LevelController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class LevelController : MonoBehaviour
    {
        private static LevelController instance;

        [SerializeField] LevelDatabase database;
        public static LevelDatabase Database => instance.database;
        [SerializeField] ComboDatabase comboDatabase;
        public static ComboDatabase ComboDatabase => instance.comboDatabase;

        [Space]
        [SerializeField] GameObject bubblePrefab;
        [SerializeField] GameObject bombPrefab;
        [SerializeField] GameObject bombPUPrefab;

        public static GameObject BombPrefab => instance.bombPrefab;
        public static GameObject BombPUPrefab => instance.bombPUPrefab;

        [Space]
        [SerializeField] IceSpecialEffect iceSpecialEffect;
        [SerializeField] CrateSpecialEffect crateSpecialEffect;
        [SerializeField] CageSpecialEffect cageSpecialEffect;

        [Space]
        [SerializeField] BubblesPhysicsData bubblesPhysicsData;
        [SerializeField] ControlsData controlsData;

        public static IceSpecialEffect IceSpecialEffect => instance.iceSpecialEffect;
        public static CrateSpecialEffect CrateSpecialEffect => instance.crateSpecialEffect;
        public static CageSpecialEffect CageSpecialEffect => instance.cageSpecialEffect;

        public static LevelBehavior LevelBehavior { get; private set; }
        public static Level Level { get; private set; }

        public static bool IsLevelCompletedForTheFirstTime => GameController.LevelID >= MaxLevelReached;

        private static TweenCase effectCheckTweenCase;

        private static AttractionSettings activeAttractionSettings;
        public static AttractionSettings ActiveAttractionSettings => activeAttractionSettings;

        private static SimpleIntSave levelSave;
        public static int MaxLevelReached { get => levelSave.Value; set => levelSave.Value = value; }

        private static int turn;
        public static int Turn
        {
            get => turn;
            private set
            {
                turn = value;
                OnTurnChanged?.Invoke();
            }
        }

        private static int turnsLimit;
        public static int TurnsLimit => turnsLimit;

        public static int TurnsLeft => turnsLimit - turn;

        public static bool IsGameplayActive { get; private set; }

        public static event SimpleCallback OnTurnChanged;

        private void Awake()
        {
            instance = this;
            bubblesPhysicsData.Init();
            controlsData.Init();
        }

        private void OnDestroy()
        {
            IsGameplayActive = false;
            OnTurnChanged = null;
        }

        public void Init()
        {
            levelSave = SaveController.GetSaveObject<SimpleIntSave>("levelNumber");

            LevelBehavior = new GameObject("Level Behavior").AddComponent<LevelBehavior>();
            LevelBehavior.Init(bubblePrefab);

            LevelBehavior.OnBubbleLaunched += TurnMade;
            LevelBehavior.OnBubbleSelected += BubbleSelected;
        }

        public static float CurrentMergeLineY { get; private set; } // hk追加：発射直後のボールが盤面ボールと衝突し始める高さ

        public void StartLevel(string gameLevelId) // hk修正：番号ではなく固有IDで受け取る
        {
            GameLevelData currentGameLevel = HKGameManager.Instance.GetCurrentLevel();
            if (currentGameLevel == null)
            {
                Debug.LogError("GameLevelDataが取得できません");
                return;
            }

            int displayIndex = database.GetIndexByGameLevelId(gameLevelId); // hk追加：UI表示や初回判定など、番号が必要な場面用に変換しておく

            Level = currentGameLevel.levelDesign; // hk修正：levels配列ではなくGameLevelDataのLevel Designから取る
            Level.Init();

            LevelShape currentShape = database.GetShape(Level.LevelShapeType); // hk追加

            // hk追加：レベル側で個別調整が指定されていればそちらを優先、無ければシェイプの自動値を使う
            CurrentMergeLineY = Level.OverrideMergeLine ? Level.MergeLineYOverride : currentShape.MergeLineY;

            LevelBehavior.ChangeShape(currentShape.Prefab);

            if (currentGameLevel.levelBackgroundPrefab != null)
                LevelBehavior.ChangeBackround(currentGameLevel.levelBackgroundPrefab);
            else
                Debug.LogWarning("背景プレハブが未設定です（levelBackgroundPrefab）");

            LevelBehavior.SetLevelItems(Level.Items, new BombData[0], database);

            turnsLimit = currentGameLevel.turnsLimit;

            Turn = 0;

            UIGame gameUI = UIController.GetPage<UIGame>();

            if (!gameUI.IsPageDisplayed)
                UIController.ShowPage<UIGame>();

            gameUI.OnLevelStarted(displayIndex); // hk修正：番号が必要なのでdisplayIndexを渡す

            LevelBehavior.InitialSpawn(() =>
            {
                if (displayIndex == 0) // hk修正：番号が必要なのでdisplayIndexを使う
                {
                    ITutorial firstLevelTutorial = new FirstLevelTutorial();

                    TutorialController.ActivateTutorial(firstLevelTutorial);

                    if (!firstLevelTutorial.IsFinished)
                    {
                        firstLevelTutorial.StartTutorial();
                    }
                }
            });

            SavePresets.CreateSave("Level " + (displayIndex + 1), "Levels"); // hk修正：番号が必要なのでdisplayIndexを使う

            IsGameplayActive = true;
        }

        public void ClearLevel()
        {
            IsGameplayActive = false;
            LevelBehavior.Clear();

            PUController.ResetBehaviors();

            ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
            if (tutorial != null && tutorial.IsActive)
            {
                tutorial.Unload();
            }
        }

        public static void OnSpecialEffectAdded()
        {
            effectCheckTweenCase.KillActive();
            effectCheckTweenCase = Tween.DelayedCall(1.5f, () =>
            {
                if (!LevelBehavior.IsActiveBubbleExists())
                {
                    LevelFail();
                }
            });
        }

        public static bool CreateRandomBubbleData(BubbleSpawnData spawnData, out BubbleData data)
        {
            return CreateBubbleData(spawnData, out data);
        }

        public static BubbleData IncrementData(BubbleData data)
        {
            // hk修正：branchを使わず、番号+1の進化ボールをBallDataから作り直す
            BubbleSpawnData spawnData = new BubbleSpawnData() { stageId = data.stageId + 1 };

            if (CreateBubbleData(spawnData, out var newData))
            {
                return newData;
            }

            return data;
        }

        public static bool IsLastStage(BubbleData data)
        {
            // hk修正：branchではなく、現在レベルのレシピのevolutionChainで最終段階を判定する
            GameLevelData level = HKGameManager.Instance.GetCurrentLevel();
            if (level == null) return true;

            RecipeEntry recipe = HKSupplyManager.Instance.RecipeData.GetRecipeById(level.recipeId);
            if (recipe == null) return true;

            return data.stageId >= recipe.evolutionChain.Count - 1;
        }

        public static bool CreateBubbleData(BubbleSpawnData settings, out BubbleData data)
        {
            data = new BubbleData();

            // hk修正：sizeはBallDataから取る（枝依存を外す＝ブランチ除去）。
            // 進化はcategory=Evolution固定、number=stageIdで一致するのでそれで引く。
            BallData ballData = HKSupplyManager.Instance.SupplyData;
            if (ballData == null)
                return false;

            BallEntry entry = ballData.GetBall(BallCategory.Evolution, settings.stageId);
            if (entry == null)
                return false;

            data.stageId = settings.stageId;
            data.size = entry.size; // hk修正：BallDataのsizeを使う

            return true;
        }

        public void TurnMade(BubbleBehavior bubble)
        {
            // hk追加：カウント管理はHKGameManagerに移譲
        }

        TweenCase gameOverCase;

        private void Tapped()
        {
            // hk追加：カウント管理はHKGameManagerに移譲
        }

        public void BubbleSelected(BubbleBehavior bubble)
        {

        }

        public static void LoadLevel(string gameLevelId) // hk修正：番号ではなく固有IDで受け取る
        {
            instance.ClearLevel();
            instance.StartLevel(gameLevelId);
        }

        public static void CloseLevel()
        {
            instance.ClearLevel();
        }

        public static void LevelFail()
        {
            if (!IsGameplayActive)
                return;

            GameController.OnLevelFailed();

            AudioController.PlaySound(AudioController.AudioClips.loseSound);

            IsGameplayActive = false;
        }

        public static void LevelComplete()
        {
            if (!IsGameplayActive)
                return;

            GameController.OnLevelCompleted();

            AudioController.PlaySound(AudioController.AudioClips.winSound);

            IsGameplayActive = false;
        }

        public static void AdjustTurnsLimit(int amount)
        {
            turnsLimit += amount;

            OnTurnChanged?.Invoke();
        }

        public static void SetAttractionSettings(AttractionSettings attractionSettings)
        {
            activeAttractionSettings = attractionSettings;

            if (LevelBehavior != null)
            {
                List<BubbleBehavior> bubbles = LevelBehavior.GetBubbles();
                if (!bubbles.IsNullOrEmpty())
                {
                    for (int i = 0; i < bubbles.Count; i++)
                    {
                        bubbles[i].OnAttractionSettingsChanged(attractionSettings);
                    }
                }
            }
        }

        public static void ResetAttractionSettings()
        {
            SetAttractionSettings(instance.bubblesPhysicsData.AttractionSettings);
        }

        public static void OnGamePopupOpened()
        {
            IsGameplayActive = false;
        }

        public static void OnGamePopupClosed()
        {
            IsGameplayActive = true;
        }
    }
}