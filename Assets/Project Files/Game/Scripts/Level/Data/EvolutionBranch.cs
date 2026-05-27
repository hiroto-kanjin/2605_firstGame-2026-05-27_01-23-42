using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class EvolutionBranch
    {
        public Branch branch;
        public Color backgroundColor;
        public GameObject requirementUIPrefab;
        public EvolutionStage[] stages;
    }
}
