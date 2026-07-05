using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Watermelon.BubbleMerge
{
    // hk追加：BallDataのInspectorに「ID表示」「ダブり警告」「追加/削除」「並べ替え」「フォルダ照合」を出す
    [CustomEditor(typeof(BallData))]
    public class BallDataEditor : Editor
    {
        // hk追加：カテゴリごとの親フォルダのパス
        private const string BALL_ROOT = "Assets/Project Files/Game/Images/Ball/";

        public override void OnInspectorGUI()
        {
            SerializedProperty balls = serializedObject.FindProperty("balls");
            // hk追加：CSV読み込み・書き出しボタン
            BallData ballData = (BallData)target;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("CSVへ書き出す（.asset→CSV）"))
            {
                BallDataCSV.ExportToCSV(ballData);
            }
            if (GUILayout.Button("CSVから読み込む（CSV→.asset）"))
            {
                if (EditorUtility.DisplayDialog("確認", "今の内容をCSVの内容で上書きします。よろしいですか？", "読み込む", "やめる"))
                {
                    BallDataCSV.ImportFromCSV(ballData);
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // hk追加：全ボール共通の見た目倍率（小数点で入力）
            SerializedProperty visualScale = serializedObject.FindProperty("visualScale");
            EditorGUILayout.PropertyField(visualScale, new GUIContent("Visual Scale（全ボール共通の見た目倍率）"));
            EditorGUILayout.Space();

            // 追加・削除ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("＋ ボールを追加"))
            {
                balls.arraySize++;
            }
            if (GUILayout.Button("－ 最後を削除") && balls.arraySize > 0)
            {
                balls.arraySize--;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // ダブりチェック用
            Dictionary<int, List<int>> usedNumbers = new Dictionary<int, List<int>>();
            HashSet<string> duplicateKeys = new HashSet<string>();

            for (int i = 0; i < balls.arraySize; i++)
            {
                SerializedProperty entry = balls.GetArrayElementAtIndex(i);
                int category = entry.FindPropertyRelative("category").intValue;
                int number = entry.FindPropertyRelative("number").intValue;

                if (!usedNumbers.ContainsKey(category))
                    usedNumbers[category] = new List<int>();

                if (usedNumbers[category].Contains(number))
                    duplicateKeys.Add(category + "_" + number);
                else
                    usedNumbers[category].Add(number);
            }

            // 各ボールを表示
            for (int i = 0; i < balls.arraySize; i++)
            {
                SerializedProperty entry = balls.GetArrayElementAtIndex(i);
                int category = entry.FindPropertyRelative("category").intValue;
                int number = entry.FindPropertyRelative("number").intValue;
                string listName = entry.FindPropertyRelative("folderName").stringValue;

                // ボールごとの区切り
                EditorGUILayout.Space(12);
                Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1f));
                EditorGUILayout.Space(6);

                string idText = category.ToString("00") + " " + number.ToString("0000");

                // ID表示と、上へ/下へボタン
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID", idText, EditorStyles.boldLabel);
                if (i > 0 && GUILayout.Button("▲上へ", GUILayout.Width(60)))
                {
                    balls.MoveArrayElement(i, i - 1);
                }
                if (i < balls.arraySize - 1 && GUILayout.Button("▼下へ", GUILayout.Width(60)))
                {
                    balls.MoveArrayElement(i, i + 1);
                }
                EditorGUILayout.EndHorizontal();

                // 番号ダブり警告
                if (duplicateKeys.Contains(category + "_" + number))
                {
                    EditorGUILayout.HelpBox("番号がダブっています： " + idText, MessageType.Error);
                }

                // ── フォルダ照合 ──
                DrawFolderCheck(category, number, listName);

                EditorGUILayout.PropertyField(entry, true);
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // hk追加：フォルダを見て、リストと一致するか判定して表示する
        private void DrawFolderCheck(int category, int number, string listName)
        {
            // カテゴリ→親フォルダ名
            string categoryFolder;
            switch (category)
            {
                case 0: categoryFolder = "EvolutionBall"; break; // 進化
                case 1: categoryFolder = "SpecialBall"; break;   // 特殊
                case 2: categoryFolder = "NuisanceBall"; break;  // お邪魔
                default: categoryFolder = ""; break;
            }

            string parentPath = BALL_ROOT + categoryFolder;
            string numberText = number.ToString("0000");

            // 親フォルダが無い場合
            if (!Directory.Exists(parentPath))
            {
                EditorGUILayout.HelpBox("フォルダ未作成（データなし）： " + categoryFolder + " が見つかりません", MessageType.None);
                return;
            }

            // 親フォルダの中から、番号(0000)で始まるフォルダを探す
            string[] dirs = Directory.GetDirectories(parentPath);
            string matchedFolder = null;
            foreach (string dir in dirs)
            {
                string name = Path.GetFileName(dir);
                if (name.StartsWith(numberText))
                {
                    matchedFolder = name;
                    break;
                }
            }

            // 番号のフォルダが無い場合
            if (matchedFolder == null)
            {
                EditorGUILayout.HelpBox("フォルダ未作成（データなし）： " + numberText + "_ のフォルダがありません", MessageType.None);
                return;
            }

            // フォルダ名を「_」で番号と名前に分ける
            int underscoreIndex = matchedFolder.IndexOf('_');
            string folderNumber = underscoreIndex >= 0 ? matchedFolder.Substring(0, underscoreIndex) : matchedFolder;
            string folderName = underscoreIndex >= 0 ? matchedFolder.Substring(underscoreIndex + 1) : "";

            // 照合表示（省スペース：小さい文字で1行にまとめる）
            GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.margin = new RectOffset(0, 0, 0, 0);
            miniStyle.padding = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.LabelField("List   → No." + numberText + " / " + listName, miniStyle);
            EditorGUILayout.LabelField("Folder → No." + folderNumber + " / " + folderName, miniStyle);

            // 判定

            bool numberMatch = (numberText == folderNumber);
            bool nameMatch = (listName == folderName);

            if (!numberMatch || !nameMatch)
            {
                string msg = "フォルダとリストが食い違っています：";
                if (!numberMatch) msg += " 番号(" + numberText + "≠" + folderNumber + ")";
                if (!nameMatch) msg += " 名前(" + listName + "≠" + folderName + ")";
                EditorGUILayout.HelpBox(msg, MessageType.Error);

                // hk追加：番号は合うが名前が違うとき、実フォルダをリスト名にリネームするボタン
                if (numberMatch && !nameMatch)
                {
                    if (GUILayout.Button("フォルダを直す（" + matchedFolder + " → " + numberText + "_" + listName + "）"))
                    {
                        string oldPath = parentPath + "/" + matchedFolder;
                        string newFolderName = numberText + "_" + listName;
                        string error = AssetDatabase.RenameAsset(oldPath, newFolderName);
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError("フォルダのリネームに失敗しました： " + error);
                        }
                        else
                        {
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }
        }
    }
}