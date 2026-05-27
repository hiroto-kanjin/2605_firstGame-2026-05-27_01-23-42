using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon;

namespace Watermelon.BubbleMerge
{
    public abstract class BubbleSpecialEffect : MonoBehaviour
    {
        [SerializeField] Type effectType;
        public Type EffectType => effectType;

        protected BubbleBehavior linkedBubble;

        public abstract void OnCreated();
        public abstract void OnBubbleMerged();
        public abstract void OnBubbleCollided(BubbleBehavior bubbleBehavior);
        public abstract bool IsMergeAllowed();
        public abstract void OnBubbleDisabled();
        public abstract bool IsDragAllowed();

        public virtual bool IsBubbleActive()
        {
            return true;
        }

        public void DisableEffect()
        {
            if (linkedBubble != null)
            {
                linkedBubble.DisableEffect();
            }
        }

        public void ApplyEffect(BubbleBehavior bubbleBehavior)
        {
            GameObject effectObject = Instantiate(gameObject);
            effectObject.transform.SetParent(bubbleBehavior.transform);
            effectObject.transform.ResetLocal();

            BubbleSpecialEffect bubbleSpecialEffect = effectObject.GetComponent<BubbleSpecialEffect>();
            bubbleSpecialEffect.linkedBubble = bubbleBehavior;

            bubbleBehavior.ApplyEffect(bubbleSpecialEffect);
        }

        public enum Type
        {
            Ice = 0,
            Crate = 1,
            Cage = 2,
        }
    }
}