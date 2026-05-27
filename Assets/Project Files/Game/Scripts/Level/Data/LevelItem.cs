#pragma warning disable 649

using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class LevelItem
    {
        [SerializeField, LevelEditorSetting] private Item type;
        [SerializeField, LevelEditorSetting] private GameObject prefab;
        [SerializeField, LevelEditorSetting] private Texture2D editorTexture; //used in level editor

        public Item Type => type;

        public GameObject Prefab => prefab;
    }
}