using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace Watermelon.BubbleMerge
{
    // hk追加：RecipeDataのInspectorを見やすくする
    [CustomEditor(typeof(RecipeData))]
    public class RecipeDataEditor : Editor
    {
        private const string BALLDATA_PATH = "Assets/Project Files/Data/HK/BallData.asset";
        private BallData cachedBallData;

        private const int BlockRows = 20; // 書き出しも読み込みと同じ20行ブロック

        // ランク名の選択肢（データは英語で保存）
        private static readonly string[] RANK_NAMES = { "Masterpiece", "Delicious", "Good", "Mediocre", "Bad" };

        public override void OnInspectorGUI()
        {
            if (cachedBallData == null)
                cachedBallData = AssetDatabase.LoadAssetAtPath<BallData>(BALLDATA_PATH);

            SerializedProperty recipes = serializedObject.FindProperty("recipes");

            // hk追加：CSV読み込み・書き出しボタン（既存UIの一番上に足す）
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("CSVから読み込み（置き換え）"))
            {
                ImportFromCsv();
            }
            if (GUILayout.Button("CSVに書き出し"))
            {
                ExportToCsv();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

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
                EditorGUILayout.PropertyField(idProp, new GUIContent("レシピID（重複禁止）"));
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
                    EditorGUILayout.HelpBox("レシピIDが重複しています： " + idProp.intValue, MessageType.Error);
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

        // hk追加：CSVファイルを選んで読み込み、今の料理リストを置き換える
        private void ImportFromCsv()
        {
            string path = EditorUtility.OpenFilePanel("レシピCSVを選択", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(path)) return;

            // 押し間違いで消えないよう、置き換え前に確認をはさむ
            bool ok = EditorUtility.DisplayDialog(
                "CSV読み込み",
                "今の料理リストをすべて消して、CSVの内容で置き換えます。よろしいですか？",
                "置き換える", "やめる");
            if (!ok) return;

            string csvText = File.ReadAllText(path);
            var parsed = RecipeDataCSV.Parse(csvText);

            RecipeData data = (RecipeData)target;
            Undo.RecordObject(data, "Import Recipe CSV"); // Ctrl+Zで戻せるようにする

            // privateなrecipesにアクセスするため、SerializedObject経由で書き換える
            SerializedProperty recipes = serializedObject.FindProperty("recipes");
            recipes.ClearArray();
            serializedObject.ApplyModifiedProperties();

            // 読み込んだ料理を1件ずつ入れ直す
            for (int i = 0; i < parsed.Count; i++)
            {
                recipes.serializedObject.Update();
                recipes.arraySize = i + 1;

                SerializedProperty r = recipes.GetArrayElementAtIndex(i);
                RecipeEntry src = parsed[i];

                r.FindPropertyRelative("recipeId").intValue = src.recipeId;
                r.FindPropertyRelative("recipeName").stringValue = src.recipeName;
                r.FindPropertyRelative("hasSecret").boolValue = src.hasSecret;
                r.FindPropertyRelative("secretNuisanceCount").intValue = src.secretNuisanceCount;
                r.FindPropertyRelative("secretIrregularCount").intValue = src.secretIrregularCount;

                CopyIntList(r.FindPropertyRelative("evolutionChain"), src.evolutionChain);
                CopyRequiredList(r.FindPropertyRelative("requiredList"), src.requiredList);
                CopyRequiredList(r.FindPropertyRelative("specialList"), src.specialList);
                CopyCompletionStages(r.FindPropertyRelative("completionStages"), src.completionStages);

                recipes.serializedObject.ApplyModifiedProperties();
            }

            serializedObject.Update();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Debug.Log("レシピCSVを読み込みました： " + parsed.Count + "件");
        }

        // hk追加：今の料理リストを、読み込みと同じ20行ブロックのCSVに書き出す
        private void ExportToCsv()
        {
            string path = EditorUtility.SaveFilePanel("レシピCSVを書き出し", Application.dataPath, "recipes", "csv");
            if (string.IsNullOrEmpty(path)) return;

            RecipeData data = (RecipeData)target;
            StringBuilder sb = new StringBuilder();

            var recipeList = data.GetRecipeListForExport();
            foreach (RecipeEntry entry in recipeList)
            {
                WriteBlock(sb, entry);
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); // BOM付きUTF-8（Excel文字化け対策）
            Debug.Log("レシピCSVを書き出しました： " + recipeList.Count + "件");
        }

        // hk追加：料理1つを20行ブロックとして書く
        private void WriteBlock(StringBuilder sb, RecipeEntry entry)
        {
            // 見出し3行（案1：簡素な固定文字。読み込みは3行飛ばすだけなので中身は自由）
            sb.AppendLine("recipeName,recipeId,進化の枠,進化ボールNo,名前,通常個数,特殊No,特殊個数,裏メニュー,お邪魔個数,イレギュラー個数,ランク,点数");
            sb.AppendLine(",,,,,,,,,,,,");
            sb.AppendLine(",,,,,,,,,,,,");

            // データ17行分を作る（段階0〜16。実際は使う分だけ埋める）
            int dataRows = BlockRows - 3; // 20 - 見出し3 = 17
            for (int row = 0; row < dataRows; row++)
            {
                string[] cells = new string[13]; // A〜M
                for (int c = 0; c < cells.Length; c++) cells[c] = "";

                // 先頭データ行にだけ、料理名・ID・裏メニューを書く
                if (row == 0)
                {
                    cells[0] = Escape(entry.recipeName);
                    cells[1] = entry.recipeId.ToString();
                    cells[8] = entry.hasSecret ? "TRUE" : "FALSE";
                    cells[9] = entry.secretNuisanceCount.ToString();
                    cells[10] = entry.secretIrregularCount.ToString();
                }

                // 進化の枠（D列）と通常レシピの個数（F列）
                if (row < entry.evolutionChain.Count)
                {
                    int number = entry.evolutionChain[row];
                    cells[3] = number.ToString();
                    cells[4] = "日本語"; // E列は飾り（読み込みでは使わない）

                    // その段階の番号に対応する通常レシピの個数を探して入れる
                    int count = FindRequiredCount(entry, number);
                    if (count > 0) cells[5] = count.ToString();
                }

                // 特殊ボール（G・H列）を上から順に
                if (row < entry.specialList.Count)
                {
                    cells[6] = entry.specialList[row].number.ToString();
                    cells[7] = entry.specialList[row].count.ToString();
                }

                // 完成データ（L・M列）を上から順に
                if (row < entry.completionStages.Count)
                {
                    cells[11] = Escape(entry.completionStages[row].rankName);
                    cells[12] = entry.completionStages[row].minScore.ToString();
                }

                sb.AppendLine(string.Join(",", cells));
            }
        }

        // hk追加：ある進化番号に対応する通常レシピの個数を返す（無ければ0）
        private int FindRequiredCount(RecipeEntry entry, int number)
        {
            foreach (RequiredItem item in entry.requiredList)
            {
                if (item.number == number) return item.count;
            }
            return 0;
        }

        // hk追加：カンマや引用符が入っても壊れないように包む
        private string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        // hk追加：int（番号）のリストをコピー
        private void CopyIntList(SerializedProperty listProp, System.Collections.Generic.List<int> src)
        {
            listProp.ClearArray();
            listProp.arraySize = src.Count;
            for (int i = 0; i < src.Count; i++)
                listProp.GetArrayElementAtIndex(i).intValue = src[i];
        }

        // hk追加：RequiredItem（番号＋個数）のリストをコピー
        private void CopyRequiredList(SerializedProperty listProp, System.Collections.Generic.List<RequiredItem> src)
        {
            listProp.ClearArray();
            listProp.arraySize = src.Count;
            for (int i = 0; i < src.Count; i++)
            {
                SerializedProperty item = listProp.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("number").intValue = src[i].number;
                item.FindPropertyRelative("count").intValue = src[i].count;
            }
        }

        // hk追加：CompletionStage（ランク名＋点数）のリストをコピー。プレハブはCSVに無いので触らない
        private void CopyCompletionStages(SerializedProperty listProp, System.Collections.Generic.List<CompletionStage> src)
        {
            listProp.ClearArray();
            listProp.arraySize = src.Count;
            for (int i = 0; i < src.Count; i++)
            {
                SerializedProperty stage = listProp.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("rankName").stringValue = src[i].rankName;
                stage.FindPropertyRelative("minScore").intValue = src[i].minScore;
            }
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