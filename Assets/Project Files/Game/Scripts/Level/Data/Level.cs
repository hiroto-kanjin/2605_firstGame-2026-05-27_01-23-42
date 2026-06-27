using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class Level : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] string note;

        [HideInInspector, SerializeField, LevelEditorSetting] int bubblesOnTheFieldAmount;
        public int BubblesOnTheFieldAmount => bubblesOnTheFieldAmount;

        [HideInInspector, SerializeField, LevelEditorSetting] int turnsLimit;
        public int TurnsLimit => turnsLimit;

        [HideInInspector, SerializeField, LevelEditorSetting] int coinsReward = 20;
        public int CoinsReward => (int)(coinsReward * (LevelController.IsLevelCompletedForTheFirstTime ? 1f : 0.3f));

        [HideInInspector, SerializeField, LevelEditorSetting] GeneralLevelTarget requirements = new GeneralLevelTarget();
        public GeneralLevelTarget Requirements => requirements;

        [HideInInspector, SerializeField, LevelEditorSetting] bool canBeUsedInRandomizer = true;
        public bool CanBeUsedInRandomizer => canBeUsedInRandomizer;

        [SerializeField, LevelEditorSetting] ItemSave[] items;
        [SerializeField, LevelEditorSetting] bool nuisanceBallsRandom = true; // hk追加
        [SerializeField, LevelEditorSetting] NuisanceBallSaveHK[] nuisanceBallPlacements = new NuisanceBallSaveHK[0]; // hk追加
        [SerializeField, LevelEditorSetting] bool specialEffectsRandom = true; // hk追加
        [SerializeField, LevelEditorSetting] SpecialEffectSaveHK[] specialEffectPlacements = new SpecialEffectSaveHK[0]; // hk追加

        public bool NuisanceBallsRandom => nuisanceBallsRandom; // hk追加
        public NuisanceBallSaveHK[] NuisanceBallPlacements => nuisanceBallPlacements; // hk追加
        public bool SpecialEffectsRandom => specialEffectsRandom; // hk追加
        public SpecialEffectSaveHK[] SpecialEffectPlacements => specialEffectPlacements; // hk追加

        [SerializeField, LevelEditorSetting] LevelShapeType levelShapeType;
        [SerializeField, LevelEditorSetting] LevelBackgroundType levelBackType;

        public ItemSave[] Items => items;
        public int ItemsAmount => items.Length;
        public LevelShapeType LevelShapeType => levelShapeType;
        public LevelBackgroundType LevelBackType => levelBackType;

        private List<BubbleSpawnData> spawnQueue;
        public List<BubbleSpawnData> SpawnQueue => spawnQueue;

        public void Init()
        {
            spawnQueue = new List<BubbleSpawnData>();

            var receipt = requirements.Recipe;

            for (int j = 0; j < receipt.Ingridients.Count; j++)
            {
                var ingridient = receipt.Ingridients[j];

                int count = ingridient.amount * (int)Mathf.Pow(2, ingridient.stageId);

                for (int k = 0; k < count; k++)
                {
                    spawnQueue.Add(new BubbleSpawnData()
                    {
                        branch = ingridient.branch,
                        stageId = 0,
                        iceHP = 0,
                        boxHP = 0,
                    });
                }
            }

            spawnQueue.Shuffle();

            List<BubbleSpawnData> bubblesWithoutEffects = new List<BubbleSpawnData>();

            for (int i = 0; i < spawnQueue.Count; i++)
            {
                if (!spawnQueue[i].HasEffect)
                    bubblesWithoutEffects.Add(spawnQueue[i]);
            }

            bubblesWithoutEffects.Shuffle();

            for (int i = 0; i < requirements.IceBubblesPerLevel && bubblesWithoutEffects.Count > 0; i++)
            {
                bubblesWithoutEffects[0].iceHP = requirements.IceBubblesHealth;
                bubblesWithoutEffects.RemoveAt(0);
            }

            for (int i = 0; i < requirements.BoxesPerLevel && bubblesWithoutEffects.Count > 0; i++)
            {
                bubblesWithoutEffects[0].boxHP = requirements.BoxesHealth;
                bubblesWithoutEffects.RemoveAt(0);
            }
        }

        public bool GetNextSpawnData(out BubbleSpawnData data)
        {
            data = default;
            if (spawnQueue.IsNullOrEmpty())
                return false;

            data = spawnQueue[0];
            spawnQueue.RemoveAt(0);

            return true;
        }

        public bool TryGetSimilarSpawnData(BubbleData data, out BubbleSpawnData similarSpawnData)
        {
            similarSpawnData = default;

            if (spawnQueue.IsNullOrEmpty())
                return false;

            for (int i = 0; i < spawnQueue.Count; i++)
            {
                var testData = spawnQueue[i];

                if (testData.branch == data.branch)
                {
                    similarSpawnData = testData;
                    spawnQueue.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void AddBubbleToQueue(BubbleData data)
        {
            int count = (int)Mathf.Pow(2, data.stageId);

            for (int i = 0; i < count; i++)
            {
                spawnQueue.Insert(Random.Range(0, spawnQueue.Count), new BubbleSpawnData
                {
                    stageId = 0,
                    branch = data.branch,
                    boxHP = 0,
                    iceHP = 0
                });
            }
        }
    }

    [System.Serializable]
    public struct Requirement
    {
        public Branch branch;
        public int stageId;
        public int amount;

        public Requirement(Branch branch, int stageId, int amount = 0)
        {
            this.branch = branch;
            this.stageId = stageId;
            this.amount = amount;
        }
    }
}