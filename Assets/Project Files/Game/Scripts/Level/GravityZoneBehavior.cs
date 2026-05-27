using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class GravityZoneBehavior : MonoBehaviour
    {
        [SerializeField] float force;

        private BoxCollider2D boxCollider;

        private Vector3 positionA;
        private Vector3 positionB;

        private Vector3 ray;

        private List<BubbleBehavior> activeBubbles = new List<BubbleBehavior>();

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();

            positionA = transform.TransformPoint(boxCollider.offset.ToVector3() + new Vector3(boxCollider.size.x, -boxCollider.size.y, 0) * 0.5f);
            positionB = transform.TransformPoint(boxCollider.offset.ToVector3() + new Vector3(-boxCollider.size.x, -boxCollider.size.y, 0) * 0.5f);

            ray = positionB - positionA;
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                Vector3 closestPoint = GetClosestPoint(activeBubbles[i].transform.position);
                Vector3 direction = (activeBubbles[i].transform.position - closestPoint).normalized;

                float power = 1 - Mathf.InverseLerp(0, transform.localScale.y, Vector3.Distance(activeBubbles[i].transform.position, closestPoint));

                activeBubbles[i].RB.AddForce(direction * force * power * Time.fixedDeltaTime, ForceMode2D.Impulse);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.parent.GetComponent<BubbleBehavior>();
                if (activeBubbles.FindIndex(x => x == bubbleBehavior) == -1)
                {
                    activeBubbles.Add(bubbleBehavior);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
            {
                BubbleBehavior bubbleBehavior = collision.transform.parent.GetComponent<BubbleBehavior>();
                if (activeBubbles.FindIndex(x => x == bubbleBehavior) != -1)
                {
                    activeBubbles.Remove(bubbleBehavior);
                }
            }
        }

        private Vector3 GetClosestPoint(Vector3 position)
        {
            float t = Vector3.Dot(position - positionA, ray) / Vector3.Dot(ray, ray);

            t = Mathf.Max(t, 0f);

            return positionA + t * ray;
        }
    }
}