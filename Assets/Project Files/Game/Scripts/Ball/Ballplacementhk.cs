using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [System.Serializable]
    public class BallPlacementHK // hk追加：配置ボール1個分のデータ
    {
        // hk修正：branch除去。branchIndex/ballLevelIndexを廃止し、indexに一本化。
        // category＝進化/特殊/お邪魔。
        // index＝進化・特殊はレシピ内の位置（evolutionChain/specialListの何番目）、お邪魔は種類番号。
        public BallCategory category;
        public int index;
        public Vector3 position;
    }
}