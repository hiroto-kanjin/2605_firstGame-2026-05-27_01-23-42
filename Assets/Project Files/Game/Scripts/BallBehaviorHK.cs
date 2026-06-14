using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        [SerializeField] private BallCategory ballCategory; // hk追加
        [SerializeField] private Branch branch; // hk追加：進化ボールの種類（Vegetables等）
        [SerializeField] private BallType ballType; // hk追加
        private BallSupplyEntry physicsEntry; // hk追加：キャッシュ用
        private Rigidbody2D cachedRb; // hk追加

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
        // hk追加：外部から値を設定するためのメソッド
        public void SetData(BallCategory category, Branch branch, BallType type)
        {
            ballCategory = category;
            this.branch = branch;
            ballType = type;
        }
        public void ApplyPhysicsData(Rigidbody2D rb)
        {
            Debug.Log("ApplyPhysicsData called: " + ballType); // hk追加：デバッグ用
            var entry = HKSupplyManager.Instance.SupplyData.GetEntry(branch, ballType);
            if (entry == null) return;

            //entry.dampingCurve = new AnimationCurve(
            //    new Keyframe(0f, 20f),
            //    new Keyframe(1f, 0f)
            //); // hk追加：一時的に強制設定（速度0〜1でブレーキ強弱）

            rb.mass = entry.mass;
            rb.linearDamping = entry.linearDamping;
            Debug.Log($"Set linearDamping to {entry.linearDamping}, actual rb.linearDamping = {rb.linearDamping}"); // hk追加：デバッグ用

            if (rb.sharedMaterial != null)
            {
                var mat = new PhysicsMaterial2D(rb.sharedMaterial.name + "_Instance");
                mat.friction = rb.sharedMaterial.friction;
                mat.bounciness = entry.bounciness;
                rb.sharedMaterial = mat;
            }

            physicsEntry = entry; // hk追加
            cachedRb = rb; // hk追加
        }
        private void Update() // hk追加
        {
            //if (physicsEntry == null || cachedRb == null) return;

            //float speed = cachedRb.linearVelocity.magnitude;
            //cachedRb.linearDamping = physicsEntry.dampingCurve.Evaluate(speed);

            //if (speed < 0.05f) // hk追加：一定以下の速度になったら強制停止
            //    cachedRb.linearVelocity = Vector2.zero;

            //Debug.Log($"speed={speed}, damping={cachedRb.linearDamping}"); // hk追加：デバッグ用
        }
        // hk追加：このボールが無効化された時に鍋から削除する
        private void OnDisable()
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