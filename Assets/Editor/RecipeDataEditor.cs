// RecipeDataEditor.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

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

        // hk追加：折りたたみの開閉状態を覚える（キー＝レシピの並び順）。無いキーは「開いている」扱い
        private Dictionary<int, bool> openRecipe = new Dictionary<int, bool>();       // レシピ全体
        private Dictionary<int, bool> openEvolution = new Dictionary<int, bool>();    // 進化の枠
        private Dictionary<int, bool> openRecipeBody = new Dictionary<int, bool>();   // 通常/特殊/裏
        private Dictionary<int, bool> openCompletion = new Dictionary<int, bool>();   // 完成データ

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

                // hk追加：レシピ全体の折りたたみ（親）。見出しにID・料理名を出す
                string recipeTitle = "完成料理 ID:" + idProp.intValue + "  " + nameProp.stringValue;
                bool recipeOpen = DrawFoldout(openRecipe, i, recipeTitle, true);
                if (!recipeOpen)
                {
                    EditorGUILayout.Space();
                    continue; // 閉じているなら見出しだけ出して次のレシピへ
                }

                EditorGUILayout.PropertyField(idProp, new GUIContent("レシピID（重複禁止）"));
                EditorGUILayout.PropertyField(nameProp, new GUIContent("料理名"));
                EditorGUILayout.PropertyField(recipe.FindPropertyRelative("recipeNameRomaji"), new GUIContent("ローマ字（フォルダ名用）")); // hk追加

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

                // hk追加：進化の枠の折りたたみ（子）
                SerializedProperty chain = recipe.FindPropertyRelative("evolutionChain");
                if (DrawFoldout(openEvolution, i, "進化の枠（最後＝イレギュラー素材）", false))
                {
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
                }

                // レシピを進化の枠に自動追従（折りたたみの外で常に実行）
                SyncRequiredList(recipe, chain);

                // hk追加：通常レシピ・特殊ボール・裏メニューをまとめた折りたたみ（子）
                if (DrawFoldout(openRecipeBody, i, "通常レシピ／特殊ボール／裏メニュー", false))
                {
                    // 進化ボールのレシピ
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("通常レシピ（個数を入力。0＝使わない）");
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
                        GUILayout.Space(54);
                        EditorGUILayout.EndHorizontal();
                    }

                    // 特殊ボールのレシピ
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("特殊ボールのレシピ（この料理に固定）");
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

                    // 裏メニュー
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("── 裏メニュー ──");
                    SerializedProperty hasSecret = recipe.FindPropertyRelative("hasSecret");
                    EditorGUILayout.PropertyField(hasSecret, new GUIContent("裏メニュー有り"));
                    if (hasSecret.boolValue)
                    {
                        EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretCookingName"), new GUIContent("裏メニューの料理名"));
                        EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretNuisanceCount"), new GUIContent("お邪魔の必要数"));
                        EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretIrregularCount"), new GUIContent("イレギュラー素材の必要数"));
                        EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretPrefab"), new GUIContent("Anomalyの演出プレハブ"));
                        EditorGUILayout.PropertyField(recipe.FindPropertyRelative("anomalySprite"), new GUIContent("Anomalyの絵")); // hk追加
                    }
                }

                // hk追加：完成データの折りたたみ（子）
                if (DrawFoldout(openCompletion, i, "完成データ", false))
                {
                    SerializedProperty stages = recipe.FindPropertyRelative("completionStages");
                    for (int st = 0; st < stages.arraySize; st++)
                    {
                        SerializedProperty stage = stages.GetArrayElementAtIndex(st);
                        SerializedProperty rankName = stage.FindPropertyRelative("rankName");
                        SerializedProperty cookingName = stage.FindPropertyRelative("cookingName");
                        SerializedProperty minScore = stage.FindPropertyRelative("minScore");
                        SerializedProperty prefab = stage.FindPropertyRelative("prefab");
                        SerializedProperty rankSprite = stage.FindPropertyRelative("rankSprite"); // hk追加

                        EditorGUILayout.BeginHorizontal();
                        int current = System.Array.IndexOf(RANK_NAMES, rankName.stringValue);
                        if (current < 0) current = 0;
                        int selected = EditorGUILayout.Popup(current, RANK_NAMES, GUILayout.Width(90));
                        rankName.stringValue = RANK_NAMES[selected];

                        cookingName.stringValue = EditorGUILayout.TextField(cookingName.stringValue, GUILayout.Width(110));

                        EditorGUILayout.LabelField("点数下限", GUILayout.Width(55));
                        minScore.intValue = EditorGUILayout.IntField(minScore.intValue, GUILayout.Width(60));
                        EditorGUILayout.PropertyField(prefab, GUIContent.none, GUILayout.Width(120));
                        EditorGUILayout.PropertyField(rankSprite, GUIContent.none, GUILayout.Width(60)); // hk追加
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
                }

                EditorGUILayout.Space();

                // hk追加：カテゴリID と カテゴリ画像の表示
                EditorGUILayout.LabelField("カテゴリ（料理パターン）", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(recipe.FindPropertyRelative("categoryId"), new GUIContent("カテゴリID"));
                EditorGUILayout.PropertyField(recipe.FindPropertyRelative("dishSprite"), new GUIContent("カテゴリ画像"));
            }
            serializedObject.ApplyModifiedProperties();
        }

        // hk追加：折りたたみを1つ描く。メモに無ければ開いている扱い。
        // isParent＝trueなら親（大きい太字）、falseなら子（普通サイズの太字＋一段下げ）
        private bool DrawFoldout(Dictionary<int, bool> memo, int key, string title, bool isParent)
        {
            bool isOpen = memo.ContainsKey(key) ? memo[key] : true; // 無ければ開いている

            if (isParent)
            {
                // 親：太字を一回り大きくしたスタイルで描く
                GUIStyle bigStyle = new GUIStyle(EditorStyles.foldoutHeader);
                bigStyle.fontSize = 14; // 通常より大きく
                isOpen = EditorGUILayout.Foldout(isOpen, title, true, bigStyle);
            }
            else
            {
                // 子：太字の折りたたみで描き、一段下げる。終わったら戻す
                GUIStyle childStyle = new GUIStyle(EditorStyles.foldout);
                childStyle.fontStyle = FontStyle.Bold; // 中身の説明より目立つよう太字に
                EditorGUI.indentLevel++;
                isOpen = EditorGUILayout.Foldout(isOpen, title, true, childStyle);
                EditorGUI.indentLevel--;
            }

            memo[key] = isOpen;
            return isOpen;
        }

        // hk追加：CSVファイルを選んで読み込み、今の料理リストを置き換える
        private void ImportFromCsv()
        {
            string path = EditorUtility.OpenFilePanel("レシピCSVを選択", Application.dataPath + "/Project Files/Data/CSV", "csv");
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
                r.FindPropertyRelative("recipeNameRomaji").stringValue = src.recipeNameRomaji; // hk追加
                r.FindPropertyRelative("hasSecret").boolValue = src.hasSecret;
                r.FindPropertyRelative("secretCookingName").stringValue = src.secretCookingName; // hk追加
                r.FindPropertyRelative("secretNuisanceCount").intValue = src.secretNuisanceCount;
                r.FindPropertyRelative("secretIrregularCount").intValue = src.secretIrregularCount;
                r.FindPropertyRelative("anomalySprite").objectReferenceValue = src.anomalySprite; // hk追加
                r.FindPropertyRelative("secretPrefab").objectReferenceValue = src.secretPrefab; // hk追加

                CopyIntList(r.FindPropertyRelative("evolutionChain"), src.evolutionChain);
                CopyRequiredList(r.FindPropertyRelative("requiredList"), src.requiredList);
                CopyRequiredList(r.FindPropertyRelative("specialList"), src.specialList);
                CopyCompletionStages(r.FindPropertyRelative("completionStages"), src.completionStages);

                // hk追加：カテゴリID と カテゴリ画像を書き戻す
                r.FindPropertyRelative("categoryId").intValue = src.categoryId;
                r.FindPropertyRelative("dishSprite").objectReferenceValue = src.dishSprite;

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
            // 見出し3行（1行目＝表題、2・3行目＝空行。読み込みは上3行を飛ばすので中身は自由）
            sb.AppendLine("recipeName,recipeId,進化の枠,進化ボールNo,名前,通常個数,特殊No,特殊個数,裏メニュー,裏料理名,お邪魔個数,イレギュラー個数,ランク,料理名,点数下限");
            sb.AppendLine(",,,,,,,,,,,,,,");
            sb.AppendLine(",,,,,,,,,,,,,,");

            // データ17行分を作る
            int dataRows = BlockRows - 3; // 20 - 見出し3 = 17
            for (int row = 0; row < dataRows; row++)
            {
                string[] cells = new string[15]; // A〜O（裏料理名が増えて14→15）
                for (int c = 0; c < cells.Length; c++) cells[c] = "";

                // 先頭データ行にだけ、料理名・ID・裏メニューを書く
                if (row == 0)
                {
                    cells[0] = Escape(entry.recipeName);
                    cells[1] = entry.recipeId.ToString();
                    cells[8] = entry.hasSecret ? "TRUE" : "FALSE";
                    cells[9] = Escape(entry.secretCookingName); // hk追加：裏メニューの料理名
                    cells[10] = entry.secretNuisanceCount.ToString();
                    cells[11] = entry.secretIrregularCount.ToString();
                }

                // hk追加：2行目（料理名の1つ下）にローマ字を書く
                if (row == 1)
                {
                    cells[0] = Escape(entry.recipeNameRomaji);
                }

                // 進化の枠（D列）と通常レシピの個数（F列）
                if (row < entry.evolutionChain.Count)
                {
                    int number = entry.evolutionChain[row];
                    cells[3] = number.ToString();
                    cells[4] = ""; // E列は空欄。スプレッドシート側の数式（番号→名前）を壊さないため

                    int count = FindRequiredCount(entry, number);
                    if (count > 0) cells[5] = count.ToString();
                }

                // 特殊ボール（G・H列）を上から順に
                if (row < entry.specialList.Count)
                {
                    cells[6] = entry.specialList[row].number.ToString();
                    cells[7] = entry.specialList[row].count.ToString();
                }

                // 完成データ（M=ランク, N=料理名, O=点数下限）を上から順に
                if (row < entry.completionStages.Count)
                {
                    cells[12] = Escape(entry.completionStages[row].rankName);
                    cells[13] = Escape(entry.completionStages[row].cookingName);
                    cells[14] = entry.completionStages[row].minScore.ToString();
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
        private void CopyIntList(SerializedProperty listProp, List<int> src)
        {
            listProp.ClearArray();
            listProp.arraySize = src.Count;
            for (int i = 0; i < src.Count; i++)
                listProp.GetArrayElementAtIndex(i).intValue = src[i];
        }

        // hk追加：RequiredItem（番号＋個数）のリストをコピー
        private void CopyRequiredList(SerializedProperty listProp, List<RequiredItem> src)
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

        // hk追加：CompletionStage（ランク名・料理名・点数・絵・プレハブ）のリストをコピー
        private void CopyCompletionStages(SerializedProperty listProp, List<CompletionStage> src)
        {
            listProp.ClearArray();
            listProp.arraySize = src.Count;
            for (int i = 0; i < src.Count; i++)
            {
                SerializedProperty stage = listProp.GetArrayElementAtIndex(i);
                stage.FindPropertyRelative("rankName").stringValue = src[i].rankName;
                stage.FindPropertyRelative("cookingName").stringValue = src[i].cookingName;
                stage.FindPropertyRelative("minScore").intValue = src[i].minScore;
                stage.FindPropertyRelative("rankSprite").objectReferenceValue = src[i].rankSprite;
                stage.FindPropertyRelative("prefab").objectReferenceValue = src[i].prefab; // hk追加
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

            var savedCounts = new Dictionary<int, int>();
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