using System.Collections;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CageBehavior : MonoBehaviour
    {
        private readonly float STUCK_RESET_TIME = 3.0f;

        private readonly int ANIMATOR_STUN_HASH = Animator.StringToHash("IsStunned");
        private readonly int ANIMATOR_CAPTURE_TRIGGER_HASH = Animator.StringToHash("Capture");

        [SerializeField] CircleCollider2D cageCollider;
        [SerializeField] CircleCollider2D cageTrigger;

        [Space]
        [SerializeField] Animator cageAnimator;
        [SerializeField] ParticleSystem stunParticleSystem;

        [Space]
        [SerializeField] float strength = 10;
        [SerializeField] float launchForce;
        [SerializeField] float captureDuration = 0.3f;

        [Space]
        [SerializeField] float minVelocity = 0.5f;

        [Space]
        [SerializeField] float stunDuration = 2.0f;

        private bool isBubbleCaptured;
        private BubbleBehavior capturedBubble;

        private Rigidbody2D cageRigidbody;

        private bool isMovementActive;

        private float targetAngle;
        private float lastAngleRecalculationTime;
        private int rotationDirection;

        private bool isCapturing;
        private bool isStunned;
        private TweenCase stunTweenCase;
        private TweenCase collisionTweenCase;
        private TweenCase captureTweenCase;

        private void Awake()
        {
            cageTrigger.enabled = true;

            cageAnimator.SetBool(ANIMATOR_STUN_HASH, false);

            cageRigidbody = GetComponent<Rigidbody2D>();

            targetAngle = Random.Range(0, 360);
            targetAngle = 1;
        }

        private void Start()
        {
            ActivateMovement();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isBubbleCaptured) return;
            if (isStunned) return;

            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.parent.GetComponent<BubbleBehavior>();
                if (bubbleBehavior != null && !bubbleBehavior.IsMerging && bubbleBehavior.BubbleSpecialEffect == null)
                {
                    CaptureBubble(bubbleBehavior);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (isBubbleCaptured) return;
            if (isStunned) return;

            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.parent.GetComponent<BubbleBehavior>();
                if (bubbleBehavior != null && !bubbleBehavior.IsMerging && bubbleBehavior.BubbleSpecialEffect == null)
                {
                    CaptureBubble(bubbleBehavior);
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.layer == PhysicsHelper.LAYER_WALL)
            {
                if(Time.time > lastAngleRecalculationTime)
                {
                    targetAngle = Random.Range(0, 360);
                    rotationDirection = Random.value < 0.5f ? 1 : -1;

                    lastAngleRecalculationTime = Time.time + STUCK_RESET_TIME;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(collision.gameObject.layer == PhysicsHelper.LAYER_WALL)
            {
                targetAngle = targetAngle - 180 + Random.Range(-60, 60);
                rotationDirection = Random.value < 0.5f ? 1 : -1;

                lastAngleRecalculationTime = Time.time + STUCK_RESET_TIME;

                return;
            }

            if (!isBubbleCaptured) return;
            if (isCapturing) return;

            if(collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior hitBubble = collision.gameObject.GetComponent<BubbleBehavior>();

                if (hitBubble.RB.GetVelocity().magnitude < minVelocity) return;

                if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
                    Stun((transform.position - collision.transform.position).normalized);
            }
        }

        public void Stun(Vector3 direction)
        {
            if (!isBubbleCaptured) return;
            if (isCapturing) return;

            DisableMovement();

            isBubbleCaptured = false;

            Physics2D.IgnoreCollision(cageCollider, capturedBubble.ColliderRef, true);

            CircleCollider2D tempCageCollider = cageCollider;
            Collider2D tempBubbleCollider = capturedBubble.ColliderRef;

            collisionTweenCase = Tween.DelayedCall(0.5f, () =>
            {
                if (tempCageCollider != null && tempBubbleCollider != null)
                    Physics2D.IgnoreCollision(tempCageCollider, tempBubbleCollider, false);
            });

            capturedBubble.transform.SetParent(LevelController.LevelBehavior.transform);
            capturedBubble.transform.position -= direction;
            capturedBubble.DisableEffect();

            capturedBubble.RB.simulated = true;
            capturedBubble.RB.AddForce(-direction * launchForce * capturedBubble.RB.GetLinearDamping(), ForceMode2D.Impulse);

            isStunned = true;

            cageAnimator.SetBool(ANIMATOR_STUN_HASH, true);

            AudioController.PlaySound(AudioController.AudioClips.cageVomitSound);
            AudioController.PlaySound(AudioController.AudioClips.cageHitSound);

            stunParticleSystem.Play();

            stunTweenCase = Tween.DelayedCall(stunDuration, () =>
            {
                isStunned = false;

                cageTrigger.enabled = true;

                cageAnimator.SetBool(ANIMATOR_STUN_HASH, false);

                stunParticleSystem.Stop();

                ActivateMovement();
            });
        }

        private void CaptureBubble(BubbleBehavior bubbleBehavior)
        {
            if (isBubbleCaptured) return;

            isCapturing = true;
            isBubbleCaptured = true;
            capturedBubble = bubbleBehavior;

            cageTrigger.enabled = false;

            DisableMovement();

            LevelController.CageSpecialEffect.ApplyEffect(bubbleBehavior);

            CageSpecialEffect cageSpecialEffect = (CageSpecialEffect)bubbleBehavior.BubbleSpecialEffect;
            cageSpecialEffect.LinkCageBehavior(this);

            bubbleBehavior.RB.simulated = false;
            bubbleBehavior.RB.SetVelocity(Vector2.zero);

            AudioController.PlaySound(AudioController.AudioClips.cageEatSound);

            cageAnimator.SetTrigger(ANIMATOR_CAPTURE_TRIGGER_HASH);

            float distance = Vector3.Distance(transform.position, bubbleBehavior.transform.position);
            captureTweenCase = transform.DoFollow(bubbleBehavior.transform, distance / captureDuration, 0.2f, 0).SetEasing(Ease.Type.CubicIn).OnComplete(() =>
            {
                bubbleBehavior.transform.SetParent(transform);

                ActivateMovement();

                isCapturing = false;
            });
        }

        private void OnDisable()
        {
            if(isBubbleCaptured)
            {
                if(capturedBubble != null)
                {
                    Transform tempBubbleTransform = capturedBubble.transform;

                    Tween.NextFrame(() =>
                    {
                        if(tempBubbleTransform != null)
                            tempBubbleTransform.SetParent(LevelController.LevelBehavior.transform);
                    });
                }    

                isBubbleCaptured = false;
            }

            captureTweenCase.KillActive();
            stunTweenCase.KillActive();

            collisionTweenCase.CompleteActive();
        }

        #region Movement
        private void ActivateMovement()
        {
            if (isMovementActive) return;

            isMovementActive = true;
        }

        private void DisableMovement()
        {
            if (!isMovementActive) return;

            isMovementActive = false;
        }

        public void OnWingAnimationCallback()
        {
            if (!isMovementActive) return;

            targetAngle += rotationDirection * Random.Range(0, 10.0f);
            targetAngle = targetAngle % 360;

            cageRigidbody.AddForce(Quaternion.AngleAxis(targetAngle, Vector3.forward) * Vector3.right * strength, ForceMode2D.Force);
        }
        #endregion
    }
}