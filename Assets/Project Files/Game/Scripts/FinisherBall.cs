using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class FinisherBall : MonoBehaviour // hk追加
    {
        public enum FinisherType // hk追加
        {
            Fire,
            Ice,
            Oil,
            Soup,
            Thunder,
            Fermentation
        }

        [SerializeField] private FinisherType finisherType;
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("くっつき設定")]
        [SerializeField] private float springFrequency = 5f; // hk追加：バネの強さ
        [SerializeField] private float springDamping = 0.5f; // hk追加：減衰
        [SerializeField] private float attachDistance = 0.5f; // hk追加：くっつく距離
        [SerializeField] private int groupSize = 5; // hk追加：何個ごとに固定するか

        private List<Rigidbody2D> attachedBalls = new List<Rigidbody2D>();
        private List<SpringJoint2D> joints = new List<SpringJoint2D>();

        public static FinisherBall CurrentInstance { get; private set; } // hk追加

        private void Awake()
        {
            CurrentInstance = this; // hk追加
        }

        private void OnDestroy()
        {
            if (CurrentInstance == this) CurrentInstance = null; // hk追加
        }

        public FinisherType GetFinisherType() => finisherType;

        public void SetData(FinisherType type, Texture icon)
        {
            finisherType = type;

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_Icon_Texture", icon);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        // hk追加：ボールをフィニッシャーにくっつける
        public void AttachBall(Rigidbody2D ballRb)
        {
            if (attachedBalls.Contains(ballRb)) return;

            attachedBalls.Add(ballRb);

            BubbleBehavior bubbleBehavior = ballRb.GetComponent<BubbleBehavior>();
            if (bubbleBehavior != null)
            {
                bubbleBehavior.DisableForFinisher(); // hk追加：BubbleBehavior側に集約した無効化処理を呼ぶ
            }

            ballRb.gameObject.layer = PhysicsHelper.LAYER_IGNORE_RAYCAST; // hk追加：レイキャストを無視
            foreach (Transform child in ballRb.transform)
            {
                child.gameObject.tag = "Untagged";
            }

            ballRb.bodyType = RigidbodyType2D.Dynamic;
            ballRb.mass = 0.01f;

            SpringJoint2D joint = ballRb.gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = GetComponent<Rigidbody2D>();
            joint.autoConfigureDistance = false;
            joint.distance = attachDistance;
            joint.frequency = springFrequency;
            joint.dampingRatio = springDamping;
            joints.Add(joint);

            Debug.Log("AttachBall called: " + ballRb.name);

            if (attachedBalls.Count % groupSize == 0)
            {
                FreezeGroup(attachedBalls.Count - groupSize, attachedBalls.Count - 1);
            }
        }

        // hk追加：指定範囲のボールをKinematicにしてフィニッシャーの子オブジェクトにする
        private void FreezeGroup(int from, int to)
        {
            for (int i = from; i <= to; i++)
            {
                if (attachedBalls[i] == null) continue;

                if (i < joints.Count && joints[i] != null)
                {
                    attachedBalls[i].SetVelocity(Vector2.zero);
                    Destroy(joints[i]);
                    joints[i] = null;
                }

                attachedBalls[i].bodyType = RigidbodyType2D.Kinematic;
                attachedBalls[i].transform.SetParent(transform);

                Vector2 randomOffset = Random.insideUnitCircle.normalized * attachDistance; // hk追加：固定時にフィニッシャー周辺のランダムな位置へ
                attachedBalls[i].transform.position = (Vector2)transform.position + randomOffset;
                foreach (Transform child in attachedBalls[i].transform) // hk追加：固定後も子オブジェクトのタグを確実にUntaggedにする
                {
                    child.gameObject.tag = "Untagged";
                }
            }
        }

        // hk追加：くっついた全てのボールを元の状態に戻し、リストをクリアする（ClearFinisher・ResetState共通で呼ぶ）
        public void DetachAllBalls(Transform returnParent)
        {
            foreach (Rigidbody2D ball in attachedBalls)
            {
                if (ball == null) continue;

                SpringJoint2D joint = ball.GetComponent<SpringJoint2D>();
                if (joint != null) Destroy(joint);

                ball.bodyType = RigidbodyType2D.Dynamic;
                ball.mass = 1f; // hk追加：質量を元に戻す
                ball.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;
                ball.transform.SetParent(returnParent);

                BubbleBehavior bubbleBehavior = ball.GetComponent<BubbleBehavior>();
                if (bubbleBehavior != null)
                {
                    bubbleBehavior.RestoreFromFinisher(); // hk追加：BubbleBehavior側に集約した復元処理を呼ぶ
                    LevelController.LevelBehavior.RemoveBubble(bubbleBehavior);
                    bubbleBehavior.gameObject.SetActive(false);
                }
            }

            attachedBalls.Clear();
            joints.Clear();
        }

#if UNITY_EDITOR
        // hk追加：デバッグ用リアルタイムパラメータ更新
        public void UpdateJointParams(float frequency, float damping)
        {
            springFrequency = frequency;
            springDamping = damping;

            for (int i = 0; i < joints.Count; i++)
            {
                if (joints[i] == null) continue;
                joints[i].frequency = frequency;
                joints[i].dampingRatio = damping;
            }
        }
#endif

        public List<Rigidbody2D> GetAttachedBalls() => attachedBalls; // hk追加
    }
}