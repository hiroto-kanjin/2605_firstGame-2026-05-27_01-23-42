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
        [SerializeField] private bool useDistanceJoint = false; // hk追加：true=DistanceJoint2D, false=SpringJoint2D
        [SerializeField] private float springFrequency = 5f; // hk追加：バネの強さ（SpringJoint2D用）
        [SerializeField] private float springDamping = 0.5f; // hk追加：減衰（SpringJoint2D用）
        [SerializeField] private float attachDistance = 0.5f; // hk追加：くっつく距離
        [SerializeField] private int groupSize = 5; // hk追加：何個ごとに固定するか

        private List<Rigidbody2D> attachedBalls = new List<Rigidbody2D>();
        private List<SpringJoint2D> springJoints = new List<SpringJoint2D>(); // hk追加
        private List<DistanceJoint2D> distanceJoints = new List<DistanceJoint2D>(); // hk追加

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

            // hk追加：両方のJointを追加し、useDistanceJointで排他的に有効化する
            SpringJoint2D springJoint = ballRb.gameObject.AddComponent<SpringJoint2D>();
            springJoint.connectedBody = GetComponent<Rigidbody2D>();
            springJoint.autoConfigureDistance = false;
            springJoint.distance = attachDistance;
            springJoint.frequency = springFrequency;
            springJoint.dampingRatio = springDamping;
            springJoint.enabled = !useDistanceJoint;
            springJoints.Add(springJoint);

            DistanceJoint2D distanceJoint = ballRb.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.connectedBody = GetComponent<Rigidbody2D>();
            distanceJoint.autoConfigureDistance = false;
            distanceJoint.distance = attachDistance;
            distanceJoint.maxDistanceOnly = false;
            distanceJoint.enabled = useDistanceJoint;
            distanceJoints.Add(distanceJoint);

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

                if (i < springJoints.Count && springJoints[i] != null)
                {
                    attachedBalls[i].SetVelocity(Vector2.zero);
                    Destroy(springJoints[i]);
                    springJoints[i] = null;
                }

                if (i < distanceJoints.Count && distanceJoints[i] != null)
                {
                    Destroy(distanceJoints[i]);
                    distanceJoints[i] = null;
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

                SpringJoint2D sJoint = ball.GetComponent<SpringJoint2D>();
                if (sJoint != null) Destroy(sJoint);

                DistanceJoint2D dJoint = ball.GetComponent<DistanceJoint2D>();
                if (dJoint != null) Destroy(dJoint);

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
            springJoints.Clear();
            distanceJoints.Clear();
        }

#if UNITY_EDITOR
        // hk追加：デバッグ用リアルタイムパラメータ更新（SpringJoint2D用）
        public void UpdateJointParams(float frequency, float damping)
        {
            springFrequency = frequency;
            springDamping = damping;

            for (int i = 0; i < springJoints.Count; i++)
            {
                if (springJoints[i] == null) continue;
                springJoints[i].frequency = frequency;
                springJoints[i].dampingRatio = damping;
            }
        }

        // hk追加：デバッグ用リアルタイム切り替え（SpringJoint2D ⇔ DistanceJoint2D）
        public void SetUseDistanceJoint(bool value)
        {
            useDistanceJoint = value;

            for (int i = 0; i < attachedBalls.Count; i++)
            {
                if (i < springJoints.Count && springJoints[i] != null)
                    springJoints[i].enabled = !value;

                if (i < distanceJoints.Count && distanceJoints[i] != null)
                    distanceJoints[i].enabled = value;
            }
        }
#endif

        public List<Rigidbody2D> GetAttachedBalls() => attachedBalls; // hk追加
    }
}