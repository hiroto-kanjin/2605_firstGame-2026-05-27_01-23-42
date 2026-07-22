#pragma warning disable 649

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class LevelEditorWindow : LevelEditorBase
    {

        private const string ITEM_ENUM_FILE_PATH = "Assets/Project Files/Game/Scripts/Level/Data/ItemEnum.cs";
        private const string LEVEL_SHAPE_ENUM_FILE_PATH = "Assets/Project Files/Game/Scripts/Level/Data/LevelShapeTypeEnum.cs";
        private const string LEVEL_BACKGROUNDS_ENUM_FILE_PATH = "Assets/Project Files/Game/Scripts/Level/Data/LevelBackgroundType.cs";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string EDITOR_SCENE_PATH = "Assets/Project Files/Game/Scenes/Level Editor.unity";
        private static string EDITOR_SCENE_NAME = "Level Editor";

        //Window configuration
        private const string TITLE = "Level Editor";
        private const float WINDOW_MIN_WIDTH = 800;
        private const float WINDOW_MIN_HEIGHT = 560;
        private const float WINDOW_MAX_WIDTH = 800;
        private const float WINDOW_MAX_HEIGHT = 700;

        //Level database fields
        private const string LEVELS_PROPERTY_NAME = "levels";
        private const string ITEMS_PROPERTY_NAME = "items";
        private const string BRANCHES_PROPERTY_NAME = "branches";
        private const string LEVEL_SHAPES_PROPERTY_NAME = "levelShapes";
        private const string LEVEL_BACKGROUNDS_PROPERTY_NAME = "levelBackgrounds";
        private const string POTION_SPRITES_PROPERTY_NAME = "potionSprites";
        private SerializedProperty levelsSerializedProperty;
        private SerializedProperty itemsSerializedProperty;
        private SerializedProperty branchesSerializedProperty;
        private SerializedProperty levelShapesSerializedProperty;
        private SerializedProperty levelBackgroundsSerializedProperty;
        private SerializedProperty potionSpritesSerializedProperty;

        //EnumObjectsList 
        private const string TYPE_PROPERTY_PATH = "type";
        private const string PREFAB_PROPERTY_PATH = "prefab";
        private const string EDITOR_TEXTURE_PROPERTY_PATH = "editorTexture";
        private const string LEVELS_SHAPE_ENUM_NAME = "LevelShapeType";
        private const string LEVELS_BACKGROUNDS_ENUM_NAME = "LevelBackgroundType";
        private bool enumCompiling;
        private EnumObjectsList itemsEnumObjectsList;
        private EnumObjectsList levelShapesEnumObjectsList;
        private EnumObjectsList levelSBackgroundsEnumObjectsList;
        private const string BALLS_TAB_NAME = "Balls"; // hk追加
        private const string EFFECTS_TAB_NAME = "Effects"; // hk追加
        private Branch hkSelectedBranch; // hk追加：Ballsタブで選択中のBranch

        //TabHandler
        private TabHandler tabHandler;
        private const string LEVELS_TAB_NAME = "Levels";
        private const string ITEMS_TAB_NAME = "Items";
        private const string LEVEL_SHAPES_TAB_NAME = "Level shapes";
        private const string LEVEL_BACKGROUNDS_TAB_NAME = "Level backgrounds";
        private const string BRANCHES_TAB_NAME = "Branches";
        private const string EDITOR_TAB_NAME = "Editor";

        //sidebar
        private LevelsHandler levelsHandler;
        private LevelRepresentation selectedLevelRepresentation;
        private const int SIDEBAR_WIDTH = 360;
        private const string OPEN_GAME_SCENE_LABEL = "Open \"Game\" scene";

        private const string REMOVE_SELECTION = "Remove selection";

        //ItemSave
        private const string POSITION_PROPERTY_PATH = "position";
        private const string ROTATION_PROPERTY_PATH = "rotation";
        private const string SCALE_PROPERTY_PATH = "scale";

        //General
        private const string YES = "Yes";
        private const string CANCEL = "Cancel";
        private const string WARNING_TITLE = "Warning";
        private SerializedProperty tempProperty;
        private string tempPropertyLabel;

        //PlayerPrefs
        private const string PREFS_LEVEL = "editor_level_index";
        private const string PREFS_WIDTH = "editor_sidebar_width";

        //rest of levels tab
        private const string ITEMS_LABEL = "Spawn items:";
        private const string FILE = "File";
        private const string COMPILING = "Compiling...";
        private const string ITEM_UNASSIGNED_ERROR = "Please assign prefab to this item in \"Items\"  tab.";
        private const string TEST_LEVEL = "Test level";

        private const float ITEMS_BUTTON_MAX_WIDTH = 120;
        private const float ITEMS_BUTTON_SPACE = 8;
        private const float ITEMS_BUTTON_WIDTH = 80;
        private const float ITEMS_BUTTON_HEIGHT = 80;
        private const string RENAME_LEVELS = "Rename Levels";
        private const string PREGENERATE_LEVEL = "Pregenerate Level";
        private const string PREGENERATE_LEVEL_WARNING = "Level fields would be modified. Continue?";
        private const string UPDATE_NOTE = "Update";
        private const string SHAPES_TAB_LABEL = "Shapes:";
        private const string BACKGROUNDS_TAB_LABEL = "Backgrounds:";
        private bool prefabAssigned;
        private GUIContent itemContent;
        private SerializedProperty currentLevelItemProperty;
        private Rect itemsListWidthRect;
        private Vector2 levelItemsScrollVector;
        private float itemPosX;
        private float itemPosY;
        private Rect itemsRect;
        private Rect itemRect;
        private int itemsPerRow;
        private int rowCount;
        private GameObject tempGameobject;
        private Item tempItem;
        private Texture2D tempTexture;
        private int selectedTab;
        private string[] levelTabs = { "Items", "Fields" };
        private int newLevelIndex;
        private int currentSideBarWidth;
        private Rect separatorRect;
        private bool separatorIsDragged;
        private bool lastActiveLevelOpened;
        private Rect levelContentRect;
        private float currentItemListWidth;
        private Color backupColor;
        private GUIContent defaultTitleContent;
        private GUIContent modifiedTitleContent;
        private Texture2D infoIcon;

        protected override string LEVELS_DATABASE_FOLDER_PATH => "Assets/Project Files/Data";

        protected override WindowConfiguration SetUpWindowConfiguration(WindowConfiguration.Builder builder)
        {
            builder.KeepWindowOpenOnScriptReload(true);
            builder.SetWindowMinSize(new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT));
            return builder.Build();
        }

        protected override Type GetLevelsDatabaseType()
        {
            return typeof(LevelDatabase);
        }

        public override Type GetLevelType()
        {
            return typeof(Level);
        }

        protected override void ReadLevelDatabaseFields()
        {
            levelsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(LEVELS_PROPERTY_NAME);
            branchesSerializedProperty = levelsDatabaseSerializedObject.FindProperty(BRANCHES_PROPERTY_NAME);
            itemsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(ITEMS_PROPERTY_NAME);
            levelShapesSerializedProperty = levelsDatabaseSerializedObject.FindProperty(LEVEL_SHAPES_PROPERTY_NAME);
            levelBackgroundsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(LEVEL_BACKGROUNDS_PROPERTY_NAME);
            potionSpritesSerializedProperty = levelsDatabaseSerializedObject.FindProperty(POTION_SPRITES_PROPERTY_NAME);
        }

        protected override void InitializeVariables()
        {
            Serializer.Init();

            enumCompiling = false;
            itemsEnumObjectsList = new EnumObjectsList(itemsSerializedProperty, TYPE_PROPERTY_PATH, PREFAB_PROPERTY_PATH, ITEM_ENUM_FILE_PATH, OnBeforeEnumFileupdateCallback);
            itemsEnumObjectsList.EnableTextureField(EDITOR_TEXTURE_PROPERTY_PATH);
            levelShapesEnumObjectsList = new EnumObjectsList(levelShapesSerializedProperty, TYPE_PROPERTY_PATH, PREFAB_PROPERTY_PATH, LEVEL_SHAPE_ENUM_FILE_PATH, OnBeforeEnumFileupdateCallback, LEVELS_SHAPE_ENUM_NAME);

            levelShapesEnumObjectsList.TabLabel = SHAPES_TAB_LABEL;


            tabHandler = new TabHandler();
            tabHandler.AddTab(new TabHandler.Tab(LEVELS_TAB_NAME, DisplayLevelsTab));
            tabHandler.AddTab(new TabHandler.Tab(ITEMS_TAB_NAME, itemsEnumObjectsList.DisplayTab));
            tabHandler.AddTab(new TabHandler.Tab(LEVEL_SHAPES_TAB_NAME, levelShapesEnumObjectsList.DisplayTab));

            tabHandler.AddTab(new TabHandler.Tab(BALLS_TAB_NAME, DisplayBallsTab)); // hk追加
            tabHandler.AddTab(new TabHandler.Tab(EFFECTS_TAB_NAME, DisplayEffectsTab)); // hk追加

            newLevelIndex = -1;
            currentSideBarWidth = PlayerPrefs.GetInt(PREFS_WIDTH, SIDEBAR_WIDTH);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            defaultTitleContent = new GUIContent(DEFAULT_LEVEL_EDITOR_TITLE);
            modifiedTitleContent = new GUIContent(DEFAULT_LEVEL_EDITOR_TITLE + '*');
        }

        private void BeforeAssemblyReload()
        {
            SaveLevelIfPosssibleAndProceed(false);
            selectedLevelRepresentation = null;
            ClearScene();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (EditorSceneManager.GetActiveScene().name != EDITOR_SCENE_NAME)
            {
                return;
            }

            if (change != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            if (levelsHandler.SelectedLevelIndex == -1)
            {
                OpenScene(GAME_SCENE_PATH);
            }
            else
            {
                TestLevel();
            }
        }

        private void OpenLastActiveLevel()
        {
            if (!lastActiveLevelOpened)
            {
                if ((levelsSerializedProperty.arraySize > 0) && PlayerPrefs.HasKey(PREFS_LEVEL))
                {
                    int levelIndex = Mathf.Clamp(PlayerPrefs.GetInt(PREFS_LEVEL, 0), 0, levelsSerializedProperty.arraySize - 1);
                    levelsHandler.CustomList.SelectedIndex = levelIndex;
                    levelsHandler.OpenLevel(levelIndex);
                }

                lastActiveLevelOpened = true;
            }
        }

        private void AddLevelCallback()
        {
            newLevelIndex = levelsSerializedProperty.arraySize - 1;
        }

        private void RemoveLevelCallback()
        {
            selectedLevelRepresentation = null;
        }

        private void OnBeforeEnumFileupdateCallback()
        {
            enumCompiling = true;
        }

        protected override void Styles()
        {
            if (levelsDatabase != null)
            {
                levelsHandler = new LevelsHandler(levelsDatabaseSerializedObject, levelsSerializedProperty);
                levelsHandler.removeElementCallback += RemoveLevelCallback;
                levelsHandler.addElementCallback += AddLevelCallback;
            }

            if (tabHandler != null)
            {
                tabHandler.SetDefaultToolbarStyle();
            }

            infoIcon = EditorCustomStyles.GetIcon("icon_info");
        }

        public override void OpenLevel(UnityEngine.Object levelObject, int index)
        {
            SaveLevelIfPosssibleAndProceed(false);
            PlayerPrefs.SetInt(PREFS_LEVEL, index);
            PlayerPrefs.Save();
            selectedLevelRepresentation = new LevelRepresentation(levelObject);
            levelsHandler.UpdateCurrentLevelLabel(GetLevelLabel(levelObject, index));
            LoadLevel();
        }

        public override string GetLevelLabel(UnityEngine.Object levelObject, int index)
        {
            LevelRepresentation levelRepresentation = new LevelRepresentation(levelObject);
            return levelRepresentation.GetLevelLabel(index, stringBuilder);
        }

        public override void ClearLevel(UnityEngine.Object levelObject)
        {
            LevelRepresentation levelRepresentation = new LevelRepresentation(levelObject);
            levelRepresentation.Clear();
        }

        protected override void DrawContent()
        {
            if (EditorSceneManager.GetActiveScene().name != EDITOR_SCENE_NAME)
            {
                DrawOpenEditorScene();
                return;
            }

            if (enumCompiling)
            {
                EditorGUILayout.LabelField(COMPILING, EditorCustomStyles.labelLargeBold);
                return;
            }

            tabHandler.DisplayTab();
        }

        private void DrawOpenEditorScene()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.HelpBox(EDITOR_SCENE_NAME + " scene required for level editor.", MessageType.Error, true);

            if (GUILayout.Button("Open \"" + EDITOR_SCENE_NAME + "\" scene"))
            {
                OpenScene(EDITOR_SCENE_PATH);
            }

            EditorGUILayout.EndVertical();
        }

        // hk追加：ボール配置タブの表示（お邪魔・進化・特殊ボールをまとめて管理）
        private void DisplayBallsTab()
        {
            if (selectedLevelRepresentation == null || selectedLevelRepresentation.NullLevel)
            {
                EditorGUILayout.LabelField("レベルを選択してください。");
                return;
            }

            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab");
            if (bubblePrefab == null)
            {
                EditorGUILayout.HelpBox("Bubble.prefab が見つかりません。", MessageType.Error);
                return;
            }

            // hk修正：進化・特殊は「今のレベルのレシピ」から並べる（レシピ依存＝レベルデザインの汎用性）
            RecipeData recipeData = AssetDatabase.LoadAssetAtPath<RecipeData>("Assets/Project Files/Data/HK/RecipeData.asset");
            RecipeEntry recipe = null;
            if (recipeData != null)
            {
                GameLevelData gameLevel = FindGameLevelForCurrentLevel();
                if (gameLevel != null)
                    recipe = recipeData.GetRecipeById(gameLevel.recipeId);
            }

            EditorGUILayout.Space();

            // ── 進化ボール（レシピのevolutionChainの数だけ） ──
            EditorGUILayout.LabelField("── 進化ボール ──", EditorStyles.boldLabel);
            if (recipe == null)
            {
                EditorGUILayout.HelpBox("このレベルのレシピが見つかりません（recipeId未設定？）。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < recipe.evolutionChain.Count; i++)
                {
                    if (GUILayout.Button("進化" + i))
                    {
                        HKSpawnAndRegisterBall(bubblePrefab, BallCategory.Evolution, i, Vector3.zero);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // ── 特殊ボール（レシピのspecialListの数だけ） ──
            EditorGUILayout.LabelField("── 特殊ボール ──", EditorStyles.boldLabel);
            if (recipe != null && recipe.specialList.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < recipe.specialList.Count; i++)
                {
                    if (GUILayout.Button("特殊" + i))
                    {
                        HKSpawnAndRegisterBall(bubblePrefab, BallCategory.Special, i, Vector3.zero);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // ── お邪魔ボール（レシピに紐づかない。種類番号をそのまま） ──
            EditorGUILayout.LabelField("── お邪魔ボール ──", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 5; i++)
            {
                if (GUILayout.Button("お邪魔" + i))
                {
                    HKSpawnAndRegisterBall(bubblePrefab, BallCategory.Nuisance, i, Vector3.zero);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Save Ball Placements"))
            {
                selectedLevelRepresentation.SaveBallPlacements(EditorSceneController.Instance.GetBallPlacements());
                selectedLevelRepresentation.ApplyChanges();
            }

            EditorGUILayout.PropertyField(selectedLevelRepresentation.ballPlacementsProperty, true);
        }
        // hk追加：今エディターで開いているLevelに紐づくGameLevelData（＝レシピ）を探す
        private GameLevelData FindGameLevelForCurrentLevel()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDatabase");
            if (guids.Length == 0) return null;

            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (db == null) return null;

            UnityEngine.Object currentLevel = levelsHandler.SelectedLevelProperty.objectReferenceValue;
            if (currentLevel == null) return null;

            foreach (GameLevelData gl in db.GameLevels)
            {
                if (gl != null && gl.levelDesign == currentLevel)
                    return gl;
            }
            return null;
        }

        // hk追加：ボールをシーンに配置してプロパティにも仮登録するヘルパー
        private void HKSpawnAndRegisterBall(GameObject prefab, BallCategory category, int index, Vector3 position)
        {
            EditorSceneController.Instance.SpawnBallPlacement(prefab, position, category, index);

            int newIndex = selectedLevelRepresentation.ballPlacementsProperty.arraySize;
            selectedLevelRepresentation.ballPlacementsProperty.arraySize++;
            SerializedProperty newElement = selectedLevelRepresentation.ballPlacementsProperty.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("category").intValue = (int)category;
            newElement.FindPropertyRelative("index").intValue = index; // hk修正：branchIndex/ballLevelIndex廃止、indexに一本化
            newElement.FindPropertyRelative("position").vector3Value = position;
            selectedLevelRepresentation.ApplyChanges();
        }

        // hk追加：SpecialEffect配置タブの表示
        // hk追加：SpecialEffect配置タブの表示
        private void DisplayEffectsTab()
        {
            if (selectedLevelRepresentation == null || selectedLevelRepresentation.NullLevel)
            {
                EditorGUILayout.LabelField("レベルを選択してください。");
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(selectedLevelRepresentation.specialEffectsRandomProperty);
            EditorGUILayout.Space();

            if (!selectedLevelRepresentation.specialEffectsRandomProperty.boolValue)
            {
                EditorGUILayout.LabelField("シーン上にSpecialEffectを配置してください。");
                EditorGUILayout.Space();

                if (GUILayout.Button("Ice を配置"))
                {
                    EditorSceneController.Instance.SpawnSpecialEffect(
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab"),
                        Vector3.zero,
                        SpecialEffectType.Ice
                    );
                    int newIndex = selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize;
                    selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize++;
                    SerializedProperty newElement = selectedLevelRepresentation.specialEffectPlacementsProperty.GetArrayElementAtIndex(newIndex);
                    newElement.FindPropertyRelative("type").intValue = (int)SpecialEffectType.Ice;
                    newElement.FindPropertyRelative("position").vector3Value = Vector3.zero;
                    selectedLevelRepresentation.ApplyChanges();
                }

                if (GUILayout.Button("Crate を配置"))
                {
                    EditorSceneController.Instance.SpawnSpecialEffect(
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab"),
                        Vector3.zero,
                        SpecialEffectType.Crate
                    );
                    int newIndex = selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize;
                    selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize++;
                    SerializedProperty newElement = selectedLevelRepresentation.specialEffectPlacementsProperty.GetArrayElementAtIndex(newIndex);
                    newElement.FindPropertyRelative("type").intValue = (int)SpecialEffectType.Crate;
                    newElement.FindPropertyRelative("position").vector3Value = Vector3.zero;
                    selectedLevelRepresentation.ApplyChanges();
                }

                if (GUILayout.Button("Cage を配置"))
                {
                    EditorSceneController.Instance.SpawnSpecialEffect(
                        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab"),
                        Vector3.zero,
                        SpecialEffectType.Cage
                    );
                    int newIndex = selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize;
                    selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize++;
                    SerializedProperty newElement = selectedLevelRepresentation.specialEffectPlacementsProperty.GetArrayElementAtIndex(newIndex);
                    newElement.FindPropertyRelative("type").intValue = (int)SpecialEffectType.Cage;
                    newElement.FindPropertyRelative("position").vector3Value = Vector3.zero;
                    selectedLevelRepresentation.ApplyChanges();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Save Effect Placements"))
                {
                    selectedLevelRepresentation.SaveSpecialEffectPlacements(EditorSceneController.Instance.GetSpecialEffectPlacements());
                    selectedLevelRepresentation.ApplyChanges();
                }
            }

            EditorGUILayout.PropertyField(selectedLevelRepresentation.specialEffectPlacementsProperty, true);
        }

        private void DisplayLevelsTab()
        {
            OpenLastActiveLevel();
            EditorGUILayout.BeginHorizontal();
            //sidebar 
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.MaxWidth(currentSideBarWidth));
            levelsHandler.DisplayReordableList();
            DisplaySidebarButtons();
            EditorGUILayout.EndVertical();

            HandleChangingSideBar();

            //level content
            levelContentRect = EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
            DisplaySelectedLevel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void HandleChangingSideBar()
        {
            separatorRect = EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(0), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            separatorRect.xMin -= GUI.skin.box.margin.right;
            separatorRect.xMax += GUI.skin.box.margin.left;
            EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);


            if (separatorRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    separatorIsDragged = true;
                    levelsHandler.IgnoreDragEvents = true;
                    Event.current.Use();
                }
            }

            if (separatorIsDragged)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    separatorIsDragged = false;
                    levelsHandler.IgnoreDragEvents = false;
                    PlayerPrefs.SetInt(PREFS_WIDTH, currentSideBarWidth);
                    PlayerPrefs.Save();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    currentSideBarWidth = Mathf.RoundToInt(Event.current.delta.x) + currentSideBarWidth;
                    Event.current.Use();
                }
            }
        }

        private void DisplaySidebarButtons()
        {
            if (GUILayout.Button(RENAME_LEVELS, EditorCustomStyles.button))
            {
                if (SaveLevelIfPosssibleAndProceed())
                {
                    levelsHandler.RenameLevels();
                }
            }

            if (GUILayout.Button(OPEN_GAME_SCENE_LABEL, EditorCustomStyles.button))
            {
                if (SaveLevelIfPosssibleAndProceed())
                {
                    selectedLevelRepresentation = null;
                    levelsHandler.ClearSelection();
                    lastActiveLevelOpened = false;
                    OpenScene(GAME_SCENE_PATH);
                }
            }

            if (GUILayout.Button(REMOVE_SELECTION, EditorCustomStyles.button))
            {
                RemoveSelection();
            }
        }

        private void RemoveSelection()
        {
            if (SaveLevelIfPosssibleAndProceed())
            {
                levelsHandler.ClearSelection();
                ClearScene();
            }
        }

        private static void ClearScene()
        {
            if (EditorSceneController.Instance != null)
            {
                EditorSceneController.Instance.Clear();
            }
        }

        private void SetAsCurrentLevel()
        {
            GlobalSave tempSave = SaveController.GetGlobalSave();
            SimpleIntSave level = tempSave.GetSaveObject<SimpleIntSave>("levelSave");

            level.Value = levelsHandler.SelectedLevelIndex;

            SaveController.SaveCustom(tempSave);
        }

        private void DisplaySelectedLevel()
        {
            if (levelsHandler.SelectedLevelIndex == -1)
            {
                return;
            }

            //handle level file field
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(levelsHandler.SelectedLevelProperty, new GUIContent(FILE));

            if (EditorGUI.EndChangeCheck())
            {
                levelsHandler.ReopenLevel();
            }

            if (selectedLevelRepresentation.NullLevel)
            {
                return;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(PREGENERATE_LEVEL, GUILayout.Width(EditorGUIUtility.labelWidth)))
            {
                if (newLevelIndex == levelsHandler.SelectedLevelIndex)
                {
                    PregenerateLevel();
                }
                else if (EditorUtility.DisplayDialog(WARNING_TITLE, PREGENERATE_LEVEL_WARNING, YES, CANCEL))
                {
                    PregenerateLevel();
                }

            }


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(selectedLevelRepresentation.levelShapeTypeProperty);

            if (EditorGUI.EndChangeCheck())
            {
                LoadLevelShape();
            }

            // hk追加：発射直後のすり抜け判定に使う中間ラインを自動計算するボタン
            if (GUILayout.Button("中間位置を計算 (Merge Line Y)"))
            {
                ComputeAndSaveMergeLineY();
            }

            EditorGUILayout.Space();
            DisplaySaveSection();
            itemsListWidthRect = EditorGUILayout.BeginVertical();
            selectedTab = GUILayout.Toolbar(selectedTab, levelTabs);
            EditorGUILayout.EndVertical();


            if (selectedTab == 0)
            {
                DisplayItemsListSection();
            }
            else
            {
                DisplayLevelFields();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(TEST_LEVEL, GUILayout.Width(EditorGUIUtility.labelWidth), GUILayout.Height(30f)))
            {
                TestLevel();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DisplaySaveSection()
        {
            if (EditorSceneController.Instance.IsLevelChanged())
            {
                EditorGUILayout.Space(5f);
                backupColor = GUI.color;
                GUI.color = Color.red;

                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                GUI.color = backupColor;
                EditorGUILayout.LabelField(new GUIContent(infoIcon), GUILayout.MaxWidth(20));
                EditorGUILayout.LabelField("Level have some unsaved changes.");

                if (GUILayout.Button("Discard"))
                {
                    LoadLevel();
                }

                if (GUILayout.Button("Save"))
                {
                    SaveLevel();
                    EditorSceneController.Instance.RegisterLevelState();
                }

                titleContent = modifiedTitleContent;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                titleContent = defaultTitleContent;
            }
        }

        private void TestLevel()
        {
            SaveLevelIfPosssibleAndProceed(false);
            SetAsCurrentLevel();
            OpenScene(GAME_SCENE_PATH);

            LevelAutoRun.EnableAutoRun(levelsHandler.SelectedLevelIndex);

            EditorApplication.isPlaying = true;
        }

        private void DisplayLevelFields()
        {
            EditorGUILayout.PropertyField(selectedLevelRepresentation.bubblesOnTheFieldAmountProperty);

            selectedLevelRepresentation.canBeUsedInRandomizerProperty.boolValue = EditorGUILayout.ToggleLeft(selectedLevelRepresentation.canBeUsedInRandomizerProperty.displayName, selectedLevelRepresentation.canBeUsedInRandomizerProperty.boolValue);
            selectedLevelRepresentation.DisplayProperties();
            selectedLevelRepresentation.UpdateNote();
            levelsHandler.UpdateCurrentLevelLabel(selectedLevelRepresentation.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder));
        }

        private void DisplayItemsListSection()
        {
            EditorGUILayout.LabelField(ITEMS_LABEL);
            //itemsListWidthRect = GUILayoutUtility.GetRect(1, Screen.width, 0, 0, GUILayout.ExpandWidth(true)); //we get this value from toolbar to avoid moving content

            if ((itemsListWidthRect.width > 1) && (Event.current.type == EventType.Repaint))
            {
                currentItemListWidth = itemsListWidthRect.width;
            }

            levelItemsScrollVector = EditorGUILayout.BeginScrollView(levelItemsScrollVector);

            itemsRect = EditorGUILayout.BeginVertical();
            itemPosX = itemsRect.x;
            itemPosY = itemsRect.y;

            //assigning space
            if (itemsSerializedProperty.arraySize != 0)
            {
                itemsPerRow = Mathf.FloorToInt((currentItemListWidth - 16) / (ITEMS_BUTTON_SPACE + ITEMS_BUTTON_WIDTH)); // 16- space for vertical scroll
                rowCount = Mathf.CeilToInt((itemsSerializedProperty.arraySize * 1f) / itemsPerRow);
                GUILayout.Space(rowCount * (ITEMS_BUTTON_SPACE + ITEMS_BUTTON_HEIGHT));
            }

            StartBehaviourMode2D();

            for (int i = 0; i < itemsSerializedProperty.arraySize; i++)
            {
                tempProperty = itemsSerializedProperty.GetArrayElementAtIndex(i);
                tempPropertyLabel = tempProperty.FindPropertyRelative(TYPE_PROPERTY_PATH).enumDisplayNames[tempProperty.FindPropertyRelative(TYPE_PROPERTY_PATH).enumValueIndex];
                prefabAssigned = (tempProperty.FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue != null);
                tempItem = (Item)tempProperty.FindPropertyRelative(TYPE_PROPERTY_PATH).enumValueIndex;
                tempTexture = tempProperty.FindPropertyRelative(EDITOR_TEXTURE_PROPERTY_PATH).objectReferenceValue as Texture2D;
                tempGameobject = (GameObject)tempProperty.FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue;

                if (tempTexture == null)
                {
                    if (prefabAssigned)
                    {
                        if (AssetPreview.GetAssetPreview(tempGameobject) == null)
                        {
                            if (AssetPreview.IsLoadingAssetPreview(tempGameobject.GetInstanceID()))
                            {
                                itemContent = new GUIContent(AssetPreview.GetAssetPreview(tempGameobject), tempPropertyLabel);
                            }
                            else
                            {
                                itemContent = new GUIContent(AssetPreview.GetMiniThumbnail(tempGameobject), tempPropertyLabel);
                            }
                        }
                        else
                        {
                            itemContent = new GUIContent(AssetPreview.GetAssetPreview(tempGameobject), tempPropertyLabel);
                        }
                    }
                    else
                    {
                        itemContent = new GUIContent(tempPropertyLabel, ITEM_UNASSIGNED_ERROR);
                    }
                }
                else
                {
                    itemContent = new GUIContent(tempTexture, tempPropertyLabel);
                }

                //check if need to start new row
                if (itemPosX + ITEMS_BUTTON_SPACE + ITEMS_BUTTON_WIDTH > currentItemListWidth - 16)
                {
                    itemPosX = itemsRect.x;
                    itemPosY = itemPosY + ITEMS_BUTTON_HEIGHT + ITEMS_BUTTON_SPACE;
                }

                itemRect = new Rect(itemPosX, itemPosY, ITEMS_BUTTON_WIDTH, ITEMS_BUTTON_HEIGHT);

                EditorGUI.BeginDisabledGroup(!prefabAssigned);

                if (GUI.Button(itemRect, itemContent, EditorCustomStyles.button))
                {
                    EditorSceneController.Instance.Spawn(tempGameobject, Vector3.zero, tempItem);
                }

                EditorGUI.EndDisabledGroup();

                itemPosX += ITEMS_BUTTON_SPACE + ITEMS_BUTTON_WIDTH;
            }

            EndBehaviourMode2D();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void LoadLevel()
        {
            if (selectedLevelRepresentation == null || selectedLevelRepresentation.NullLevel) return; // hk追加

            EditorSceneController.Instance.Clear();

            LoadLevelShape();
            LoadLevelBackground();
            LoadLevelItems();
            LoadBallPlacements(); // hk追加
            LoadSpecialEffects(); // hk追加
            EditorSceneController.Instance.RegisterLevelState();
        }
        // hk追加：配置ボールをシーンに復元する
        private void LoadBallPlacements()
        {
            if (selectedLevelRepresentation.ballPlacementsProperty.arraySize == 0) return;

            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab");
            if (bubblePrefab == null) return;

            for (int i = 0; i < selectedLevelRepresentation.ballPlacementsProperty.arraySize; i++)
            {
                SerializedProperty element = selectedLevelRepresentation.ballPlacementsProperty.GetArrayElementAtIndex(i);
                BallCategory category = (BallCategory)element.FindPropertyRelative("category").intValue;
                int index = element.FindPropertyRelative("index").intValue; // hk修正：branchIndex/ballLevelIndex廃止、indexに一本化
                Vector3 position = element.FindPropertyRelative("position").vector3Value;
                EditorSceneController.Instance.SpawnBallPlacement(bubblePrefab, position, category, index);
            }
        }

        // hk追加：SpecialEffectをシーンに復元する
        private void LoadSpecialEffects()
        {
            if (selectedLevelRepresentation.specialEffectsRandomProperty.boolValue) return;

            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project Files/Game/Prefabs/Bubble.prefab");
            if (bubblePrefab == null) return;

            for (int i = 0; i < selectedLevelRepresentation.specialEffectPlacementsProperty.arraySize; i++)
            {
                SerializedProperty element = selectedLevelRepresentation.specialEffectPlacementsProperty.GetArrayElementAtIndex(i);
                SpecialEffectType type = (SpecialEffectType)element.FindPropertyRelative("type").intValue;
                Vector3 position = element.FindPropertyRelative("position").vector3Value;
                EditorSceneController.Instance.SpawnSpecialEffect(bubblePrefab, position, type);
            }
        }

        private void LoadLevelShape()
        {
            LevelShapeType shapeType = (LevelShapeType)selectedLevelRepresentation.levelShapeTypeProperty.intValue;
            GameObject prefab = GetLevelShapePrefab(shapeType);

            if (prefab != null)
            {
                EditorSceneController.Instance.SpawnLevelShape(prefab);
            }
        }

        private void LoadLevelBackground()
        {
            // hk修正：Level.levelBackTypeを廃止したため、エディターでの背景下敷き表示を撤去。
        }

        private void LoadLevelItems()
        {
            ItemSave tempItemSave;

            for (int i = 0; i < selectedLevelRepresentation.itemsProperty.arraySize; i++)
            {
                tempItemSave = PropertyToItemSave(i);
                EditorSceneController.Instance.Spawn(tempItemSave, GetItemPrefab(tempItemSave.Type));
            }
        }

        private bool SaveLevelIfPosssibleAndProceed(bool canUseCancel = true) //true == proceed 
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != EDITOR_SCENE_NAME)
            {
                return true;
            }

            if (selectedLevelRepresentation == null)
            {
                return true;
            }

            if (selectedLevelRepresentation.NullLevel)
            {
                return true;
            }

            if (EditorSceneController.Instance.IsLevelChanged())
            {
                if (canUseCancel)
                {
                    int optionIndex = EditorUtility.DisplayDialogComplex($"Level was modified", "Do you want to save the changes ?", "Save", "Cancel", "Don`t save");

                    if (optionIndex == 0) //save
                    {
                        SaveLevel();
                        levelsHandler.SetLevelLabels();
                        return true;
                    }
                    else if (optionIndex == 1) //Cancel
                    {
                        return false;
                    }
                    else // don`t save
                    {
                        return true;
                    }
                }
                else
                {
                    if (EditorUtility.DisplayDialog($"Level was modified", "Do you want to save the changes ?", "Save", "Don`t save"))
                    {
                        SaveLevel();
                    }

                    return true;
                }

            }
            else
            {
                selectedLevelRepresentation.ApplyChanges();
                levelsHandler.UpdateCurrentLevelLabel(selectedLevelRepresentation.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder));
                AssetDatabase.SaveAssets();
            }

            return true;
        }

        private void SaveLevel()
        {
            SaveLevelItems();

            selectedLevelRepresentation.UpdateNote();
            levelsHandler.UpdateCurrentLevelLabel(selectedLevelRepresentation.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder));
            AssetDatabase.SaveAssets();
        }

        private void SaveLevelItems()
        {
            ItemSave[] levelItems = EditorSceneController.Instance.GetLevelItems();
            selectedLevelRepresentation.itemsProperty.arraySize = levelItems.Length;

            for (int i = 0; i < levelItems.Length; i++)
            {
                ItemSaveToProperty(levelItems[i], i);
            }
        }

        private void ItemSaveToProperty(ItemSave levelItem, int index)
        {
            currentLevelItemProperty = selectedLevelRepresentation.itemsProperty.GetArrayElementAtIndex(index);
            currentLevelItemProperty.FindPropertyRelative(TYPE_PROPERTY_PATH).intValue = (int)levelItem.Type;
            currentLevelItemProperty.FindPropertyRelative(POSITION_PROPERTY_PATH).vector3Value = levelItem.Position;
            currentLevelItemProperty.FindPropertyRelative(ROTATION_PROPERTY_PATH).vector3Value = levelItem.Rotation;
            currentLevelItemProperty.FindPropertyRelative(SCALE_PROPERTY_PATH).vector3Value = levelItem.Scale;
        }

        private ItemSave PropertyToItemSave(int index)
        {
            currentLevelItemProperty = selectedLevelRepresentation.itemsProperty.GetArrayElementAtIndex(index);
            return new ItemSave(
                (Item)currentLevelItemProperty.FindPropertyRelative(TYPE_PROPERTY_PATH).intValue,
                currentLevelItemProperty.FindPropertyRelative(POSITION_PROPERTY_PATH).vector3Value,
                currentLevelItemProperty.FindPropertyRelative(ROTATION_PROPERTY_PATH).vector3Value,
                currentLevelItemProperty.FindPropertyRelative(SCALE_PROPERTY_PATH).vector3Value);
        }

        private GameObject GetItemPrefab(Item item)
        {
            for (int i = 0; i < itemsSerializedProperty.arraySize; i++)
            {
                if ((Item)itemsSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(TYPE_PROPERTY_PATH).intValue == item)
                {
                    return (GameObject)itemsSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue;
                }
            }

            Debug.LogError("GetItemPrefab element not found");
            return null;
        }

        private GameObject GetLevelShapePrefab(LevelShapeType levelShapeType)
        {
            for (int i = 0; i < levelShapesSerializedProperty.arraySize; i++)
            {
                if ((LevelShapeType)levelShapesSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(TYPE_PROPERTY_PATH).intValue == levelShapeType)
                {
                    return (GameObject)levelShapesSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue;
                }
            }

            Debug.LogError("GetLevelShapePrefab element not found");
            return null;
        }

        // hk追加：選択中のLevel Shape Typeに対応するシェイプのprefabから、壁の当たり判定を集めて中間の高さを計算し、mergeLineYに書き込む
        private void ComputeAndSaveMergeLineY()
        {
            LevelShapeType shapeType = (LevelShapeType)selectedLevelRepresentation.levelShapeTypeProperty.intValue;

            for (int i = 0; i < levelShapesSerializedProperty.arraySize; i++)
            {
                SerializedProperty shapeElement = levelShapesSerializedProperty.GetArrayElementAtIndex(i);

                if ((LevelShapeType)shapeElement.FindPropertyRelative(TYPE_PROPERTY_PATH).intValue != shapeType)
                    continue;

                GameObject prefab = (GameObject)shapeElement.FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue;

                if (prefab == null)
                {
                    Debug.LogError("ComputeAndSaveMergeLineY: prefabが未設定です");
                    return;
                }

                Collider2D[] colliders = prefab.GetComponentsInChildren<Collider2D>();

                if (colliders.Length == 0)
                {
                    Debug.LogError("ComputeAndSaveMergeLineY: Collider2Dが見つかりません");
                    return;
                }

                float minY = float.MaxValue;
                float maxY = float.MinValue;

                foreach (Collider2D collider in colliders)
                {
                    minY = Mathf.Min(minY, collider.bounds.min.y);
                    maxY = Mathf.Max(maxY, collider.bounds.max.y);
                }

                float middleY = (minY + maxY) / 2f;

                shapeElement.FindPropertyRelative("mergeLineY").floatValue = middleY;
                levelShapesSerializedProperty.serializedObject.ApplyModifiedProperties();

                Debug.Log($"中間位置を計算しました: {middleY}");
                return;
            }

            Debug.LogError("ComputeAndSaveMergeLineY: 該当するシェイプが見つかりません");
        }

        private GameObject GetLevelBackgroundPrefab(LevelBackgroundType levelBackgroundType)
        {
            if (levelBackgroundsSerializedProperty == null)
            {
                return null;
            }

            for (int i = 0; i < levelBackgroundsSerializedProperty.arraySize; i++)
            {
                if ((LevelBackgroundType)levelBackgroundsSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(TYPE_PROPERTY_PATH).intValue == levelBackgroundType)
                {
                    return (GameObject)levelBackgroundsSerializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative(PREFAB_PROPERTY_PATH).objectReferenceValue;
                }
            }

            Debug.LogError("GetLevelBackgroundPrefab element not found");
            return null;
        }

        private void OnDestroy()
        {
            SaveLevelIfPosssibleAndProceed(false);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                OpenScene(GAME_SCENE_PATH);
            }
        }

        private void PregenerateLevel()
        {

            selectedLevelRepresentation.levelShapeTypeProperty.enumValueIndex = UnityEngine.Random.Range(0, levelShapesSerializedProperty.arraySize);
            selectedLevelRepresentation.levelBackTypeProperty.enumValueIndex = UnityEngine.Random.Range(0, levelBackgroundsSerializedProperty.arraySize);
            selectedLevelRepresentation.bubblesOnTheFieldAmountProperty.intValue = UnityEngine.Random.Range(5, 10);

            // hk修正：レシピ・氷・箱・爆弾の自動生成は削除（turnsLimit/requirementsがLevelから無くなったため）

            selectedTab = 1;
            LoadLevelShape();
            LoadLevelBackground();

            selectedLevelRepresentation.UpdateNote();
            levelsHandler.UpdateCurrentLevelLabel(selectedLevelRepresentation.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder));
        }


        protected class LevelRepresentation : LevelRepresentationBase
        {

            private const string NOTE_PROPERTY_NAME = "note";

            private const string BUBBLES_ON_THE_FIELD_PROPERTY_NAME = "bubblesOnTheFieldAmount";

            private const string CAN_BE_USED_IN_RANDOMIZER_PROPERTY_NAME = "canBeUsedInRandomizer";

            private const string ITEMS_PROPERTY_NAME = "items";
            private const string LEVEL_SHAPE_TYPE_PROPERTY_NAME = "levelShapeType";
            private const string LEVEL_BACK_TYPE_PROPERTY_NAME = "levelBackType";

            private const string BALL_PLACEMENTS_PROPERTY_NAME = "ballPlacements"; // hk追加
            private const string SPECIAL_EFFECTS_RANDOM_PROPERTY_NAME = "specialEffectsRandom"; // hk追加
            private const string SPECIAL_EFFECT_PLACEMENTS_PROPERTY_NAME = "specialEffectPlacements"; // hk追加
            public SerializedProperty noteProperty;

            public SerializedProperty bubblesOnTheFieldAmountProperty;
            public SerializedProperty canBeUsedInRandomizerProperty;
            public SerializedProperty itemsProperty;
            public SerializedProperty levelShapeTypeProperty;
            public SerializedProperty levelBackTypeProperty;
            public SerializedProperty ballPlacementsProperty; // hk追加
            public SerializedProperty specialEffectsRandomProperty; // hk追加
            public SerializedProperty specialEffectPlacementsProperty; // hk追加

            //this empty constructor is nessesary
            public LevelRepresentation(UnityEngine.Object levelObject) : base(levelObject)
            {
            }


            protected override void ReadFields()
            {
                noteProperty = serializedLevelObject.FindProperty(NOTE_PROPERTY_NAME);
                bubblesOnTheFieldAmountProperty = serializedLevelObject.FindProperty(BUBBLES_ON_THE_FIELD_PROPERTY_NAME);
                canBeUsedInRandomizerProperty = serializedLevelObject.FindProperty(CAN_BE_USED_IN_RANDOMIZER_PROPERTY_NAME);
                itemsProperty = serializedLevelObject.FindProperty(ITEMS_PROPERTY_NAME);
                levelShapeTypeProperty = serializedLevelObject.FindProperty(LEVEL_SHAPE_TYPE_PROPERTY_NAME);
                ballPlacementsProperty = serializedLevelObject.FindProperty(BALL_PLACEMENTS_PROPERTY_NAME); // hk追加
                specialEffectsRandomProperty = serializedLevelObject.FindProperty(SPECIAL_EFFECTS_RANDOM_PROPERTY_NAME); // hk追加
                specialEffectPlacementsProperty = serializedLevelObject.FindProperty(SPECIAL_EFFECT_PLACEMENTS_PROPERTY_NAME); // hk追加
            }



            public override void Clear()
            {
                if (!NullLevel)
                {
                    noteProperty.stringValue = string.Empty;
                    bubblesOnTheFieldAmountProperty.intValue = 0;
                    canBeUsedInRandomizerProperty.boolValue = true;
                    itemsProperty.arraySize = 0;
                    levelShapeTypeProperty.enumValueIndex = 0;

                    ApplyChanges();
                }
            }

            public void UpdateNote()
            {
                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append(levelShapeTypeProperty.enumNames[levelShapeTypeProperty.enumValueIndex]);

                GetItemsString(stringBuilder);

                noteProperty.stringValue = stringBuilder.ToString();
                ApplyChanges();
            }

            public void GetItemsString(StringBuilder stringBuilder)
            {
                if (itemsProperty.arraySize == 0)
                {
                    return;
                }

                stringBuilder.Append(" + ");

                int currentType;
                int previousType = -1;
                int counter = 0;
                SerializedProperty type = null;

                for (int i = 0; i < itemsProperty.arraySize; i++)
                {
                    type = itemsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(TYPE_PROPERTY_PATH);
                    currentType = type.enumValueIndex;

                    if (currentType == previousType)
                    {
                        counter++;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            previousType = currentType;
                            counter = 1;
                        }
                        else
                        {
                            stringBuilder.Append("x" + counter + " ");
                            stringBuilder.Append(type.enumNames[previousType]);
                            stringBuilder.Append(" + ");
                            previousType = currentType;
                            counter = 1;
                        }
                    }
                }

                stringBuilder.Append("x" + counter + " ");
                stringBuilder.Append(type.enumNames[previousType]);
            }

            // hk追加：ボール配置を保存する
            // hk追加：ボール配置を保存する
            public void SaveBallPlacements(BallPlacementHK[] placements)
            {
                ballPlacementsProperty.arraySize = placements.Length;
                for (int i = 0; i < placements.Length; i++)
                {
                    SerializedProperty element = ballPlacementsProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("category").intValue = (int)placements[i].category;
                    element.FindPropertyRelative("index").intValue = placements[i].index; // hk修正：branchIndex/ballLevelIndex廃止、indexに一本化
                    element.FindPropertyRelative("position").vector3Value = placements[i].position;
                }
            }

            // hk追加：SpecialEffect配置を保存する
            public void SaveSpecialEffectPlacements(SpecialEffectSaveHK[] placements)
            {
                specialEffectPlacementsProperty.arraySize = placements.Length;
                for (int i = 0; i < placements.Length; i++)
                {
                    SerializedProperty element = specialEffectPlacementsProperty.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("type").intValue = (int)placements[i].type;
                    element.FindPropertyRelative("position").vector3Value = placements[i].position;
                }
            }



            public override string GetLevelLabel(int index, StringBuilder stringBuilder)
            {
                if (NullLevel)
                {
                    return base.GetLevelLabel(index, stringBuilder);
                }
                else
                {
                    stringBuilder.Clear();
                    stringBuilder.Append(NUMBER);
                    stringBuilder.Append(index + 1);
                    stringBuilder.Append(SEPARATOR);
                    stringBuilder.Append(levelObject.name);
                    return stringBuilder.ToString();
                }
            }
        }
    }
}