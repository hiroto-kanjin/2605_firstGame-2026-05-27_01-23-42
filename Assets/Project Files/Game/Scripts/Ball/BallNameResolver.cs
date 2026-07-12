using UnityEngine;

namespace Watermelon.BubbleMerge
{
    // hk追加：ボール名を引く共通の入り口。Resources上のBallDataを読み、category＋numberからballNameを返す
    // 名前表示が必要な画面は、すべてここを通す（参照方法を1本に統一するため）
    public static class BallNameResolver
    {
        private static BallData cachedBallData; // 一度読んだら使い回す

        // Resources/BallData を読む（置き場所：Assets/Resources/BallData.asset）
        private static BallData GetBallData()
        {
            if (cachedBallData == null)
            {
                cachedBallData = Resources.Load<BallData>("BallData");
            }
            return cachedBallData;
        }

        // category＋number から名前を返す。見つからなければ空文字
        public static string GetBallName(BallCategory category, int number)
        {
            BallData data = GetBallData();
            if (data == null) return "";

            BallEntry entry = data.GetBall(category, number);
            if (entry == null) return "";

            return entry.ballName;
        }
    }
}