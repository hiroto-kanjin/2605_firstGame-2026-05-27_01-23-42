using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    public class UIGame : UIPage
    {
        [SerializeField] TMP_Text movesLeft;
        [SerializeField] TMP_Text finalCount; // hk追加：ファイナルカウントダウン用テキスト
        [SerializeField] TextMeshProUGUI levelText;

        [SerializeField] Image requirementsResultImage;
        public Image RequirementsResultImage => requirementsResultImage;

        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] RectTransform requirementsParent;
        public RectTransform RequirementsParent => requirementsParent;

        [SerializeField] FlyingObjects flyingObjects;
        public FlyingObjects FlyingObjects => flyingObjects;

        [SerializeField] Button gameSettingsButton;

        [Space]
        [SerializeField] TMP_Text comboText;
        [SerializeField] PUUIController powerUpsUIController;
        public PUUIController PowerUpsUIController => powerUpsUIController;

        [Header("Dev")]
        [SerializeField] TMP_InputField levelInputDev;

        public override void Init()
        {
            flyingObjects.Init();

            gameSettingsButton.onClick.AddListener(() => OnSettingsButtonClicked());

            LevelController.OnTurnChanged += OnTurnChanged;

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

            finalCount.gameObject.SetActive(false); // hk追加：最初は非表示
        }

        private void OnDestroy()
        {
            flyingObjects.Unload();
        }

        #region Show/Hide
        public override void PlayHideAnimation()
        {
            UILevelNumberText.Hide(false);
            UIController.OnPageClosed(this);
        }

        public override void PlayShowAnimation()
        {
            UILevelNumberText.Show(false);
            UIController.OnPageOpened(this);
        }
        #endregion

        public void OnLevelStarted(int level)
        {
            levelText.text = string.Format("LEVEL {0}", level + 1);
            PUController.OnLevelLoaded(level);
        }

        private void OnTurnChanged()
        {
            movesLeft.text = (LevelController.TurnsLimit - LevelController.Turn).ToString();
        }

        // hk追加：ゲーム開始時にUIを初期状態に戻す
        public void ResetCountUI()
        {
            movesLeft.gameObject.SetActive(true);
            finalCount.gameObject.SetActive(false);
        }

        // hk追加：通常カウントダウンを更新する
        public void UpdateShotsRemaining(int shotsRemaining)
        {
            movesLeft.text = shotsRemaining.ToString();
        }

        // hk追加：ファイナルカウントダウンに切り替える
        public void SwitchToFinalCountdown(int shotsRemaining)
        {
            movesLeft.gameObject.SetActive(false);
            finalCount.gameObject.SetActive(true);
            finalCount.text = shotsRemaining.ToString();
        }

        // hk追加：ファイナルカウントダウンを更新する
        public void UpdateFinalCountdown(int shotsRemaining)
        {
            finalCount.text = shotsRemaining.ToString();
        }

        #region Combo

        public void ShowCombo()
        {
            comboText.DOFade(1, 0.2f);
        }

        public void HideCombo()
        {
            comboText.DOFade(0, 0.2f);
        }

        public void SetCombo(int count)
        {
            comboText.text = $"{count}!";
        }

        #endregion

        public void ShowExitPopUp()
        {
            LevelController.OnGamePopupOpened();

            UILevelQuitPopUp.Show((exit) => {

                if (exit)
                {
                    LivesSystem.UnlockLife(true);
                    GameController.CloseLevel();
                    UIController.HidePage<UIGameSettings>();
                }
                else
                {
                    LevelController.OnGamePopupClosed();
                }

                AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            });
        }

        private void OnSettingsButtonClicked()
        {
            UIController.ShowPage<UIGameSettings>();
        }
    }
}