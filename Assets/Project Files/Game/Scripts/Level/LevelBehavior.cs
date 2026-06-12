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
                return SpawnBubble(spawnData, data, position, false, Vector2.zero);
            }

            Debug.LogError("SpawnBallHK: ボールデータの生成に失敗しました");
            return null;
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

        private BubbleBehavior SpawnBubble(BubbleSpawnData spawnData, BubbleData data, Vector3 position, bool quickAppearance, Vector2 startVelocity)
        {
            BubbleBehavior bubble = bubblesPool.GetPooledComponent();
            bubble.transform.SetParent(transform);
            bubble.transform.position = position;
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

                if (hit.collider != null && hit.transform.gameObject.CompareTag(PhysicsHelper.TAG_BUBBLE))
                {
                    BubbleBehavior tempBubble = hit.transform.gameObject.GetComponent<BubbleBehavior>();
                    if (tempBubble.IsActive())
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
                        ActivateMinDrag();

                        selectedBubble.Launch(ray.origin.SetZ(selectedBubble.transform.position.z));

                        OnBubbleLaunched?.Invoke(selectedBubble);

                        selectedBubble.ColliderRef.gameObject.layer = PhysicsHelper.LAYER_BUBBLE;

                        AudioController.PlaySound(AudioController.AudioClips.launchBubbleSound);
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

                        if (spawnNewBubble)
                        {
                            while (bubbles.Count < LevelController.Level.BubblesOnTheFieldAmount && LevelController.Level.SpawnQueue.Count > 0)
                            {
                                if (SpawnRandomBubble(true) == null)
                                {
                                    SpawnRandomBubble(false);
                                }
                            }
                        }

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
                bubbles[i].RB.SetLinearDamping(BubblesPhysicsData.BubbleDragMin);
            }

            dragCase.KillActive();
            dragCase = Tween.DoFloat(BubblesPhysicsData.BubbleDragMin, BubblesPhysicsData.BubbleDragMax, BubblesPhysicsData.DragTransitionDuration,
                (value) =>
                {
                    for (int i = 0; i < bubbles.Count; i++)
                    {
                        if (!bubbles[i].IsMerging && (bubbles[i].BubbleSpecialEffect == null || bubbles[i].BubbleSpecialEffect.IsDragAllowed()))
                        {
                            bubbles[i].RB.SetLinearDamping(value);
                        }
                    }
                }, BubblesPhysicsData.MinDragDuration).SetCurveEasing(BubblesPhysicsData.BubbleDragCurve);
        }

        public static bool HasPair()
        {
            return LevelController.LevelBehavior.PairAvailable();
        }

        public Vector3 GetRandomPosition()
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

        public void OnBubblesMerged(BubbleBehavior bubble1, BubbleBehavior bubble2, Vector3 position)
        {
            if (LevelController.CreateBubbleData(new Requirement(bubble1.Data.branch, bubble1.Data.stageId + 1), out var data))
            {
                var newBubble = SpawnBubble(data, position.xy(), true, (bubble1.RB.GetVelocity()));

                CheckRequirements(newBubble);

                OnBubbleMerged?.Invoke(newBubble);
            }
            else
            {
                var newBubble = SpawnRandomBubble(true);

                if (newBubble != null)
                {
                    CheckRequirements(newBubble);
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