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

        // hk追加：カテゴリに関係なく、このボールの番号を返す共通の窓口。
        // 進化・特殊・お邪魔すべてBallIndexに番号が入っている（ブランチ除去済み）ので、それを返す。
        public int GetNumber()
        {
            return ballIndex;
        }

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
            if (ballCategory == BallCategory.Evolution)
            {
                return GetPhysicsPattern_Evolution();
            }
            else
            {
                return GetPhysicsPattern_SpecialNuisance();
            }
        }

        // hk追加：進化ボール用（新BallDataから物理を引く）
        private BubblesPhysicsData GetPhysicsPattern_Evolution()
        {
            var ballData = HKSupplyManager.Instance.SupplyData;
            int number = GetEvolutionNumber(ballType);
            var entry = ballData.GetBall(BallCategory.Evolution, number);
            if (entry == null) return null;
            return entry.physicsPattern;
        }

        // hk追加：進化ボールのballTypeを段階番号(0〜)に変換する。段階数は決め打ちしない
        public static int GetEvolutionNumber(BallType ballType)
        {
            return (int)ballType; // enumの並び順(EvolutionBall_01=0)をそのまま番号にする
        }

        // hk追加：特殊・お邪魔ボール用（新BallDataをグループ＋番号で引く）
        private BubblesPhysicsData GetPhysicsPattern_SpecialNuisance()
        {
            var ballData = HKSupplyManager.Instance.SupplyData;
            var entry = ballData.GetBall(ballCategory, ballIndex);
            if (entry == null) return null;
            return entry.physicsPattern;
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