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

        [SerializeField] private float autoSlotSpacing = 150f; // hk追加：自動配置の間隔（Inspectorで調整可）

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

            // hk追加：食材数に合う手動レイアウトを探す（無ければnull）
            SlotLayout layout = GetLayout(ingredients.Count);

            BallData ballData = HKSupplyManager.Instance.SupplyData;

            for (int i = 0; i < ingredients.Count; i++)
            {
                GameObject slotObj = Instantiate(recipeSlotPrefab, slotParent);
                RecipeSlotUI slot = slotObj.GetComponent<RecipeSlotUI>();
                slot.SetData(ingredients[i]);

                // hk修正：手動レイアウトがあればその座標、無ければ自動配置（横一列）の座標を使う
                Vector2 pos;
                if (layout != null && i < layout.positions.Length)
                    pos = layout.positions[i];
                else
                    pos = GetAutoPosition(i, ingredients.Count);

                slotObj.GetComponent<RectTransform>().anchoredPosition = pos;

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
                var key = (ball.GetBallCategory(), ball.GetNumber()); // hk修正：共通のGetNumber()を使う
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

        private SlotLayout GetLayout(int count) // hk追加
        {
            foreach (SlotLayout layout in layouts)
            {
                if (layout.ingredientCount == count)
                    return layout;
            }
            return null;
        }

        // hk追加：手動レイアウト未設定時の保険。横一列に等間隔で並べる座標を返す
        private Vector2 GetAutoPosition(int index, int totalCount)
        {
            // 全体を中央そろえにする：左端の位置を出してから、間隔ぶんずらす
            float totalWidth = (totalCount - 1) * autoSlotSpacing;
            float startX = -totalWidth * 0.5f;
            float x = startX + index * autoSlotSpacing;
            return new Vector2(x, 0f);
        }
    }

    [System.Serializable]
    public class SlotLayout // hk追加：食材数ごとのレイアウトデータ
    {
        public int ingredientCount; // hk追加：食材の数
        public Vector2[] positions; // hk追加：各スロットの相対座標
    }
}