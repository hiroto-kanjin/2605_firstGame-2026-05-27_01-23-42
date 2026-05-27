#pragma warning disable 649

using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class LevelShape
    {
        [SerializeField, LevelEditorSetting] private LevelShapeType type;
        [SerializeField, LevelEditorSetting] private GameObject prefab;

        public LevelShapeType Type => type;
        public GameObject Prefab => prefab;
    }
}