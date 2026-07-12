using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class IceSpecialEffect : BubbleSpecialEffect
    {
        [SerializeField] GameObject graphicsObject;
        [SerializeField] SpriteRenderer iceSpriteRenderer;
        [SerializeField] Sprite flyingObjectSprite;

        [Space]
        [SerializeField] string crackParticleName;

        [Space]
        [SerializeField] bool useVelocity;
        [SerializeField] float minVelocity;

        [Space]
        [SerializeField] int health = 1;
        public int Health { set { health = value; } get { return health; } }

        [SerializeField] Sprite[] crackSprites;

        private int currentHealth;
        private bool isIceActive;

        private TweenCase kinematicTweenCase;

        public override bool IsMergeAllowed()
        {
            return !isIceActive;
        }

        public void UpdateSpriteIce()
        {
            if(crackSprites.IsInRange(currentHealth - 1))
            {
                iceSpriteRenderer.sprite = crackSprites[currentHealth - 1];
            }
        }

        public void PlayFadeAnimation()
        {
            iceSpriteRenderer.color = iceSpriteRenderer.color.SetAlpha(0.0f);
            iceSpriteRenderer.DOFade(1.0f, 0.4f);
        }

        public void LaunchAndDisablePhysics(Vector3 force, float delay = 0.3f)
        {
            kinematicTweenCase.KillActive();

            Rigidbody2D bubleRididbody = linkedBubble.RB;

            bubleRididbody.bodyType = RigidbodyType2D.Dynamic;
            bubleRididbody.AddForce(force, ForceMode2D.Impulse);

            kinematicTweenCase = Tween.DoFloat(1.0f, 0.0f, delay, (value) =>
            {
                bubleRididbody.SetVelocity(bubleRididbody.GetVelocity() * value);
            }).OnComplete(() =>
            {
                bubleRididbody.bodyType = RigidbodyType2D.Kinematic;
            });
        }

        public override void OnBubbleCollided(BubbleBehavior bubbleBehavior)
        {
            if (useVelocity && bubbleBehavior.RB.GetVelocity().magnitude < minVelocity) return;
            if (bubbleBehavior.BubbleSpecialEffect != null && bubbleBehavior.BubbleSpecialEffect.EffectType == Type.Ice) return;

            Hit();

#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif
        }

        public void Hit()
        {
            currentHealth--;

            AudioController.PlaySound(AudioController.AudioClips.iceCrackSound);

            UpdateSpriteIce();

            ParticlesController.PlayParticle(crackParticleName).SetPosition(transform.position);

            if (currentHealth <= 0)
            {
                kinematicTweenCase.KillActive();

                linkedBubble.RB.bodyType = RigidbodyType2D.Dynamic;
                linkedBubble.IsMagnetActive = true;

                graphicsObject.SetActive(false);

                isIceActive = false;

                DisableEffect();
            }
        }

        public override void OnBubbleMerged()
        {
            DisableEffect();
        }

        public override void OnBubbleDisabled()
        {
            kinematicTweenCase.KillActive();

            graphicsObject.SetActive(false);

            Destroy(gameObject);
        }

        public override void OnCreated()
        {
            isIceActive = true;

            linkedBubble.RB.SetVelocity(Vector2.zero);
            linkedBubble.RB.bodyType = RigidbodyType2D.Static;
            linkedBubble.IsMagnetActive = false;

            currentHealth = health;

            iceSpriteRenderer.sprite = crackSprites[crackSprites.Length - 1];
        }

        public override bool IsBubbleActive()
        {
            return !isIceActive;
        }


        public override bool IsDragAllowed()
        {
            return false;
        }
    }
}