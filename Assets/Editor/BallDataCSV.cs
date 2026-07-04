using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Watermelon.BubbleMerge
{
    // hk追加：BallDataとCSVの読み込み・書き出しを行う
    public static class BallDataCSV
    {
        private const string CSV_PATH = "Assets/Project Files/Data/CSV/BallData.csv";
        private const string PHYSICS_FOLDER = "Assets/Project Files/Data/Ball Physics Data";
        private const string PHYSICS_PREFIX = "Bubble Physics Data_";

        // ── .asset → CSV（書き出し）──
        public static void ExportToCSV(BallData ballData)
        {
            SerializedObject so = new SerializedObject(ballData);
            SerializedProperty balls = so.FindProperty("balls");

            StringBuilder sb = new StringBuilder();
            // 見出し行
            sb.AppendLine("ballName,category,number,folderName,size,physicsPattern,canMerge");

            for (int i = 0; i < balls.arraySize; i++)
            {
                SerializedProperty entry = balls.GetArrayElementAtIndex(i);
                string ballName = entry.FindPropertyRelative("ballName").stringValue;
                int categoryInt = entry.FindPropertyRelative("category").intValue;
                string category = CategoryToText(categoryInt);
                int number = entry.FindPropertyRelative("number").intValue;
                string folderName = entry.FindPropertyRelative("folderName").stringValue;
                float size = entry.FindPropertyRelative("size").floatValue;

                // 物理パターン：本物のアセット名から接頭辞を外して短い名前にする
                var physicsObj = entry.FindPropertyRelative("physicsPattern").objectReferenceValue;
                string physicsShort = "";
                if (physicsObj != null)
                {
                    string fullName = physicsObj.name; // 例：Bubble Physics Data_feather
                    physicsShort = fullName.StartsWith(PHYSICS_PREFIX)
                        ? fullName.Substring(PHYSICS_PREFIX.Length) // feather
                        : fullName;
                }

                bool canMerge = entry.FindPropertyRelative("canMerge").boolValue;
                sb.AppendLine(ballName + "," + category + "," + number + "," + folderName + "," + size + "," + physicsShort + "," + canMerge);
            }

            // フォルダが無ければ作る
            string dir = Path.GetDirectoryName(CSV_PATH);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(CSV_PATH, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            Debug.Log("CSVへ書き出しました： " + CSV_PATH);
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
            if (lines.Length <= 1)
            {
                Debug.LogError("CSVにデータ行がありません");
                return;
            }

            SerializedObject so = new SerializedObject(ballData);
            SerializedProperty balls = so.FindProperty("balls");
            balls.ClearArray(); // 既存を全部消してCSVで入れ替える

            // 1行目は見出しなので、2行目(i=1)から
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] cols = lines[i].Split(',');
                if (cols.Length < 6) continue;

                balls.arraySize++;
                SerializedProperty entry = balls.GetArrayElementAtIndex(balls.arraySize - 1);

                entry.FindPropertyRelative("ballName").stringValue = cols[0];
                entry.FindPropertyRelative("category").intValue = TextToCategory(cols[1]);
                int.TryParse(cols[2], out int number);
                entry.FindPropertyRelative("number").intValue = number;
                entry.FindPropertyRelative("folderName").stringValue = cols[3];
                float.TryParse(cols[4], out float size);
                entry.FindPropertyRelative("size").floatValue = size;

                // 物理パターン：短い名前(feather)から本物のアセットを探す
                string physicsShort = cols[5].Trim();
                entry.FindPropertyRelative("physicsPattern").objectReferenceValue = FindPhysics(physicsShort);

                // canMerge：7列目があれば読む（無ければfalse）
                bool canMerge = false;
                if (cols.Length >= 7) bool.TryParse(cols[6].Trim(), out canMerge);
                entry.FindPropertyRelative("canMerge").boolValue = canMerge;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ballData);
            AssetDatabase.SaveAssets();
            Debug.Log("CSVから読み込みました： " + (lines.Length - 1) + "件");
        }

        // 物理パターンのアセットを名前で探す
        private static BubblesPhysicsData FindPhysics(string shortName)
        {
            if (string.IsNullOrEmpty(shortName)) return null;

            string fullName = PHYSICS_PREFIX + shortName; // Bubble Physics Data_feather
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