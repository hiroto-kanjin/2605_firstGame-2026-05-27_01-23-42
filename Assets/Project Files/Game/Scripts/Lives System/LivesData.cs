using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Lives Data", menuName = "Data/Lives")]
    public class LivesData : ScriptableObject
    {
        [SerializeField] int maxLivesCount = 5;
        public int MaxLivesCount => maxLivesCount;

        [Tooltip("In seconds")]
        [SerializeField] int oneLifeRestorationDuration = 1200;
        public int OneLifeRestorationDuration => oneLifeRestorationDuration;

        [Header("Reward")]
        [SerializeField] Sprite rewardPreviewSprite;
        public Sprite RewardPreviewSprite => rewardPreviewSprite;

        [Foldout("Custom reward", "Customize Reward Prefabs", 1)]
        [SerializeField] GameObject rewardPreviewPrefab;
        public GameObject RewardPreviewPrefab => rewardPreviewPrefab;

        [Foldout("Custom reward")]
        [SerializeField] GameObject rewardMaxLivesPreviewPrefab;
        public GameObject RewardMaxLivesPreviewPrefab => rewardMaxLivesPreviewPrefab;

        [Foldout("Custom reward")]
        [SerializeField] GameObject rewardInfiniteModePreviewPrefab;
        public GameObject RewardInfiniteModePreviewPrefab => rewardInfiniteModePreviewPrefab;
    }
}