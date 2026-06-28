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

        public void SetData(FinisherType type, Texture icon)
        {
            finisherType = type;

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_Icon_Texture", icon);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public void AttachBall(Rigidbody2D ballRb) // hk追加
        {
            if (attachedBalls.Contains(ballRb)) return;

            attachedBalls.Add(ballRb);

            foreach (Collider2D col in ballRb.GetComponentsInChildren<Collider2D>(true))
            {
                col.enabled = false;
            }

            ballRb.gameObject.tag = "Untagged";
            foreach (Transform child in ballRb.transform)
            {
                child.gameObject.tag = "Untagged";
            }

            ballRb.bodyType = RigidbodyType2D.Dynamic;
            ballRb.mass = 0.01f;
            ballRb.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            SpringJoint2D joint = ballRb.gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = GetComponent<Rigidbody2D>();
            joint.autoConfigureDistance = false;
            joint.distance = 0.9f;
            Debug.Log("joint.distance set to: " + joint.distance); // hk追加：デバッグ用
            joint.frequency = springFrequency;
            joint.dampingRatio = springDamping;
            joints.Add(joint);

            Debug.Log("AttachBall called: " + ballRb.name);

            if (attachedBalls.Count % groupSize == 0)
            {
                FreezeGroup(attachedBalls.Count - groupSize, attachedBalls.Count - 1);
            }
        }

        private void FreezeGroup(int from, int to) // hk追加
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
                Vector2 randomOffset = Random.insideUnitCircle.normalized * 0.5f;
                attachedBalls[i].transform.position = (Vector2)transform.position + randomOffset;
            }
        }

#if UNITY_EDITOR
        public void UpdateJointParams(float frequency, float damping) // hk追加：デバッグ用リアルタイムパラメータ更新
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

        public FinisherType GetFinisherType() => finisherType; // hk追加
        public List<Rigidbody2D> GetAttachedBalls() => attachedBalls; // hk追加
    }
}