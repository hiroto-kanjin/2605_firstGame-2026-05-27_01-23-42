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

        // 各ブロックのデータ開始列（0始まり）。各ブロックは「画像1列＋データ7列＝8列」ずつずれる
        private static readonly int[] BlockStartCols = { 1, 9, 17 }; // 進化=B, 特殊=J, お邪魔=R

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

            // ③ 1行目＝空行（画像列を含めて全部空）
            sb.AppendLine(EmptyRow());

            // ④ 2行目＝見出し（3ブロックぶん横に並べる。各ブロック前に画像列の空セル）
            string headBlock = "ballName,category,number,folderName,size,physicsPattern,canMerge";
            sb.AppendLine("," + headBlock + ",," + headBlock + ",," + headBlock);

            // ⑤ 3行目から、一番多い件数ぶん回す
            // 1行は必ず24セル（3ブロック×(画像1＋データ7)）。箱を用意して位置で埋める
            for (int row = 0; row < maxRows; row++)
            {
                string[] cells = new string[24];
                for (int c = 0; c < 24; c++) cells[c] = ""; // 全部空で初期化

                for (int block = 0; block < 3; block++)
                {
                    // このブロックのデータ開始位置（画像列を1つ空けた次）
                    // 進化=1, 特殊=9, お邪魔=17
                    int start = block * 8 + 1;

                    if (row < byCategory[block].Count)
                    {
                        string[] item = EntryToCells(byCategory[block][row]).Split(',');
                        for (int k = 0; k < item.Length && k < ItemColumns; k++)
                            cells[start + k] = item[k]; // 7項目を決まった位置に入れる
                    }
                    // 在庫が無ければ、その7セルは空のまま（初期化済み）
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

            // 物理パターン：本物のアセット名から接頭辞を外して短い名前にする
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

        // hk追加：画像列を含む空行（見出しと同じ列数ぶんのカンマ）
        private static string EmptyRow()
        {
            // 3ブロック×（画像1＋データ7）＝24セル → カンマ23個
            return new string(',', 23);
        }

        // ── CSV → .asset（読み込み）──
        public static void ImportFromCSV(BallData ballData)
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

            SerializedObject so = new SerializedObject(ballData);
            SerializedProperty balls = so.FindProperty("balls");
            balls.ClearArray(); // 既存を全部消してCSVで入れ替える

            int added = 0;

            // 上2行（空行＋見出し）を飛ばし、3行目(i=HeaderRows)から
            for (int i = HeaderRows; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] cols = lines[i].Split(',');

                // 3ブロックを、開始列を変えて読む（同じ読み方を3回）
                for (int block = 0; block < 3; block++)
                {
                    int start = BlockStartCols[block];
                    if (TryAddEntry(balls, cols, start))
                        added++;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ballData);
            AssetDatabase.SaveAssets();
            Debug.Log("CSVから読み込みました（横3ブロック）： " + added + "件");
        }

        // hk追加：指定の開始列から7項目を読み、1件を追加する。ballNameが空なら追加しない（trueを返したら追加した）
        private static bool TryAddEntry(SerializedProperty balls, string[] cols, int start)
        {
            // 列が足りない（その行にそのブロックが無い）
            if (start + ItemColumns > cols.Length) return false;

            string ballName = GetCol(cols, start + 0);
            if (string.IsNullOrWhiteSpace(ballName)) return false; // 空の在庫は飛ばす

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

            // フォルダから見た目・アイコンのプレハブを名前で自動取得してセットする
            AssignPrefabsFromFolder(entry, categoryInt, number, folderName);

            return true;
        }

        // hk追加：列を安全に取り出す（前後の空白を落とす）
        private static string GetCol(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length) return "";
            return cols[index].Trim();
        }

        // ── ここから下は、縦1枚のときと同じ部品（変更なし）──

        private const string BALL_ROOT = "Assets/Project Files/Game/Images/Ball/";
        private const string VISUAL_PREFAB_NAME = "CookingBall";
        private const string ICON_PREFAB_NAME = "CookingIcon";

        private static void AssignPrefabsFromFolder(SerializedProperty entry, int category, int number, string folderName)
        {
            string categoryFolder = CategoryToFolder(category);
            string numberText = number.ToString("0000");
            string folderPath = BALL_ROOT + categoryFolder + "/" + numberText + "_" + folderName;

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