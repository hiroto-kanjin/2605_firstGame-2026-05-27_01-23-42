using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CookingAreaManager : MonoBehaviour // hk追加
    {
        public static CookingAreaManager Instance { get; private set; }

        // 鍋の中にいるボールのリスト
        private List<BallBehaviorHK> ballsInPot = new List<BallBehaviorHK>(); // hk追加

        private void Awake()
        {
            Instance = this;
        }

        // 何かが鍋に入ってきた時
        private void OnTriggerEnter2D(Collider2D other) // hk追加
        {
            Debug.Log("OnTriggerEnter2D called: " + other.gameObject.name); // hk追加：デバッグ用        
            // フィニッシャーかどうかチェック
            FinisherBall finisher = other.GetComponent<FinisherBall>();
            if (finisher != null)
            {
                // フィニッシャーが入った → レシピ照合へ
                StartRecipeJudgement();
                HKSupplyManager.Instance.OnFinisherEnteredPot();
                return;
            }

            // 食材ボールかどうかチェック
            BallBehaviorHK ball = other.GetComponentInParent<BallBehaviorHK>(); // hk追加：子コライダーからも親のBallBehaviorHKを取得
            if (ball != null && !ballsInPot.Contains(ball)) // hk追加：重複追加を防ぐ
            {
                ballsInPot.Add(ball);
                Debug.Log("ballsInPot に追加: " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
            }
        }

        // 何かが鍋から出ていった時
        private void OnTriggerExit2D(Collider2D other) // hk追加
        {
            BallBehaviorHK ball = other.GetComponentInParent<BallBehaviorHK>(); // hk追加：子コライダーからも親のBallBehaviorHKを取得
            if (ball != null)
            {
                ballsInPot.Remove(ball);
                Debug.Log("ballsInPot から削除: " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
            }
        }

        // レシピ照合を呼び出す
        private void StartRecipeJudgement() // hk追加
        {
            RecipeManager.Instance.Judge(ballsInPot);
        }

        // 鍋の中身を外から確認できるようにする
        public List<BallBehaviorHK> GetBallsInPot() // hk追加
        {
            return ballsInPot;
        }

        // 鍋の中身をリセットする（ステージ開始時に使う）
        public void ResetPot() // hk追加
        {
            ballsInPot.Clear();
        }
        // hk追加：ボールが破棄された時に外部から呼ばれる
        public void RemoveFromPot(BallBehaviorHK ball)
        {
            if (ballsInPot.Remove(ball))
            {
                Debug.Log("ballsInPot から削除(破棄): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
            }
        }
    }
}