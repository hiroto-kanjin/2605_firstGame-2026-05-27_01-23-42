using UnityEngine;
using System.Collections.Generic;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "GameLevelDatabase", menuName = "HK/GameLevelDatabase")]
    public class GameLevelDatabase : ScriptableObject // hk追加：全ゲームレベルのデータをまとめて管理
    {
        [SerializeField] private List<GameLevelData> levels = new List<GameLevelData>();
        [SerializeField] private bool loopLastLevel = true; // hk追加：最後のゲームレベルをループするか

        public GameLevelData GetLevel(int levelId) // hk追加
        {
            if (levels.Count == 0)
            {
                Debug.LogError("GameLevelDatabase: ゲームレベルデータがありません");
                return null;
            }

            if (levelId < levels.Count)
                return levels[levelId];

            if (loopLastLevel)
                return levels[levels.Count - 1];

            Debug.LogError($"GameLevelDatabase: ゲームレベル{levelId}のデータがありません");
            return null;
        }

        public int LevelCount => levels.Count; // hk追加
    }
}