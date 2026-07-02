using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class DebugManager : MonoBehaviour // hk追加：デバッグ機能の処理担当
    {
        public static DebugManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SpawnRandomBalls() // hk追加：盤面にランダム進化ボールを配置する
        {
            if (LevelController.LevelBehavior == null) return;

            System.Array branches = System.Enum.GetValues(typeof(Branch));

            for (int i = 0; i < 4; i++)
            {
                Branch randomBranch = (Branch)branches.GetValue(Random.Range(0, branches.Length));
                int randomStage = Random.Range(0, 3);
                Vector3 randomPos = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(-2f, 2f),
                    0f
                );
                LevelController.LevelBehavior.SpawnBallHK(randomBranch, randomStage, randomPos);
            }

            Debug.Log("デバッグ：ランダムボール配置");
        }

        public void ForceStartFinisher() // hk追加：フィニッシャーを即起動する
        {
            if (HKSupplyManager.Instance == null) return;

            HKSupplyManager.Instance.OnRecipeCompleted();
            Debug.Log("デバッグ：フィニッシャー即起動");
        }

        public void UpdateSpringJointParams(float frequency, float damping) // hk追加：SpringJointパラメータ更新
        {
#if UNITY_EDITOR
            if (FinisherBall.CurrentInstance == null)
            {
                Debug.Log("UpdateSpringJointParams: フィニッシャーが存在しません");
                return;
            }
            FinisherBall.CurrentInstance.UpdateJointParams(frequency, damping);
            Debug.Log("UpdateSpringJointParams: frequency=" + frequency + " damping=" + damping);
#endif
        }

        public void SetUseDistanceJoint(bool value) // hk追加：SpringJoint2D⇔DistanceJoint2D切り替え
        {
#if UNITY_EDITOR
            if (FinisherBall.CurrentInstance == null)
            {
                Debug.Log("SetUseDistanceJoint: フィニッシャーが存在しません");
                return;
            }
            FinisherBall.CurrentInstance.SetUseDistanceJoint(value);
            Debug.Log("SetUseDistanceJoint: " + value);
#endif
        }
    }
}