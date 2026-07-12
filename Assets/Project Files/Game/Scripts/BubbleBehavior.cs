using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
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
        private BubbleScalePunch scalePunch; // hk追加
        public BubbleBehavior MergingPartner { get { return mergingPartner; } set { mergingPartner = value; } }

        private bool isMagnetActive;
        public bool IsMagnetActive { get { return isMagnetActive; } set { isMagnetActive = value; } }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            scalePunch = GetComponent<BubbleScalePunch>(); // hk追加：付いていなければnullのまま（無くても動く）
        }

        private void OnDisable()
        {
            DisableEffect();
        }

        public void Launch(Vector3 startPosition)
        {
            Vector3 directionVector = transform.position - startPosition;
            directionVector.z = 0;

            BallBehaviorHK ballHK = GetComponent<BallBehaviorHK>();
            BubblesPhysicsData pattern = ballHK != null ? ballHK.GetPhysicsPattern() : null;
            if (pattern == null) return;

            // hk修正：まず「どれくらい引っ張ったか」を0〜1の割合で求める（Visual Drag Maxが基準）
            float pullRatio = Mathf.Clamp01(directionVector.magnitude * ControlsData.ControlsPower / pattern.VisualDragMax);

            // hk修正：その割合をカーブに当てはめて、0〜1の補正値を得る
            float curveValue = ControlsData.ControlsCurve.Evaluate(pullRatio);

            // hk修正：補正値を、Forceの範囲（実際の力の強さ）に当てはめる。ここでもう縮小しない
            float magnitude = Mathf.Lerp(pattern.ForceMin, pattern.ForceMax, curveValue);

            rb.AddForce(directionVector.normalized * magnitude * forceMult, ForceMode2D.Impulse);
            graphics.SetTargetSquish(directionVector.xy().normalized, 0);
        }

        public void SetTargetSquish(Vector3 touchWorldPos)
        {
            if (bubbleSpecialEffect != null)
                return;

            BallBehaviorHK ballHK = GetComponent<BallBehaviorHK>();
            BubblesPhysicsData pattern = ballHK != null ? ballHK.GetPhysicsPattern() : null;
            if (pattern == null) return;

            var direction = (transform.position - touchWorldPos).xy();

            var t = ControlsData.ControlsCurve.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(0, pattern.VisualDragMax, direction.magnitude * ControlsData.ControlsPower)));
            graphics.SetTargetSquish(direction.normalized, t);
        }

        public void Init(BubbleData data, bool quickAppearance, Vector2 startVelocity)
        {
            DisableEffect();

            // hk追加：フィニッシャー等で一時変更された状態を必ず元に戻す（出現時の最終防衛ライン）
            gameObject.tag = PhysicsHelper.TAG_BUBBLE;
            gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
            transform.SetParent(LevelController.LevelBehavior.transform);
            //graphics.transform.localPosition = Vector3.zero; // hk追加：見た目の位置ズレをリセット

            Data = data;

            rb.bodyType = RigidbodyType2D.Dynamic;
            GetComponent<BallBehaviorHK>()?.ApplyPhysicsData(rb); // hk追加：ボールごとの物理パラメータを適用
            rb.SetVelocity(startVelocity * 0.35f);
            colliderRef.enabled = true;
            colliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

            BallBehaviorHK ballHKForAttraction = GetComponent<BallBehaviorHK>();
            BubblesPhysicsData attractionPattern = ballHKForAttraction != null ? ballHKForAttraction.GetPhysicsPattern() : null;
            if (attractionPattern != null)
            {
                OnAttractionSettingsChanged(attractionPattern.AttractionSettings);
            }

            // for test
            if (startVelocity != Vector2.zero)
            {
                ParticlesController.PlayParticle("Bubble Merge").SetPosition(transform.position);
            }

            ApplyVisualPrefab(); // hk修正：メッシュに絵を貼るのをやめ、①BallDataのプレハブを差し込む

            scaleTween.KillActive();
            transform.localScale = Vector3.zero;

            scaleTween = transform.DOScale(data.size, quickAppearance ? 0.15f : 0.5f).SetCustomEasing(Ease.GetCustomEasingFunction("BackOutLight"));

            name = gameObject.name; // hk修正：icon廃止に伴い、iconの名前ではなく自分の名前を使う（nullクラッシュ防止）

            IsMerging = false;
            isMagnetActive = true;

            MergingPartner = null;

            EnablePhysics();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            bool didMerge = false; // hk追加：今回のぶつかりで合体したかどうかの目印
            Vector2 velocityBeforeCollision = rb.linearVelocity; // hk追加：ぶつかる前の速度（向きも強さも）を保存

            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                // hk追加：フィニッシャーにぶつかったらくっつく
                FinisherBall finisherBall = collision.gameObject.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    if (!IsNuisance()) // hk追加：お邪魔ボールはくっつかない
                    {
                        finisherBall.AttachBall(rb);
                    }
                    return;
                }

                BubbleBehavior bubble = collision.gameObject.GetComponent<BubbleBehavior>();
                if (bubble == null || GetComponent<FinisherBall>() != null) return; // hk追加：自分がフィニッシャーなら何もしない

                if (bubbleSpecialEffect != null)
                    bubbleSpecialEffect.OnBubbleCollided(bubble);

                if (CanBeMerge() && bubble.CanBeMerge() && Compare(bubble) && !HKSupplyManager.Instance.IsFinisherActive()
                    && !IsNuisance() && !bubble.IsNuisance() && IsNextStageAllowed()) // hk追加：BallDataに次の段階が実在する場合のみ進化する
                {
                    didMerge = true; // hk追加：合体することが決まった目印

                    bubble.IsMerging = true;
                    IsMerging = true;

                    MergingPartner = bubble;
                    bubble.MergingPartner = this;

                    // hk追加：合体時の拡縮（速度をゼロにする前に、ぶつかった強さを使う）
                    if (scalePunch != null && scalePunch.enabled)
                        scalePunch.PlayMergePunch(rb.mass * collision.relativeVelocity.magnitude);

                    // hk追加：相手側は自分のOnCollisionEnter2Dが呼ばれない可能性があるため、ここで明示的に呼ぶ
                    if (bubble.scalePunch != null && bubble.scalePunch.enabled)
                        bubble.scalePunch.PlayMergePunch(bubble.rb.mass * collision.relativeVelocity.magnitude);

                    // hk修正：合体決定の瞬間、跳ね返りによる動きを両方ともゼロにする（バウンド防止）
                    bubble.RB.SetVelocity(Vector3.zero);
                    bubble.DisablePhysics();
                    RB.SetVelocity(Vector3.zero);
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
                    {
                        graphics.Squish(collision.GetContact(0));
                        if (scalePunch != null && scalePunch.enabled)
                            scalePunch.PlayPunch(rb.mass * collision.relativeVelocity.magnitude);
                    }
                }
            }
            else
            {
                if (!IsMerging)
                {
                    graphics.Squish(collision.GetContact(0));
                    if (scalePunch != null && scalePunch.enabled)
                        scalePunch.PlayPunch(rb.mass * collision.relativeVelocity.magnitude);
                }
            }

            AudioController.PlaySound(AudioController.AudioClips.bubbleHitSound);

            // hk追加：質量差による過剰な吹き飛びを抑える
            BallBehaviorHK ballHKForLimit = GetComponent<BallBehaviorHK>();
            BubblesPhysicsData patternForLimit = ballHKForLimit != null ? ballHKForLimit.GetPhysicsPattern() : null;
            if (patternForLimit != null && rb.linearVelocity.magnitude > patternForLimit.MaxCollisionSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * patternForLimit.MaxCollisionSpeed;
            }

            // hk追加：質量の倍率を計算する（基準より重いほど、大きくめり込む）
            float massRatio = 1f;
            BubblesPhysicsData basePattern = HKSupplyManager.Instance.BaseSquishPhysicsData;
            if (basePattern != null && basePattern.Mass > 0f && patternForLimit != null)
            {
                massRatio = patternForLimit.Mass / basePattern.Mass;
            }

            // hk追加：ぶつかった時の見た目の重なり演出（ぶつかる前の向き・強さ・質量倍率を使う）
            if (patternForLimit != null)
            {
                graphics.MoveTowardsOnCollision(
                    velocityBeforeCollision.normalized,
                    velocityBeforeCollision.magnitude * massRatio,
                    patternForLimit.SquishOnCollisionMaxDistance,
                    patternForLimit.SquishOnCollisionSensitivity
                );
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

        // hk修正：②RecipeDataの進化の枠を見て、次の段階があるか判定する
        // branch名ではなく、今のレベル(③)のrecipeIdで枠を引く
        public bool IsNextStageAllowed()
        {
            BallBehaviorHK ballHK = GetComponent<BallBehaviorHK>();
            if (ballHK == null) return false;

            // 今の段階番号（0始まり）
            int currentNumber = ballHK.GetNumber(); // hk修正：共通のGetNumber()を使う

            // 今のレベル(③)からレシピIDを取り、②の完成料理を引く
            GameLevelData level = HKGameManager.Instance.GetCurrentLevel();
            if (level == null) return false;

            RecipeEntry recipe = HKSupplyManager.Instance.RecipeData.GetRecipeById(level.recipeId);
            if (recipe == null) return false;

            // 進化の枠の段階数
            int chainLength = recipe.evolutionChain.Count;

            // 次の段階（currentNumber+1）が枠の中にあるか
            return (currentNumber + 1) < chainLength;
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
        // hk追加：フィニッシャーにくっつくときに物理判定を無効化する
        public void DisableForFinisher()
        {
            colliderRef.enabled = false;
            attractionTrigger.enabled = false;
            foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true)) // hk追加：子も含めて全コライダーを無効化
            {
                col.enabled = false;
            }
            gameObject.tag = "Untagged";
        }

        // hk追加：フィニッシャーから離すときに物理判定を元に戻す
        public void RestoreFromFinisher()
        {
            colliderRef.enabled = true;
            attractionTrigger.enabled = true;
            gameObject.tag = PhysicsHelper.TAG_BUBBLE;
        }

        public bool IsNuisance() // hk追加：お邪魔ボールかどうか
        {
            BallBehaviorHK ballHK = GetComponent<BallBehaviorHK>();
            return ballHK != null && ballHK.GetBallCategory() == BallCategory.Nuisance;
        }

        private GameObject currentVisual; // hk追加：今差し込んでいる見た目プレハブ
        private float visualBaseScale; // hk追加：拡縮演出用の固定基準サイズ
        public Transform VisualTransform => currentVisual != null ? currentVisual.transform : null; // hk追加
        public float VisualBaseScale => visualBaseScale; // hk追加

        // hk追加：①BallDataのvisualPrefabを、ボールの見た目として差し込む
        // ボールは使い回す（プール）ので、前の見た目を消してから新しいものを入れる
        private void ApplyVisualPrefab()
        {
            // 前の見た目を片付ける
            if (currentVisual != null)
            {
                Destroy(currentVisual);
                currentVisual = null;
            }

            BallBehaviorHK ballHK = GetComponent<BallBehaviorHK>();
            if (ballHK == null) return;

            BallData ballData = HKSupplyManager.Instance.SupplyData;
            if (ballData == null) return;

            // hk修正：カテゴリに関係なくBallIndexで番号を取る（Ball Type依存を廃止＝ブランチ除去）
            BallCategory category = ballHK.GetBallCategory();
            int number = ballHK.GetBallIndex();

            BallEntry entry = ballData.GetBall(category, number);

            // hk追加：確認用ログ。透明になったとき、どのcategory/numberで、何がnullだったかを出す
            if (entry == null)
            {
                Debug.LogWarning($"[透明の原因] entryが見つからない category={category} number={number}");
                return;
            }
            if (entry.visualPrefab == null)
            {
                Debug.LogWarning($"[透明の原因] visualPrefabが空 category={category} number={number} name={entry.ballName}");
                return;
            }

            // プレハブを中央に差し込む（大きさ = 固有size × 全体共通のvisualScale）
            currentVisual = Instantiate(entry.visualPrefab, transform);
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localScale = Vector3.one * entry.size * ballData.VisualScale;
            visualBaseScale = entry.size * ballData.VisualScale; // hk追加：拡縮演出の基準サイズを固定で記録
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