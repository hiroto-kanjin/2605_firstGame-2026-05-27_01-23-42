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

        public void StartLevel(int levelId)
        {
            Level = database.GetLevel(levelId);
            Level.Init();

            LevelBehavior.ChangeShape(database.GetShape(Level.LevelShapeType).Prefab);
            // hk修正：背景はGameLevelDataのlevelBackgroundPrefabを直接使う（共通配列は廃止）
            GameLevelData currentGameLevel = HKGameManager.Instance.GetCurrentLevel();
            if (currentGameLevel != null && currentGameLevel.levelBackgroundPrefab != null)
                LevelBehavior.ChangeBackround(currentGameLevel.levelBackgroundPrefab);
            else
                Debug.LogWarning("背景プレハブが未設定です（levelBackgroundPrefab）");

            // hk修正：爆弾は使わないため空配列を渡す（Level.Requirements依存を外す）
            LevelBehavior.SetLevelItems(Level.Items, new BombData[0], database);

            // hk修正：turnsLimitはLevel（盤面デザイン）ではなくGameLevelData（レベル管理）から取る
            if (currentGameLevel != null)
                turnsLimit = currentGameLevel.turnsLimit;
            else
                Debug.LogWarning("GameLevelDataが取得できず、turnsLimitが未設定です");

            Turn = 0;

            UIGame gameUI = UIController.GetPage<UIGame>();

            if (!gameUI.IsPageDisplayed)
                UIController.ShowPage<UIGame>();

            gameUI.OnLevelStarted(levelId);

            LevelBehavior.InitialSpawn(() =>
            {
                if (levelId == 0)
                {
                    ITutorial firstLevelTutorial = new FirstLevelTutorial();

                    TutorialController.ActivateTutorial(firstLevelTutorial);

                    if (!firstLevelTutorial.IsFinished)
                    {
                        firstLevelTutorial.StartTutorial();
                    }
                }
            });

            SavePresets.CreateSave("Level " + (levelId + 1), "Levels");

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

        public static bool TryGetSpawnData(BubbleData data, out BubbleSpawnData spawnData)
        {
            return Level.TryGetSimilarSpawnData(data, out spawnData);
        }

        public static bool GetRandomSpawnBubble(out BubbleSpawnData data)
        {
            return Level.GetNextSpawnData(out data);
        }

        public static bool CreateRandomBubbleData(BubbleSpawnData spawnData, out BubbleData data)
        {
            return CreateBubbleData(spawnData, out data);
        }

        public static bool CreateRandomBubbleData(out BubbleData data)
        {
            Level.GetNextSpawnData(out var spawnData);
            return CreateBubbleData(spawnData, out data);
        }

        public static BubbleData IncrementData(BubbleData data)
        {
            var newData = data;

            var branch = Database.GetBranch(data.branch);

            if (newData.stageId < branch.stages.Length)
            {
                newData.stageId++;

                newData.icon = branch.stages[newData.stageId].icon;
            }

            return newData;
        }

        public static bool IsLastStage(BubbleData data)
        {
            var branch = Database.GetBranch(data.branch);

            return data.stageId == branch.stages.Length - 1;
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

            // branch/stageIdはCompare（合体判定）でまだ使うので残す。除去は次段階。
            data.branch = settings.branch;
            data.stageId = settings.stageId;
            data.size = entry.size; // hk修正：BallDataのsizeを使う

            // icon/colorは読み出し箇所が無いため設定しない（除去2aで確認済み）

            return true;
        }

        public static bool CreateBubbleData(Requirement settings, out BubbleData data)
        {
            data = new BubbleData();

            EvolutionBranch branch = Database.GetBranch(settings.branch);
            if (branch == null)
                return false;

            if (branch.stages.Length <= settings.stageId)
                return false;

            var stage = branch.stages[settings.stageId];

            data.branch = settings.branch;
            data.stageId = settings.stageId;
            data.color = branch.backgroundColor;
            data.icon = stage.icon;
            data.size = stage.size; // hk追加

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

        public static void LoadLevel(int levelId)
        {
            instance.ClearLevel();
            instance.StartLevel(levelId);
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