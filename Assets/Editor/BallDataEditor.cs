using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Watermelon.BubbleMerge
{
    // hk追加：BallDataのInspectorを種類ごとにまとめ、折りたためるようにする
    [CustomEditor(typeof(BallData))]
    public class BallDataEditor : Editor
    {
        private const string BALL_ROOT = "Assets/Project Files/Game/Images/Ball/";

        // hk追加：各グループの開閉状態を覚えておく（進化・特殊・お邪魔）
        private bool[] groupOpen = { true, true, true };

        // グループの見出しに出す名前
        private static readonly string[] GROUP_TITLES = { "進化ボール（Evolution）", "特殊ボール（Special）", "お邪魔ボール（Nuisance）" };

        public override void OnInspectorGUI()
        {
            SerializedProperty balls = serializedObject.FindProperty("balls");
            BallData ballData = (BallData)target;

            // CSV読み込み・書き出しボタン
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

            // 全ボール共通の見た目倍率
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

            // ダブりチェック（種類ごとに番号の重複を調べる）
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

            // hk追加：全ボールを種類ごとに仕分ける（値は「balls内の位置」）
            List<int>[] groups = { new List<int>(), new List<int>(), new List<int>() };
            for (int i = 0; i < balls.arraySize; i++)
            {
                int category = balls.GetArrayElementAtIndex(i).FindPropertyRelative("category").intValue;
                if (category >= 0 && category <= 2)
                    groups[category].Add(i);
            }

            // hk追加：グループごとに、見出し（開閉）＋中身を表示する
            for (int g = 0; g < 3; g++)
            {
                EditorGUILayout.Space(6);

                // 見出し（クリックで開閉）。件数も出す
                groupOpen[g] = EditorGUILayout.Foldout(groupOpen[g], GROUP_TITLES[g] + "　［" + groups[g].Count + "件］", true, EditorStyles.foldoutHeader);

                if (!groupOpen[g]) continue; // 畳んでいるなら中身は出さない

                // このグループのボールを順に表示
                for (int idx = 0; idx < groups[g].Count; idx++)
                {
                    int ballIndex = groups[g][idx]; // balls内の実際の位置
                    DrawBall(balls, ballIndex, duplicateKeys, groups[g], idx);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // hk追加：ボール1個を表示する（groupList＝同じ種類の位置リスト、posInGroup＝その中での順番）
        private void DrawBall(SerializedProperty balls, int i, HashSet<string> duplicateKeys, List<int> groupList, int posInGroup)
        {
            SerializedProperty entry = balls.GetArrayElementAtIndex(i);
            int category = entry.FindPropertyRelative("category").intValue;
            int number = entry.FindPropertyRelative("number").intValue;
            string listName = entry.FindPropertyRelative("folderName").stringValue;

            EditorGUILayout.Space(12);
            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 1f));
            EditorGUILayout.Space(6);

            string idText = category.ToString("00") + " " + number.ToString("0000");

            // ID表示と、上へ/下へボタン（同じ種類の中だけで動かす）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", idText, EditorStyles.boldLabel);

            // 「上へ」：グループ内で1つ前のボールと、balls内の位置を入れ替える
            if (posInGroup > 0 && GUILayout.Button("▲上へ", GUILayout.Width(60)))
            {
                int prevBallIndex = groupList[posInGroup - 1];
                balls.MoveArrayElement(i, prevBallIndex);
            }
            // 「下へ」：グループ内で1つ後のボールと入れ替える
            if (posInGroup < groupList.Count - 1 && GUILayout.Button("▼下へ", GUILayout.Width(60)))
            {
                int nextBallIndex = groupList[posInGroup + 1];
                balls.MoveArrayElement(i, nextBallIndex);
            }
            EditorGUILayout.EndHorizontal();

            // 番号ダブり警告
            if (duplicateKeys.Contains(category + "_" + number))
            {
                EditorGUILayout.HelpBox("番号がダブっています： " + idText, MessageType.Error);
            }

            // フォルダ照合
            DrawFolderCheck(category, number, listName);

            EditorGUILayout.PropertyField(entry, true);
            EditorGUILayout.Space();
        }

        // hk追加：フォルダを見て、リストと一致するか判定して表示する（変更なし）
        private void DrawFolderCheck(int category, int number, string listName)
        {
            string categoryFolder;
            switch (category)
            {
                case 0: categoryFolder = "EvolutionBall"; break;
                case 1: categoryFolder = "SpecialBall"; break;
                case 2: categoryFolder = "NuisanceBall"; break;
                default: categoryFolder = ""; break;
            }

            string parentPath = BALL_ROOT + categoryFolder;
            string numberText = number.ToString("0000");

            if (!Directory.Exists(parentPath))
            {
                EditorGUILayout.HelpBox("フォルダ未作成（データなし）： " + categoryFolder + " が見つかりません", MessageType.None);
                return;
            }

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

            if (matchedFolder == null)
            {
                EditorGUILayout.HelpBox("フォルダ未作成（データなし）： " + numberText + "_ のフォルダがありません", MessageType.None);
                return;
            }

            int underscoreIndex = matchedFolder.IndexOf('_');
            string folderNumber = underscoreIndex >= 0 ? matchedFolder.Substring(0, underscoreIndex) : matchedFolder;
            string folderName = underscoreIndex >= 0 ? matchedFolder.Substring(underscoreIndex + 1) : "";

            GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel);
            miniStyle.margin = new RectOffset(0, 0, 0, 0);
            miniStyle.padding = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.LabelField("List   → No." + numberText + " / " + listName, miniStyle);
            EditorGUILayout.LabelField("Folder → No." + folderNumber + " / " + folderName, miniStyle);

            bool numberMatch = (numberText == folderNumber);
            bool nameMatch = (listName == folderName);

            if (!numberMatch || !nameMatch)
            {
                string msg = "フォルダとリストが食い違っています：";
                if (!numberMatch) msg += " 番号(" + numberText + "≠" + folderNumber + ")";
                if (!nameMatch) msg += " 名前(" + listName + "≠" + folderName + ")";
                EditorGUILayout.HelpBox(msg, MessageType.Error);

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