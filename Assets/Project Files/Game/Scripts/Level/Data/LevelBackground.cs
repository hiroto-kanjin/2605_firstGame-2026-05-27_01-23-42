using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class LevelBackground
    {
        [SerializeField, LevelEditorSetting] private LevelBackgroundType type;
        [SerializeField, LevelEditorSetting] private GameObject prefab;

        public LevelBackgroundType Type => type;
        public GameObject Prefab => prefab;
    }
}