using UnityEngine;
using UnityEngine.UI;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public sealed class DefaultFlyingObjectBehavior : FlyingObjectBehavior
    {
        [SerializeField] Image iconImage;
        [SerializeField] JuicyBounce juicyBounce;

        [Space]
        [SerializeField] float duration = 1.0f;
        [SerializeField] float delay = 0.05f;
        [SerializeField] Ease.Type easing = Ease.Type.SineInOut;

        [Space]
        [SerializeField] float scale = 0.4f;
        [SerializeField] float scaleDuration = 1.1f;
        [SerializeField] AnimationCurve scaleCurve;

        public override void Activate(Sprite icon)
        {
            transform.localScale = Vector3.one;

            iconImage.sprite = icon;

            juicyBounce.Init(transform);
            juicyBounce.Bounce();

            transform.DOScale(scale, scaleDuration, delay).SetCurveEasing(scaleCurve);
            transform.DOMove(requirementBehavior.transform.position, duration, delay).SetEasing(easing).OnComplete(() =>
            {
                juicyBounce.Bounce();

                requirementBehavior.OnFlyingObjectHitted();

                Tween.DelayedCall(0.1f, () =>
                {
                    gameObject.SetActive(false);

                    onCompleted?.Invoke();
                });
            });
        }

        public override void Clear()
        {

        }
    }
}
