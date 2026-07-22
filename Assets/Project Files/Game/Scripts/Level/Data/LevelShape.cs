#pragma warning disable 649

using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class LevelShape
    {
        [SerializeField, LevelEditorSetting] private LevelShapeType type;
        [SerializeField, LevelEditorSetting] private GameObject prefab;
        [SerializeField, LevelEditorSetting] private float mergeLineY; // hk追加：発射直後のボールが盤面ボールと衝突し始める高さ（自動計算値）

        public LevelShapeType Type => type;
        public GameObject Prefab => prefab;
        public float MergeLineY => mergeLineY; // hk追加
    }
}