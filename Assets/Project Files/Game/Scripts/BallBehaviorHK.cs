using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        // ボールのカテゴリ（進化・特殊・お邪魔）
        [SerializeField] private BallCategory ballCategory; // hk追加

        // ボールの種類（レシピ照合で使う）
        [SerializeField] private BallType ballType; // hk追加

        public BallCategory GetBallCategory() // hk追加
        {
            return ballCategory;
        }

        public BallType GetBallType() // hk追加
        {
            return ballType;
        }
    }

    // ボールのカテゴリ定義
    public enum BallCategory // hk追加
    {
        Evolution,  // 進化ボール
        Special,    // 特殊ボール
        Nuisance    // お邪魔ボール
    }

    // ボールの種類定義（レシピで使う食材の識別子）
    public enum BallType // hk追加
    {
        // 進化ボール（11段階）
        Evo_Stage1,
        Evo_Stage2,
        Evo_Stage3,
        Evo_Stage4,
        Evo_Stage5,
        Evo_Stage6,
        Evo_Stage7,
        Evo_Stage8,
        Evo_Stage9,
        Evo_Stage10,
        Evo_Stage11,

        // 特殊ボール（今後追加）
        Special_001,
        Special_002,

        // お邪魔ボール
        Nuisance_001
    }
}