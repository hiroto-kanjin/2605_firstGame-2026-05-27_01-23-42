using UnityEngine;

namespace Watermelon
{
    public static class PhysicsHelper
    {
        public static readonly int LAYER_DEFAULT = LayerMask.NameToLayer("Default");
        public static readonly int LAYER_PLAYER = LayerMask.NameToLayer("Player");

        public static readonly int LAYER_BUBBLE = LayerMask.NameToLayer("Bubble");
        public static readonly int LAYER_BUBBLE_ACTIVE = LayerMask.NameToLayer("BubbleActive");
        public static readonly int LAYER_BUBBLE_MERGING = LayerMask.NameToLayer("BubbleMerging");
        public static readonly int LAYER_WALL = LayerMask.NameToLayer("Wall");
        public static readonly int LAYER_MAGNET = LayerMask.NameToLayer("Magnet");

        public const string TAG_PLAYER = "Player";
        public const string TAG_BUBBLE = "Bubble";

        public static void Init()
        {

        }
    }
}