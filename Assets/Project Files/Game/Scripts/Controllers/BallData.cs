using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    [CreateAssetMenu(fileName = "BallData", menuName = "HK/BallData")]
    public class BallData : ScriptableObject // hk追加
    {
        // hk追加：ボール1個ぶんの箱を、リストで並べて持つ
        [SerializeField] private List<BallEntry> balls = new List<BallEntry>();

        // hk追加：グループと番号で1個のボールを取り出す
        public BallEntry GetBall(BallCategory category, int number)
        {
            foreach (var ball in balls)
            {
                if (ball.category == category && ball.number == number)
                    return ball;
            }
            return null;
        }
    }

    // hk追加：ボール1個ぶんの箱（素材）
    [System.Serializable]
    public class BallEntry
    {
        public string ballName;               // 企画名
        public BallCategory category;         // グループ（00=進化 / 01=特殊 / 02=お邪魔）
        public int number;                    // 番号（0000〜。グループ内での通し番号）
        public string folderName;             // 画像一式が入ったフォルダ名
        public float size = 1.2f;             // 大きさ（デフォルト1.2）
        public BubblesPhysicsData physicsPattern; // 動き方
        public bool canMerge;                 // マージするか（進化=する、特殊・お邪魔=しない）
    }
}