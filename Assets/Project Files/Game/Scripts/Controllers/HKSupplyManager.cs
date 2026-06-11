using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class HKSupplyManager : MonoBehaviour // hk追加
    {
        public static HKSupplyManager Instance { get; private set; }

        // インスペクターで設定するプレハブ
        [SerializeField] private GameObject finisherPrefab;

        // 供給ボールの出現率設定（インスペクターで設定）
        [SerializeField] private BallSupplyData supplyData;

        // 現在・ネクスト・ネクストネクストのボールデータ
        private BallType currentBall;   // hk追加
        private BallType nextBall;      // hk追加
        private BallType nextNextBall;  // hk追加

        // フィニッシャーの状態管理
        private bool isFinisherActive = false;       // hk追加
        private GameObject currentFinisher = null;   // hk追加

        // 助手悪魔の位置（インスペクターで設定）
        [SerializeField] private Transform assistantPosition;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // ゲーム開始時に3つ分のボールを準備する
            currentBall = supplyData.GetRandomBallType();
            nextBall = supplyData.GetRandomBallType();
            nextNextBall = supplyData.GetRandomBallType();

            UpdateUI();
        }

        // 助手悪魔のボールが弾かれた時
        public void OnAssistantBallLaunched() // hk追加
        {
            ShiftQueue();
        }

        // 盤面のボールが弾かれた時
        public void OnFieldBallLaunched() // hk追加
        {
            // 保持中のボールを破棄してキューをずらす
            ShiftQueue();
        }

        // キューを1つ前にずらして新しいネクストネクストを生成する
        private void ShiftQueue() // hk追加
        {
            currentBall = nextBall;
            nextBall = nextNextBall;
            nextNextBall = supplyData.GetRandomBallType();

            UpdateUI();
        }

        // レシピ成立時にRecipeManagerから呼ばれる
        public void OnRecipeCompleted() // hk追加
        {
            // すでにフィニッシャーが盤面にいる場合は何もしない
            if (isFinisherActive) return;

            // フィニッシャーを生成する
            currentFinisher = Instantiate(
                finisherPrefab,
                assistantPosition.position,
                Quaternion.identity
            );
            isFinisherActive = true;
        }

        // フィニッシャーが鍋に入った時にCookingAreaManagerから呼ばれる
        public void OnFinisherEnteredPot() // hk追加
        {
            isFinisherActive = false;
            currentFinisher = null;
        }

        // 現在のボールの種類を返す
        public BallType GetCurrentBallType() // hk追加
        {
            return currentBall;
        }

        // ネクストボールの種類を返す
        public BallType GetNextBallType() // hk追加
        {
            return nextBall;
        }

        // ネクストネクストボールの種類を返す
        public BallType GetNextNextBallType() // hk追加
        {
            return nextNextBall;
        }

        // UIの更新（次のステップで実装）
        private void UpdateUI() // hk追加
        {
            // HKGameManagerのUI更新を呼ぶ（⑧で実装）
        }
    }
}