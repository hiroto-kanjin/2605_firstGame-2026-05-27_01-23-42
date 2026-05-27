using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon;

namespace Watermelon.BubbleMerge
{
    public class SpikesZoneBehavior : MonoBehaviour
    {
        [SerializeField] Transform graphicsTransform;
        [SerializeField] JuicyBounce graphicsBounce;
        [SerializeField] string particleName;

        private void Awake()
        {
            graphicsBounce.Init(graphicsTransform);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.GetComponent<BubbleBehavior>();

                LevelController.Level.AddBubbleToQueue(bubbleBehavior.Data);

                if (bubbleBehavior.IsMerging && bubbleBehavior.MergingPartner != null)
                {
                    LevelController.Level.AddBubbleToQueue(bubbleBehavior.MergingPartner.Data);
                    bubbleBehavior.MergingPartner.Pop();
                }

                bubbleBehavior.Pop();
                graphicsBounce.Bounce();

                ParticlesController.PlayParticle(particleName).SetPosition(bubbleBehavior.transform.position);

                LevelController.LevelBehavior.OnBubblePopped(bubbleBehavior);
            }
        }
    }
}