using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class WindZoneBehavior : MonoBehaviour
    {
        [SerializeField] ParticleSystem windParticle;

        [Space]
        [SerializeField] float force;
        [SerializeField] float initialForce;
        [SerializeField] float sideForce;

        [Space]
        [SerializeField] float sideZoneSize = 2.0f;

        private BoxCollider2D boxCollider;

        private List<BubbleBehavior> activeBubbles = new List<BubbleBehavior>();

        private Vector3 endPosition;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();

            endPosition = transform.position + new Vector3(boxCollider.size.x, 0, 0);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                LevelController.LevelBehavior.ActivateMinDrag();
                Rigidbody2D bubbleRigidbody = activeBubbles[i].RB;

                float power = 1 - Mathf.InverseLerp(0, sideZoneSize, Vector3.Distance(activeBubbles[i].transform.position, endPosition));
                if (power > 0)
                {
                    Vector3 direction = activeBubbles[i].transform.position.y > transform.position.y ? Vector3.up : -Vector3.up;
                    bubbleRigidbody.AddForce(transform.right * force * power * Time.fixedDeltaTime, ForceMode2D.Force);
                    bubbleRigidbody.AddForce(direction * sideForce * Time.fixedDeltaTime, ForceMode2D.Force);
                }
                else
                {
                    bubbleRigidbody.AddForce(transform.right * force * Time.fixedDeltaTime, ForceMode2D.Force);
                }
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

                    LevelController.LevelBehavior.ActivateMinDrag();

                    Rigidbody2D bubbleRigidbody = bubbleBehavior.RB;
                    bubbleRigidbody.SetVelocity(bubbleRigidbody.GetVelocity() * 0.9f);
                    bubbleRigidbody.AddForce(transform.right * initialForce * bubbleRigidbody.GetLinearDamping() * Time.fixedDeltaTime, ForceMode2D.Impulse);
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