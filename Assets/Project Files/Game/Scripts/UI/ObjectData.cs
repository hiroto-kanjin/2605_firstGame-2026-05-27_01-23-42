using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    [System.Serializable]
    public class ObjectData
    {
        public Branch branch;
        public GameObject prefab;

        private Pool pool;
        public Pool Pool => pool;

        public void Init(Transform containerTransform)
        {
            pool = new Pool(prefab, $"Object_{prefab.name}", containerTransform);
        }

        public void Unload()
        {
            if(pool != null)
            {
                PoolManager.DestroyPool(pool);

                pool = null;
            }
        }
    }
}
