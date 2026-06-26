using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class RecipeDisplayUI : MonoBehaviour // hk追加
    {
        public static RecipeDisplayUI Instance { get; private set; }

        [SerializeField] private GameObject recipeSlotPrefab; // hk追加
        [SerializeField] private Transform slotParent; // hk追加

        [Header("食材数ごとのレイアウト")] // hk追加
        [SerializeField] private SlotLayout[] layouts; // hk追加：食材数ごとのレイアウト

        private List<RecipeSlotUI> slots = new List<RecipeSlotUI>(); // hk追加

        private void Awake()
        {
            Instance = this;
        }

        public void SetupRecipe(List<RecipeIngredient> ingredients) // hk追加
        {
            foreach (var slot in slots)
                Destroy(slot.gameObject);
            slots.Clear();

            // hk追加：食材数に合うレイアウトを探す
            SlotLayout layout = GetLayout(ingredients.Count);

            for (int i = 0; i < ingredients.Count; i++)
            {
                GameObject slotObj = Instantiate(recipeSlotPrefab, slotParent);
                RecipeSlotUI slot = slotObj.GetComponent<RecipeSlotUI>();
                slot.SetData(ingredients[i]);

                // hk追加：レイアウトの座標を適用する
                if (layout != null && i < layout.positions.Length)
                {
                    slotObj.GetComponent<RectTransform>().anchoredPosition = layout.positions[i];
                }

                // hk追加：アイコンをセットする
                EvolutionBranch branch = LevelController.Database.GetBranch(ingredients[i].branch);
                if (branch != null)
                {
                    int stageId = GetStageIdFromBallType(ingredients[i].ballType);
                    if (stageId < branch.stages.Length)
                        slot.SetIcon(branch.stages[stageId].icon);
                }

                slots.Add(slot);
            }
        }

        public void UpdatePotContents(List<BallBehaviorHK> ballsInPot) // hk追加
        {
            Dictionary<(Branch, BallType), int> potContents = new Dictionary<(Branch, BallType), int>();
            foreach (BallBehaviorHK ball in ballsInPot)
            {
                var key = (ball.GetBranch(), ball.GetBallType());
                if (potContents.ContainsKey(key))
                    potContents[key]++;
                else
                    potContents[key] = 1;
            }

            foreach (RecipeSlotUI slot in slots)
            {
                var key = (slot.GetIngredient().branch, slot.GetIngredient().ballType);
                int count = potContents.ContainsKey(key) ? potContents[key] : 0;
                slot.UpdateCount(count);
            }
        }

        private SlotLayout GetLayout(int count) // hk追加
        {
            foreach (SlotLayout layout in layouts)
            {
                if (layout.ingredientCount == count)
                    return layout;
            }
            return null;
        }

        private int GetStageIdFromBallType(BallType ballType) // hk追加
        {
            switch (ballType)
            {
                case BallType.EvolutionBall_01: return 0;
                case BallType.EvolutionBall_02: return 1;
                case BallType.EvolutionBall_03: return 2;
                case BallType.EvolutionBall_04: return 3;
                case BallType.EvolutionBall_05: return 4;
                case BallType.EvolutionBall_06: return 5;
                case BallType.EvolutionBall_07: return 6;
                case BallType.EvolutionBall_08: return 7;
                case BallType.EvolutionBall_09: return 8;
                case BallType.EvolutionBall_10: return 9;
                case BallType.EvolutionBall_11: return 10;
                default: return 0;
            }
        }
    }

    [System.Serializable]
    public class SlotLayout // hk追加：食材数ごとのレイアウトデータ
    {
        public int ingredientCount; // hk追加：食材の数
        public Vector2[] positions; // hk追加：各スロットの相対座標
    }
}