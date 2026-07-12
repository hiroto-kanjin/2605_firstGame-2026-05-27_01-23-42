using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon.BubbleMerge
{
    public class UILevelPopUp : UIPage, IPopupWindow
    {
        [SerializeField] TMP_Text levelText;
        [SerializeField] Image resultPreview;
        [SerializeField] Button playButton;
        [SerializeField] Button closeButton;

        private int LevelId { get; set; }

        public bool IsOpened => gameObject.activeSelf;


        public override void Init()
        {
            playButton.onClick.AddListener(PlayButton);
            closeButton.onClick.AddListener(CloseButton);
            GetComponent<Button>().onClick.AddListener(OnBackgroundClicked);
        }

        public override void PlayShowAnimation()
        {
            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);
        }

        public static void Show(int levelId)
        {
            UILevelPopUp popUp = UIController.GetPage<UILevelPopUp>();
            popUp.LevelId = levelId;
            popUp.levelText.text = $"LEVEL {levelId + 1}";

            UIController.ShowPage(popUp);
        }

        public void OnBackgroundClicked()
        {
            UIController.HidePage<UILevelPopUp>();
        }

        public void CloseButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            UIController.HidePage<UILevelPopUp>();
        }

        public void PlayButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            UIController.HidePage<UILevelPopUp>();
            GameController.OnLevelStart(LevelId);
        }

    }
}