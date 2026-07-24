using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Level Database", menuName = "Data/Level Database")]
    public partial class LevelDatabase : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] GameLevelData[] gameLevels;
        [SerializeField, LevelEditorSetting] LevelShape[] levelShapes;

        public LevelShape[] LevelShapes => levelShapes;
        public GameLevelData[] GameLevels => gameLevels;
        public int AmountOfGameLevels => gameLevels.Length;

        private void OnValidate()
        {
            if (gameLevels == null) return;

            HashSet<string> usedIds = new HashSet<string>();

            for (int i = 0; i < gameLevels.Length; i++)
            {
                if (gameLevels[i] == null) continue;

                bool needsNewId = string.IsNullOrEmpty(gameLevels[i].gameLevelId) || usedIds.Contains(gameLevels[i].gameLevelId);

                if (needsNewId)
                {
                    gameLevels[i].gameLevelId = System.Guid.NewGuid().ToString();
                }

                usedIds.Add(gameLevels[i].gameLevelId);
            }
        }

        public GameLevelData GetGameLevelById(string gameLevelId)
        {
            if (gameLevels == null || string.IsNullOrEmpty(gameLevelId)) return null;

            foreach (GameLevelData level in gameLevels)
            {
                if (level != null && level.gameLevelId == gameLevelId)
                    return level;
            }

            return null;
        }

        public int GetIndexByGameLevelId(string gameLevelId)
        {
            if (gameLevels == null || string.IsNullOrEmpty(gameLevelId)) return -1;

            for (int i = 0; i < gameLevels.Length; i++)
            {
                if (gameLevels[i] != null && gameLevels[i].gameLevelId == gameLevelId)
                    return i;
            }

            return -1;
        }

        public GameLevelData GetGameLevel(int levelId)
        {
            if (levelId < gameLevels.Length)
            {
                return gameLevels[levelId % gameLevels.Length];
            }

            return gameLevels[levelId % gameLevels.Length];
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
    }
}