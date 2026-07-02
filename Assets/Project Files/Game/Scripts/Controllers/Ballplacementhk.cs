using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class BallPlacementHK // hk追加：配置ボール1個分のデータ
    {
        public BallCategory category;  // Nuisance / Evolution / Special
        public int branchIndex;        // 系統インデックス（進化ボールのみ使用）
        public int ballLevelIndex;     // 進化段階 0〜10（進化ボールのみ使用）／お邪魔ボール種類インデックス（Nuisanceのみ使用）
        public Vector3 position;
    }
}