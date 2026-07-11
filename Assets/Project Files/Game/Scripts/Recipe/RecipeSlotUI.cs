using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BubbleMerge
{
    public class RecipeSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;    // hk修正：アイコンを表示するImage
        [SerializeField] private TMP_Text countText;

        private RecipeSlotData slotData;
        private int currentCount = 0;

        public void SetData(RecipeSlotData data)
        {
            slotData = data;
            countText.text = "0 / " + slotData.count;
        }

        // hk修正：Sprite（絵）をImageに直接セットする
        public void SetIcon(Sprite icon)
        {
            if (iconImage == null) return;
            iconImage.sprite = icon;
        }

        public void UpdateCount(int count)
        {
            currentCount = count;
            countText.text = currentCount + " / " + slotData.count;
        }

        public RecipeSlotData GetSlotData() => slotData;
    }
}