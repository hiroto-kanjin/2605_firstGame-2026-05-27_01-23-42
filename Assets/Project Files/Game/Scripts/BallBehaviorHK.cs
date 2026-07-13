using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class BallBehaviorHK : MonoBehaviour // hk追加
    {
        [SerializeField] private BallCategory ballCategory; // hk追加
        [SerializeField] private int ballIndex = 0; // hk修正：branch/ballType廃止。番号はballIndexに一本化
        private Rigidbody2D cachedRb; // hk追加

        public BallCategory GetBallCategory() => ballCategory; // hk追加
        public int GetBallIndex() => ballIndex; // hk追加

        // hk追加：カテゴリに関係なく、このボールの番号を返す共通の窓口。
        // 進化・特殊・お邪魔すべてballIndexに番号が入っている（ブランチ除去済み）。
        public int GetNumber()
        {
            return ballIndex;
        }

        // hk修正：branch除去に伴い、SetDataをカテゴリ＋番号の一本に統一
        public void SetData(BallCategory category, int index)
        {
            ballCategory = category;
            ballIndex = index;
        }

        // hk追加：このボールが使うBubblesPhysicsDataを取得する（3種類共通の窓口）
        public BubblesPhysicsData GetPhysicsPattern()
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
}