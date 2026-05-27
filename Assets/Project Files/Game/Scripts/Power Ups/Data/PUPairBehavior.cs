using Watermelon.BubbleMerge;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class PUPairBehavior : PUBehavior
    {
        private TweenCaseCollection tweenCaseCollection;

        public override void Init()
        {

        }

        public override bool Activate()
        {
            BubblesPair bubblesPair = GetRandomPair();
            if (bubblesPair != null)
            {
                IsBusy = true;

                tweenCaseCollection = Tween.BeginTweenCaseCollection();

                bubblesPair.bubbleBehavior1.StartMergin(bubblesPair.bubbleBehavior2);
                bubblesPair.bubbleBehavior2.StartMergin(bubblesPair.bubbleBehavior1);

                bubblesPair.bubbleBehavior1.DisablePhysics();
                bubblesPair.bubbleBehavior2.DisablePhysics();

                bubblesPair.bubbleBehavior1.transform.DOScale(1.1f, 0.3f).SetEasing(Ease.Type.BackOut);
                bubblesPair.bubbleBehavior2.transform.DOScale(1.1f, 0.3f).SetEasing(Ease.Type.BackOut);

                bubblesPair.bubbleBehavior1.transform.DOMove(bubblesPair.bubbleBehavior2.transform.position, 0.3f).OnComplete(() =>
                {
                    AudioController.PlaySound(AudioController.AudioClips.bubbleMergeSound);

                    LevelController.LevelBehavior.OnBubblesMerged(bubblesPair.bubbleBehavior1, bubblesPair.bubbleBehavior2, (bubblesPair.bubbleBehavior1.transform.position));

                    bubblesPair.bubbleBehavior1.Pop();
                    bubblesPair.bubbleBehavior2.Pop();

                    TrajectoryController.OnBubblePoped(bubblesPair.bubbleBehavior1);
                    TrajectoryController.OnBubblePoped(bubblesPair.bubbleBehavior2);

                    IsBusy = false;
                });

                Tween.EndTweenCaseCollection();

                return true;
            }
            else
            {
                PUController.PowerUpsUIController.SpawnFloatingText("ONE OF THE ELEMENTS ISN'T AVAILABLE!");
            }

            return false;
        }

        private BubblesPair GetRandomPair()
        {
            List<RequirementBehavior> requirements = new List<RequirementBehavior>(LevelController.LevelBehavior.GetRequirements());
            requirements.Shuffle();

            for (int i = 0; i < requirements.Count; i++)
            {
                if (!requirements[i].IsSetCompleted)
                {
                    Branch selectedBranch = requirements[i].Requirement.branch;
                    for (int l = requirements[i].Requirement.stageId - 1; l >= 0; l--)
                    {
                        BubblesPair bubblesPair = LevelController.LevelBehavior.GetPair(selectedBranch, l, false);
                        if (bubblesPair != null)
                        {
                            return bubblesPair;
                        }
                    }
                }
            }

            return null;
        }

        public override void ResetBehavior()
        {
            IsBusy = false;

            if(tweenCaseCollection != null)
                tweenCaseCollection.Kill();
        }
    }
}
