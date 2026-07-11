using UnityEditor;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：PreviewableBehaviourを継承したすべてのUIに、プレビュー用ボタンを自動で出す共通エディタ
    [CustomEditor(typeof(PreviewableBehaviour), true)] // trueで派生クラス全部に適用
    public class PreviewableBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 通常のInspector（各UIのフィールド）を先に描く
            DrawDefaultInspector();

            EditorGUILayout.Space();

            PreviewableBehaviour target = (PreviewableBehaviour)base.target;

            if (GUILayout.Button("プレビュー生成"))
            {
                target.BuildPreview();
            }

            if (GUILayout.Button("プレビュークリア"))
            {
                target.ClearPreview();
            }
        }
    }
}