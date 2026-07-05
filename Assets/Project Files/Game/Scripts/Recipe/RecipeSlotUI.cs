using TMPro;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class RecipeSlotUI : MonoBehaviour // hk追加
    {
        [SerializeField] private Transform iconParent; // hk修正：アイコンプレハブを差し込む場所
        [SerializeField] private TMP_Text countText;   // hk追加：必要個数

        private RecipeSlotData slotData;   // hk修正：category＋number＋count を持つ
        private GameObject currentIcon;    // hk追加：今差し込んでいるアイコンプレハブ
        private int currentCount = 0;      // hk追加：現在の鍋の個数

        public void SetData(RecipeSlotData data) // hk修正：食材データをセットする
        {
            slotData = data;
            countText.text = "0 / " + slotData.count;
        }

        // hk修正：アイコンをプレハブとして差し込む（Image廃止）
        public void SetIconPrefab(GameObject iconPrefab)
        {
            if (currentIcon != null)
            {
                Destroy(currentIcon);
                currentIcon = null;
            }
            if (iconPrefab == null || iconParent == null) return;

            currentIcon = Instantiate(iconPrefab, iconParent);
            currentIcon.transform.localPosition = Vector3.zero;
        }

        public void UpdateCount(int count) // hk追加：鍋の個数が変わった時に呼ばれる
        {
            currentCount = count;
            countText.text = currentCount + " / " + slotData.count;
        }

        public RecipeSlotData GetSlotData() => slotData; // hk修正
    }
}