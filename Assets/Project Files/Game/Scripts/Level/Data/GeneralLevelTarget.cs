using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class GeneralLevelTarget
    {
        [SerializeField] RequirementReceipt recipe;
        public RequirementReceipt Recipe => recipe;

        [Space]
        [SerializeField] int iceBubblesPerLevel;
        public int IceBubblesPerLevel => iceBubblesPerLevel;
        [SerializeField] int iceBubblesHealth;
        public int IceBubblesHealth => iceBubblesHealth;

        [SerializeField] int boxesPerLevel;
        public int BoxesPerLevel => boxesPerLevel;
        [SerializeField] int boxesHealth;
        public int BoxesHealth => boxesHealth;

        [SerializeField] List<BombData> bombsData = new List<BombData>();
        public List<BombData> BombsData => bombsData;

        public List<int> DoneRequirementsAmountsList { get; private set; }

        public int TotalRequirementsAmount { get; private set; }
        public int TotalDoneRequirements { get; private set; }



        public void Init()
        {
            PrepareVariables();
        }

        private void PrepareVariables()
        {
            DoneRequirementsAmountsList = new List<int>();

            TotalRequirementsAmount = 0;
            TotalDoneRequirements = 0;

            List<Requirement> requirements = recipe.Ingridients;
            for (int i = 0; i < requirements.Count; i++)
            {
                TotalRequirementsAmount += requirements[i].amount;

                DoneRequirementsAmountsList.Add(0);
            }
        }


        public int GetRequirementsLeftAmount(int setIndex)
        {
            return recipe.Ingridients[setIndex].amount - DoneRequirementsAmountsList[setIndex];
        }

        public void OnRequirementDone(int index)
        {
            DoneRequirementsAmountsList[index]++;
            TotalDoneRequirements++;
        }

        public List<Requirement> GetActiveRequirements()
        {
            List<Requirement> reqs = new List<Requirement>();

            List<Requirement> requirements = recipe.Ingridients;
            for (int i = 0; i < requirements.Count; i++)
            {
                // if there are still some reqs in current set
                if (DoneRequirementsAmountsList[i] < requirements[i].amount)
                {
                    reqs.Add(requirements[i]);
                }
                // otherwise adding requirement with level -1
                else
                {
                    reqs.Add(new Requirement(requirements[i].branch, -1));
                }
            }

            return reqs;
        }

        public void SetIceDataDev(int health, int amount)
        {
            iceBubblesPerLevel = amount;
            iceBubblesHealth = health;
        }

        public void SetBoxDataDev(int health, int amount)
        {
            boxesPerLevel = amount;
            boxesHealth = health;
        }

        public void SetReceipeDev(RequirementReceipt recipe)
        {
            this.recipe = recipe;
        }
    }
}