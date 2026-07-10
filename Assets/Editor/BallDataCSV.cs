using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Watermelon.BubbleMerge
{
    // hk追加：BallDataとCSVの読み込み・書き出しを行う（横3ブロック・レイアウト）
    public static class BallDataCSV
    {
        private const string CSV_PATH = "Assets/Project Files/Data/CSV/BallData.csv";
        private const string PHYSICS_FOLDER = "Assets/Project Files/Data/Ball Physics Data";
        private const string PHYSICS_PREFIX = "Bubble Physics Data_";

        private const int HeaderRows = 2;   // 上2行（空行＋見出し）は飛ばす
        private const int ItemColumns = 7;  // 1ブロックのデータ項目数（ballName〜canMerge）

        // 各ブロックの「判定(use)」列（0始まり）。画像列の次。B=1, K=10, T=19
        private static readonly int[] BlockUseCols = { 1, 10, 19 };

        // 各ブロックのデータ開始列（0始まり）。判定の次。C=2, L=11, U=20
        private static readonly int[] BlockStartCols = { 2, 11, 20 };

        // 1ブロックの幅（画像1＋判定1＋データ7＝9列）
        private const int BlockWidth = 9;

        // 行の総セル数（3ブロック×9＝27）
        private const int TotalCells = BlockWidth * 3;

        // ── .asset → CSV（書き出し）──
        public static void ExportToCSV(BallData ballData)
        {
            SerializedObject so = new SerializedObject(ballData);
            SerializedProperty balls = so.FindProperty("balls");

            // ① カテゴリごとに仕分ける（0=進化, 1=特殊, 2=お邪魔）
            List<SerializedProperty>[] byCategory = { new List<SerializedProperty>(), new List<SerializedProperty>(), new List<SerializedProperty>() };
            for (int i = 0; i < balls.arraySize; i++)
            {
                SerializedProperty entry = balls.GetArrayElementAtIndex(i);
                int cat = entry.FindPropertyRelative("category").intValue;
                if (cat >= 0 && cat <= 2)
                    byCategory[cat].Add(entry);
            }

            // ② 一番多い件数に行数を合わせる
            int maxRows = 0;
            foreach (var list in byCategory)
                if (list.Count > maxRows) maxRows = list.Count;

            StringBuilder sb = new StringBuilder();

            // ③ 1行目＝空行（全セル空）
            sb.AppendLine(new string(',', TotalCells - 1));

            // ④ 2行目＝見出し（3ブロックぶん横に並べる。各ブロック＝画像空+判定+データ7）
            string headBlock = "use,ballName,category,number,folderName,size,physicsPattern,canMerge";
            sb.AppendLine("," + headBlock + ",," + headBlock + ",," + headBlock);

            // ⑤ 3行目から、一番多い件数ぶん回す
            for (int row = 0; row < maxRows; row++)
            {
                string[] cells = new string[TotalCells];
                for (int c = 0; c < TotalCells; c++) cells[c] = ""; // 全部空で初期化

                for (int block = 0; block < 3; block++)
                {
                    int useCol = BlockUseCols[block];
                    int start = BlockStartCols[block];

                    if (row < byCategory[block].Count)
                    {
                        cells[useCol] = "TRUE"; // 取り込んだデータは有効印

                        string[] item = EntryToCells(byCategory[block][row]).Split(',');
                        for (int k = 0; k < item.Length && k < ItemColumns; k++)
                            cells[start + k] = item[k];
                    }
                }

                sb.AppendLine(string.Join(",", cells));
            }

            string dir = Path.GetDirectoryName(CSV_PATH);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(CSV_PATH, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            Debug.Log("CSVへ書き出しました（横3ブロック）： " + CSV_PATH);
        }

        // hk追加：ボール1件を、7項目のカンマ区切り文字にする
        private static string EntryToCells(SerializedProperty entry)
        {
            string ballName = entry.FindPropertyRelative("ballName").stringValue;
            string category = CategoryToText(entry.FindPropertyRelative("category").intValue);
            int number = entry.FindPropertyRelative("number").intValue;
            string folderName = entry.FindPropertyRelative("folderName").stringValue;
            float size = entry.FindPropertyRelative("size").floatValue;

            var physicsObj = entry.FindPropertyRelative("physicsPattern").objectReferenceValue;
            string physicsShort = "";
            if (physicsObj != null)
            {
                string fullName = physicsObj.name;
                physicsShort = fullName.StartsWith(PHYSICS_PREFIX)
                    ? fullName.Substring(PHYSICS_PREFIX.Length)
                    : fullName;
            }

            bool canMerge = entry.FindPropertyRelative("canMerge").boolValue;

            return ballName + "," + category + "," + number + "," + folderName + "," + size + "," + physicsShort + "," + canMerge;
        }

        // ── CSV → .asset（読み込み）── ※Editor側のSerializedObjectを受け取る
        public static void ImportFromCSV(SerializedObject so)
        {
            if (!File.Exists(CSV_PATH))
            {
                Debug.LogError("CSVファイルが見つかりません： " + CSV_PATH);
                return;
            }

            string[] lines = File.ReadAllLines(CSV_PATH);
            if (lines.Length <= HeaderRows)
            {
                Debug.LogError("CSVにデータ行がありません");
                return;
            }

            SerializedProperty balls = so.FindProperty("balls");
            balls.ClearArray(); // 既存を全部消してCSVで入れ替える

            int added = 0;

            for (int i = HeaderRows; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] cols = lines[i].Split(',');

                for (int block = 0; block < 3; block++)
                {
                    int useCol = BlockUseCols[block];
                    int start = BlockStartCols[block];

                    // 判定がTRUE（大文字小文字問わず）の行だけ取り込む。それ以外は全部飛ばす
                    if (!IsUseTrue(cols, useCol)) continue;

                    if (TryAddEntry(balls, cols, start))
                        added++;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CSVから読み込みました（横3ブロック）： " + added + "件");
        }

        // hk追加：判定列がTRUE（大文字小文字問わず）かどうか。それ以外は全部false
        private static bool IsUseTrue(string[] cols, int useCol)
        {
            string v = GetCol(cols, useCol);
            return v.Equals("TRUE", System.StringComparison.OrdinalIgnoreCase);
        }

        // hk追加：指定の開始列から7項目を読み、1件を追加する
        private static bool TryAddEntry(SerializedProperty balls, string[] cols, int start)
        {
            if (start + ItemColumns > cols.Length) return false;

            string ballName = GetCol(cols, start + 0);
            if (string.IsNullOrWhiteSpace(ballName)) return false;

            int categoryInt = TextToCategory(GetCol(cols, start + 1));
            int.TryParse(GetCol(cols, start + 2), out int number);
            string folderName = GetCol(cols, start + 3);
            float.TryParse(GetCol(cols, start + 4), out float size);
            string physicsShort = GetCol(cols, start + 5);
            bool.TryParse(GetCol(cols, start + 6), out bool canMerge);

            balls.arraySize++;
            SerializedProperty entry = balls.GetArrayElementAtIndex(balls.arraySize - 1);

            entry.FindPropertyRelative("ballName").stringValue = ballName;
            entry.FindPropertyRelative("category").intValue = categoryInt;
            entry.FindPropertyRelative("number").intValue = number;
            entry.FindPropertyRelative("folderName").stringValue = folderName;
            entry.FindPropertyRelative("size").floatValue = size;
            entry.FindPropertyRelative("physicsPattern").objectReferenceValue = FindPhysics(physicsShort);
            entry.FindPropertyRelative("canMerge").boolValue = canMerge;

            AssignPrefabsFromFolder(entry, categoryInt, number, folderName);

            return true;
        }

        // hk追加：列を安全に取り出す（前後の空白を落とす）
        private static string GetCol(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length) return "";
            return cols[index].Trim();
        }

        // ── フォルダ・プレハブまわり ──

        private const string BALL_ROOT = "Assets/Project Files/Game/Images/Ball/";
        private const string VISUAL_PREFAB_NAME = "CookingBall";
        private const string ICON_PREFAB_NAME = "CookingIcon";

        private static void AssignPrefabsFromFolder(SerializedProperty entry, int category, int number, string folderName)
        {
            string categoryFolder = CategoryToFolder(category);
            string numberText = number.ToString("0000");
            string parentPath = BALL_ROOT + categoryFolder;
            string wantFolderName = numberText + "_" + folderName;

            // フォルダをID基準で用意し、実際に使うべきフォルダ名を受け取る
            string actualFolderName = EnsureFolder(parentPath, numberText, wantFolderName);

            // 用意できなかった（危険で中止した）場合は、プレハブ設定をせず抜ける
            if (string.IsNullOrEmpty(actualFolderName)) return;

            string folderPath = parentPath + "/" + actualFolderName;

            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning("フォルダが見つかりません（プレハブ未設定）： " + folderPath);
                return;
            }

            var visual = LoadPrefabInFolder(folderPath, VISUAL_PREFAB_NAME);
            var icon = LoadPrefabInFolder(folderPath, ICON_PREFAB_NAME);

            entry.FindPropertyRelative("visualPrefab").objectReferenceValue = visual;
            entry.FindPropertyRelative("uiIconPrefab").objectReferenceValue = icon;
        }

        // hk追加：そのIDのフォルダをディスク上に必ず1つ用意し、実際に使うべきフォルダ名を返す
        // ・無ければ空で新規作成 → その名前を返す
        // ・名前だけ違えばリネーム → 新しい名前を返す
        // ・危ない状況（リネーム先が既にある／同じIDが2つ）では消さずに警告して止める → 空文字を返す
        private static string EnsureFolder(string parentPath, string numberText, string wantFolderName)
        {
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
                AssetDatabase.Refresh();
            }

            string wantPath = parentPath + "/" + wantFolderName;

            // 同じID（numberText_）で始まるフォルダを、ディスクから全部探す
            List<string> sameIdFolders = new List<string>();
            foreach (string dir in Directory.GetDirectories(parentPath))
            {
                string name = Path.GetFileName(dir);
                if (name.StartsWith(numberText + "_"))
                    sameIdFolders.Add(name);
            }

            // 同じIDのフォルダが2つ以上 → 勝手にどちらか消さない。止めて空を返す
            if (sameIdFolders.Count >= 2)
            {
                Debug.LogWarning("同じID(" + numberText + ")のフォルダが複数あります。手動で整理してください： "
                    + string.Join(" / ", sameIdFolders));
                return "";
            }

            // ちょうど1つある場合
            if (sameIdFolders.Count == 1)
            {
                string current = sameIdFolders[0];

                // 望ましい名前と同じなら、そのまま使う
                if (current == wantFolderName) return wantFolderName;

                // 名前が違う → リネームしたい。ただしリネーム先が既にあるなら危険なので止める
                if (Directory.Exists(wantPath))
                {
                    Debug.LogWarning("リネーム先が既に存在するため中止します： "
                        + current + " → " + wantFolderName);
                    return "";
                }

                string oldPath = parentPath + "/" + current;
                string error = AssetDatabase.RenameAsset(oldPath, wantFolderName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning("フォルダのリネームに失敗しました： " + error);
                    return "";
                }

                Debug.Log("フォルダをリネームしました： " + current + " → " + wantFolderName);
                return wantFolderName;
            }

            // 同じIDのフォルダが1つも無い → 空で新規作成して、その名前を返す
            AssetDatabase.CreateFolder(parentPath, wantFolderName);
            return wantFolderName;
        }

        private static GameObject LoadPrefabInFolder(string folderPath, string prefabName)
        {
            string prefabPath = folderPath + "/" + prefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                Debug.LogWarning("プレハブが見つかりません（空にします）： " + prefabPath);
            return prefab;
        }

        private static string CategoryToFolder(int category)
        {
            switch (category)
            {
                case 0: return "EvolutionBall";
                case 1: return "SpecialBall";
                case 2: return "NuisanceBall";
                default: return "EvolutionBall";
            }
        }

        private static BubblesPhysicsData FindPhysics(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return null;

            string fullName = PHYSICS_PREFIX + shortName;
            string[] guids = AssetDatabase.FindAssets(fullName + " t:BubblesPhysicsData", new[] { PHYSICS_FOLDER });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == fullName)
                {
                    return AssetDatabase.LoadAssetAtPath<BubblesPhysicsData>(path);
                }
            }

            Debug.LogWarning("物理パターンが見つかりません（空にします）： " + fullName);
            return null;
        }

        private static string CategoryToText(int category)
        {
            switch (category)
            {
                case 0: return "Evolution";
                case 1: return "Special";
                case 2: return "Nuisance";
                default: return "Evolution";
            }
        }

        private static int TextToCategory(string text)
        {
            switch (text.Trim())
            {
                case "Evolution": return 0;
                case "Special": return 1;
                case "Nuisance": return 2;
                default: return 0;
            }
        }
    }
}