using UnityEngine;
using UnityEditor;

namespace Watermelon.BubbleMerge
{
    // hk追加：RecipeDataのInspectorを見やすくする
    [CustomEditor(typeof(RecipeData))]
    public class RecipeDataEditor : Editor
    {
        private const string BALLDATA_PATH = "Assets/Project Files/Data/HK/BallData.asset";
        private BallData cachedBallData;

        // ランク名の選択肢（データは英語で保存）
        private static readonly string[] RANK_NAMES = { "Masterpiece", "Delicious", "Good", "Mediocre", "Bad" };

        public override void OnInspectorGUI()
        {
            if (cachedBallData == null)
                cachedBallData = AssetDatabase.LoadAssetAtPath<BallData>(BALLDATA_PATH);

            SerializedProperty recipes = serializedObject.FindProperty("recipes");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("＋ 完成料理を追加"))
            {
                recipes.arraySize++;
            }
            if (GUILayout.Button("－ 最後を削除") && recipes.arraySize > 0)
            {
                recipes.arraySize--;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            for (int i = 0; i < recipes.arraySize; i++)
            {
                SerializedProperty recipe = recipes.GetArrayElementAtIndex(i);

                EditorGUILayout.Space(12);
                Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
                EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 1f));
                EditorGUILayout.Space(6);

