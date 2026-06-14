using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class FinisherBall : MonoBehaviour // hk追加
    {
        public enum FinisherType // hk追加
        {
            Fire,
            Ice,
            Oil,
            Soup,
            Thunder,
            Fermentation
        }

        [SerializeField] private FinisherType finisherType;
        [SerializeField] private MeshRenderer meshRenderer; // hk追加

        public FinisherType GetFinisherType() // hk追加
        {
            return finisherType;
        }

        // hk追加：外部から見た目を設定する（BallBehaviorHK.SetDataと同じ思想）
        public void SetData(FinisherType type, Texture icon)
        {
            finisherType = type;

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_Icon_Texture", icon);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}