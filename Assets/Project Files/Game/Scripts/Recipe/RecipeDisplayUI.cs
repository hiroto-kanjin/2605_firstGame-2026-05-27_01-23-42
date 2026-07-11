using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk修正：MonoBehaviourではなくPreviewableBehaviourを継承（プレビュー機能を使うため）
    public class RecipeDisplayUI : PreviewableBehaviour
    {
        public static RecipeDisplayUI Instance { get; private set; }

        [SerializeField] private GameObject recipeSlotPrefab;
        [SerializeField] private Transform slotParent;

        [Header("レイアウト設定")]
        [SerializeField] private int columns = 3;            // 1行あたりの数
        [SerializeField] private float slotSpacingX = 150f;  // 横の間隔
        [SerializeField] private float slotSpacingY = 150f;  // 縦の間隔
        [SerializeField] private float paddingLeft = 0f;     // 左パディング
        [SerializeField] private float paddingTop = 0f;      // 上パディング

        [Header("プレビュー設定（エディタ用）")]
        [SerializeField] private int previewEvolutionCount = 6; // hk追加：プレビューで並べる進化ダミー数
        [SerializeField] private int previewSpecialCount = 3;   // hk追加：プレビューで並べる特殊ダミー数

        private List<RecipeSlotUI> slots = new List<RecipeSlotUI>();

        private void Awake()
        {
            Instance = this;
        }

        public void SetupRecipe(int recipeId)
        {
            ClearSlots();

            List<RecipeSlotData> ingredients = HKSupplyManager.Instance.RecipeData.BuildSlotDataList(recipeId);
            BallData ballData = HKSupplyManager.Instance.SupplyData;

            List<RecipeSlotData> evolutionList = new List<RecipeSlotData>();
            List<RecipeSlotData> specialList = new List<RecipeSlotData>();
            foreach (RecipeSlotData data in ingredients)
            {
                if (data.category == BallCategory.Special)
                    specialList.Add(data);
                else
                    evolutionList.Add(data);
            }

            int row = 0;
            row = PlaceGroup(evolutionList, row, ballData);
            PlaceGroup(specialList, row, ballData);
        }

        // hk修正：実データ用。1グループを並べる
        private int PlaceGroup(List<RecipeSlotData> group, int startRow, BallData ballData)
        {
            for (int i = 0; i < group.Count; i++)
            {
                GameObject slotObj = Instantiate(recipeSlotPrefab, slotParent);
                RecipeSlotUI slot = slotObj.GetComponent<RecipeSlotUI>();
                slot.SetData(group[i]);

                PlaceAt(slotObj, i, startRow);

                BallEntry entry = ballData.GetBall(group[i].category, group[i].number);
                if (entry != null)
                    slot.SetIcon(entry.uiSprite);

                slots.Add(slot);
            }
            return startRow + RowsUsed(group.Count);
        }

        // hk追加：3列グリッドの座標を計算して配置する（左上基準）
        private void PlaceAt(GameObject slotObj, int indexInGroup, int startRow)
        {
            int col = indexInGroup % columns;
            int row = startRow + (indexInGroup / columns);
            float x = paddingLeft + col * slotSpacingX;
            float y = -paddingTop - row * slotSpacingY;
            slotObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }

        // hk追加：グループが使う行数（切り上げ）
        private int RowsUsed(int count)
        {
            return (count + columns - 1) / columns;
        }

        // hk追加：スロットを全部消す
        private void ClearSlots()
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            slots.Clear();
        }

        // ── プレビュー（エディタ用・ダミー配置）──

        // hk追加：ダミーのスロットを並べて、レイアウトだけ確認する
        public override void BuildPreview()
        {
            ClearPreview();

            // 進化ダミー
            int row = 0;
            row = PlaceDummyGroup(previewEvolutionCount, row);
            // 特殊ダミー（次の行から）
            PlaceDummyGroup(previewSpecialCount, row);
        }

        // hk追加：ダミーをN個並べる
        private int PlaceDummyGroup(int count, int startRow)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject slotObj = Instantiate(recipeSlotPrefab, slotParent);
                slotObj.name = "PreviewSlot(Dummy)"; // 後で消せるよう名前を付ける
                PlaceAt(slotObj, i, startRow);
            }
            return startRow + RowsUsed(count);
        }

        // hk追加：プレビューのダミーを全部消す
        public override void ClearPreview()
        {
            if (slotParent == null) return;

            // 後ろから消す（消しながらのインデックスずれを防ぐ）
            for (int i = slotParent.childCount - 1; i >= 0; i--)
            {
                Transform child = slotParent.GetChild(i);
                if (child.name.StartsWith("PreviewSlot(Dummy)"))
                    DestroyImmediate(child.gameObject);
            }
        }

        public void UpdatePotContents(List<BallBehaviorHK> ballsInPot)
        {
            Dictionary<(BallCategory, int), int> potContents = new Dictionary<(BallCategory, int), int>();
            foreach (BallBehaviorHK ball in ballsInPot)
            {
                var key = (ball.GetBallCategory(), ball.GetNumber());
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
    }
}