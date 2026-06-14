using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Watermelon;

namespace Watermelon.BubbleMerge
{
    public class BubbleBehavior : MonoBehaviour, IRequirementObject
    {
        public const float DEFAULT_RADIUS = 0.5f;

        [SerializeField] float bubbleSize = 0.8f;
        [SerializeField] float forceMult;
        [SerializeField] Collider2D colliderRef;
        [SerializeField] CircleCollider2D attractionTrigger;
        [SerializeField] BubbleGraphicsBehavior graphics;

        public Collider2D ColliderRef => colliderRef;
        public BubbleGraphicsBehavior Graphics => graphics;

        private Rigidbody2D rb;
        public Rigidbody2D RB => rb;

        public bool IsMerging { get; private set; }

        public BubbleData Data { get; set; }

        private BubbleSpecialEffect bubbleSpecialEffect;
        public BubbleSpecialEffect BubbleSpecialEffect => bubbleSpecialEffect;

        private TweenCase scaleTween;

        private BubbleBehavior mergingPartner;
        public BubbleBehavior MergingPartner { get { return mergingPartner; } set { mergingPartner = value; } }

        private bool isMagnetActive;
        public bool IsMagnetActive { get { return isMagnetActive; } set { isMagnetActive = value; } }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void OnDisable()
        {
            DisableEffect();
        }

        public void Launch(Vector3 startPosition)
        {
            Vector3 directionVector = transform.position - startPosition;
            directionVector.z = 0;

            float magnitude = Mathf.Clamp(directionVector.magnitude * ControlsData.ControlsPower, BubblesPhysicsData.ForceMin, BubblesPhysicsData.ForceMax);
            magnitude *= ControlsData.ControlsCurve.Evaluate(magnitude / BubblesPhysicsData.ForceMax);
            rb.AddForce(directionVector.normalized * magnitude * forceMult, ForceMode2D.Impulse);
            graphics.SetTargetSquish(directionVector.xy().normalized, 0);
        }

