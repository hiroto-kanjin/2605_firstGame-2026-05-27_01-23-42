using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class UIGameOver : UIPage
    {
        [SerializeField] UIScaleAnimation levelFailed;

        [SerializeField] UIFadeAnimation backgroundFade;

        [SerializeField] UIScaleAnimation tryAgainButtonScalable;
        [SerializeField] Button tryAgainButton;
        [SerializeField] Button quitInMenuButton;
        [SerializeField] Transform levelResultsHolder;
        [SerializeField] LivesIndicator livesIndicator;
        [SerializeField] RectTransform safeAreaRectTransform;

        private TweenCase continuePingPongCase;
        private List<RequirementBehavior> requirements = new List<RequirementBehavior>();

        [NonSerialized]
        public float HiddenPageDelay = 0f;

        public override void Init()
        {
            tryAgainButton.onClick.AddListener(TryAgainButton);
            quitInMenuButton.onClick.AddListener(QuitInMenuButton);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            levelFailed.Hide(immediately: true);
            tryAgainButtonScalable.Hide(immediately: true);

            float fadeDuration = 0.3f;
            backgroundFade.Show(fadeDuration);

            Tween.DelayedCall(fadeDuration * 0.8f, delegate
            {

                levelFailed.Show();

                tryAgainButtonScalable.Show(scaleMultiplier: 1.05f);

                continuePingPongCase = tryAgainButtonScalable.Transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

                UIController.OnPageOpened(this);
            });

            List<RequirementBehavior> activeReqsList = LevelController.LevelBehavior.GetRequirements();

            for (int i = 0; i < requirements.Count; i++)
            {
                requirements[i].ClearAndHide();
            }

            for (int i = 0; i < activeReqsList.Count; i++)
            {
                EvolutionBranch branch = LevelController.Database.GetBranch(activeReqsList[i].Requirement.branch);

                GameObject requirementObject = Instantiate(branch.requirementUIPrefab);

                requirementObject.transform.SetParent(levelResultsHolder);
                requirementObject.transform.ResetLocal();

                RequirementBehavior requirement = requirementObject.GetComponent<RequirementBehavior>();

                requirement.Init(activeReqsList[i].Requirement, i);

                if (activeReqsList[i].IsSetCompleted)
                {
                    requirement.SetVisuallyCompleted();
                }
                else
                {
                    requirement.SetVisuallyFailed();
                }

                requirements.Add(requirement);
            }
        }

        public override void PlayHideAnimation()
        {
            HiddenPageDelay = 0.3f;

            backgroundFade.Hide(0.3f);

            Tween.DelayedCall(0.3f, delegate
            {

                if (continuePingPongCase != null && continuePingPongCase.IsActive)
                    continuePingPongCase.Kill();

                UIController.OnPageClosed(this);
            });
        }

        #endregion

        #region Buttons 

        public void TryAgainButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
            {
                LivesSystem.LockLife();

                GameController.ReplayLevel();
            } 
            else
            {
                UIAddLivesPanel.Show((lifeRecieved) =>
                {
                    if(lifeRecieved)
                    {
                        GameController.ReplayLevel();
                    }
                });
            }
        }

        public void QuitInMenuButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            GameController.CloseLevel();
            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIMainMenu>();
        }

        #endregion
    }
}