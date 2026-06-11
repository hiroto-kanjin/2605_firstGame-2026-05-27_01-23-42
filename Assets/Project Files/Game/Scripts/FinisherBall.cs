using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class FinisherBall : MonoBehaviour // hk追加
    {
        public enum FinisherType // hk追加
        {
            Fire,
            Ice,
            Oil,
            Soup,
            Thunder,
            Fermentation
        }

        [SerializeField] private FinisherType finisherType;

        public FinisherType GetFinisherType() // hk追加
        {
            return finisherType;
        }
    }
}