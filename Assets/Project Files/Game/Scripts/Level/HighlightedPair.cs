using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HighlightedPair
    {
        private readonly int PARTICLE_HASH = "Tutorial Highlight".GetHashCode();
        public const float HIGHLIGHT_DELAY = 10f;

        private bool isActive;
        public bool IsActive => isActive;

        private ParticleCase firstParticleCase;
        private ParticleCase secondParticleCase;

        private BubbleBehavior bubble1;
        public BubbleBehavior Bubble1 => bubble1;

        private BubbleBehavior bubble2;
        public BubbleBehavior Bubble2 => bubble2;

        private TweenCase delayTweenCase;

        public void ActivateWithDelay(float delay)
        {
            delayTweenCase.KillActive();

            delayTweenCase = Tween.DelayedCall(delay, () =>
            {
                ActivateIfPairExists();
            });
        }

        public void ActivateIfPairExists()
        {
            List<BubbleBehavior> bubbles = LevelController.LevelBehavior.GetBubbles();

            BubbleBehavior bubble1 = null;
            BubbleBehavior bubble2 = null;
            float distance = float.MaxValue;

            for (int i = 0; i < bubbles.Count - 1; i++)
            {
                for (int j = i + 1; j < bubbles.Count; j++)
                {
                    if (bubbles[i].Compare(bubbles[j]))
                    {
                        float tempDistance = Vector3.Distance(bubbles[i].transform.position, bubbles[j].transform.position);
                        if(tempDistance < distance)
                        {
                            distance = tempDistance;

                            bubble1 = bubbles[i];
                            bubble2 = bubbles[j];
                        }
                    }
                }
            }

            if(bubble1 != null && bubble2 != null)
            {
                Activate(bubble1, bubble2);
            }
        }

        public void Activate(BubbleBehavior bubble1, BubbleBehavior bubble2)
        {
            if (isActive) Disable();

            this.bubble1 = bubble1;
            this.bubble2 = bubble2;

            firstParticleCase = ParticlesController.PlayParticle(PARTICLE_HASH).SetPosition(bubble1.transform.position).SetTarget(bubble1.transform, Vector3.zero);
            secondParticleCase = ParticlesController.PlayParticle(PARTICLE_HASH).SetPosition(bubble2.transform.position).SetTarget(bubble2.transform, Vector3.zero);

            isActive = true;
        }

        public void Disable()
        {
            if (!isActive) return;

            if (firstParticleCase != null)
            {
                firstParticleCase.ForceDisable(ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (secondParticleCase != null)
            {
                secondParticleCase.ForceDisable(ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            firstParticleCase = null;
            secondParticleCase = null;

            isActive = false;
        }

        public void Reset()
        {
            delayTweenCase.KillActive();

            Disable();
        }

        public void OnRequirementComplete()
        {
            delayTweenCase.KillActive();

            ActivateWithDelay(HIGHLIGHT_DELAY);
        }

        public bool IsHighlightedBubble(BubbleBehavior bubble)
        {
            if (!isActive) return false;

            return bubble1.Compare(bubble) || bubble2.Compare(bubble);
        }
    }
}