#pragma warning disable 649

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Watermelon.BubbleMerge
{
    public class EditorSceneController : MonoBehaviour
    {

#if UNITY_EDITOR
        private static EditorSceneController instance;


        public static EditorSceneController Instance { get => instance; }

        [SerializeField] private GameObject container;

        [SerializeField] private GameObject levelShapeContainer;
        [SerializeField] private GameObject levelBackgroundContainer;
        [SerializeField] private Sprite teleportSprite;
        [SerializeField] private Vector3 teleportSpriteScale;
        private bool levelChanged;
        private ItemSave[] itemsCached;

        public GameObject Container { get => container; set => container = value; }

        public EditorSceneController()
        {
            instance = this;
        }

        //used when user spawns objects by clicking on object name in level editor
        public void Spawn(GameObject prefab, Vector3 defaultPosition, Item item)
        {
            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(container.transform);
            gameObject.transform.position = defaultPosition;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.name = prefab.name + " ( el # " + container.transform.childCount + ")";

            SavableItem savableItem = gameObject.AddComponent<SavableItem>();
            savableItem.Item = item;
            HandleTeleportIfNessesary(gameObject, item);

            SelectGameObject(gameObject);
        }

        //used when level loads in level editor
        public void Spawn(ItemSave tempItemSave, GameObject prefab)
        {
            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(container.transform);
            gameObject.transform.position = tempItemSave.Position;
            gameObject.transform.rotation = Quaternion.Euler(tempItemSave.Rotation);
            gameObject.name = prefab.name + "(el # " + container.transform.childCount + ")";
            gameObject.transform.localScale = tempItemSave.Scale;
            SavableItem savableItem = gameObject.AddComponent<SavableItem>();
            savableItem.Item = tempItemSave.Type;
            HandleTeleportIfNessesary(gameObject, tempItemSave.Type);
            SelectGameObject(gameObject);
        }
        // hk追加：配置ボール（お邪魔・進化・特殊）をシーンに配置する
        public void SpawnBallPlacement(GameObject prefab, Vector3 position, BallCategory category, int index)
        {
            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(container.transform);
            gameObject.transform.position = position;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.name = category + "_" + index + " (el # " + container.transform.childCount + ")";

            SavableItemHK savableItemHK = gameObject.AddComponent<SavableItemHK>();
            savableItemHK.Category = BallCategoryToPlacementCategory(category);
            savableItemHK.TypeIndex = index; // hk修正：indexに一本化

            // hk追加：エディター上でボールの見た目のサイズを反映する
            Sprite sprite = GetEditorSpriteForBall(category, index, out float size);
            if (sprite != null)
            {
                GameObject spriteHolder = new GameObject("HK_EditorSprite");
                spriteHolder.transform.SetParent(gameObject.transform);
                spriteHolder.transform.localPosition = Vector3.zero;
                spriteHolder.transform.localRotation = Quaternion.identity;

                float displaySize = size / sprite.pixelsPerUnit * 10f;
                spriteHolder.transform.localScale = Vector3.one * displaySize;

                SpriteRenderer sr = spriteHolder.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 10;
            }

            SelectGameObject(gameObject);
        }

        private Sprite GetEditorSpriteForBall(BallCategory category, int index, out float size)
        {
            size = 1f;

            // hk修正：branch除去に伴い、BallDataからサイズを取る
            BallData ballData = AssetDatabase.LoadAssetAtPath<BallData>("Assets/Project Files/Data/HK/BallData.asset");
            if (ballData == null) return null;

            BallEntry entry = ballData.GetBall(category, index);
            if (entry == null) return null;

            size = entry.size;
            return null; // hk修正：画像はフォルダ方式で別途対応。エディタ表示ではサイズのみ反映
        }

        // hk追加：BallCategoryをPlacementCategoryに変換する
        private PlacementCategory BallCategoryToPlacementCategory(BallCategory category)
        {
            switch (category)
            {
                case BallCategory.Nuisance: return PlacementCategory.Nuisance;
                case BallCategory.Evolution: return PlacementCategory.Evolution;
                case BallCategory.Special: return PlacementCategory.Special;
                default:
                    Debug.LogWarning("BallCategoryToPlacementCategory: 未対応のカテゴリ " + category);
                    return PlacementCategory.Nuisance;
            }
        }

        // hk追加：配置ボールの配置データを取得する
        public BallPlacementHK[] GetBallPlacements()
        {
            SavableItemHK[] savableItems = container.GetComponentsInChildren<SavableItemHK>();
            List<BallPlacementHK> result = new List<BallPlacementHK>();

            for (int i = 0; i < savableItems.Length; i++)
            {
                PlacementCategory pc = savableItems[i].Category;
                if (pc == PlacementCategory.SpecialEffect) continue; // エフェクトは除外

                BallCategory ballCategory;
                switch (pc)
                {
                    case PlacementCategory.Nuisance: ballCategory = BallCategory.Nuisance; break;
                    case PlacementCategory.Evolution: ballCategory = BallCategory.Evolution; break;
                    case PlacementCategory.Special: ballCategory = BallCategory.Special; break;
                    default: continue;
                }

                result.Add(new BallPlacementHK()
                {
                    category = ballCategory,
                    index = savableItems[i].TypeIndex, // hk修正：branchIndex廃止。TypeIndexをindexに一本化
                    position = savableItems[i].transform.position
                });
            }

            return result.ToArray();
        }

        // hk追加：SpecialEffectをシーンに配置する
        public void SpawnSpecialEffect(GameObject prefab, Vector3 defaultPosition, SpecialEffectType effectType)
        {
            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(container.transform);
            gameObject.transform.position = defaultPosition;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.name = "SpecialEffect_" + effectType + " (el # " + container.transform.childCount + ")";

            SavableItemHK savableItemHK = gameObject.AddComponent<SavableItemHK>();
            savableItemHK.Category = PlacementCategory.SpecialEffect;
            savableItemHK.TypeIndex = (int)effectType;

            SelectGameObject(gameObject);
        }

        // hk追加：SpecialEffectの配置データを取得する
        public SpecialEffectSaveHK[] GetSpecialEffectPlacements()
        {
            SavableItemHK[] savableItems = container.GetComponentsInChildren<SavableItemHK>();
            List<SpecialEffectSaveHK> result = new List<SpecialEffectSaveHK>();

            for (int i = 0; i < savableItems.Length; i++)
            {
                if (savableItems[i].Category == PlacementCategory.SpecialEffect)
                {
                    result.Add(new SpecialEffectSaveHK()
                    {
                        type = (SpecialEffectType)savableItems[i].TypeIndex,
                        position = savableItems[i].transform.position
                    });
                }
            }

            return result.ToArray();
        }

        private void HandleTeleportIfNessesary(GameObject gameObject, Item type)
        {
            if (type != Item.Teleport)
            {
                return;
            }

            GameObject spriteHolder = new GameObject("Sprite Holder");
            spriteHolder.transform.SetParent(gameObject.transform);
            spriteHolder.transform.localPosition = Vector3.zero;
            spriteHolder.transform.localRotation = Quaternion.identity;
            spriteHolder.transform.localScale = teleportSpriteScale;
            SpriteRenderer spriteRenderer = spriteHolder.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = teleportSprite;
        }

        public void SpawnLevelShape(GameObject prefab)
        {
            for (int i = levelShapeContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(levelShapeContainer.transform.GetChild(i).gameObject);
            }

            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(levelShapeContainer.transform);
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
        }

        public void SpawnLevelBackground(GameObject prefab)
        {
            for (int i = levelBackgroundContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(levelBackgroundContainer.transform.GetChild(i).gameObject);
            }

            GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            gameObject.transform.SetParent(levelBackgroundContainer.transform);
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
        }

        public void SelectGameObject(GameObject selectedGameObject)
        {
            Selection.activeGameObject = selectedGameObject;
        }

        public void Clear()
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(container.transform.GetChild(i).gameObject);
            }
        }

        public ItemSave[] GetLevelItems()
        {
            SavableItem[] savableItems = container.GetComponentsInChildren<SavableItem>();
            List<ItemSave> result = new List<ItemSave>();

            for (int i = 0; i < savableItems.Length; i++)
            {
                result.Add(HandleParse(savableItems[i]));
            }

            return result.ToArray();
        }

        private ItemSave HandleParse(SavableItem savableItem)
        {
            return new ItemSave(savableItem.Item, savableItem.gameObject.transform.position, savableItem.gameObject.transform.rotation.eulerAngles, savableItem.gameObject.transform.localScale);
        }

        public void RegisterLevelState()
        {
            levelChanged = false;
            itemsCached = GetLevelItems();
        }

        public bool IsLevelChanged()
        {
            if (levelChanged)
            {
                return true;
            }

            if (container == null)
            {
                return false;
            }

            if (itemsCached == null)
            {
                return false;
            }

            if (!itemsCached.SequenceEqual<ItemSave>(GetLevelItems()))
            {
                levelChanged = true;
                return levelChanged;
            }

            return levelChanged;
        }
#endif
    }
}