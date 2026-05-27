using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BubbleGraphicsBehavior : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        public MeshRenderer MeshRenderer => meshRenderer;

        private MeshFilter meshFilter;

        private MaterialPropertyBlock propertyBlock;
        private List<Vector3> verticesRef = new List<Vector3>();
        private List<Vector3> vertices = new List<Vector3>();

        [SerializeField] AnimationCurve squishCurve;
        [SerializeField] AnimationCurve distanceCurve;
        [SerializeField] float maxDist;
        [SerializeField] float maxForce;

        [Space]
        [SerializeField] AnimationCurve mergeCurve;
        [SerializeField] AnimationCurve mergeScaleCurve;

        private Coroutine squishCoroutine;
        private Coroutine mergeCoroutine;

        private TeleportBehavior teleport;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();

            propertyBlock = new MaterialPropertyBlock();

            verticesRef = new List<Vector3>(meshFilter.mesh.vertices);
            vertices = new List<Vector3>(meshFilter.mesh.vertices);
        }

        public void SetData(BubbleData data)
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.SetTexture("_Icon_Texture", data.icon.texture);
            propertyBlock.SetColor("_Color", data.color);

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetTargetSquish(Vector2 direction, float force)
        {
            var point = direction;

            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = verticesRef[i].xy();

                var dist = (vertex - point).magnitude;
                var distT = Mathf.InverseLerp(maxDist, 0, dist);

                var forceShift = maxForce * distanceCurve.Evaluate(distT) * force;

                vertices[i] = vertex - (vertex - point).normalized * forceShift * 5;
            }

            meshFilter.mesh.SetVertices(vertices);
        }

        public void Squish(ContactPoint2D contact)
        {
            if (!gameObject.activeInHierarchy) return;
            var point = new Vector3(contact.point.x, contact.point.y, transform.position.z);

            squishCoroutine = StartCoroutine(SquishCoroutine(transform.position - point, contact.normalImpulse));
        }

        private IEnumerator SquishCoroutine(Vector2 point, float impulse)
        {
            float duration = 0.4f;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                var t = time / duration;

                for(int i = 0; i < vertices.Count; i++)
                {
                    var vertex = verticesRef[i].xy();

                    var dist = (vertex - point).magnitude;
                    var distT = Mathf.InverseLerp(maxDist, 0, dist);

                    var force = maxForce * distanceCurve.Evaluate(distT) * squishCurve.Evaluate(t) * Mathf.Clamp01(impulse / 2);

                    vertices[i] = vertex + (vertex - point).normalized * force;
                }

                meshFilter.mesh.SetVertices(vertices);

                yield return null;
                
            }

            squishCoroutine = null;
        }

        public void DoMerge(Transform point, SimpleCallback onComplete = null)
        {
            if(squishCoroutine != null) StopCoroutine(squishCoroutine);
            mergeCoroutine = StartCoroutine(MergeCoroutine(point, onComplete));
        }

        private IEnumerator MergeCoroutine(Transform point, SimpleCallback onComplete = null)
        {
            float duration = 0.2f;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                var t = time / duration;

                Vector2 point2D = transform.position - point.position;
                var bublesDist = point2D.magnitude;
                var bubblesDir = point2D.normalized;

                for (int i = 0; i < vertices.Count; i++)
                {
                    var vertex = verticesRef[i].xy();

                    var dif = vertex - point2D;

                    vertices[i] = Mathf.Clamp01(1 - mergeScaleCurve.Evaluate(t)) * vertex + bubblesDir * (onComplete == null ? 1 : 0) * bublesDist * mergeCurve.Evaluate(t);
                }

                meshFilter.mesh.SetVertices(vertices);

                yield return null;
            }

            meshFilter.mesh.SetVertices(verticesRef);
            onComplete?.Invoke();

            mergeCoroutine = null;
        }

        public void AbortMerge()
        {
            if(mergeCoroutine != null)
            {
                StopCoroutine(mergeCoroutine);
                meshFilter.mesh.SetVertices(verticesRef);
            }
        }

        public void SetTeleport(TeleportBehavior teleport)
        {
            this.teleport = teleport;

            if(teleport == null) 
            {
                meshFilter.mesh.SetVertices(verticesRef);
            }
        }

        private void LateUpdate()
        {
            if(teleport != null)
            {
                var teleportPosition = transform.position - teleport.transform.position;
                if (!teleport.IsPointInside(transform.position))
                {
                    teleportPosition = transform.rotation * teleportPosition;
                }

                for (int i = 0; i < verticesRef.Count; i++)
                {
                    var vertex = verticesRef[i].xy();

                    if(teleport.IsPointInside(transform.rotation * verticesRef[i] + transform.position))
                    {
                        vertex = teleportPosition;
                    }

                    vertices[i] = vertex;
                }
                meshFilter.mesh.SetVertices(vertices);
            }
        }
    }
}