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

        private void HandleTeleportIfNessesary(GameObject gameObject, Item type)
        {
            if(type != Item.Teleport)
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
            if(container == null)
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

            if(container == null)
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
