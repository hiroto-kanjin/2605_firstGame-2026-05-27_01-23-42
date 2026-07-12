using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CrateSpecialEffect : BubbleSpecialEffect
    {
        [SerializeField] GameObject graphicsObject;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Space]
        [SerializeField] PhysicsMaterial2D physicMaterial;
        [SerializeField] float mass = 1.0f;
        [SerializeField] float linearDrag = 0.4f;

        [Space]
        [SerializeField] string crackParticleName;

        [Space]
        [SerializeField] bool useVelocity;
        [SerializeField] float minVelocity;

        [Space]
        [SerializeField] int dropBubbles = 3;
        [SerializeField] float dropForce = 2;

        [SerializeField] int health = 1;
        public int Health { set { health = value; } get { return health; } }

        [SerializeField] Sprite[] crackSprites;

        private int currentHealth;

        private PhysicsMaterial2D tempPhysicMaterial;
        private float tempMass = 1.0f;
        private float tempLinearDrag = 0.4f;

        public override void OnBubbleCollided(BubbleBehavior bubbleBehavior)
        {
            if (useVelocity && bubbleBehavior.RB.GetVelocity().magnitude < minVelocity)
                return;

#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_MEDIUM);
#endif

            Hit();
        }

        public void Hit()
        {
            currentHealth--;

            AudioController.PlaySound(AudioController.AudioClips.crateCrackSound);

            UpdateSprite();

            ParticlesController.PlayParticle(crackParticleName).SetPosition(transform.position);

            if (currentHealth <= 0)
            {
                // hk修正：旧供給（SpawnRandomBubble）で箱からボールを撒く挙動を一旦撤去。
                // あとでレシピ依存の箱として作り直す。

                graphicsObject.SetActive(false);

                DisableEffect();
            }
        }

        public override void OnBubbleMerged()
        {
            DisableEffect();
        }

        public override void OnBubbleDisabled()
        {
            linkedBubble.RB.sharedMaterial = tempPhysicMaterial;
            linkedBubble.RB.mass = tempMass;
            linkedBubble.RB.SetLinearDamping(tempLinearDrag);

            linkedBubble.IsMagnetActive = true;
            linkedBubble.Graphics.MeshRenderer.enabled = true;

            graphicsObject.SetActive(false);

            Destroy(gameObject);
        }

        public override void OnCreated()
        {
            linkedBubble.Graphics.MeshRenderer.enabled = false;

            Rigidbody2D rigidbody2D = linkedBubble.RB;
            tempPhysicMaterial = rigidbody2D.sharedMaterial;
            tempLinearDrag = rigidbody2D.GetLinearDamping();
            tempMass = rigidbody2D.mass;

            rigidbody2D.sharedMaterial = physicMaterial;
            rigidbody2D.SetLinearDamping(linearDrag);
            rigidbody2D.mass = mass;

            linkedBubble.IsMagnetActive = false;

            currentHealth = health;

            UpdateSprite();
        }

        public void UpdateSprite()
        {
            int spriteIndex = Mathf.Clamp(currentHealth - 1, 0, crackSprites.Length);
            if (crackSprites.IsInRange(spriteIndex))
            {
                spriteRenderer.sprite = crackSprites[spriteIndex];
            }
        }

        public override bool IsBubbleActive()
        {
            return false;
        }

        public override bool IsMergeAllowed()
        {
            return false;
        }

        public override bool IsDragAllowed()
        {
            return true;
        }
    }
}