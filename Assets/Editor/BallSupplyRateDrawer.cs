using UnityEditor;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：BallSupplyRateのInspector表示。Numberの隣にBallData由来の名前を出す
    // 名前はBallNameResolver（Resources経由の共通入り口）から引く
    [CustomPropertyDrawer(typeof(BallSupplyRate))]
    public class BallSupplyRateDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty categoryProp = property.FindPropertyRelative("category");
            SerializedProperty numberProp = property.FindPropertyRelative("number");
            SerializedProperty spawnRateProp = property.FindPropertyRelative("spawnRate");

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float y = position.y;

            // Category
            Rect categoryRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(categoryRect, categoryProp);
            y += line + spacing;

            // Number ＋ 右側に名前ラベル
            Rect numberRect = new Rect(position.x, y, position.width * 0.6f, line);
            EditorGUI.PropertyField(numberRect, numberProp);

            BallCategory category = (BallCategory)categoryProp.enumValueIndex;
            string ballName = BallNameResolver.GetBallName(category, numberProp.intValue);

            Rect nameRect = new Rect(position.x + position.width * 0.62f, y, position.width * 0.38f, line);
            EditorGUI.LabelField(nameRect, ballName);
            y += line + spacing;

            // Spawn Rate
            Rect rateRect = new Rect(position.x, y, position.width, line);
            EditorGUI.PropertyField(rateRect, spawnRateProp);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return line * 3 + spacing * 2; // Category / Number / SpawnRate の3行
        }
    }
}