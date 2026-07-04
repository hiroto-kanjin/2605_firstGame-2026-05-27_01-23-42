using UnityEngine;
using UnityEditor;

namespace Watermelon.BubbleMerge
{
    // hk追加：RecipeDataのInspectorを見やすくする（追加/削除・区切り・レシピ自動追従）
    [CustomEditor(typeof(RecipeData))]
    public class RecipeDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty recipes = serializedObject.FindProperty("recipes");

            // 完成料理の追加・削除ボタン
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

            // 各完成料理を表示
            for (int i = 0; i < recipes.arraySize; i++)
            {
                SerializedProperty recipe = recipes.GetArrayElementAtIndex(i);

                // 区切り線
                EditorGUILayout.Space(12);
                Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
                EditorGUI.DrawRect(lineRect, new Color(0.4f, 0.4f, 0.4f, 1f));
                EditorGUILayout.Space(6);

                // 料理名を見出しに
                SerializedProperty nameProp = recipe.FindPropertyRelative("recipeName");
                EditorGUILayout.LabelField("完成料理：" + nameProp.stringValue, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(nameProp);

                // 進化の枠
                SerializedProperty chain = recipe.FindPropertyRelative("evolutionChain");
                EditorGUILayout.PropertyField(chain, new GUIContent("進化の枠（最後＝イレギュラー素材）"), true);

                // レシピを進化の枠に自動追従させる
                SyncRequiredList(recipe, chain);

                // レシピ（自動追従。個数だけ入力）
                EditorGUILayout.LabelField("通常レシピ（個数を入力。0＝使わない）", EditorStyles.boldLabel);
                SerializedProperty requiredList = recipe.FindPropertyRelative("requiredList");
                for (int r = 0; r < requiredList.arraySize; r++)
                {
                    SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                    int number = item.FindPropertyRelative("number").intValue;
                    SerializedProperty count = item.FindPropertyRelative("count");
                    count.intValue = EditorGUILayout.IntField("number " + number + " の個数", count.intValue);
                }

                // 完成データ
                EditorGUILayout.PropertyField(recipe.FindPropertyRelative("completionStages"),
                    new GUIContent("完成データ（パーフェクト/グレート/グッド/バッド）"), true);

                // 裏メニュー（レシピの近くに表示）
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("── 裏メニュー（テリブル）──", EditorStyles.boldLabel);
                SerializedProperty hasSecret = recipe.FindPropertyRelative("hasSecret");
                EditorGUILayout.PropertyField(hasSecret, new GUIContent("裏メニュー有り"));
                if (hasSecret.boolValue)
                {
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretNuisanceCount"), new GUIContent("お邪魔の必要数"));
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretIrregularCount"), new GUIContent("イレギュラー素材の必要数"));
                    EditorGUILayout.PropertyField(recipe.FindPropertyRelative("secretImage"), new GUIContent("テリブルの絵"));
                }

                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // hk追加：レシピを進化の枠に自動でそろえる（最後のnumberは除外。個数は保持）
        private void SyncRequiredList(SerializedProperty recipe, SerializedProperty chain)
        {
            SerializedProperty requiredList = recipe.FindPropertyRelative("requiredList");

            // 進化の枠から、最後の1つ（イレギュラー素材）を除いたnumber一覧を作る
            int usableCount = chain.arraySize - 1; // 最後を除く
            if (usableCount < 0) usableCount = 0;

            // 既存の個数を、numberをキーにして覚えておく
            var savedCounts = new System.Collections.Generic.Dictionary<int, int>();
            for (int r = 0; r < requiredList.arraySize; r++)
            {
                SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                int num = item.FindPropertyRelative("number").intValue;
                int cnt = item.FindPropertyRelative("count").intValue;
                if (!savedCounts.ContainsKey(num)) savedCounts[num] = cnt;
            }

            // レシピを、進化の枠（最後を除く）に合わせて作り直す
            requiredList.arraySize = usableCount;
            for (int r = 0; r < usableCount; r++)
            {
                int number = chain.GetArrayElementAtIndex(r).intValue;
                SerializedProperty item = requiredList.GetArrayElementAtIndex(r);
                item.FindPropertyRelative("number").intValue = number;
                // 前の個数を引き継ぐ（無ければ0）
                item.FindPropertyRelative("count").intValue = savedCounts.ContainsKey(number) ? savedCounts[number] : 0;
            }
        }
    }
}