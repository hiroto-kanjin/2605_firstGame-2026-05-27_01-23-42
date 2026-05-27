using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.IAPStore
{
    public class UIStore : UIPage
    {
        private const int OVERLAY_SORTING_LAYER = 610;
        private const float DEFAULT_STORE_HEIGHT_OFFSET = 300;

        [BoxGroup("References", "References")]
        [SerializeField] RectTransform safeAreaTransform;
        [BoxGroup("References")]
        [SerializeField] CurrencyUIPanelSimple coinsUI;

        [BoxGroup("Scroll View", "Scroll View")]
        [SerializeField] VerticalLayoutGroup layout;
        [BoxGroup("Scroll View")]
        [SerializeField] RectTransform content;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button closeButton;
        
        private IStoreElement[] offersElements;

        private bool isOverlay;
        private int defaultSortingOrder;

        private void Awake()
        {
            offersElements = new IStoreElement[content.childCount];
            for (int i = 0; i < offersElements.Length; i++)
            {
                Transform child = content.GetChild(i);

                IStoreElement storeElement = child.GetComponent<IStoreElement>();
                if(storeElement == null)
                {
                    storeElement = new DefaultStoreElement((RectTransform)child);
                }

                storeElement.Init();

                offersElements[i] = storeElement;
            }

            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        public override void Init()
        {
            defaultSortingOrder = canvas.sortingOrder;

            NotchSaveArea.RegisterRectTransform(safeAreaTransform);

            coinsUI.Init();
        }

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);

            if(isOverlay)
            {
                canvas.sortingOrder = defaultSortingOrder;

                isOverlay = false;
            }
        }

        public override void PlayShowAnimation()
        {
            float height = layout.padding.top + layout.padding.bottom + DEFAULT_STORE_HEIGHT_OFFSET;

            IStoreElement[] activeOffers = offersElements.Where(x => x.IsActive).ToArray();
            for (int i = 0; i < activeOffers.Length; i++)
            {
                IStoreElement offer = activeOffers[i];

                offer.KillTweenCases();
                offer.PlayAnimation(i);

                height += offer.Height;
            }

            height += activeOffers.Length * layout.spacing;

            closeButton.gameObject.SetActive(isOverlay);
            closeButton.transform.localScale = Vector3.zero;
            closeButton.transform.DOScale(1.0f, 0.3f, 0.2f, unscaledTime: true).SetEasing(Ease.Type.BackOut);

            content.sizeDelta = new Vector2(0, height);
            content.anchoredPosition = Vector2.zero;

            UIController.OnPageOpened(this);
        }

        public void Hide()
        {
            foreach(IStoreElement offer in offersElements)
            {
                offer.KillTweenCases();
            }

            UIController.HidePage<UIStore>();
        }

        private void OnCloseButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_HARD);
#endif

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            UIController.HidePage<UIStore>();
        }

        public void SpawnCurrencyCloud(RectTransform spawnRectTransform, CurrencyType currencyType, int amount, SimpleCallback completeCallback = null)
        {
            FloatingCloud.SpawnCurrency(currencyType.ToString(), spawnRectTransform, coinsUI.RectTransform, amount, null, completeCallback);
        }

        public static void OpenAsOverlay()
        {
            UIStore storeUI = UIController.GetPage<UIStore>();
            storeUI.isOverlay = true;

            storeUI.canvas.overrideSorting = true;
            storeUI.canvas.sortingOrder = OVERLAY_SORTING_LAYER;

            UIController.ShowPage(storeUI);
        }
    }
}