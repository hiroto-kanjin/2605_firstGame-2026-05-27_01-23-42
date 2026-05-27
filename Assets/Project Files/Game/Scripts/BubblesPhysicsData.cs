using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Bubble Physics Data", menuName = "Data/Bubble Physics Data")]
    public class BubblesPhysicsData : ScriptableObject
    {
        private static BubblesPhysicsData instance;

        [SerializeField] DuoFloat bubbleDragRange;
        [SerializeField] float minDragDuration = 0.5f;
        [SerializeField] float dragTransitionDuration = 1f;
        [SerializeField] DuoFloat force;
        [SerializeField] AnimationCurve bubbleDragCurve;
        [SerializeField] AttractionSettings attractionSettings;

        public static float BubbleDragMin => instance.bubbleDragRange.firstValue;
        public static float BubbleDragMax => instance.bubbleDragRange.secondValue;

        public static float MinDragDuration => instance.minDragDuration;
        public static float DragTransitionDuration => instance.dragTransitionDuration;

        public static float ForceMin => instance.force.firstValue;
        public static float ForceMax => instance.force.secondValue;

        public static AnimationCurve BubbleDragCurve => instance.bubbleDragCurve;

        public static AttractionSettings DefaultAttractionSettings => instance.attractionSettings;

        public void Init()
        {
            instance = this;

            LevelController.SetAttractionSettings(attractionSettings);
        }
    }
}