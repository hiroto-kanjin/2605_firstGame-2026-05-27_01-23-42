using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        [SerializeField] private BallCategory ballCategory; // hk追加
        [SerializeField] private Branch branch; // hk追加：進化ボールのみ使用
        [SerializeField] private BallType ballType; // hk追加：進化ボールのみ使用
        [SerializeField] private int ballIndex = 0; // hk追加：特殊・お邪魔ボールのインデックス
        private EvolutionBallEntry physicsEntry; // hk追加：キャッシュ用
        private Rigidbody2D cachedRb; // hk追加

        public BallCategory GetBallCategory() => ballCategory; // hk追加
        public Branch GetBranch() => branch; // hk追加
        public BallType GetBallType() => ballType; // hk追加
        public int GetBallIndex() => ballIndex; // hk追加

        public void SetData(BallCategory category, Branch branch, BallType type) // hk追加：進化ボール用
        {
            ballCategory = category;
            this.branch = branch;
            ballType = type;
        }

        public void SetData(BallCategory category, int index) // hk追加：特殊・お邪魔ボール用
        {
            ballCategory = category;
            ballIndex = index;
        }

        public void ApplyPhysicsData(Rigidbody2D rb) // hk追加
        {
            Debug.Log("ApplyPhysicsData called: " + ballCategory);

            var ballData = HKSupplyManager.Instance.SupplyData;
            float mass = 1f;
            float linearDamping = 25f;
            float bounciness = 0.3f;
            AnimationCurve dampingCurve = null;

            if (ballCategory == BallCategory.Evolution)
            {
                var entry = ballData.GetEntry(branch, ballType);
                if (entry == null) return;
                mass = entry.mass;
                linearDamping = entry.linearDamping;
                bounciness = entry.bounciness;
                dampingCurve = entry.dampingCurve;
                physicsEntry = entry;
            }
            else if (ballCategory == BallCategory.Special)
            {
                var entry = ballData.GetSpecialEntry(ballIndex);
                if (entry == null) return;
                mass = entry.mass;
                linearDamping = entry.linearDamping;
                bounciness = entry.bounciness;
                dampingCurve = entry.dampingCurve;
            }
            else if (ballCategory == BallCategory.Nuisance)
            {
                var entry = ballData.GetNuisanceEntry(ballIndex);
                if (entry == null) return;
                mass = entry.mass;
                linearDamping = entry.linearDamping;
                bounciness = entry.bounciness;
                dampingCurve = entry.dampingCurve;
            }

            rb.mass = mass;
            rb.linearDamping = linearDamping;
            Debug.Log($"Set linearDamping to {linearDamping}, actual rb.linearDamping = {rb.linearDamping}");

            if (rb.sharedMaterial != null)
            {
                var mat = new PhysicsMaterial2D(rb.sharedMaterial.name + "_Instance");
                mat.friction = rb.sharedMaterial.friction;
                mat.bounciness = bounciness;
                rb.sharedMaterial = mat;
            }

            cachedRb = rb;
        }

        private void Update() { } // hk追加

        private void OnDisable() // hk追加
        {
            if (CookingAreaManager.Instance != null)
            {
                CookingAreaManager.Instance.RemoveFromPot(this);
            }
        }
    }

    public enum BallCategory // hk追加
    {
        Evolution,
        Special,
        Nuisance
    }

    public enum BallType // hk追加：進化ボールのみ
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