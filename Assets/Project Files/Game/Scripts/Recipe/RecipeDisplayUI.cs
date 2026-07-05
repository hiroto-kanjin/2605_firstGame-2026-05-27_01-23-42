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

        // hk修正：レシピIDから②の食材（進化＋特殊）を組み立てて並べる
        public void SetupRecipe(int recipeId)
        {
            foreach (var slot in slots)
                Destroy(slot.gameObject);
            slots.Clear();

            List<RecipeSlotData> ingredients = HKSupplyManager.Instance.RecipeData.BuildSlotDataList(recipeId);

            // hk追加：食材数に合うレイアウトを探す
            SlotLayout layout = GetLayout(ingredients.Count);

            BallData ballData = HKSupplyManager.Instance.SupplyData;

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

                // hk修正：アイコンは①BallDataのuiIconPrefabから引く（category＋number）
                if (ballData != null)
                {
                    BallEntry entry = ballData.GetBall(ingredients[i].category, ingredients[i].number);
                    if (entry != null)
                        slot.SetIconPrefab(entry.uiIconPrefab);
                }

                slots.Add(slot);
            }
        }

        public void UpdatePotContents(List<BallBehaviorHK> ballsInPot) // hk修正：category＋numberで数える
        {
            Dictionary<(BallCategory, int), int> potContents = new Dictionary<(BallCategory, int), int>();
            foreach (BallBehaviorHK ball in ballsInPot)
            {
                var key = (ball.GetBallCategory(), GetNumber(ball));
                if (potContents.ContainsKey(key))
                    potContents[key]++;
                else
                    potContents[key] = 1;
            }

            foreach (RecipeSlotUI slot in slots)
            {
                RecipeSlotData data = slot.GetSlotData();
                var key = (data.category, data.number);
                int count = potContents.ContainsKey(key) ? potContents[key] : 0;
                slot.UpdateCount(count);
            }
        }

        // hk追加：ボールのnumberを取り出す（進化は段階番号、特殊はインデックス）
        private int GetNumber(BallBehaviorHK ball)
        {
            if (ball.GetBallCategory() == BallCategory.Evolution)
                return BallBehaviorHK.GetEvolutionNumber(ball.GetBallType());
            return ball.GetBallIndex();
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
    }

    [System.Serializable]
    public class SlotLayout // hk追加：食材数ごとのレイアウトデータ
    {
        public int ingredientCount; // hk追加：食材の数
        public Vector2[] positions; // hk追加：各スロットの相対座標
    }
}