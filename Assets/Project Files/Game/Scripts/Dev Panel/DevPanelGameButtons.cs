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
                int prevIndex = Mathf.Clamp(GameController.LevelID - 1, 0, LevelController.Database.GameLevels.Length - 1);
                GameController.LevelSaveId.Value = LevelController.Database.GameLevels[prevIndex].gameLevelId;
                GameController.OnLevelManuallyChanged();
                LevelController.LoadLevel(GameController.LevelSaveId.Value);

                SaveController.MarkAsSaveIsRequired();

                Overlay.Hide(0.3f);
            });

            devPanel.DisablePanel();
        }

        private void OnNextLevelButtonClicked()
        {
            Overlay.Show(0.3f, () =>
            {
                int nextIndex = Mathf.Clamp(GameController.LevelID + 1, 0, LevelController.Database.GameLevels.Length - 1);
                GameController.LevelSaveId.Value = LevelController.Database.GameLevels[nextIndex].gameLevelId;

                if (nextIndex > LevelController.MaxLevelReached)
                    LevelController.MaxLevelReached = nextIndex;

                GameController.OnLevelManuallyChanged();
                LevelController.LoadLevel(GameController.LevelSaveId.Value);

                Overlay.Hide(0.3f);
            });

            devPanel.DisablePanel();
        }
    }
}