                SerializedProperty idProp = recipe.FindPropertyRelative("recipeId");
                SerializedProperty nameProp = recipe.FindPropertyRelative("recipeName");
                EditorGUILayout.LabelField("完成料理 ID:" + idProp.intValue + "  " + nameProp.stringValue, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(idProp, new GUIContent("料理ID（重複禁止）"));
                EditorGUILayout.PropertyField(nameProp, new GUIContent("料理名"));

                // ID重複チェック
                int dupCount = 0;
                for (int k = 0; k < recipes.arraySize; k++)
                {
                    if (recipes.GetArrayElementAtIndex(k).FindPropertyRelative("recipeId").intValue == idProp.intValue)
                        dupCount++;
                }
                if (dupCount > 1)
                {
                    EditorGUILayout.HelpBox("料理IDが重複しています： " + idProp.intValue, MessageType.Error);
                }

                // 進化の枠（名前表示つき）
                SerializedProperty chain = recipe.FindPropertyRelative("evolutionChain");
                EditorGUILayout.LabelField("進化の枠（最後＝イレギュラー素材）", EditorStyles.boldLabel);
                for (int c = 0; c < chain.arraySize; c++)
                {
                    SerializedProperty numProp = chain.GetArrayElementAtIndex(c);
                    string ballName = GetBallName(BallCategory.Evolution, numProp.intValue);
                    bool isLast = (c == chain.arraySize - 1);
                    string tag = isLast ? "（イレギュラー素材）" : "";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("段階 " + c, GUILayout.Width(60));
                    numProp.intValue = EditorGUILayout.IntField(numProp.intValue, GUILayout.Width(40));
                    EditorGUILayout.LabelField(ballName + tag, GUILayout.Width(200));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        chain.DeleteArrayElementAtIndex(c);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("＋ 進化段階を追加"))
                {
                    chain.arraySize++;
                }

                // レシピを進化の枠に自動追従
                SyncRequiredList(recipe, chain);

                // 進化ボールのレシピ
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("通常レシピ（個数を入力。0＝使わない）", EditorStyles.boldLabel);
                SerializedProperty requiredList = recipe.FindPropertyRelative("requiredList");
                for (int r = 0; r < requiredList.arraySize; r++)
                {
                    SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                    int number = item.FindPropertyRelative("number").intValue;
                    string ballName = GetBallName(BallCategory.Evolution, number);
                    SerializedProperty count = item.FindPropertyRelative("count");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("No." + number + "  " + ballName, GUILayout.Width(200));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("個数", GUILayout.Width(30));
                    count.intValue = EditorGUILayout.IntField(count.intValue, GUILayout.Width(60));
                    GUILayout.Space(54); // 削除ボタン分のスペースを空けて個数の列をそろえる
                    EditorGUILayout.EndHorizontal();
                }

                // 特殊ボールのレシピ
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("特殊ボールのレシピ（この料理に固定）", EditorStyles.boldLabel);
                SerializedProperty specialList = recipe.FindPropertyRelative("specialList");
                for (int s = 0; s < specialList.arraySize; s++)
                {
                    SerializedProperty item = specialList.GetArrayElementAtIndex(s);
                    SerializedProperty numProp = item.FindPropertyRelative("number");
                    SerializedProperty count = item.FindPropertyRelative("count");
                    string ballName = GetBallName(BallCategory.Special, numProp.intValue);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("特殊No.", GUILayout.Width(50));
                    numProp.intValue = EditorGUILayout.IntField(numProp.intValue, GUILayout.Width(40));
                    EditorGUILayout.LabelField(ballName, GUILayout.Width(110));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("個数", GUILayout.Width(30));
                    count.intValue = EditorGUILayout.IntField(count.intValue, GUILayout.Width(60));
                    GUILayout.Space(4);
                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        specialList.DeleteArrayElementAtIndex(s);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("＋ 特殊ボールを追加"))
                {
                    specialList.arraySize++;
                }

                // 裏メニュー（通常レシピの直下）
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("── 裏メニュー（テリブル）──", EditorStyles.boldLabel);
                SerializedProperty hasSecret = recipe.FindPropertyRelative("hasSecret");
                EditorGUILayout.PropertyField(hasSecret, new GUIContent("裏メニュー有り"));
                if (hasSecret.boolValue)
                {
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretNuisanceCount"), new GUIContent("お邪魔の必要数"));
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretIrregularCount"), new GUIContent("イレギュラー素材の必要数"));
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretPrefab"), new GUIContent("Anomalyの演出プレハブ"));
                }

                // 完成データ（ランク名は選択式）
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("完成データ", EditorStyles.boldLabel);
                SerializedProperty stages = recipe.FindPropertyRelative("completionStages");
                for (int st = 0; st < stages.arraySize; st++)
                {
                    SerializedProperty stage = stages.GetArrayElementAtIndex(st);
                    SerializedProperty rankName = stage.FindPropertyRelative("rankName");
                    SerializedProperty minScore = stage.FindPropertyRelative("minScore");
                    SerializedProperty prefab = stage.FindPropertyRelative("prefab");

                    EditorGUILayout.BeginHorizontal();
                    int current = System.Array.IndexOf(RANK_NAMES, rankName.stringValue);
                    if (current < 0) current = 0;
                    int selected = EditorGUILayout.Popup(current, RANK_NAMES, GUILayout.Width(90));
                    rankName.stringValue = RANK_NAMES[selected];

                    EditorGUILayout.LabelField("点数下限", GUILayout.Width(55));
                    minScore.intValue = EditorGUILayout.IntField(minScore.intValue, GUILayout.Width(60));
                    EditorGUILayout.PropertyField(prefab, GUIContent.none, GUILayout.Width(120));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(50)))
                    {
                        stages.DeleteArrayElementAtIndex(st);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("＋ 完成データを追加"))
                {
                    stages.arraySize++;
                }

                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private string GetBallName(BallCategory category, int number)
        {
            if (cachedBallData == null) return "（BallData未検出）";
            BallEntry entry = cachedBallData.GetBall(category, number);
            return entry != null ? entry.ballName : "（該当なし）";
        }

        private void SyncRequiredList(SerializedProperty recipe, SerializedProperty chain)
        {
            SerializedProperty requiredList = recipe.FindPropertyRelative("requiredList");

            int usableCount = chain.arraySize - 1;
            if (usableCount < 0) usableCount = 0;

            var savedCounts = new System.Collections.Generic.Dictionary<int, int>();
            for (int r = 0; r < requiredList.arraySize; r++)
            {
                SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                int num = item.FindPropertyRelative("number").intValue;
                int cnt = item.FindPropertyRelative("count").intValue;
                if (!savedCounts.ContainsKey(num)) savedCounts[num] = cnt;
            }

            requiredList.arraySize = usableCount;
            for (int r = 0; r < usableCount; r++)
            {
                int number = chain.GetArrayElementAtIndex(r).intValue;
                SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                item.FindPropertyRelative("number").intValue = number;
                item.FindPropertyRelative("count").intValue = savedCounts.ContainsKey(number) ? savedCounts[number] : 0;
            }
        }
    }
}