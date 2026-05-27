using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class BubbleSpawnData
    {
        public Branch branch;
        public int stageId;

        public int iceHP;
        public int boxHP;

        public bool HasEffect => iceHP > 0 || boxHP > 0;
    }
}
