using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class PointerBehavior : MonoBehaviour
    {
        [SerializeField] float duration = 0.3f;
        [SerializeField] Ease.Type easing = Ease.Type.Linear;

        private TweenCase dragTweenCase;

        public void ActivateTrajectory()
        {
            dragTweenCase.KillActive();

            ITutorial tutorial = TutorialController.GetTutorial(TutorialID.FirstLevel);
            if(tutorial != null)
            {
                FirstLevelTutorial firstLevelTutorial = (FirstLevelTutorial)tutorial;
                if(!firstLevelTutorial.IsFinished && firstLevelTutorial.SelectedBubble != null)
                {
                    TrajectoryController.BeginTutorialDrag(firstLevelTutorial.SelectedBubble.transform, Vector3.up);

                    dragTweenCase = Tween.DoFloat(0.0f, 1.0f, duration, (value) =>
                    {
                        TrajectoryController.TutorialDrag(value);
                    }).SetEasing(easing);
                }
            }
        }

        public void DisableTrajectory()
        {
            TrajectoryController.EndTutorialDrag();
        }
    }
}