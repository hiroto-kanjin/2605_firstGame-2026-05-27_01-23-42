using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class CookingAreaManager : MonoBehaviour // hk追加
    {
        public static CookingAreaManager Instance { get; private set; }

        private List<BallBehaviorHK> ballsInPot = new List<BallBehaviorHK>();
        [SerializeField] private float stillVelocityThreshold = 0.1f;
        [SerializeField] private float finisherJudgeDelay = 0.5f;
        private bool isFinisherInPot = false;
        private Coroutine finisherJudgeCoroutine = null;

        private void Awake()
        {
            Instance = this;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return;

            FinisherBall finisher = other.GetComponentInParent<FinisherBall>();
            if (finisher != null)
            {
                isFinisherInPot = true;
                return;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.isTrigger) return;

            FinisherBall finisher = other.GetComponentInParent<FinisherBall>();
            if (finisher != null)
            {
                isFinisherInPot = false;
                if (finisherJudgeCoroutine != null)
                {
                    StopCoroutine(finisherJudgeCoroutine);
                    finisherJudgeCoroutine = null;
                }
                Debug.Log("フィニッシャーが鍋から出ました。判定キャンセル");
                return;
            }

            BallBehaviorHK ball = other.GetComponentInParent<BallBehaviorHK>();
            if (ball != null && ballsInPot.Contains(ball))
            {
                ballsInPot.Remove(ball);
                Debug.Log("ballsInPot から削除(鍋の外へ): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count);
                HKGameManager.Instance.OnPotContentsChanged();
                RecipeDisplayUI.Instance.UpdatePotContents(ballsInPot);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.isTrigger) return;

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
                Debug.Log("ballsInPot に追加(停止): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count);
                HKGameManager.Instance.OnPotContentsChanged();
                RecipeDisplayUI.Instance.UpdatePotContents(ballsInPot);
            }
            else if (!isBallSlowEnough && ballsInPot.Contains(ball))
            {
                ballsInPot.Remove(ball);
                Debug.Log("ballsInPot から削除(動いている): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count);
                HKGameManager.Instance.OnPotContentsChanged();
                RecipeDisplayUI.Instance.UpdatePotContents(ballsInPot);
            }
        }

        private IEnumerator FinisherJudgeAfterDelay()
        {
            yield return new WaitForSeconds(finisherJudgeDelay);

            finisherJudgeCoroutine = null;

            if (!isFinisherInPot)
            {
                Debug.Log("フィニッシャーが鍋から出ているため判定スキップ");
                yield break;
            }
            // hk追加：デバッグモードでクリア判定が無効の場合はスキップ
            if (DebugUIManager.Instance != null && DebugUIManager.Instance.IsClearJudgeDisabled)
            {
                Debug.Log("デバッグモード：クリア判定スキップ");
                finisherJudgeCoroutine = null;
                yield break;
            }
            var ballsInPotSnapshot = new List<BallBehaviorHK>(ballsInPot);
            bool recipeMatched = HKGameManager.Instance.IsRecipeReady();
            int completionScore = CompletionScoreCalculator.Instance.Calculate(
                ballsInPotSnapshot,
                HKGameManager.Instance.GetCurrentLevel().recipeId
            );

            HKGameManager.Instance.OnJudgementResult(recipeMatched, completionScore);
            HKSupplyManager.Instance.OnFinisherEnteredPot();
        }

        public List<BallBehaviorHK> GetBallsInPot() => ballsInPot;

        public void ResetPot()
        {
            ballsInPot.Clear();
            isFinisherInPot = false;
            if (finisherJudgeCoroutine != null)
            {
                StopCoroutine(finisherJudgeCoroutine);
                finisherJudgeCoroutine = null;
            }
        }

        public void RemoveFromPot(BallBehaviorHK ball)
        {
            if (ballsInPot.Remove(ball))
            {
                Debug.Log("ballsInPot から削除(破棄): " + ball.GetBallType() + " 現在の個数: " + ballsInPot.Count);
                HKGameManager.Instance.OnPotContentsChanged();
                RecipeDisplayUI.Instance.UpdatePotContents(ballsInPot);
            }
        }
    }
}