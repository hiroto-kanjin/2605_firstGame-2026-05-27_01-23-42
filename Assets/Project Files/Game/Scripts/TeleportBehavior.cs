using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class TeleportBehavior : MonoBehaviour
    {
        [SerializeField] TeleportBehavior neighbour;
        [SerializeField] Transform teleportPoint;
        [SerializeField] Transform forwardPoint;

        [Space]
        [SerializeField] Transform upPos;
        [SerializeField] Transform downPos;

        public Vector3 teleportPosition => teleportPoint.position;
        public Vector3 forwardDirection => (forwardPoint.position - teleportPoint.position).normalized;

        private List<BubbleBehavior> teleportedBubbles = new List<BubbleBehavior>();

        Dictionary<BubbleBehavior, float> times = new Dictionary<BubbleBehavior, float>();

        private PointPos InsidePosition { get; set; }
        public TeleportBehavior Neighbour { get => neighbour; set => neighbour = value; }

        private void Start()
        {
            InsidePosition = GetPointPos(teleportPosition);
        }

        public void TeleportBubble(BubbleBehavior bubble)
        {
            if (!teleportedBubbles.Contains(bubble))
            {
                teleportedBubbles.Add(bubble);
                times.Add(bubble, Time.time);
            }
            

            bubble.transform.position = teleportPoint.position;

            bubble.RB.AddForce(forwardDirection * 10, ForceMode2D.Impulse);

            bubble.SetTeleport(this);
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != PhysicsHelper.LAYER_BUBBLE) return;

            BubbleBehavior bubble = other.attachedRigidbody.GetComponent<BubbleBehavior>();

            if (bubble.IsMerging) return;
            if (bubble.BubbleSpecialEffect != null) return;
            if (teleportedBubbles.Contains(bubble)) return;

            bubble.DisablePhysics();
            bubble.RB.SetVelocity(Vector3.zero);
            bubble.DOMove(teleportPoint.position, 0.15f).SetEasing(Ease.Type.QuadIn).OnComplete(() =>
            {
                neighbour.TeleportBubble(bubble);

                if (teleportedBubbles.Contains(bubble))
                {
                    teleportedBubbles.Remove(bubble);
                    times.Remove(bubble);
                }
            });

            bubble.SetTeleport(this);
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer != PhysicsHelper.LAYER_BUBBLE || other.gameObject.layer != PhysicsHelper.LAYER_BUBBLE_MERGING) return;

            var bubble = other.attachedRigidbody.GetComponent<BubbleBehavior>();

            teleportedBubbles.Remove(bubble);
            times.Remove(bubble);
            bubble.EnablePhysics();
            bubble.SetTeleport(null);
        }

        private void Update()
        {
            if (teleportedBubbles.Count == 0) return;

            List<BubbleBehavior> removeList = new List<BubbleBehavior>();
            foreach(var bubble in times.Keys)
            {
                if (Time.time - times[bubble] > 0.15f)
                {
                    bubble.EnablePhysics();
                    bubble.SetTeleport(null);
                    removeList.Add(bubble);
                }
            }

            for (int i = 0; i < removeList.Count; i++)
            {
                teleportedBubbles.Remove(removeList[i]);
                times.Remove(removeList[i]);

                removeList[i].SetTeleport(null);
            }
        }

        public bool IsPointInside(Vector2 point)
        {
            return GetPointPos(teleportPosition) == GetPointPos(point);
        }

        private PointPos GetPointPos(Vector2 point)
        {
            var up = upPos.position;
            var down = downPos.position;

            var f = down - up;

            var torque = f.x * (point.y - up.y) - f.y * (point.x - up.x);

            if(torque > 0)
            {
                return PointPos.Left;
            } if(torque < 0)
            {
                return PointPos.Right;
            } else
            {
                return PointPos.On;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(forwardPoint.position, 0.1f);

            if(neighbour != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, neighbour.transform.position);
            }
        }

        private enum PointPos
        {
            On, Left, Right,
        }
    }
}