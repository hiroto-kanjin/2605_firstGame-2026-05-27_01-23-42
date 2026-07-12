using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：衝突時の拡縮演出。BubbleGraphicsBehaviorの震え演出とは別の選択肢。
    // このコンポーネントのenabledチェックボックスでオン/オフを切り替える（呼び出し側で判定）
    public class BubbleScalePunch : MonoBehaviour
    {
        [Header("通常衝突時（合体しなかった時）")]
        [Tooltip("衝撃の強さ(0〜1)に対する拡大率。基準は最大2倍（例：0で1、中間で2、1で1に戻る）")]
        [SerializeField]
        AnimationCurve punchCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 2f),
            new Keyframe(1f, 1f)
        );
        [SerializeField] float duration = 0.2f;
        [Tooltip("これ未満の強さ（質量×速度）では反応しない")]
        [SerializeField] float minStrength = 1f;
        [Tooltip("これ以上の強さで、拡縮が最大（カーブそのまま）になる")]
        [SerializeField] float maxStrength = 5f;

        [Header("合体時")] // hk追加
        [Tooltip("合体時の衝撃の強さ(0〜1)に対する拡大率")]
        [SerializeField]
        AnimationCurve mergeCurve = new AnimationCurve( // hk追加
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 2f),
            new Keyframe(1f, 1f)
        );
        [SerializeField] float mergeDuration = 0.2f; // hk追加
        [Tooltip("これ未満の強さでは反応しない（合体時）")]
        [SerializeField] float mergeMinStrength = 1f; // hk追加
        [Tooltip("これ以上の強さで最大になる（合体時）")]
        [SerializeField] float mergeMaxStrength = 5f; // hk追加

        private BubbleBehavior bubbleBehavior;
        private TweenCase punchTween;

        private void Awake()
        {
            bubbleBehavior = GetComponent<BubbleBehavior>();
        }

        // hk追加：impactStrength = ぶつかった相手の質量×相対速度（呼び出し元で計算）
        public void PlayPunch(float impactStrength)
        {
            Play(impactStrength, punchCurve, duration, minStrength, maxStrength);
        }

        // hk追加：合体時専用。パラメーターが別枠になっている
        public void PlayMergePunch(float impactStrength)
        {
            Play(impactStrength, mergeCurve, mergeDuration, mergeMinStrength, mergeMaxStrength);
        }

        // hk追加：通常・合体共通の再生処理（重複を避けるため1箇所にまとめた）
        private void Play(float impactStrength, AnimationCurve curve, float animDuration, float animMinStrength, float animMaxStrength)
        {
            if (impactStrength < animMinStrength) return; // 弱い衝突は無反応

            Transform visual = bubbleBehavior.VisualTransform;
            if (visual == null) return;

            float power = Mathf.InverseLerp(animMinStrength, animMaxStrength, impactStrength);
            float baseScale = bubbleBehavior.VisualBaseScale;

            punchTween.KillActive();

            punchTween = Tween.DoFloat(0f, 1f, animDuration, (float t) =>
            {
                float curveValue = curve.Evaluate(t);
                float scaleMultiplier = Mathf.Lerp(1f, curveValue, power);
                visual.localScale = Vector3.one * baseScale * scaleMultiplier;
            }).OnComplete(() =>
            {
                visual.localScale = Vector3.one * baseScale;
            });
        }
    }
}