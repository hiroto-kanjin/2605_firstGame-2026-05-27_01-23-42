using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BubbleMerge;
using Watermelon.IAPStore;
using Watermelon.Map;

namespace Watermelon
{
    public class UIMainMenu : UIPage
    {
        public readonly float STORE_AD_RIGHT_OFFSET_X = 300F;

        [Space]
        [SerializeField] RectTransform tapToPlayRect;
        [SerializeField] UILevelPopUp levelPopUp;

        [Space]
        [SerializeField] UIScaleAnimation coinsLabelScalable;
        [SerializeField] CurrencyUIPanelSimple coinsPanel;

        [Space]
        [SerializeField] UIMainMenuButton storeButton;
        [SerializeField] UIMainMenuButton noAdsButton;

        [Space]
        [SerializeField] LivesIndicator indicator;
        
        [Space]
        [SerializeField] RectTransform safeAreaRectTransform;

        private TweenCase tapToPlayPingPong;
        private TweenCase showHideStoreAdButtonDelayTweenCase;

        private void OnEnable()
        {
            IAPManager.PurchaseCompleted += OnAdPurchased;
        }

        private void OnDisable()
        {
            IAPManager.PurchaseCompleted -= OnAdPurchased;
        }

        public override void Init()
        {
            coinsPanel.Init();
            coinsPanel.AddButton.onClick.AddListener(StoreButton);

            storeButton.Init(STORE_AD_RIGHT_OFFSET_X);
            noAdsButton.Init(STORE_AD_RIGHT_OFFSET_X);

            storeButton.Button.onClick.AddListener(StoreButton);           

            if (!AdsManager.IsForcedAdEnabled())
            {
                noAdsButton.Hide(true);
            } else
            {
                noAdsButton.Button.onClick.AddListener(NoAdButton);
            }

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        public void ShowLevelPopup(int levelId)
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            UILevelPopUp.Show(levelId);
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            showHideStoreAdButtonDelayTweenCase?.Kill();

            HideAdButton(true);

            ShowTapToPlay(false);

            coinsLabelScalable.Show();
            storeButton.Show(false);
            UILevelNumberText.Show(false);

            showHideStoreAdButtonDelayTweenCase = Tween.DelayedCall(0.12f, delegate
            {
                ShowAdButton(immediately: false);
            });

            MapBehavior.EnableScroll();

            indicator.transform.DOScale(1, 0.5f).SetEasing(Ease.Type.BackOut);

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            showHideStoreAdButtonDelayTweenCase?.Kill();

            HideTapToPlayText(immediately: true);

            coinsLabelScalable.Hide(immediately: true);

            HideAdButton(immediately: true);
            storeButton.Hide(immediately: true);

            indicator.transform.localScale = Vector3.zero;

            MapBehavior.DisableScroll();

            UIController.OnPageClosed(this);
        }

        #endregion

        #region Tap To Play Label

        public void ShowTapToPlay(bool immediately = true)
        {
            if (tapToPlayPingPong != null && tapToPlayPingPong.IsActive)
                tapToPlayPingPong.Kill();

            if (immediately)
            {
                tapToPlayRect.localScale = Vector3.one;

                tapToPlayPingPong = tapToPlayRect.transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

                return;
            }

            // RESET
            tapToPlayRect.localScale = Vector3.zero;

            tapToPlayRect.DOPushScale(Vector3.one * 1.2f, Vector3.one, 0.35f, 0.2f, Ease.Type.CubicOut, Ease.Type.CubicIn).OnComplete(delegate
            {

                tapToPlayPingPong = tapToPlayRect.transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

            });

        }

        public void HideTapToPlayText(bool immediately = true)
        {
            if (tapToPlayPingPong != null && tapToPlayPingPong.IsActive)
                tapToPlayPingPong.Kill();

            if (immediately)
            {
                tapToPlayRect.localScale = Vector3.zero;

                return;
            }

            tapToPlayRect.DOPushScale(Vector3.one * 1.2f, Vector3.zero, 0.2f, 0.35f, Ease.Type.CubicOut, Ease.Type.CubicIn);
        }

        #endregion

        #region Ad Button Label

        private void ShowAdButton(bool immediately = false)
        {
            if (AdsManager.IsForcedAdEnabled())
            {
                noAdsButton.Show(immediately);
            }
            else
            {
                noAdsButton.Hide(immediately: true);
            }
        }

        private void HideAdButton(bool immediately = false)
        {
            if (immediately || AdsManager.IsForcedAdEnabled()) noAdsButton.Hide(immediately);
        }

        private void OnAdPurchased(ProductKeyType productKeyType)
        {
            if (productKeyType == ProductKeyType.NoAds || productKeyType == ProductKeyType.StarterPack)
            {
                HideAdButton(immediately: true);
            }
        }

        #endregion

        #region Buttons

        public void TapToPlayButtonTemp()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            UILevelPopUp.Show(GameController.LevelID);
        }

        public void StoreButton()
        {
            if (UIController.IsDisplayed<UIStore>())
            {
                return;
            }

            UIController.HidePage<UIMainMenu>();

            UIMainMenu uiMainMenu = UIController.GetPage<UIMainMenu>();

            UILevelNumberText.Hide(false);

            UIStore.OpenAsOverlay();

            // reopening main menu only after store page was opened throug main menu
            UIController.PageClosed += OnIapStoreStoreClosed;

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        private void OnIapStoreStoreClosed(UIPage page, System.Type pageType)
        {
            if (pageType.Equals(typeof(UIStore)))
            {
                UIController.PageClosed -= OnIapStoreStoreClosed;

                UIController.ShowPage<UIMainMenu>();
            }
        }

        public void NoAdButton()
        {
            UIController.ShowPage<UINoAdsPopUp>();

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        #endregion
    }


}
