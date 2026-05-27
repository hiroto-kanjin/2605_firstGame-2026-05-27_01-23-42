using UnityEngine;
using System.Collections.Generic;
using Watermelon.BubbleMerge;
using System;

namespace Watermelon
{
    public class FirstLevelTutorial : ITutorial
    {
        public TutorialID TutorialID => TutorialID.FirstLevel;

        private bool isActive;
        public bool IsActive => isActive;

        public bool IsFinished => saveData.isFinished;
        public int Progress => saveData.progress;

        private TutorialBaseSave saveData;

        private bool isInitialised;
        public bool IsInitialised => isInitialised;

        private BubbleBehavior selectedBubble;
        public BubbleBehavior SelectedBubble => selectedBubble;

        public FirstLevelTutorial()
        {
            TutorialController.RegisterTutorial(this);
        }

        public void Init()
        {
            if (isInitialised)
                return;

            isInitialised = true;

            // Load save file
            saveData = SaveController.GetSaveObject<TutorialBaseSave>(string.Format(ITutorial.SAVE_IDENTIFIER, TutorialID.ToString()));
        }

        public void StartTutorial()
        {
            if (isActive)
                return;

            isActive = true;

            BubbleBehavior lowerBubble = null;
            float lowerBubbleYPosition = float.MaxValue;

            List<BubbleBehavior> bubbles = LevelController.LevelBehavior.GetBubbles();
            for(int i = 0; i < bubbles.Count; i++)
            {
                if(bubbles[i].transform.position.y < lowerBubbleYPosition)
                {
                    lowerBubbleYPosition = bubbles[i].transform.position.y;

                    lowerBubble = bubbles[i];
                }
            }

            if(lowerBubble != null)
            {
                selectedBubble = lowerBubble;

                TutorialCanvasController.ActivatePointer(lowerBubble.transform.position, TutorialCanvasController.POINTER_DEFAULT);

                LevelController.LevelBehavior.OnTapHappened += OnTapHappened;
            }
        }

        private void OnTapHappened()
        {
            TutorialCanvasController.ResetPointer();

            LevelController.LevelBehavior.OnTapHappened -= OnTapHappened;

            FinishTutorial();
        }

        public void FinishTutorial()
        {
            saveData.isFinished = true;
        }

        public void Unload()
        {
            TutorialCanvasController.ResetPointer();

            LevelController.LevelBehavior.OnTapHappened -= OnTapHappened;

            isActive = false;
        }
    }
}