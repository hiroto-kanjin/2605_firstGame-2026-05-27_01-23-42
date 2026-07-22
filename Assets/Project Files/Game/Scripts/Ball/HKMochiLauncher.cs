using UnityEngine;
using UnityEngine.InputSystem; // hk追加：このプロジェクトの入力方式(Input System)を使う

namespace Watermelon.BubbleMerge
{
    // hk追加：ボールを引っ張って放すと、引っ張った距離に応じた威力で反対方向へ飛ばす発射スクリプト。
    // 既存の発射(Launch)には一切繋がない完全独立版。付けるだけ・外すだけで動く。
    // ★キャラ自身は動かさない。マウス位置を見るだけで、放した瞬間に今のボール位置から発射する。
    [RequireComponent(typeof(Rigidbody2D))]
    public class HKMochiLauncher : MonoBehaviour
    {
        [Header("威力の設定")]
        [SerializeField] private float minForce = 2f;   // 弱く引いた時の飛ぶ強さ
        [SerializeField] private float maxForce = 12f;  // 強く引いた時の飛ぶ強さ

        [Header("引っ張りの設定")]
        [SerializeField] private float maxPullDistance = 3f; // これ以上引いても威力が上がらない上限

        [Header("威力カーブ（横：引っ張り具合0〜1／縦：威力の割合0〜1）")]
        [SerializeField] private AnimationCurve forceCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("発射直後のすり抜け設定")] // hk追加
        [SerializeField] private string launchingLayerName = "LaunchingBall"; // hk追加：発射直後の間だけ使うレイヤー名
        [SerializeField] private string normalLayerName = "Bubble";           // hk修正：Defaultではなく盤面ボールのレイヤーに戻す

        private Rigidbody2D rb;   // 飛ばすための物理の体
        private bool isDragging;  // 今引っ張り中かどうか

        private bool isLaunching;        // hk追加：今すり抜け状態中かどうか

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            // 押した瞬間：自分のボールを掴んだかを判定
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (IsMouseOnMe())
                    isDragging = true;
            }

            // 放した瞬間：距離を威力に変換して発射
            if (isDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DoRelease();
            }

            // hk修正：すり抜け状態中なら、毎フレーム中間ラインをチェックする
            if (isLaunching)
            {
                CheckClearLine();
            }
        }

        // 今のマウス位置を、ゲーム世界の座標に変換して返す
        private Vector3 GetMouseWorld()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            world.z = transform.position.z;
            return world;
        }

        // マウスの下に自分のボールの当たり判定があるか調べる
        private bool IsMouseOnMe()
        {
            Vector3 mouseWorld = GetMouseWorld();
            Vector2 mouse2D = new Vector2(mouseWorld.x, mouseWorld.y);

            Collider2D[] hits = Physics2D.OverlapPointAll(mouse2D);
            foreach (Collider2D hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                    return true;
            }
            return false;
        }

        // 放した瞬間：今のボール位置とマウス位置から、距離を威力に変換して発射
        private void DoRelease()
        {
            isDragging = false;

            // 起点＝今のボール位置、指先＝今のマウス位置（どちらも記憶せず今の値を使う）
            Vector3 start = transform.position;
            Vector3 mouse = GetMouseWorld();

            // 引っ張りの向きと距離
            Vector3 pullVector = mouse - start;
            pullVector.z = 0;

            float pullDistance = pullVector.magnitude;
            if (pullDistance <= 0.001f) return; // ほぼ引いてなければ発射しない

            // 上限で頭打ちにする
            if (pullDistance > maxPullDistance)
                pullDistance = maxPullDistance;

            // ①引っ張り距離を0〜1の割合にする
            float pullRatio = Mathf.Clamp01(pullDistance / maxPullDistance);

            // ②割合をカーブに通す
            float curveValue = forceCurve.Evaluate(pullRatio);

            // ③最小威力〜最大威力に当てはめる
            float force = Mathf.Lerp(minForce, maxForce, curveValue);

            // ④引っ張りと反対方向へ発射（キャラはここまで一切動かしていない）
            Vector3 launchDir = -pullVector.normalized;
            rb.AddForce(launchDir * force, ForceMode2D.Impulse);

            // hk追加：発射直後のすり抜け状態を開始する
            StartLaunchingState();
        }

        // hk修正：レイヤー変更のみ行う（距離の記録は不要になった）
        private void StartLaunchingState()
        {
            isLaunching = true;
            SetLayerRecursive(transform, LayerMask.NameToLayer(launchingLayerName));
        }

        // hk修正：発射地点からの距離ではなく、盤面の中間ライン(Y座標)を超えたかで判定する
        private void CheckClearLine()
        {
            if (transform.position.y >= LevelController.CurrentMergeLineY)
            {
                isLaunching = false;
                SetLayerRecursive(transform, LayerMask.NameToLayer(normalLayerName));
            }
        }

        // hk追加：自分自身と、すべての子オブジェクトのレイヤーをまとめて変更する
        private void SetLayerRecursive(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target)
            {
                SetLayerRecursive(child, layer);
            }
        }
    }
}