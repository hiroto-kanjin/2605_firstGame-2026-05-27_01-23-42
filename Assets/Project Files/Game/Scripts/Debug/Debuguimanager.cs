using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BubbleMerge
{
    public class DebugUIManager : MonoBehaviour // hk追加：デバッグUI管理
    {
        public static DebugUIManager Instance { get; private set; }

        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Toggle masterToggle;
        [SerializeField] private GameObject debugContents; // ON時のみ表示するグループ
        [SerializeField] private Toggle clearJudgeToggle;
        [SerializeField] private Button spawnRandomBallsButton;
        [SerializeField] private Button forceFinisherButton;
        [SerializeField] private Toggle useDistanceJointToggle; // hk追加：SpringJoint2D⇔DistanceJoint2D切り替え
        [SerializeField] private Slider springFrequencySlider;
        [SerializeField] private Slider springDampingSlider;
        [SerializeField] private Text springFrequencyLabel;
        [SerializeField] private Text springDampingLabel;

        private const string PREFS_MASTER = "debug_master";
        private const string PREFS_CLEAR_JUDGE = "debug_clear_judge";
        private const string PREFS_USE_DISTANCE_JOINT = "debug_use_distance_joint"; // hk追加
        private const string PREFS_SPRING_FREQ = "debug_spring_freq";
        private const string PREFS_SPRING_DAMP = "debug_spring_damp";

        public bool IsClearJudgeDisabled { get; private set; } = false;

        private void Awake()
        {
            Instance = this;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (debugPanel != null) debugPanel.SetActive(false);
            return;
#endif
            if (debugPanel != null) debugPanel.SetActive(true);
            LoadPrefs();
            SetupListeners();
        }

        private void LoadPrefs() // hk追加：前回の状態を復元する
        {
            bool master = PlayerPrefs.GetInt(PREFS_MASTER, 0) == 1;
            masterToggle.isOn = master;
            if (debugContents != null) debugContents.SetActive(master);

            IsClearJudgeDisabled = PlayerPrefs.GetInt(PREFS_CLEAR_JUDGE, 0) == 1;
            if (clearJudgeToggle != null) clearJudgeToggle.isOn = IsClearJudgeDisabled;

            if (useDistanceJointToggle != null)
                useDistanceJointToggle.isOn = PlayerPrefs.GetInt(PREFS_USE_DISTANCE_JOINT, 0) == 1;

            if (springFrequencySlider != null)
                springFrequencySlider.value = PlayerPrefs.GetFloat(PREFS_SPRING_FREQ, 5f);
            if (springDampingSlider != null)
                springDampingSlider.value = PlayerPrefs.GetFloat(PREFS_SPRING_DAMP, 0.5f);

            UpdateSliderLabels();
        }

        private void SetupListeners() // hk追加
        {
            masterToggle?.onValueChanged.AddListener(OnMasterToggleChanged);
            clearJudgeToggle?.onValueChanged.AddListener(OnClearJudgeToggleChanged);
            spawnRandomBallsButton?.onClick.AddListener(OnSpawnRandomBallsClicked);
            forceFinisherButton?.onClick.AddListener(OnForceFinisherClicked);
            useDistanceJointToggle?.onValueChanged.AddListener(OnUseDistanceJointChanged);
            springFrequencySlider?.onValueChanged.AddListener(OnSpringFrequencyChanged);
            springDampingSlider?.onValueChanged.AddListener(OnSpringDampingChanged);
        }

        private void OnMasterToggleChanged(bool value) // hk追加
        {
            if (debugContents != null) debugContents.SetActive(value);
            PlayerPrefs.SetInt(PREFS_MASTER, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("デバッグモード: " + value);
        }

        private void OnClearJudgeToggleChanged(bool value) // hk追加
        {
            IsClearJudgeDisabled = value;
            PlayerPrefs.SetInt(PREFS_CLEAR_JUDGE, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("クリア判定無効: " + value);
        }

        private void OnSpawnRandomBallsClicked() // hk追加
        {
            DebugManager.Instance?.SpawnRandomBalls();
        }

        private void OnForceFinisherClicked() // hk追加
        {
            DebugManager.Instance?.ForceStartFinisher();
        }

        private void OnUseDistanceJointChanged(bool value) // hk追加
        {
            PlayerPrefs.SetInt(PREFS_USE_DISTANCE_JOINT, value ? 1 : 0);
            PlayerPrefs.Save();
            DebugManager.Instance?.SetUseDistanceJoint(value);
        }

        private void OnSpringFrequencyChanged(float value) // hk追加
        {
            PlayerPrefs.SetFloat(PREFS_SPRING_FREQ, value);
            PlayerPrefs.Save();
            UpdateSliderLabels();
            DebugManager.Instance?.UpdateSpringJointParams(value, springDampingSlider.value);
        }

        private void OnSpringDampingChanged(float value) // hk追加
        {
            PlayerPrefs.SetFloat(PREFS_SPRING_DAMP, value);
            PlayerPrefs.Save();
            UpdateSliderLabels();
            DebugManager.Instance?.UpdateSpringJointParams(springFrequencySlider.value, value);
        }

        private void UpdateSliderLabels() // hk追加
        {
            if (springFrequencyLabel != null && springFrequencySlider != null)
                springFrequencyLabel.text = "SpringFrequency: " + springFrequencySlider.value.ToString("F1");
            if (springDampingLabel != null && springDampingSlider != null)
                springDampingLabel.text = "SpringDamping: " + springDampingSlider.value.ToString("F2");
        }
    }
}