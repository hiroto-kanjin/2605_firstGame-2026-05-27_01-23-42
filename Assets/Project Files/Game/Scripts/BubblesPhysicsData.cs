using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Bubble Physics Data", menuName = "Data/Bubble Physics Data")]
    public class BubblesPhysicsData : ScriptableObject
    {
        [SerializeField] DuoFloat bubbleDragRange;
        [SerializeField] float minDragDuration = 0.5f;
        [SerializeField] float dragTransitionDuration = 1f;
        [SerializeField] DuoFloat force;
        [SerializeField] AnimationCurve bubbleDragCurve;
        [SerializeField] AttractionSettings attractionSettings;

        [Header("Ball Physics (hk追加：ボールの性格を決める項目)")]
        [SerializeField] float mass = 1f;
        [SerializeField] float linearDamping = 1f;
        [Range(0f, 1f)]
        [SerializeField] float bounciness = 0.3f;
        [SerializeField] AnimationCurve dampingCurve = AnimationCurve.Linear(0, 10, 1, 0.5f);
        [SerializeField] float visualDragMax = 3f; // hk追加：見た目上の引っ張り判定の基準値（実際の力の強さとは別）
        [SerializeField] float maxCollisionSpeed = 15f; // hk追加：衝突直後の速度上限

        [Header("Collision Squish (hk追加：ぶつかった瞬間の見た目のめり込み演出)")]
        [SerializeField] float squishOnCollisionMaxDistance = 0.1f; // hk追加：めり込みの最大量（これ以上は近づかない）
        [SerializeField] float squishOnCollisionSensitivity = 0.05f; // hk追加：速度からめり込み量への変換の効き具合

        // hk修正：static(共通の1個だけ)をやめて、このアセット自身の値を返すようにする
        public float BubbleDragMin => bubbleDragRange.firstValue;
        public float BubbleDragMax => bubbleDragRange.secondValue;

        public float MinDragDuration => minDragDuration;
        public float DragTransitionDuration => dragTransitionDuration;

        public float ForceMin => force.firstValue;
        public float ForceMax => force.secondValue;

        public AnimationCurve BubbleDragCurve => bubbleDragCurve;

        public AttractionSettings AttractionSettings => attractionSettings;

        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float Bounciness => bounciness;
        public AnimationCurve DampingCurve => dampingCurve;
        public float VisualDragMax => visualDragMax;
        public float MaxCollisionSpeed => maxCollisionSpeed; // hk追加
        public float SquishOnCollisionMaxDistance => squishOnCollisionMaxDistance; // hk追加
        public float SquishOnCollisionSensitivity => squishOnCollisionSensitivity; // hk追加

        // hk修正：ゲーム開始時に「基準となる1個」を全体設定として引き継ぐためだけに残す
        public void Init()
        {
            LevelController.SetAttractionSettings(attractionSettings);
        }
    }
}