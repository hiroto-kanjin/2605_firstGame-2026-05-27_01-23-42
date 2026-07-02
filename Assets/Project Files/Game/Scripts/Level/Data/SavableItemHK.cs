using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class SavableItemHK : MonoBehaviour // hk追加
    {
        public PlacementCategory Category;
        public int TypeIndex;
        public int BranchIndex; // hk追加：系統インデックス（進化ボールのみ使用）
    }
}