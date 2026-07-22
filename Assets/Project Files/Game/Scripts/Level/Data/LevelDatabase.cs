using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "Level Database", menuName = "Data/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        // hk注記：下記3項目（levels / items / levelShapes）は現在未使用。表示のみ停止中（HideInInspector）。将来的に削除予定。
        [TextArea]
        [SerializeField] string _未整理メモ = "※ levels / items / levelShapes は休眠中（未使用）。表示を止めているだけでデータは残存。整理時に削除すること。";

        [HideInInspector, SerializeField] Level[] levels;                    // hk修正：未使用のため非表示（休眠）。削除予定
        [SerializeField, LevelEditorSetting] GameLevelData[] gameLevels;     // hk追加：1レベル分の設定一式を並べる配列（統合先・現役）
        [HideInInspector, SerializeField] LevelItem[] items;                 // hk修正：未使用のため非表示（休眠）。削除予定
        [HideInInspector, SerializeField] LevelShape[] levelShapes;          // hk修正：未使用のため非表示（休眠）。削除予定

        public LevelItem[] Items => items;
        public LevelShape[] LevelShapes => levelShapes;
        public GameLevelData[] GameLevels => gameLevels; // hk追加：外から gameLevels を読むため
        public int AmountOfGameLevels => gameLevels.Length; // hk追加：ゲームレベルの総数

        public int AmountOfLevels => levels.Length;

        // hk追加：Unityがこのデータの変更を検知するたびに自動で呼ばれる。IDの空欄・重複を自動修復する。
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

        // hk追加：固有IDでGameLevelDataを探す（これからはこちらを検索の基本にする）
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

        // hk追加：固有IDに対応する配列上の番号を返す（保存データが番号を必要とする場面向け）
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
        // hk追加：レベルIDに対応するGameLevelDataを返す（GetLevelと同じ考え方）
        public GameLevelData GetGameLevel(int levelId)
        {
            if (levelId < gameLevels.Length)
            {
                return gameLevels[levelId % gameLevels.Length];
            }

            return gameLevels[levelId % gameLevels.Length];
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
    }
}