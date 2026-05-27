using UnityEngine;

namespace Watermelon
{
    public abstract class PUCustomSettings : PUSettings
    {
        [Group("UI")]
        [SerializeField] Color backgroundSolidColor = Color.white;
        public Color BackgroundSolidColor => backgroundSolidColor;
    }
}
