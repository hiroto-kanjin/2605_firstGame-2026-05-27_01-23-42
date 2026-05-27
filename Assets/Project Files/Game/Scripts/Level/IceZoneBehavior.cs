using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class IceZoneBehavior : MonoBehaviour
    {
        [SerializeField] Transform graphicsTransform;
        [SerializeField] SpriteRenderer graphicsSpriteRenderer;
        [SerializeField] float defaultWidth = 5;
        [SerializeField] float defaultHeight = 4;
        [SerializeField] float defaultScale = 0.2f;

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
                if (!bubbleBehavior.IsMerging && bubbleBehavior.BubbleSpecialEffect == null)
                {
                    LevelController.IceSpecialEffect.Health = 1;
                    LevelController.IceSpecialEffect.ApplyEffect(bubbleBehavior);

                    IceSpecialEffect iceSpecialEffect = (IceSpecialEffect)bubbleBehavior.BubbleSpecialEffect;
                    iceSpecialEffect.PlayFadeAnimation();

                    AudioController.PlaySound(AudioController.AudioClips.freezingSound);

                    graphicsBounce.Bounce();

                    iceSpecialEffect.LaunchAndDisablePhysics((transform.position - bubbleBehavior.transform.position).normalized * 10, 0.5f);
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