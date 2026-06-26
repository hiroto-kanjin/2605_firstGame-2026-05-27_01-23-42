using UnityEngine;
using System.Collections.Generic;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "StageDatabase", menuName = "HK/StageDatabase")]
    public class StageDatabase : ScriptableObject // hk追加
    {
        [SerializeField] private List<StageData> stages = new List<StageData>();
        [SerializeField] private bool loopLastStage = true; // hk追加：最後のステージをループするか

        public StageData GetStage(int levelId) // hk追加
        {
            if (stages.Count == 0)
            {
                Debug.LogError("StageDatabase: ステージデータがありません");
                return null;
            }

            if (levelId < stages.Count)
                return stages[levelId];

            if (loopLastStage)
                return stages[stages.Count - 1]; // hk追加：最後のステージを繰り返す

            Debug.LogError($"StageDatabase: レベル{levelId}のデータがありません");
            return null;
        }

        public int StageCount => stages.Count; // hk追加
    }
}