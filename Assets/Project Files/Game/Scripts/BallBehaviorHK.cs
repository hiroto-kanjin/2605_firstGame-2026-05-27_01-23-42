using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        [SerializeField] private BallCategory ballCategory; // hk追加
        [SerializeField] private Branch branch; // hk追加：進化ボールの種類（Vegetables等）
        [SerializeField] private BallType ballType; // hk追加

        public BallCategory GetBallCategory() // hk追加
        {
            return ballCategory;
        }

        public Branch GetBranch() // hk追加
        {
            return branch;
        }

        public BallType GetBallType() // hk追加
        {
            return ballType;
        }
    }

    public enum BallCategory // hk追加
    {
        Evolution,
        Special,
        Nuisance
    }

    public enum BallType // hk追加
    {
        EvolutionBall_01,
        EvolutionBall_02,
        EvolutionBall_03,
        EvolutionBall_04,
        EvolutionBall_05,
        EvolutionBall_06,
        EvolutionBall_07,
        EvolutionBall_08,
        EvolutionBall_09,
        EvolutionBall_10,
        EvolutionBall_11,

        SpecialBall_001,
        SpecialBall_002,

        NuisanceBall_001
    }
}