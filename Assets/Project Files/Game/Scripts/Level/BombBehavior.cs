using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BombBehavior : MonoBehaviour
    {
        private readonly int SHADER_OVERLAY_HASH = Shader.PropertyToID("_OverlayValue");

        [SerializeField] Transform graphicsTransform;
        [SerializeField] SpriteRenderer bombSpriteRenderer;
        [SerializeField] ParticleSystem fuseParticleSystem;

        [Space]
        [SerializeField] float explosionForce;
        [SerializeField] float explosionDistance;

        [SerializeField] float explosionDelay = 2.0f;
        [SerializeField] AnimationCurve explosionAnimation;

        [SerializeField] float minVelocity;

        [SerializeField] JuicyBounce graphicsBounce;

        [Space]
        [SerializeField] string particleName;

        [Header("Development")]
        [SerializeField] bool devMode;

        private bool isExploded;

        private MaterialPropertyBlock materialPropertyBlock;
        private TweenCase explosionTween;

        private float timeMultiplier;

        private void Awake()
        {
            graphicsBounce.Init(graphicsTransform);

            materialPropertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            Reset();
            isExploded = false;

            transform.localScale = Vector3.zero;
            transform.DOScale(1.0f, 0.5f).SetCustomEasing(Ease.GetCustomEasingFunction("BackOutLight"));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.gameObject.GetComponent<BubbleBehavior>();

                if (bubbleBehavior.RB.GetVelocity().magnitude < minVelocity) return;

                ForceExplode(1.0f);
            }
        }

        public void ForceExplode(float timeMultiplier = 1.0f)
        {
            if (isExploded) return;

            isExploded = true;

            this.timeMultiplier = timeMultiplier;

            Explode();
        }

        [Button]
        private void Explode()
        {
            graphicsBounce.Bounce();

#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_HARD);
#endif

            AudioController.PlaySound(AudioController.AudioClips.bombWickSound);

            bombSpriteRenderer.color = bombSpriteRenderer.color.SetAlpha(1.0f);

            bombSpriteRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetFloat(SHADER_OVERLAY_HASH, 0.0f);
            bombSpriteRenderer.SetPropertyBlock(materialPropertyBlock);

            fuseParticleSystem.Play();

            explosionTween = Tween.DoFloat(0.0f, 1.0f, explosionDelay * timeMultiplier, (value) =>
            {
                bombSpriteRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(SHADER_OVERLAY_HASH, explosionAnimation.Evaluate(value));
                bombSpriteRenderer.SetPropertyBlock(materialPropertyBlock);
            }).OnComplete(() =>
            {
#if MODULE_HAPTIC
                Haptic.Play(Haptic.HAPTIC_HARD);
#endif

                ParticlesController.PlayParticle(particleName).SetPosition(transform.position);

                AudioController.PlaySound(AudioController.AudioClips.bombExplosionSound);

                LevelController.LevelBehavior.ActivateMinDrag();

                List<BubbleBehavior> bubbles = LevelController.LevelBehavior.GetBubbles();
                for (int i = 0; i < bubbles.Count; i++)
                {
                    Vector3 direction = bubbles[i].transform.position - transform.position;

                    float distance = Vector3.Distance(transform.position, bubbles[i].transform.position);
                    float power = 1 - Mathf.InverseLerp(0, explosionDistance, distance);

                    if (power > 0)
                    {
                        bubbles[i].RB.AddForce(direction * power * explosionForce, ForceMode2D.Impulse);

                        if (bubbles[i].BubbleSpecialEffect != null)
                        {
                            if (bubbles[i].BubbleSpecialEffect.EffectType == BubbleSpecialEffect.Type.Ice)
                            {
                                IceSpecialEffect iceSpecialEffect = (IceSpecialEffect)bubbles[i].BubbleSpecialEffect;
                                iceSpecialEffect.Hit();
                            }
                            else if (bubbles[i].BubbleSpecialEffect.EffectType == BubbleSpecialEffect.Type.Crate)
                            {
                                CrateSpecialEffect crateSpecialEffect = (CrateSpecialEffect)bubbles[i].BubbleSpecialEffect;
                                crateSpecialEffect.Hit();
                            }
                            else if (bubbles[i].BubbleSpecialEffect.EffectType == BubbleSpecialEffect.Type.Cage)
                            {
                                CageSpecialEffect cageSpecialEffect = (CageSpecialEffect)bubbles[i].BubbleSpecialEffect;
                                CageBehavior cageBehavior = cageSpecialEffect.CageBehavior;
                                if(cageBehavior != null)
                                {
                                    cageBehavior.Stun(direction.normalized);
                                }
                            }
                        }
                    }
                }

                graphicsBounce.Bounce();

                bombSpriteRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(SHADER_OVERLAY_HASH, 1.0f);
                bombSpriteRenderer.SetPropertyBlock(materialPropertyBlock);

                explosionTween = bombSpriteRenderer.DOColor(bombSpriteRenderer.color.SetAlpha(0.0f), 0.2f * timeMultiplier).OnComplete(() =>
                {
                    if (devMode)
                        return;

                    gameObject.SetActive(false);
                });
            });
        }

        [Button]
        public void Bounce()
        {
            graphicsBounce.Bounce();
        }

        [Button]
        private void Reset()
        {
            bombSpriteRenderer.color = bombSpriteRenderer.color.SetAlpha(1.0f);
            
            bombSpriteRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetFloat(SHADER_OVERLAY_HASH, 0.0f);
            bombSpriteRenderer.SetPropertyBlock(materialPropertyBlock);

            fuseParticleSystem.Stop();
        }

        private void OnDestroy()
        {
            explosionTween.KillActive();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionDistance);
        }
    }
}