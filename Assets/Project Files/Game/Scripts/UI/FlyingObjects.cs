using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    [System.Serializable]
    public class FlyingObjects
    {
        [SerializeField] Transform containerTransform;

        [Space]
        [SerializeField] ObjectData defaultCase;
        [SerializeField] ObjectData[] customCases;

        public void Init()
        {
            defaultCase.Init(containerTransform);
            for (int i = 0; i < customCases.Length; i++)
            {
                customCases[i].Init(containerTransform);              
            }
        }

        public void Unload()
        {
            defaultCase.Unload();
            for (int i = 0; i < customCases.Length; i++)
            {
                customCases[i].Unload();
            }
        }

        private ObjectData GetObjectData(Branch branch)
        {
            for(int i = 0; i < customCases.Length; i++)
            {
                if (customCases[i].branch == branch)
                    return customCases[i];
            }

            return defaultCase;
        }

        public FlyingObjectBehavior Activate(Vector3 position, Sprite icon, RequirementBehavior requirementBehavior, SimpleCallback onCompleted)
        {
            ObjectData objectData = GetObjectData(requirementBehavior.Requirement.branch);

            GameObject flyingObject = objectData.Pool.GetPooledObject();
            flyingObject.transform.position = position;

            FlyingObjectBehavior flyingObjectBehavior = flyingObject.GetComponent<FlyingObjectBehavior>();
            flyingObjectBehavior.Init(objectData, requirementBehavior, onCompleted);
            flyingObjectBehavior.Activate(icon);

            return flyingObjectBehavior;
        }

    }
}
