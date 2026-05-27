using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILevelNumberText : MonoBehaviour
    {
        private const string LEVEL_LABEL = "LEVEL {0}";
        private static UILevelNumberText instance;

        [SerializeField] UIScaleAnimation uIScalableObject;

        private static UIScaleAnimation UIScalableObject => instance.uIScalableObject;
        private static TextMeshProUGUI levelNumberText;

        private static bool IsDisplayed = false;

        private void Awake()
        {
            instance = this;
            levelNumberText = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            UpdateLevelNumber();
        }

        private void OnEnable()
        {
            GameController.OnLevelChanged += UpdateLevelNumber;
        }

        private void OnDisable()
        {
            GameController.OnLevelChanged -= UpdateLevelNumber;
        }

        public static void Show(bool immediately = true)
        {
            if (IsDisplayed)
                return;

            if (levelNumberText == null)
                return;

            IsDisplayed = true;

            levelNumberText.enabled = true;
            UIScalableObject.Show(scaleMultiplier: 1.05f, immediately: immediately);
        }

        public static void Hide(bool immediately = true)
        {
            if (!IsDisplayed)
                return;

            if (levelNumberText == null)
                return;

            if (immediately)
                IsDisplayed = false;

            UIScalableObject.Hide(scaleMultiplier: 1.05f, immediately: immediately, onCompleted: delegate
            {

                IsDisplayed = false;
                levelNumberText.enabled = false;
            });
        }

        private void UpdateLevelNumber()
        {
            levelNumberText.text = string.Format(LEVEL_LABEL, (GameController.LevelID + 1));
        }

    }
}
