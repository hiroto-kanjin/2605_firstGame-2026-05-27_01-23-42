using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class PUCustomUIBehavior : PUUIBehavior
    {
        [Group("Refs")]
        [SerializeField] Image backgroundSolidImage;

        protected override void ApplyVisuals()
        {
            base.ApplyVisuals();

            PUCustomSettings customSettings = (PUCustomSettings)settings;

            backgroundSolidImage.color = customSettings.BackgroundSolidColor;
        }
    }
}
