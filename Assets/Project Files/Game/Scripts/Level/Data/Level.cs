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

        [SerializeField, LevelEditorSetting] bool overrideMergeLine; // hk追加：このレベルだけ判定ラインを個別調整するか
        [SerializeField, LevelEditorSetting] float mergeLineYOverride; // hk追加：個別調整する場合の高さ

        public ItemSave[] Items => items;
        public int ItemsAmount => items.Length;
        public LevelShapeType LevelShapeType => levelShapeType;
        public bool OverrideMergeLine => overrideMergeLine; // hk追加
        public float MergeLineYOverride => mergeLineYOverride; // hk追加

        public void Init()
        {
            // hk修正：spawnQueue（旧供給の仕組み）を廃止。今の供給はGameLevelDataが担当するため、Initは何もしない。
        }
    }
}