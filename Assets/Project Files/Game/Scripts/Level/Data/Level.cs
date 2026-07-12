// Level.cs
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class Level : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] string note;

        [HideInInspector, SerializeField, LevelEditorSetting] int bubblesOnTheFieldAmount;
        public int BubblesOnTheFieldAmount => bubblesOnTheFieldAmount;

        [HideInInspector, SerializeField, LevelEditorSetting] bool canBeUsedInRandomizer = true;
        public bool CanBeUsedInRandomizer => canBeUsedInRandomizer;

        [SerializeField, LevelEditorSetting] ItemSave[] items;
        [SerializeField, LevelEditorSetting] BallPlacementHK[] ballPlacements = new BallPlacementHK[0]; // hk追加
        [SerializeField, LevelEditorSetting] bool specialEffectsRandom = true; // hk追加
        [SerializeField, LevelEditorSetting] SpecialEffectSaveHK[] specialEffectPlacements = new SpecialEffectSaveHK[0]; // hk追加

        public BallPlacementHK[] BallPlacements => ballPlacements; // hk追加
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
            // hk修正：レシピ（GeneralLevelTarget.recipe）依存だったspawnQueue生成を削除。
            // 今の供給はGameLevelData.ballSupplyRates／nuisanceSpawnEventsが担当しているため、ここは空で持つだけにする。
            spawnQueue = new List<BubbleSpawnData>();
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