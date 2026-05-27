using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public abstract class FlyingObjectBehavior : MonoBehaviour
    {
        protected ObjectData objectData;
        protected RequirementBehavior requirementBehavior;
        protected SimpleCallback onCompleted;

        public void Init(ObjectData objectData, RequirementBehavior requirementBehavior, SimpleCallback onCompleted)
        {
            this.objectData = objectData;
            this.requirementBehavior = requirementBehavior;
            this.onCompleted = onCompleted;
        }

        public abstract void Activate(Sprite icon);
        public abstract void Clear();
    }
}
