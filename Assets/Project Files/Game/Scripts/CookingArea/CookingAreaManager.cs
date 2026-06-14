using System.Collections; // hk追加
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CookingAreaManager : MonoBehaviour // hk追加
    {
        public static CookingAreaManager Instance { get; private set; }

        // 鍋の中にいるボールのリスト
        private List<BallBehaviorHK> ballsInPot = new List<BallBehaviorHK>(); // hk追加
        [SerializeField] private float stillVelocityThreshold = 0.1f; // hk追加：この速度以下なら「停止」とみなす
        [SerializeField] private float finisherJudgeDelay = 0.5f; // hk追加：フィニッシャー停止後、判定までの待機時間
        private bool isFinisherInPot = false; // hk追加：フィニッシャーが鍋の中にいるか
        private Coroutine finisherJudgeCoroutine = null; // hk追加

        private void Awake()
        {
            Instance = this;
        }

        private void OnTriggerEnter2D(Collider2D other) // hk追加
        {
            // フィニッシャーかどうかチェック
            FinisherBall finisher = other.GetComponentInParent<FinisherBall>(); // hk追加：子コライダーからも親のFinisherBallを取得
            if (finisher != null)
            {
                // hk追加：フィニッシャーが鍋に入った。判定は停止後に行う
                isFinisherInPot = true;
                return;
            }
            // hk追加：食材ボールはOnTriggerStay2Dで判定するため、ここでは何もしない
        }

        // 何かが鍋から出ていった時
        private void OnTriggerExit2D(Collider2D other) // hk追加
        {
            // hk追加：フィニッシャーが鍋から出た場合
            FinisherBall finisher = other.GetComponentInParent<FinisherBall>();
            if (finisher != null)
            {
                isFinisherInPot = false;
                if (finisherJudgeCoroutine != null)
                {
                    StopCoroutine(finisherJudgeCoroutine);
                    finisherJudgeCoroutine = null;
                }
                return;
            }

            BallBehaviorHK ball = other.GetComponentInParent<BallBehaviorHK>(); // hk追加：子コライダーからも親のBallBehaviorHKを取得
            if (ball != null && ballsInPot.Contains(ball)) // hk追加：入っている場合のみ削除
            {
                ballsInPot.Remove(ball);
                Debug.Log("ballsInPot から削除(鍋の外へ): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
                HKGameManager.Instance.OnPotContentsChanged(); // hk追加
            }
        }
        // hk追加：鍋エリア内にいる間、毎フレーム呼ばれる
        private void OnTriggerStay2D(Collider2D other)
        {
            // hk追加：フィニッシャーの停止チェック
            FinisherBall finisher = other.GetComponentInParent<FinisherBall>();
            if (finisher != null)
            {
                if (!isFinisherInPot) return;

                Rigidbody2D finisherRb = finisher.GetComponent<Rigidbody2D>();
                if (finisherRb == null) return;

                bool isSlowEnough = finisherRb.linearVelocity.magnitude <= stillVelocityThreshold;

                if (isSlowEnough && finisherJudgeCoroutine == null)
                {
                    finisherJudgeCoroutine = StartCoroutine(FinisherJudgeAfterDelay());
                }
                return;
            }

            BallBehaviorHK ball = other.GetComponentInParent<BallBehaviorHK>();
            if (ball == null) return;

            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            bool isBallSlowEnough = rb.linearVelocity.magnitude <= stillVelocityThreshold;

            if (isBallSlowEnough && !ballsInPot.Contains(ball))
            {
                ballsInPot.Add(ball);
                Debug.Log("ballsInPot に追加(停止): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
                HKGameManager.Instance.OnPotContentsChanged();
            }
            else if (!isBallSlowEnough && ballsInPot.Contains(ball))
            {
                ballsInPot.Remove(ball);
                Debug.Log("ballsInPot から削除(動いている): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count); // hk追加：デバッグ用
                HKGameManager.Instance.OnPotContentsChanged();
            }
        }
        // hk追加：フィニッシャー停止後、指定時間待ってから判定する
        private IEnumerator FinisherJudgeAfterDelay()
        {
            yield return new WaitForSeconds(finisherJudgeDelay);

            finisherJudgeCoroutine = null;

            if (isFinisherInPot)
            {
                StartRecipeJudgement();
                HKSupplyManager.Instance.OnFinisherEnteredPot();
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
                HKGameManager.Instance.OnPotContentsChanged(); // hk追加
            }
        }
    }
}