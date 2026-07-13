using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class LevelBehavior : MonoBehaviour
    {
        private GameObject levelShape;
        private GameObject levelBackground;

        private PoolGeneric<BubbleBehavior> bubblesPool;

        private List<BubbleBehavior> bubbles = new List<BubbleBehavior>();
        private Dictionary<BubbleBehavior, Coroutine> dragCoroutines = new Dictionary<BubbleBehavior, Coroutine>(); // hk追加：ボールごとのDrag処理を管理

        private List<GameObject> items = new List<GameObject>();
        private List<BombData> bombData = new List<BombData>();

       

        public event SimpleCallback OnTapHappened;

        private BubbleBehavior selectedBubble;

        private static Vector3 screenPoint;

        public delegate void BubbleCallback(BubbleBehavior bubble);

        public event BubbleCallback OnBubbleSelected;
        public event BubbleCallback OnBubbleLaunched;
        public event BubbleCallback OnBubbleMerged;

        private ComboManager comboManager;

        private List<Vector2> positionsList = new List<Vector2>();

        public List<BubbleBehavior> GetBubbles() => bubbles;

        private UIGame gameUI;
        private Pool bombPool;
        private Pool bombPUPool;

        private TweenCase dragCase;

        private static HighlightedPair highlightedPair;

        public void Init(GameObject bubblePrefab)
        {
            bubblesPool = new PoolGeneric<BubbleBehavior>(bubblePrefab, $"Bubble", transform);

            bombPool = new Pool(LevelController.BombPrefab, $"Bomb");
            bombPUPool = new Pool(LevelController.BombPUPrefab, $"BombPU");

            comboManager = gameObject.AddComponent<ComboManager>();
            comboManager.Init(LevelController.ComboDatabase);

            gameUI = UIController.GetPage<UIGame>();

            highlightedPair = new HighlightedPair();
        }

        private void OnDestroy()
        {
            PoolManager.DestroyPool(bubblesPool);

            PoolManager.DestroyPool(bombPool);
            PoolManager.DestroyPool(bombPUPool);
        }

        public void ChangeShape(GameObject shapePrefab)
        {
            if (levelShape != null && shapePrefab.name == levelShape.name)
                return;

            if (levelShape != null)
                Destroy(levelShape);

            levelShape = Instantiate(shapePrefab);
        }

        public void ChangeBackround(GameObject backPrefab)
        {
            if (levelBackground != null && backPrefab.name == levelBackground.name)
                return;

            if (levelBackground != null)
                Destroy(levelBackground);

            levelBackground = Instantiate(backPrefab);
        }

        public void SetLevelItems(ItemSave[] levelItems, BombData[] bombs, LevelDatabase database)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Destroy(items[i]);
            }

            items.Clear();

            GameObject newItem;
            TeleportBehavior firstOfPair = null;
            TeleportBehavior secondOfPair = null;
            bool lookingForPair = false;

            for (int i = 0; i < levelItems.Length; i++)
            {


                newItem = Instantiate(database.GetItem(levelItems[i].Type).Prefab, levelItems[i].Position, Quaternion.Euler(levelItems[i].Rotation));
                newItem.transform.localScale = levelItems[i].Scale;

                if (levelItems[i].Type == Item.Teleport)
                {
                    if (lookingForPair)
                    {
                        secondOfPair = newItem.GetComponent<TeleportBehavior>();
                        firstOfPair.Neighbour = secondOfPair;
                        secondOfPair.Neighbour = firstOfPair;

                        lookingForPair = false;
                    }
                    else
                    {
                        firstOfPair = newItem.GetComponent<TeleportBehavior>();
                        lookingForPair = true;
                    }
                }

                items.Add(newItem);
            }

            bombData.Clear();
            bombData.AddRange(bombs);
        }



       

        public void Clear()
        {
            highlightedPair.Reset();

            bubbles.ForEach((bubble) =>
            {
                if (bubble == null) return; // hk追加：破棄済みオブジェクトをスキップ
                bubble.RB.simulated = true;
                bubble.DisableEffect();
                bubble.transform.SetParent(transform);
                bubble.gameObject.SetActive(false);
            });

            
            bombPool.ReturnToPoolEverything();
            bombPUPool.ReturnToPoolEverything();

            bubbles.Clear();
     

            for (int i = 0; i < items.Count; i++)
            {
                Destroy(items[i]);
            }

            items.Clear();

            if (levelShape != null)
                Destroy(levelShape);

            if (levelBackground != null)
                Destroy(levelBackground);

            TrajectoryController.EndDrag();
        }

        public void InitialSpawn(SimpleCallback onSpawned = null)
        {
            float width = 6;
            float height = 8;
            float minSpacing = 1.0f;

            positionsList.Clear();

            PoissonDiscSampler sampler = new PoissonDiscSampler(width, height, minSpacing);
            Vector2 originPosition = Vector2.zero.SetY(0.25f) - new Vector2(width * 0.5f, height * 0.5f);

            foreach (Vector2 sample in sampler.Samples())
            {
                RaycastHit2D cast = Physics2D.CircleCast(originPosition + new Vector2(sample.x, sample.y), 0.3f, Vector2.zero);

                if (cast.transform == null)
                    positionsList.Add(originPosition + new Vector2(sample.x, sample.y));
            }

            positionsList.Shuffle();

            StartCoroutine(InitialSpawnCoroutine(onSpawned));
        }

        private IEnumerator InitialSpawnCoroutine(SimpleCallback onSpawned = null)
        {
            // hk追加：自動スポーンを無効化（HKSupplyManagerが代わりに担当）
            // for (int i = 0; i < LevelController.Level.BubblesOnTheFieldAmount; i++)
            // {
            //     SpawnRandomBubble(false, false);
            //     yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            // }

            // hk追加：コンパイルエラー回避
            yield return null;

            CheckIfBombSpawnRequired();

            // hk追加：ペア確認スポーンも無効化
            // if (!PairAvailable())
            // {
            //     SpawnRandomBubble(false, false);
            // }

            for (int i = 0; i < bubbles.Count; i++)
            {
                BallBehaviorHK ballHK = bubbles[i].GetComponent<BallBehaviorHK>();
                BubblesPhysicsData pattern = ballHK != null ? ballHK.GetPhysicsPattern() : null;
                if (pattern != null)
                {
                    bubbles[i].RB.SetLinearDamping(pattern.BubbleDragMax);
                }
            }

            highlightedPair.ActivateWithDelay(HighlightedPair.HIGHLIGHT_DELAY);

            onSpawned?.Invoke();
        }

        private void CheckIfBombSpawnRequired()
        {
            for (int i = 0; i < bombData.Count; i++)
            {
                if (bombData[i].MovesToSpawn <= LevelController.Turn)
                {
                    SpawnBomb();

                    bombData.RemoveAt(i);
                    i--;
                }
            }
        }

        public BombBehavior SpawnBomb()
        {
            GameObject bombItem = bombPool.GetPooledObject();
            bombItem.transform.SetParent(transform);
            bombItem.transform.position = GetRandomPosition();

            return bombItem.GetComponent<BombBehavior>();
        }

        public BombBehavior SpawnPUBomb(List<Vector3> usedPositions)
        {
            GameObject bombItem = bombPUPool.GetPooledObject();
            bombItem.transform.SetParent(transform);
            bombItem.transform.position = GetRandomPosition(usedPositions);

            return bombItem.GetComponent<BombBehavior>();
        }
        // hk修正：indexを引数で受け取り、SpawnBubble経由でInit前にSetDataさせる（後付けをやめ、正しい順番にする）
        public BubbleBehavior SpawnNuisanceBallHK(int index, Vector3 position)
        {
            BubbleSpawnData spawnData = new BubbleSpawnData() { branch = Branch.kinoko, stageId = 0 };
            if (LevelController.CreateRandomBubbleData(spawnData, out var data))
            {
                return SpawnBubble(spawnData, data, position, false, Vector2.zero, BallCategory.Nuisance, index);
            }
            return null;
        }
        // hk追加：特殊ボールを盤面に出す。SpawnBubble経由でInit前にSetDataさせる（進化・お邪魔と同じ正しい順番）
        public BubbleBehavior SpawnSpecialBallHK(int number, Vector3 position)
        {
            BubbleSpawnData spawnData = new BubbleSpawnData() { branch = Branch.kinoko, stageId = 0 };
            if (LevelController.CreateRandomBubbleData(spawnData, out var data))
            {
                return SpawnBubble(spawnData, data, position, false, Vector2.zero, BallCategory.Special, number);
            }
            return null;
        }
        // hk追加：ゲーム開始時に手動配置ボールをスポーンする（Level.BallPlacementsを使用）
        // hk修正：Nuisanceだけでなく、Evolution・Specialも配置できるようcategoryで振り分ける
        // hk追加：ゲーム開始時に手動配置ボールをスポーンする（Level.BallPlacementsを使用）
        // hk修正：branch除去。indexをレシピ（evolutionChain/specialList）経由でnumberに変換して撒く。
        public void SpawnBallPlacementsHK()
        {
            Level level = LevelController.Level;
            if (level == null) return;

            GameLevelData gameLevel = HKGameManager.Instance.GetCurrentLevel();
            if (gameLevel == null) return;

            RecipeEntry recipe = HKSupplyManager.Instance.RecipeData.GetRecipeById(gameLevel.recipeId);
            if (recipe == null) return;

            foreach (BallPlacementHK placement in level.BallPlacements)
            {
                switch (placement.category)
                {
                    case BallCategory.Evolution:
                        // 進化：レシピのevolutionChainのindex番目からnumberを引く
                        if (placement.index >= 0 && placement.index < recipe.evolutionChain.Count)
                        {
                            int number = recipe.evolutionChain[placement.index];
                            SpawnBallHK(Branch.kinoko, number, placement.position);
                        }
                        break;

                    case BallCategory.Special:
                        // 特殊：レシピのspecialListのindex番目からnumberを引く
                        if (placement.index >= 0 && placement.index < recipe.specialList.Count)
                        {
                            int number = recipe.specialList[placement.index].number;
                            SpawnSpecialBallHK(number, placement.position);
                        }
                        break;

                    case BallCategory.Nuisance:
                        // お邪魔：レシピに紐づかない。indexをそのまま種類番号として使う
                        SpawnNuisanceBallHK(placement.index, placement.position);
                        break;

                    default:
                        Debug.LogWarning($"SpawnBallPlacementsHK: 未対応のカテゴリ({placement.category})が配置にありました");
                        break;
                }
            }
        }

        // hk追加：指定ショット数に対応するお邪魔ボール出現イベントを処理する（GameLevelDataを使用）
        public void TrySpawnNuisanceEventsHK(int currentShot)
        {
            GameLevelData gameLevelData = HKGameManager.Instance.GetCurrentLevel(); // hk追加
            if (gameLevelData == null) return;

            List<NuisanceSpawnEvent> events = gameLevelData.GetEventsForShot(currentShot);
            foreach (NuisanceSpawnEvent spawnEvent in events)
            {
                for (int i = 0; i < spawnEvent.count; i++)
                {
                    Vector3 position = GetRandomPosition();
                    // hk修正：indexを引数で渡し、Init前にSetDataさせる（後付けを廃止）
                    SpawnNuisanceBallHK((int)spawnEvent.ballType, position);
                }
            }
        }

        // hk追加：HKSupplyManagerからボールを生成するための公開メソッド
        // hk修正：Ball Typeではなくindex（番号）を渡す形に変更（ブランチ除去）
        public BubbleBehavior SpawnBallHK(Branch branch, int stageId, Vector3 position)
        {
            BubbleSpawnData spawnData = new BubbleSpawnData()
            {
                branch = branch,
                stageId = stageId
            };

            if (LevelController.CreateRandomBubbleData(spawnData, out var data))
            {
                // hk修正：indexにstageId（番号）を渡す。番号はBallIndexから引かれる（Ball Type依存を廃止）
               BubbleBehavior bubble = SpawnBubble(spawnData, data, position, false, Vector2.zero, BallCategory.Evolution, stageId);

                return bubble;
            }

            Debug.LogError("SpawnBallHK: ボールデータの生成に失敗しました");
            return null;
        }


        



        public void SwapSmallestBubble()
        {
            var smallest = GetSmallestBubble();
            var secondSmallest = GetSmallestBubble(smallest);

            if (smallest == null || secondSmallest == null)
                return;

            smallest.SwapData(secondSmallest.Data);
        }
       private BubbleBehavior SpawnBubble(BubbleSpawnData spawnData, BubbleData data, Vector3 position, bool quickAppearance, Vector2 startVelocity, BallCategory? category = null, int? index = null)
        {
            BubbleBehavior bubble = bubblesPool.GetPooledComponent();
            bubble.enabled = true; // hk修正：プール再利用時にBubbleBehaviorが無効のままになるのを防ぐ
            bubble.transform.SetParent(transform);
            bubble.transform.position = position;

            // hk修正：Initより前にSetDataを呼ぶ（プールから使い回した古いデータのままInitされるのを防ぐ）
            if (category.HasValue && index.HasValue)
            {
                BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();
                if (ballHK != null)
                {
                    ballHK.SetData(category.Value, index.Value);
                }
            }

            bubble.Init(data, quickAppearance, startVelocity);

            if (IsActiveBubbleExists())
            {
                if (spawnData.iceHP > 0)
                {
                    LevelController.IceSpecialEffect.Health = spawnData.iceHP;
                    LevelController.IceSpecialEffect.ApplyEffect(bubble);
                }
                else if (spawnData.boxHP > 0)
                {
                    LevelController.CrateSpecialEffect.Health = spawnData.boxHP;
                    LevelController.CrateSpecialEffect.ApplyEffect(bubble);
                }
            }

            bubbles.Add(bubble);

            CheckIfBombSpawnRequired();

            return bubble;
        }

        private BubbleBehavior SpawnBubble(BubbleData data, Vector3 position, bool quickAppearance, Vector2 startVelocity)
        {
            BubbleBehavior bubble = bubblesPool.GetPooledComponent();
            bubble.transform.SetParent(transform);
            bubble.transform.position = position;
            bubble.Init(data, quickAppearance, startVelocity);

            bubbles.Add(bubble);

            return bubble;
        }

        private void Update()
        {
            if (!LevelController.IsGameplayActive)
                return;
            if (UIController.IsPopupOpened)
                return;

            if (InputController.ClickAction.WasPressedThisFrame())
            {
                var ray = Camera.main.ScreenPointToRay(InputController.MousePosition);

                RaycastHit2D hit = Physics2D.Raycast(ray.origin.SetZ(0), Vector2.zero, 100, ~PhysicsHelper.LAYER_BUBBLE);
                if (hit.collider != null)
                    
                if (hit.collider != null && hit.transform.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
                {
                    BubbleBehavior tempBubble = hit.transform.GetComponentInParent<BubbleBehavior>(); // hk追加：親も含めて検索
                    
                    if (tempBubble.IsActive() && !tempBubble.IsNuisance() && (!HKSupplyManager.Instance.IsFinisherActive() || tempBubble.GetComponent<FinisherBall>() != null)) // hk追加：フィニッシャー中はフィニッシャー以外選択不可
                    {
                        selectedBubble = tempBubble;

                        OnBubbleSelected?.Invoke(selectedBubble);
                        OnTapHappened?.Invoke();

                        screenPoint = Camera.main.WorldToScreenPoint(selectedBubble.transform.position);

                        TrajectoryController.BeginDrag(selectedBubble);

                        selectedBubble.ColliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE_ACTIVE;

                        highlightedPair.Reset();
                        highlightedPair.ActivateWithDelay(HighlightedPair.HIGHLIGHT_DELAY);
                    }
                }
            }
            else if (InputController.ClickAction.WasReleasedThisFrame())
            {
                if (selectedBubble != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(InputController.MousePosition);
                    if (Vector3.Distance(ray.origin.SetZ(selectedBubble.transform.position.z), selectedBubble.transform.position) > 0.25f)
                    {
                        // hk追加：ファイナルカウントが0の時は発射しない
                        if (!HKGameManager.Instance.IsFinalCountZero())
                        {
                            ActivateMinDrag();

                            selectedBubble.Launch(ray.origin.SetZ(selectedBubble.transform.position.z));

                            OnBubbleLaunched?.Invoke(selectedBubble);

                            selectedBubble.ColliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

                            AudioController.PlaySound(AudioController.AudioClips.launchBubbleSound);
                        }
                    }

                    TrajectoryController.EndDrag();

                    selectedBubble = null;
                }
            }
            else if (InputController.ClickAction.IsPressed())
            {
                if (selectedBubble != null)
                {
                    Vector3 currentScreenPoint = new Vector3(InputController.MousePosition.x, InputController.MousePosition.y, screenPoint.z);
                    Vector3 currentPosition = Camera.main.ScreenToWorldPoint(currentScreenPoint);

                    var direction = (selectedBubble.transform.position - currentPosition).xy();

                    BallBehaviorHK ballHK = selectedBubble.GetComponent<BallBehaviorHK>();
                    BubblesPhysicsData pattern = ballHK != null ? ballHK.GetPhysicsPattern() : null;

                    if (pattern != null)
                    {
                        var t = ControlsData.ControlsCurve.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(0, pattern.VisualDragMax, direction.magnitude * ControlsData.ControlsPower)));

                        TrajectoryController.Drag(selectedBubble.transform.position, selectedBubble.transform.position - currentPosition, t);

                        selectedBubble.SetTargetSquish(currentPosition);
                    }
                }
            }

            // hk追加：自動スポーンを無効化（HKSupplyManagerが代わりに担当）
            // if (Time.frameCount % 60 == 0)
            // {
            //     if (!PairAvailable())
            //     {
            //         if (SpawnRandomBubble(true, false) == null)
            //         {
            //             SpawnRandomBubble(false, false);
            //         }
            //
            //         if (!IsActiveBubbleExists())
            //         {
            //             LevelController.LevelFail();
            //         }
            //     }
            // }
        }

        public void OnBubblePopped(BubbleBehavior bubbleBehavior)
        {
            // hk修正：旧供給（SpawnQueueからの自動補充）を撤去。供給はHKSupplyManagerが担当。
        }
        private bool PairAvailable()
        {
            for (int i = 0; i < bubbles.Count - 1; i++)
            {
                for (int j = i + 1; j < bubbles.Count; j++)
                {
                    if (bubbles[i].Compare(bubbles[j]) && !(bubbles[i].BubbleSpecialEffect != null && bubbles[j].BubbleSpecialEffect != null))
                        return true;
                }
            }

            return false;
        }

        public BubblesPair GetPair(Branch branch, int stageIndex, bool spawnNew = false)
        {
            BubbleBehavior firstBubbleBehavior = null;
            int firstBubbleIndex = -1;

            for (int i = 0; i < bubbles.Count - 1; i++)
            {
                if (bubbles[i].Data.branch == branch && bubbles[i].Data.stageId == stageIndex && bubbles[i].BubbleSpecialEffect == null)
                {
                    firstBubbleBehavior = bubbles[i];
                    firstBubbleIndex = i;
                    break;
                }
            }

            if (firstBubbleBehavior == null)
                return null;

            BubbleBehavior secondBubbleBehavior = null;

            for (int i = firstBubbleIndex + 1; i < bubbles.Count; i++)
            {
                if (bubbles[i].Data.branch == branch && bubbles[i].Data.stageId == stageIndex && bubbles[i].BubbleSpecialEffect == null)
                {
                    secondBubbleBehavior = bubbles[i];
                    break;
                }
            }

            if (secondBubbleBehavior == null)
            {
                if (spawnNew)
                {
                    BubbleSpawnData bubbleSpawnData = new BubbleSpawnData() { branch = branch, stageId = stageIndex };
                    BubbleData newBubbleData;

                    if (LevelController.CreateRandomBubbleData(bubbleSpawnData, out newBubbleData))
                    {
                        secondBubbleBehavior = SpawnBubble(newBubbleData, GetRandomPosition(), true, Vector2.zero);
                        return new BubblesPair() { bubbleBehavior1 = firstBubbleBehavior, bubbleBehavior2 = secondBubbleBehavior };
                    }
                }

                return null;
            }

            return new BubblesPair() { bubbleBehavior1 = firstBubbleBehavior, bubbleBehavior2 = secondBubbleBehavior };
        }


        public void ActivateMinDrag()
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                if (bubbles[i] == null) continue; // hk追加：破棄済みオブジェクトをスキップ

                BubblesPhysicsData pattern = GetBubblePhysicsPattern(bubbles[i]);
                if (pattern == null) continue;

                // hk追加：同じボールに対して前の処理がまだ動いていたら、先に止める
                if (dragCoroutines.TryGetValue(bubbles[i], out Coroutine existingCoroutine))
                {
                    if (existingCoroutine != null)
                        StopCoroutine(existingCoroutine);
                    dragCoroutines.Remove(bubbles[i]);
                }

                float multiplier = GetDragMultiplier(bubbles[i]);
                bubbles[i].RB.SetLinearDamping(pattern.BubbleDragMin * multiplier);

                Coroutine newCoroutine = StartCoroutine(DragTransitionCoroutine(bubbles[i], pattern));
                dragCoroutines[bubbles[i]] = newCoroutine;
            }
        }

        // hk追加：ボールごとに、そのボール専用の設定で抵抗を時間経過させる
        private IEnumerator DragTransitionCoroutine(BubbleBehavior bubble, BubblesPhysicsData pattern)
        {
            yield return new WaitForSeconds(pattern.MinDragDuration);

            float elapsed = 0f;
            while (elapsed < pattern.DragTransitionDuration)
            {
                if (bubble == null)
                {
                    dragCoroutines.Remove(bubble);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = pattern.BubbleDragCurve.Evaluate(elapsed / pattern.DragTransitionDuration);
                float value = Mathf.Lerp(pattern.BubbleDragMin, pattern.BubbleDragMax, t);

                if (!bubble.IsMerging && (bubble.BubbleSpecialEffect == null || bubble.BubbleSpecialEffect.IsDragAllowed()))
                {
                    float multiplier = GetDragMultiplier(bubble);
                    bubble.RB.SetLinearDamping(value * multiplier);
                }

                yield return null;
            }

            dragCoroutines.Remove(bubble); // hk追加：処理が正常に終わったら記録から消す
        }

        // hk追加：ボールが使うBubblesPhysicsDataを取得する共通の窓口
        private BubblesPhysicsData GetBubblePhysicsPattern(BubbleBehavior bubble)
        {
            BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();
            if (ballHK == null) return null;
            return ballHK.GetPhysicsPattern();
        }

        // hk追加：ボールごとのDamping倍率を取得（基本値＋速度に応じたカーブ補正の加算）
        private float GetDragMultiplier(BubbleBehavior bubble)
        {
            BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();

            if (ballHK == null || !ballHK.enabled)
            {
                FinisherBall finisherBall = bubble.GetComponent<FinisherBall>();
                if (finisherBall != null)
                {
                    var finisherEntry = HKSupplyManager.Instance.FinisherData.GetEntry(finisherBall.GetFinisherType());
                    if (finisherEntry != null)
                    {
                        float finisherSpeed = bubble.RB.GetVelocity().magnitude;
                        return finisherEntry.linearDamping + finisherEntry.dampingCurve.Evaluate(finisherSpeed);
                    }
                }
                return 1f;
            }

            BubblesPhysicsData pattern = ballHK.GetPhysicsPattern();
            if (pattern == null) return 1f;

            float speed = bubble.RB.GetVelocity().magnitude;
            return pattern.LinearDamping + pattern.DampingCurve.Evaluate(speed);
        }

       
        public Vector3 GetRandomPosition() // hk追加：引数なし版
        {
            Vector3 randomPosition = positionsList[Random.Range(0, positionsList.Count)];
            int loops = 0;
            while (loops < 15)
            {
                RaycastHit2D cast = Physics2D.CircleCast(randomPosition, 0.2f, Vector2.zero);
                if (cast.transform == null)
                {
                    break;
                }

                randomPosition = positionsList[Random.Range(0, positionsList.Count)];
                loops++;
            }

            return randomPosition;
        }
        public Vector3 GetRandomPosition(List<Vector3> excludedPositions)
        {
            int iterations = 0;
            Vector3 result = Vector3.zero;

            while (iterations < 10)
            {
                result = GetRandomPosition();

                if (!excludedPositions.Contains(result))
                {
                    iterations = 10;
                    return result;
                }
            }

            return result;
        }

        private BubbleBehavior GetSmallestBubble(BubbleBehavior ignoredBubble = null)
        {
            BubbleBehavior smallestBubble = null;
            int smallestStage = 100000;

            for (int i = 0; i < bubbles.Count; i++)
            {
                var bubble = bubbles[i];

                if (bubble == ignoredBubble)
                    continue;

                if (bubble.Data.stageId < smallestStage)
                {
                    smallestStage = bubble.Data.stageId;
                    smallestBubble = bubble;
                }
            }

            return smallestBubble;
        }


        public bool IsActiveBubbleExists()
        {
            if (!bubbles.IsNullOrEmpty())
            {
                for (int i = 0; i < bubbles.Count; i++)
                {
                    if (!bubbles[i].IsMerging && bubbles[i].IsActive())
                        return true;
                }

                return false;
            }

            return true;
        }

        public void RemoveBubble(BubbleBehavior bubble)
        {
            bubbles.Remove(bubble);
        }
        public void AddBubble(BubbleBehavior bubble) // hk追加
        {
            bubbles.Add(bubble);
        }
        public void OnBubblesMerged(BubbleBehavior bubble1, BubbleBehavior bubble2, Vector3 position)
        {
            // hk修正：次の段階のボールを、Ball Index方式のSpawnBallHKで作る（Requirement/ballType依存を廃止＝ブランチ除去）
            int nextNumber = bubble1.Data.stageId + 1;
            BubbleBehavior newBubble = SpawnBallHK(bubble1.Data.branch, nextNumber, position);

            if (newBubble != null)
            {
                // 合体は勢いを引き継ぐ（元のボールの速度を渡す）
                newBubble.RB.SetVelocity(bubble1.RB.GetVelocity());

                OnBubbleMerged?.Invoke(newBubble);
            }
            // hk修正：旧供給（SpawnRandomBubble）を撤去。次段階が作れない場合は何もしない。
        }

        #region Dev

        public void RemoveRandomBubbleDev()
        {
            if (bubbles.Count > 0)
            {
                bubbles[Random.Range(0, bubbles.Count)].Pop();
            }
        }

        public void UpdateBubblesSize(float newSize)
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                bubbles[i].transform.localScale = Vector3.one * newSize;
            }
        }

        #endregion
    }

    public delegate void RequirementCallback(bool spawnNewBubble);
}