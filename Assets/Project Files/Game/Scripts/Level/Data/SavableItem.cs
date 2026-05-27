#pragma warning disable 649
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class SavableItem : MonoBehaviour
    {
        [SerializeField] Item item;

        public Item Item { get => item; set => item = value; }
    }
}