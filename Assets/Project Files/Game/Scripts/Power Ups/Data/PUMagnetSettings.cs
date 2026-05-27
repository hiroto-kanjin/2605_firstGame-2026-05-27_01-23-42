using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "PU Magnet Settings", menuName = "Data/Power Ups/PU Magnet Settings")]
    public class PUMagnetSettings : PUCustomSettings
    {
        [LineSpacer]
        [SerializeField] float magnetDuration = 15;
        public float MagnetDuration => magnetDuration;

        [SerializeField] AttractionSettings attractionSettings;
        public AttractionSettings AttractionSettings => attractionSettings;

        public override void Init()
        {

        }
    }
}
