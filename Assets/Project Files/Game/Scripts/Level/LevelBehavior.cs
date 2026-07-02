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

        private List<GameObject> items = new List<GameObject>();
        private List<BombData> bombData = new List<BombData>();

        private List<RequirementBehavior> requirements = new List<RequirementBehavior>();

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

        public void InitialiseRequirements(List<Requirement> requirementsInfo, RequirementReceipt requirementReceipt)
        {
            if (!requirements.IsNullOrEmpty())
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    Destroy(requirements[i].gameObject);
                }

                requirements.Clear();
            }

            gameUI.RequirementsResultImage.sprite = requirementReceipt.ResultPreview;

            for (int i = 0; i < requirementsInfo.Count; i++)
            {
                EvolutionBranch branch = LevelController.Database.GetBranch(requirementsInfo[i].branch);

                GameObject requirementObject = Instantiate(branch.requirementUIPrefab);
                requirementObject.transform.SetParent(gameUI.RequirementsParent);
                requirementObject.transform.ResetLocal();

                RequirementBehavior requirement = requirementObject.GetComponent<RequirementBehavior>();

                requirement.Init(requirementsInfo[i], i);

                requirements.Add(requirement);
            }

            for (int i = 0; i < bubbles.Count; i++)
            {
                CheckRequirements(bubbles[i]);
            }
        }

        public void SetRequirements(List<Requirement> requirementsInfo)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                requirements[i].Init(requirementsInfo[i], i);
            }

            for (int i = 0; i < bubbles.Count; i++)
            {
                CheckRequirements(bubbles[i]);
            }
        }

        public List<RequirementBehavior> GetRequirements()
        {
            return requirements;
        }

        public void SpawnIceBubble()
        {
            if (LevelController.CreateRandomBubbleData(out var data))
            {
                var pos = new Vector3(Random.Range(-2f, 2f), Random.Range(-3f, 3f));

                BubbleBehavior bubbleBehavior = SpawnBubble(data, pos, false, Vector2.zero);

                LevelController.IceSpecialEffect.ApplyEffect(bubbleBehavior);
            }
            else
            {
                Debug.LogError("Couldn't generate new random bubble");
            }
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

            requirements.ForEach((requirement) => Destroy(requirement.gameObject));
            bombPool.ReturnToPoolEverything();
            bombPUPool.ReturnToPoolEverything();

            bubbles.Clear();
            requirements.Clear();

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
                bubbles[i].RB.SetLinearDamping(BubblesPhysicsData.BubbleDragMax);
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

        // hk追加：お邪魔・特殊ボールを生成するための公開メソッド
        public BubbleBehavior SpawnNuisanceBallHK(Sprite icon, Vector3 position)
        {
            BubbleSpawnData spawnData = new BubbleSpawnData() { branch = Branch.Vegetables, stageId = 0 };
            if (LevelController.CreateRandomBubbleData(spawnData, out var data))
            {
                data.icon = icon; // hk追加：アイコンを上書きする
                return SpawnBubble(spawnData, data, position, false, Vector2.zero);
            }
            return null;
        }

        // hk追加：ゲーム開始時に手動配置ボールをスポーンする（Level.BallPlacementsを使用）
        public void SpawnBallPlacementsHK()
        {
            Level level = LevelController.Level;
            if (level == null) return;

            foreach (BallPlacementHK placement in level.BallPlacements)
            {
                if (placement.category == BallCategory.Nuisance)
                {
                    NuisanceBallEntry entry = HKSupplyManager.Instance.SupplyData.GetNuisanceEntry(placement.ballLevelIndex);
                    if (entry == null) continue;

                    BubbleBehavior bubble = SpawnNuisanceBallHK(entry.icon, placement.position);
                    if (bubble != null)
                    {
                        BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();
                        if (ballHK != null)
                            ballHK.SetData(BallCategory.Nuisance, placement.ballLevelIndex);
                    }
                }
                // Evolution・Special は今後ここに追加
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
                NuisanceBallEntry entry = HKSupplyManager.Instance.SupplyData.GetNuisanceEntry((int)spawnEvent.ballType);
                if (entry == null) continue;

                for (int i = 0; i < spawnEvent.count; i++)
                {
                    Vector3 position = GetRandomPosition();
                    BubbleBehavior bubble = SpawnNuisanceBallHK(entry.icon, position);
                    if (bubble != null)
                    {
                        BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();
                        if (ballHK != null)
                            ballHK.SetData(BallCategory.Nuisance, (int)spawnEvent.ballType);
                    }
                }
            }
        }

        // hk追加：HKSupplyManagerからボールを生成するための公開メソッド
        public BubbleBehavior SpawnBallHK(Branch branch, int stageId, Vector3 position)
        {
            BubbleSpawnData spawnData = new BubbleSpawnData()
            {
                branch = branch,
                stageId = stageId
            };

            if (LevelController.CreateRandomBubbleData(spawnData, out var data))
            {
                // hk修正：ボールの種類(SetData)をInitより前に確定させ、正しい物理データを適用させる
                BallType ballType = GetBallTypeFromStageId(stageId);
                BubbleBehavior bubble = SpawnBubble(spawnData, data, position, false, Vector2.zero, BallCategory.Evolution, branch, ballType);

                return bubble;
            }

            Debug.LogError("SpawnBallHK: ボールデータの生成に失敗しました");
            return null;
        }

        // hk追加：stageIdをBallTypeに変換する（プログラム0始まり→企画1始まり）
        private BallType GetBallTypeFromStageId(int stageId)
        {
            switch (stageId)
            {
                case 0: return BallType.EvolutionBall_01;
                case 1: return BallType.EvolutionBall_02;
                case 2: return BallType.EvolutionBall_03;
                case 3: return BallType.EvolutionBall_04;
                case 4: return BallType.EvolutionBall_05;
                case 5: return BallType.EvolutionBall_06;
                case 6: return BallType.EvolutionBall_07;
                case 7: return BallType.EvolutionBall_08;
                case 8: return BallType.EvolutionBall_09;
                case 9: return BallType.EvolutionBall_10;
                case 10: return BallType.EvolutionBall_11;
                default: return BallType.EvolutionBall_01;
            }
        }

        public BubbleBehavior SpawnRandomBubble(bool checkAvailable, bool checkAmount = true)
        {
            if (bubbles.Count > LevelController.Level.BubblesOnTheFieldAmount && checkAmount)
                return null;

            if (PairAvailable() || !checkAvailable)
            {
                if (!LevelController.GetRandomSpawnBubble(out var bubbleSpawnData))
                    return null;

                if (LevelController.CreateRandomBubbleData(bubbleSpawnData, out var data))
                {
                    return SpawnBubble(bubbleSpawnData, data, GetRandomPosition(), false, Vector2.zero);
                }
                else
                {
                    Debug.LogError("Couldn't generate new random bubble");
                    return null;
                }
            }
            else
            {
                for (int i = 0; i < bubbles.Count; i++)
                {
                    var bubble = bubbles[i];

                    if (bubble.Data.stageId == 0 && !IsBranchCompleted(bubble.Data.branch) && LevelController.TryGetSpawnData(bubble.Data, out var spawnData))
                    {
                        LevelController.CreateRandomBubbleData(spawnData, out var data);

                        return SpawnBubble(spawnData, data, GetRandomPosition(), false, Vector2.zero);
                    }
                }
            }

            return null;
        }

        private bool IsBranchCompleted(Branch branch)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i].Requirement.branch == branch)
                    return requirements[i].IsSetCompleted;
            }

            return true;
        }

        public void SwapSmallestBubble()
        {
            var smallest = GetSmallestBubble();
            var secondSmallest = GetSmallestBubble(smallest);

            if (smallest == null || secondSmallest == null)
                return;

            smallest.SwapData(secondSmallest.Data);
        }

        // hk修正：category/ballBranch/ballTypeを追加。渡された場合、Initより前にSetDataを呼び、正しい物理データを適用させる
        private BubbleBehavior SpawnBubble(BubbleSpawnData spawnData, BubbleData data, Vector3 position, bool quickAppearance, Vector2 startVelocity, BallCategory? category = null, Branch ballBranch = default, BallType ballType = default)
        {
            BubbleBehavior bubble = bubblesPool.GetPooledComponent();
            bubble.transform.SetParent(transform);
            bubble.transform.position = position;

            // hk修正：Initより前にSetDataを呼ぶ（プールから使い回した古いデータのままInitされるのを防ぐ）
            if (category.HasValue)
            {
                BallBehaviorHK ballHK = bubble.GetComponent<BallBehaviorHK>();
                if (ballHK != null)
                {
                    ballHK.SetData(category.Value, ballBranch, ballType);
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
                    Debug.Log("Hit: " + hit.collider.gameObject.name + " Layer: " + hit.collider.gameObject.layer); // hk追加：デバッグ用
                if (hit.collider != null && hit.transform.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
                {
                    BubbleBehavior tempBubble = hit.transform.GetComponentInParent<BubbleBehavior>(); // hk追加：親も含めて検索
                    Debug.Log("tempBubble: " + (tempBubble == null ? "null" : tempBubble.name)); // hk追加：デバッグ用
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

                    var t = ControlsData.ControlsCurve.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(0, BubblesPhysicsData.ForceMax, direction.magnitude * ControlsData.ControlsPower)));

                    TrajectoryController.Drag(selectedBubble.transform.position, selectedBubble.transform.position - currentPosition, t);

                    selectedBubble.SetTargetSquish(currentPosition);
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
            while (bubbles.Count < LevelController.Level.BubblesOnTheFieldAmount && LevelController.Level.SpawnQueue.Count > 0)
            {
                if (SpawnRandomBubble(true) == null)
                {
                    SpawnRandomBubble(false);
                }
            }
        }

        public void CheckRequirements(IRequirementObject requirementObject)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                RequirementBehavior requirement = requirements[i];

                if (!requirement.IsDone && requirement.Check(requirementObject.Data) && !requirement.IsSetCompleted)
                {
                    requirement.MarkDone();

                    AudioController.PlaySound(AudioController.AudioClips.requirementMetSound);

                    highlightedPair.OnRequirementComplete();

                    requirementObject.OnRequirementMet(requirement, (bool spawnNewBubble) =>
                    {
                        requirement.OnRequirementMet();

                        // hk追加：自動スポーンを無効化（HKSupplyManagerが代わりに担当）
                        // if (spawnNewBubble)
                        // {
                        //     while (bubbles.Count < LevelController.Level.BubblesOnTheFieldAmount && LevelController.Level.SpawnQueue.Count > 0)
                        //     {
                        //         if (SpawnRandomBubble(true) == null)
                        //         {
                        //             SpawnRandomBubble(false);
                        //         }
                        //     }
                        // }

                        LevelController.OnRequirementDone(i);
                        LevelController.UpdateRequirements();
                    });

                    break;
                }
            }
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
                float multiplier = GetDragMultiplier(bubbles[i]);
                bubbles[i].RB.SetLinearDamping(BubblesPhysicsData.BubbleDragMin * multiplier);
            }

            dragCase.KillActive();
            dragCase = Tween.DoFloat(BubblesPhysicsData.BubbleDragMin, BubblesPhysicsData.BubbleDragMax, BubblesPhysicsData.DragTransitionDuration,
                (value) =>
                {
                    for (int i = 0; i < bubbles.Count; i++)
                    {
                        if (bubbles[i] == null) continue; // hk追加：破棄済みオブジェクトをスキップ
                        if (!bubbles[i].IsMerging && (bubbles[i].BubbleSpecialEffect == null || bubbles[i].BubbleSpecialEffect.IsDragAllowed()))
                        {
                            float multiplier = GetDragMultiplier(bubbles[i]);
                            bubbles[i].RB.SetLinearDamping(value * multiplier);
                        }
                    }
                }, BubblesPhysicsData.MinDragDuration).SetCurveEasing(BubblesPhysicsData.BubbleDragCurve);
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

            var entry = HKSupplyManager.Instance.SupplyData.GetEntry(ballHK.GetBranch(), ballHK.GetBallType());
            if (entry == null) return 1f;

            float speed = bubble.RB.GetVelocity().magnitude;
            return entry.linearDamping + entry.dampingCurve.Evaluate(speed);
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

        public RequirementBehavior GetRequirementBehavior(Branch branch)
        {
            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i].Requirement.branch == branch)
                    return requirements[i];
            }

            return null;
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
            if (LevelController.CreateBubbleData(new Requirement(bubble1.Data.branch, bubble1.Data.stageId + 1), out var data))
            {
                // hk修正：ボールの種類(SetData)をInitより前に確定させ、正しい物理データを適用させる
                BallType ballType = GetBallTypeFromStageId(data.stageId);
                BubbleSpawnData spawnData = new BubbleSpawnData() { branch = data.branch, stageId = data.stageId };
                var newBubble = SpawnBubble(spawnData, data, position, true, (bubble1.RB.GetVelocity()), BallCategory.Evolution, data.branch, ballType);

                // hk追加：テンプレートのRequirements判定を無効化（HKのレシピシステムを使うため）
                // CheckRequirements(newBubble);

                OnBubbleMerged?.Invoke(newBubble);
            }
            else
            {
                var newBubble = SpawnRandomBubble(true);

                if (newBubble != null)
                {
                    // hk追加：テンプレートのRequirements判定を無効化（HKのレシピシステムを使うため）
                    // CheckRequirements(newBubble);
                    OnBubbleMerged?.Invoke(newBubble);
                }
            }

            // hk追加：自動スポーンを無効化（HKSupplyManagerが代わりに担当）
            // if (bubbles.Count - 1 <= LevelController.Level.BubblesOnTheFieldAmount)
            // {
            //     var newRandomBubble = SpawnRandomBubble(true, false);
            //     if (newRandomBubble != null)
            //     {
            //         CheckRequirements(newRandomBubble);
            //     }
            // }
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