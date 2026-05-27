using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class RequirementReceipt
    {
        public Sprite ResultPreview;
        public List<Requirement> Ingridients = new List<Requirement>();
    }
}
