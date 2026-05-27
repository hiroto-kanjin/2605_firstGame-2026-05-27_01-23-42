using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class GravityZoneRoundBehavior : MonoBehaviour
    {
        [SerializeField] float force;

        private CircleCollider2D circleCollider;

        private List<BubbleBehavior> activeBubbles = new List<BubbleBehavior>();

        private void Awake()
        {
            circleCollider = GetComponent<CircleCollider2D>();
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                Vector3 direction = (activeBubbles[i].transform.position - transform.position).normalized;

                float power = 1 - Mathf.InverseLerp(0, circleCollider.radius, Vector3.Distance(activeBubbles[i].transform.position, transform.position));

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
    }
}