        public void SetTargetSquish(Vector3 touchWorldPos)
        {
            if (bubbleSpecialEffect != null)
                return;

            var direction = (transform.position - touchWorldPos).xy();

            var t = ControlsData.ControlsCurve.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(0, BubblesPhysicsData.ForceMax, direction.magnitude * ControlsData.ControlsPower)));
            graphics.SetTargetSquish(direction.normalized, t);
        }

        public void Init(BubbleData data, bool quickAppearance, Vector2 startVelocity)
        {
            DisableEffect();

            Data = data;

            rb.bodyType = RigidbodyType2D.Dynamic;
            GetComponent<BallBehaviorHK>()?.ApplyPhysicsData(rb); // hk追加：ボールごとの物理パラメータを適用
            rb.SetVelocity(startVelocity * 0.35f);
            colliderRef.enabled = true;
            colliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

            OnAttractionSettingsChanged(LevelController.ActiveAttractionSettings);

            // for test
            if (startVelocity != Vector2.zero)
            {
                ParticlesController.PlayParticle("Bubble Merge").SetPosition(transform.position);
            }

            graphics.SetData(data);

            scaleTween.KillActive();
            transform.localScale = Vector3.zero;

            scaleTween = transform.DOScale(data.size, quickAppearance ? 0.15f : 0.5f).SetCustomEasing(Ease.GetCustomEasingFunction("BackOutLight"));

            name = data.icon.name;

            IsMerging = false;
            isMagnetActive = true;

            MergingPartner = null;

            EnablePhysics();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubble = collision.gameObject.GetComponent<BubbleBehavior>();
                if(bubble == null || collision.gameObject.GetComponent<FinisherBall>() != null || GetComponent<FinisherBall>() != null) return; // hk追加：FinisherBall関連オブジェクトとの衝突を無視

                if (bubbleSpecialEffect != null)
                    bubbleSpecialEffect.OnBubbleCollided(bubble);

                if (CanBeMerge() && bubble.CanBeMerge() && Compare(bubble) && !HKSupplyManager.Instance.IsFinisherActive()) // hk追加：フィニッシャー出現後は合体しない
                {
                    bubble.IsMerging = true;
                    IsMerging = true;

                    MergingPartner = bubble;
                    bubble.MergingPartner = this;

                    var velocity = (bubble.RB.GetVelocity() + RB.GetVelocity()) / 2;
                    bubble.RB.SetVelocity(Vector3.zero);
                    bubble.DisablePhysics();
                    RB.SetVelocity(velocity);
                    bubble.graphics.DoMerge(transform);

                    graphics.DoMerge(bubble.transform, () =>
                    {
#if MODULE_HAPTIC
                        Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

                        AudioController.PlaySound(AudioController.AudioClips.bubbleMergeSound);

                        LevelController.LevelBehavior.OnBubblesMerged(this, bubble, (transform.position));

                        bubble.Pop();
                        Pop();

                        colliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
                        gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

                        bubble.colliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
                        bubble.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

                        MergingPartner = null;
                        IsMerging = false;

                        bubble.MergingPartner = null;
                        bubble.IsMerging = false;
                    });

                    TrajectoryController.OnBubblePoped(bubble);
                    TrajectoryController.OnBubblePoped(this);
                }
                else
                {
                    if (!IsMerging && (bubble.BubbleSpecialEffect != null && bubble.BubbleSpecialEffect.EffectType != BubbleSpecialEffect.Type.Cage))
                        graphics.Squish(collision.GetContact(0));
                }
            }
            else
            {
                if (!IsMerging)
                    graphics.Squish(collision.GetContact(0));
            }

            AudioController.PlaySound(AudioController.AudioClips.bubbleHitSound);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (!isMagnetActive)
                return;
            if (IsMerging)
                return;
            if (collision.gameObject.layer != PhysicsHelper.LAYER_MAGNET)
                return;

            if (name.Equals(collision.gameObject.transform.parent.name))
            {
                AttractionSettings attractionSettings = LevelController.ActiveAttractionSettings;

                Transform other = collision.gameObject.transform.parent;
                float distance = Vector3.Magnitude(transform.position - other.position);

                if (distance <= attractionSettings.MaxAtrDistance)
                {
                    BubbleBehavior bubbleBehavior = other.GetComponent<BubbleBehavior>();
                    if (bubbleBehavior.IsMagnetActive)
                    {
                        float force = attractionSettings.MinMaxAttractionForce.Lerp((attractionSettings.MaxAtrDistance - distance) / attractionSettings.MaxAtrDistance);

                        rb.AddForce((other.position - transform.position).normalized * force * Time.fixedDeltaTime, ForceMode2D.Impulse);

                        bubbleBehavior.rb.AddForce((transform.position - other.position).normalized * force * Time.fixedDeltaTime, ForceMode2D.Impulse);
                    }
                }
            }
        }

        public bool Compare(BubbleBehavior bubble)
        {
            return Data.branch == bubble.Data.branch && Data.stageId == bubble.Data.stageId;
        }

        public bool IsActive()
        {
            if (bubbleSpecialEffect != null)
                return bubbleSpecialEffect.IsBubbleActive();

            return true;
        }

        public bool CanBeMerge()
        {
            if (!gameObject.activeSelf)
                return false;

            if (bubbleSpecialEffect != null)
            {
                return bubbleSpecialEffect.IsMergeAllowed();
            }

            if (IsMerging)
                return false;

            return true;
        }

        public void StartMergin(BubbleBehavior mergingBubble)
        {
            IsMerging = true;
            MergingPartner = mergingBubble;
        }

        public void Pop()
        {
            if (IsMerging)
            {
                graphics.AbortMerge();
                colliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
                IsMerging = false;
                colliderRef.enabled = true;

                IsMerging = false;
                if (MergingPartner != null)
                {
                    var partner = MergingPartner;
                    MergingPartner = null;
                    partner.MergingPartner = null;
                    partner.Pop();
                }
            }

            gameObject.SetActive(false);

            if (bubbleSpecialEffect != null)
            {
                bubbleSpecialEffect.OnBubbleMerged();
            }

            LevelController.LevelBehavior.RemoveBubble(this);

            TrajectoryController.OnBubblePoped(this);

            AudioController.PlaySound(AudioController.AudioClips.bubblePopSound);
        }

        public void DisablePhysics()
        {
            colliderRef.enabled = false;
            attractionTrigger.enabled = false;
        }

        public void EnablePhysics()
        {
            colliderRef.enabled = true;
            attractionTrigger.enabled = true;
        }

        private TweenCase completeTaskMoveXTweenCase;
        public bool IsCompletedTaskBubble => completeTaskMoveXTweenCase != null && completeTaskMoveXTweenCase.IsActive;

        public void CompleteTask(Vector3 targetPosition, System.Action OnComplete)
        {
            float moveTime = 1.8f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            colliderRef.enabled = false;


            completeTaskMoveXTweenCase = transform.DOMoveX(targetPosition.x, moveTime).SetEasing(Ease.Type.SineInOut);
            transform.DOMoveZ(targetPosition.z, moveTime).SetEasing(Ease.Type.SineInOut);
            transform.DOMoveY(targetPosition.y + 2f, moveTime * 0.5f).SetEasing(Ease.Type.SineIn).OnComplete(() =>
            {
                transform.DOMoveY(targetPosition.y, moveTime * 0.5f).SetEasing(Ease.Type.SineOut).OnComplete(() =>
                {
                    OnComplete?.Invoke();
                    completeTaskMoveXTweenCase = null;
                    gameObject.SetActive(false);
                });
            });
        }

        public void ApplyEffect(BubbleSpecialEffect bubbleSpecialEffect)
        {
            DisableEffect();

            this.bubbleSpecialEffect = bubbleSpecialEffect;

            bubbleSpecialEffect.OnCreated();

            LevelController.OnSpecialEffectAdded();
        }

        public void DisableEffect()
        {
            if (bubbleSpecialEffect != null)
            {
                bubbleSpecialEffect.OnBubbleDisabled();
                bubbleSpecialEffect = null;
            }
        }

        public void SwapData(BubbleData data, SimpleCallback onComplete = null)
        {
            transform.DOScale(1f, 0.15f).OnComplete(() =>
            {
                Init(data, true, rb.GetVelocity());
                onComplete?.Invoke();
            });
        }

        public void OnRequirementMet(RequirementBehavior requirementBehavior, RequirementCallback completeRequirement)
        {
            UIGame gameUI = UIController.GetPage<UIGame>();

            Pop();

            gameUI.FlyingObjects.Activate(transform.position, Data.icon, requirementBehavior, () =>
            {
                completeRequirement?.Invoke(true);
            });
        }

        public void SetTeleport(TeleportBehavior teleport)
        {
            graphics.SetTeleport(teleport);
        }

        public void OnAttractionSettingsChanged(AttractionSettings attractionSettings)
        {
            attractionTrigger.gameObject.SetActive(attractionSettings.AttractionEnabled);
            attractionTrigger.radius = attractionSettings.MaxAtrDistance * 2;
        }

        [Button]
        private void PrintData()
        {
            Debug.Log(Data.ObjectToString());
        }
    }

    public struct BubbleData
    {
        public Branch branch;
        public int stageId;
        public Sprite icon;
        public Color color;
        public float size; // hk追加
    }
}