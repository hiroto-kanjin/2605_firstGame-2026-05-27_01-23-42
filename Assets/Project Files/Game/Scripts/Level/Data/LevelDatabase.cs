using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Level Database", menuName = "Data/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] Level[] levels;
        [SerializeField, LevelEditorSetting] GameLevelData[] gameLevels; // hk追加：1レベル分の設定一式を並べる配列（統合先）
        [SerializeField, LevelEditorSetting] LevelItem[] items;
        [SerializeField, LevelEditorSetting] LevelShape[] levelShapes;
        [SerializeField, LevelEditorSetting] LevelBackground[] levelBackgrounds;
        [SerializeField] EvolutionBranch[] branches;
        [SerializeField] Sprite[] potionSprites; //used for generation in level editor

        public LevelItem[] Items => items;
        public LevelShape[] LevelShapes => levelShapes;
        public LevelBackground[] LevelBackgrounds => levelBackgrounds;

        public GameLevelData[] GameLevels => gameLevels; // hk追加：外から gameLevels を読むため
        public int AmountOfGameLevels => gameLevels.Length; // hk追加：ゲームレベルの総数

        public int AmountOfLevels => levels.Length;

        public Level GetLevel(int levelId)
        {
            if (levelId < levels.Length)
            {
                return levels[levelId % levels.Length];
            }

            int randomLevelIndex = 0;

            do
            {
                randomLevelIndex = Random.Range(0, levels.Length);
            }
            while (!levels[randomLevelIndex].CanBeUsedInRandomizer);

            return levels[randomLevelIndex];
        }

        public EvolutionBranch GetBranch(Branch branchType)
        {
            for (int i = 0; i < branches.Length; i++)
            {
                if (branches[i].branch == branchType)
                    return branches[i];
            }

            return null;
        }

        public LevelItem GetItem(Item itemType)
        {
            foreach (LevelItem item in Items)
            {
                if (item.Type == itemType)
                {
                    return item;
                }
            }

            return null;
        }

        public LevelShape GetShape(LevelShapeType levelShapeType)
        {
            foreach (LevelShape item in levelShapes)
            {
                if (item.Type == levelShapeType)
                {
                    return item;
                }
            }

            return null;
        }

        public LevelBackground GetBackground(LevelBackgroundType levelBackType)
        {
            foreach (LevelBackground item in levelBackgrounds)
            {
                if (item.Type == levelBackType)
                {
                    return item;
                }
            }

            return null;
        }
    }
}