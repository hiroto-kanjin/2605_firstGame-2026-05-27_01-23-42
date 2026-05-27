using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Watermelon.BubbleMerge;

namespace Watermelon
{
    [RequireComponent(typeof(DevPanel))]
    public class DevPanelGameButtons : MonoBehaviour
    {
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button prevLevelButton;
        [SerializeField] Button addPUButton;
        [SerializeField] Button resetPUButton;
        [SerializeField] Button getMoneyButton;

        private DevPanel devPanel;

        private void Awake()
        {
            devPanel = GetComponent<DevPanel>();

            nextLevelButton.onClick.AddListener(() => OnNextLevelButtonClicked());
            prevLevelButton.onClick.AddListener(() => OnPrevLevelButtonClicked());
            addPUButton.onClick.AddListener(() => OnAddPUButtonClicked());
            resetPUButton.onClick.AddListener(() => OnResetPUButtonClicked());
            getMoneyButton.onClick.AddListener(() => OnGetMoneyButtonClicked());
        }

        private void OnGetMoneyButtonClicked()
        {
            CurrencyController.Add(CurrencyType.Coins, 1000);
        }

        private void OnResetPUButtonClicked()
        {
            PUBehavior[] powerUp = PUController.ActivePowerUps;
            foreach (PUBehavior pu in powerUp)
            {
                PUController.SetPowerUpAmount(pu.Settings.Type, 0);
            }
        }

        private void OnAddPUButtonClicked()
        {
            PUBehavior[] powerUp = PUController.ActivePowerUps;
            foreach (PUBehavior pu in powerUp)
            {
                PUController.SetPowerUpAmount(pu.Settings.Type, 99);
            }
        }

        private void OnPrevLevelButtonClicked()
        {
            Overlay.Show(0.3f, () =>
            {
                GameController.LevelSave.Value = Mathf.Clamp(GameController.LevelID - 1, 0, int.MaxValue);
                GameController.OnLevelManuallyChanged();
                LevelController.LoadLevel(GameController.LevelID);

                SaveController.MarkAsSaveIsRequired();

                Overlay.Hide(0.3f);
            });

            devPanel.DisablePanel();
        }

        private void OnNextLevelButtonClicked()
        {
            Overlay.Show(0.3f, () =>
            {
                GameController.LevelSave.Value++;
                if (GameController.LevelID > LevelController.MaxLevelReached)
                    LevelController.MaxLevelReached = GameController.LevelID;
                GameController.OnLevelManuallyChanged();
                LevelController.LoadLevel(GameController.LevelID);

                Overlay.Hide(0.3f);
            });

            devPanel.DisablePanel();
        }
    }
}
