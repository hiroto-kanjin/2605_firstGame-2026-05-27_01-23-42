// Level.cs
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
        [SerializeField, LevelEditorSetting] float mergeLineYOffset; // hk追加：中間位置の微調整（+で上、-で下）

        public ItemSave[] Items => items;
        public int ItemsAmount => items.Length;
        public LevelShapeType LevelShapeType => levelShapeType;
        public float MergeLineYOffset => mergeLineYOffset; // hk追加

        public void Init()
        {
            // hk修正：spawnQueue（旧供給の仕組み）を廃止。今の供給はGameLevelDataが担当するため、Initは何もしない。
        }
    }
}