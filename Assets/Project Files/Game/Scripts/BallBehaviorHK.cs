using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        [SerializeField] private BallCategory ballCategory; // hk追加
        [SerializeField] private Branch branch; // hk追加：進化ボールのみ使用
        [SerializeField] private BallType ballType; // hk追加：進化ボールのみ使用
        [SerializeField] private int ballIndex = 0; // hk追加：特殊・お邪魔ボールのインデックス
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

        // hk追加：このボールが使うBubblesPhysicsDataを取得する（3種類共通の窓口）
        public BubblesPhysicsData GetPhysicsPattern()
        {
            var ballData = HKSupplyManager.Instance.SupplyData;

            if (ballCategory == BallCategory.Evolution)
            {
                var entry = ballData.GetEntry(branch, ballType);
                if (entry == null) return null;
                return entry.physicsPattern;
            }
            else if (ballCategory == BallCategory.Special)
            {
                var entry = ballData.GetSpecialEntry(ballIndex);
                if (entry == null) return null;
                return entry.physicsPattern;
            }
            else if (ballCategory == BallCategory.Nuisance)
            {
                var entry = ballData.GetNuisanceEntry(ballIndex);
                if (entry == null) return null;
                return entry.physicsPattern;
            }

            return null;
        }

        public void ApplyPhysicsData(Rigidbody2D rb) // hk修正：BubblesPhysicsDataから値を取得するように変更
        {
            BubblesPhysicsData pattern = GetPhysicsPattern();
            if (pattern == null) return;

            rb.mass = pattern.Mass;
            rb.linearDamping = pattern.LinearDamping;

            if (rb.sharedMaterial != null)
            {
                var mat = new PhysicsMaterial2D(rb.sharedMaterial.name + "_Instance");
                mat.friction = rb.sharedMaterial.friction;
                mat.bounciness = pattern.Bounciness;
                rb.sharedMaterial = mat;
            }

            cachedRb = rb;
        }
        // hk追加：合体しなかった場合に、本来の跳ね返りやすさを反映する
        public void RestoreBounciness(Rigidbody2D rb)
        {
            BubblesPhysicsData pattern = GetPhysicsPattern();
            if (pattern == null) return;

            if (rb.sharedMaterial != null)
            {
                var mat = new PhysicsMaterial2D(rb.sharedMaterial.name + "_Instance");
                mat.friction = rb.sharedMaterial.friction;
                mat.bounciness = pattern.Bounciness;
                rb.sharedMaterial = mat;
            }
        }


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
        SpecialBall_003,
        SpecialBall_004,
        SpecialBall_005,
        SpecialBall_006,

        NuisanceBall_001,
        NuisanceBall_002,
        NuisanceBall_003,
        NuisanceBall_004,
        NuisanceBall_005
    }
}