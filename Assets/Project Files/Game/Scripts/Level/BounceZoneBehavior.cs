using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BounceZoneBehavior : MonoBehaviour
    {
        [SerializeField] float force;

        [Space]
        [SerializeField] Transform graphicsTransform;
        [SerializeField] SpriteRenderer graphicsSpriteRenderer;
        [SerializeField] float defaultWidth = 5;
        [SerializeField] float defaultHeight = 4;
        [SerializeField] float defaultScale = 0.2f;

        [Space]
        [SerializeField] string particleName;

        [Space]
        [SerializeField] JuicyBounce graphicsBounce;

        private void Awake()
        {
            graphicsBounce.Init(graphicsTransform);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.GetComponent<BubbleBehavior>();
                if(!bubbleBehavior.IsMerging)
                {
                    LevelController.LevelBehavior.ActivateMinDrag();

                    ParticlesController.PlayParticle(particleName).SetPosition(transform.position).SetRotation(transform.rotation);

                    bubbleBehavior.RB.AddForce((transform.position - collision.transform.position - Random.insideUnitSphere).normalized * force, ForceMode2D.Impulse);

                    graphicsBounce.Bounce();

                    AudioController.PlaySound(AudioController.AudioClips.bouncePlatformSound);
                }
            }
        }

        [Button]
        public void Bounce()
        {
            graphicsBounce.Bounce();
        }

        [Button]
        private void Resize()
        {
            if (graphicsSpriteRenderer == null) return;

            graphicsSpriteRenderer.transform.localScale = new Vector3(defaultScale / transform.localScale.y, defaultScale / transform.localScale.x, defaultScale);
            graphicsSpriteRenderer.size = new Vector2(defaultWidth * transform.localScale.y, defaultHeight * transform.localScale.x);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }
    }
}