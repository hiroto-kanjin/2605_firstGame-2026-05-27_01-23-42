using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BubbleMerge
{
    public class RequirementBehavior : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text totalAmountText;
        [SerializeField] GameObject checkmarkObject;
        [SerializeField] GameObject crossObject;

        [Space]
        [SerializeField] JuicyBounce juicyBounce;

        public Requirement Requirement { get; private set; }

        public bool IsDone { get; private set; }
        public bool IsSetCompleted { get; private set; }
        public int ID { get; private set; }

        public void MarkDone()
        {
            IsDone = true;
        }

        public void Init(Requirement requirement, int id)
        {
            int prevStage = Requirement.stageId;
            Requirement = requirement;
            ID = id;
            IsSetCompleted = false;
            crossObject.SetActive(false);

            juicyBounce.Init(transform);

            if (requirement.stageId == -1)
            {
                IsSetCompleted = true;
                Requirement = new Requirement(requirement.branch, prevStage);
            }

            LevelController.CreateBubbleData(Requirement, out var data);

            iconImage.sprite = data.icon;

            if(IsSetCompleted)
            {
                checkmarkObject.SetActive(true);
                totalAmountText.gameObject.SetActive(false);
            }
            else
            {
                checkmarkObject.SetActive(false);
                totalAmountText.gameObject.SetActive(true);

                totalAmountText.text = string.Format("x{0}", LevelController.GeneralLevelTargets.GetRequirementsLeftAmount(id));
            }

            IsDone = false;
        }

        public void OnFlyingObjectHitted()
        {
            juicyBounce.Bounce();
        }

        public bool Check(BubbleData data)
        {
            return Requirement.branch == data.branch && Requirement.stageId == data.stageId;
        }

        public void OnRequirementMet()
        {

        }

        // used in level failded screen
        public void SetVisuallyCompleted()
        {
            checkmarkObject.SetActive(true);
            crossObject.SetActive(false);
            totalAmountText.gameObject.SetActive(false);
        }

        // used in level failded screen
        public void SetVisuallyFailed()
        {
            crossObject.SetActive(true);
            checkmarkObject.SetActive(false);
            totalAmountText.gameObject.SetActive(false);
        }

        public void Loose()
        {
            ClearAndHide();
        }

        public void ClearAndHide()
        {
            gameObject.SetActive(false);
        }
    }
}