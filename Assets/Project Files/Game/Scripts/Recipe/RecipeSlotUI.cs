using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.BubbleMerge
{
    public class RecipeSlotUI : MonoBehaviour // hk追加
    {
        [SerializeField] private Image icon; // hk追加：食材アイコン
        [SerializeField] private TMP_Text countText; // hk追加：必要個数

        private RecipeIngredient ingredient; // hk追加：このスロットの食材データ
        private int currentCount = 0; // hk追加：現在の鍋の個数

        public void SetData(RecipeIngredient ingredient) // hk追加：食材データをセットする
        {
            this.ingredient = ingredient;
            countText.text = "0 / " + ingredient.requiredCount;
        }

        public void SetIcon(Sprite sprite) // hk追加：アイコン画像をセットする
        {
            icon.sprite = sprite;
        }

        public void UpdateCount(int count) // hk追加：鍋の個数が変わった時に呼ばれる
        {
            currentCount = count;
            countText.text = currentCount + " / " + ingredient.requiredCount;
        }

        public RecipeIngredient GetIngredient() => ingredient; // hk追加
    }
}