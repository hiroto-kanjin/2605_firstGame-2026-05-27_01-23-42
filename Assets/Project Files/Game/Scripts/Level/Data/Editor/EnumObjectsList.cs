#pragma warning disable 649

using System.Collections.Generic;
using UnityEditor;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using UnityEngine;

namespace Watermelon
{
    public class EnumObjectsList
    {
        //variables for display
        private const float ITEMS_MAX_WIDTH = 600;
        private const int FIRST_COLUMN_WIDTH = 80;
        private const int CUSTOM_LABEL_WIDTH = 45;
        private const string EDIT_ENUM = "Modify";
        private const string SAVE = "Save";
        private const string CANCEL = "Cancel";
        private const string ENUM = " enum: ";
        private const string ENUM_NAME = "Name:";
        private const string ENUM_VALUE = "Value:";
        private const string ADD = "+";
        private const string REMOVE = "X";
        private const string MOVE = "↑↓";
        private const string PREFAB_LABEL = "Prefab:";
        private const string TEXTURE_LABEL = "Editor Texture:";

        private const string DEFAULT_ENUM_NAME = "Item";
        private const string REGEX_PATTERN = "^[A-Z][0-9a-zA-Z_]+$";
        public SerializedProperty array;
        public string enumPropertyPath;
        public string objectReferencePropertyPath;
        private Regex regex;
        public string enumFilePath;
        public Action onBeforeEnumFileUpdate;
        public string enumName;
        private string tabLabel;
        private int acceptablePositionGap;

        private Vector2 scrollView;
        string[] enumDisplayNames;
        private SerializedProperty tempProperty;
        private string tempPropertyLabel;
        private bool editEnamModeEnabled;
        private bool moveEnumModeEnabled;
        private int movableElementIndex;
        private Dictionary<int, string> enumDictionary;
        private List<EditedEnumValue> values;
        private List<string> usedNames;
        private List<int> usedEnumIndexes;
        private List<string> errors;
        private float defaultLabelWidth;
        private GUIContent prefabContent;

        //move enum view 
        private int maxPosition;
        private int currentIndex;

        //texture field
        string texturePropertyPath;
        string textureLabel;
        bool textureFieldEnabled;
        private IEnumerable<SerializedProperty> displayProperties;

        public string TabLabel { get => tabLabel; set => tabLabel = value; }
        public int AcceptablePositionGap { get => acceptablePositionGap; set => acceptablePositionGap = value; }
        public bool TextureFieldEnabled => textureFieldEnabled;

        public bool EditEnamModeEnabled => editEnamModeEnabled;

        public EnumObjectsList(SerializedProperty array, string enumPropertyPath, string objectReferencePropertyPath, string enumFilePath, Action onBeforeEnumFileUpdate, string enumName = DEFAULT_ENUM_NAME, bool modifyEnam = true)
        {
            this.array = array;
            this.enumPropertyPath = enumPropertyPath;
            this.objectReferencePropertyPath = objectReferencePropertyPath;
            this.enumFilePath = enumFilePath;
            this.onBeforeEnumFileUpdate = onBeforeEnumFileUpdate;
            this.enumName = enumName;
            regex = new Regex(REGEX_PATTERN, RegexOptions.Singleline | RegexOptions.Compiled);
            tabLabel = "Spawn items:";
            acceptablePositionGap = 8;
            prefabContent = new GUIContent(PREFAB_LABEL);

            if (modifyEnam)
            {
                SortAndAddMissingItems();
            }
        }

        public void EnableTextureField(string texturePropertyPath, string textureLabel = TEXTURE_LABEL)
        {
            this.textureFieldEnabled = true;
            this.texturePropertyPath = texturePropertyPath;
            this.textureLabel = textureLabel;
        }


        private void SortAndAddMissingItems()
        {
            array.arraySize = Mathf.Max(array.arraySize, 1);
            enumDisplayNames = array.GetArrayElementAtIndex(0).FindPropertyRelative(enumPropertyPath).enumDisplayNames;

            bool found;
            int currentEnumValue;
            int tempEnumValue;
            array.arraySize = Mathf.Max(array.arraySize, enumDisplayNames.Length);

            for (int sortedIndex = 0; sortedIndex < enumDisplayNames.Length; sortedIndex++)
            {
                currentEnumValue = array.GetArrayElementAtIndex(sortedIndex).FindPropertyRelative(enumPropertyPath).enumValueIndex;

                if (currentEnumValue == sortedIndex) //value sorted
                {
                    continue;
                }
                else
                {
                    found = false;

                    for (int unsortedIndex = sortedIndex + 1; unsortedIndex < array.arraySize; unsortedIndex++)
                    {
                        tempEnumValue = array.GetArrayElementAtIndex(unsortedIndex).FindPropertyRelative(enumPropertyPath).enumValueIndex;

                        if (tempEnumValue == sortedIndex)
                        {
                            found = true;
                            array.MoveArrayElement(unsortedIndex, sortedIndex);
                            break;
                        }
                    }

                    if (!found) // insert new element if missing
                    {
                        array.InsertArrayElementAtIndex(sortedIndex);
                        array.GetArrayElementAtIndex(sortedIndex).FindPropertyRelative(enumPropertyPath).enumValueIndex = sortedIndex;
                        array.GetArrayElementAtIndex(sortedIndex).FindPropertyRelative(objectReferencePropertyPath).objectReferenceValue = null;
                    }
                }
            }

            array.arraySize = enumDisplayNames.Length; // remove extra elements
        }

        public void DisplayTab()
        {
            EditorGUILayout.BeginVertical();
            scrollView = EditorGUILayout.BeginScrollView(scrollView);
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(ITEMS_MAX_WIDTH));

            if (editEnamModeEnabled)
            {
                if (moveEnumModeEnabled)
                {
                    MoveEnumView();
                }
                else
                {
                    EditEnumView();
                }
            }
            else
            {
                DefaultView();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public void DisplayEditEnamMode()
        {
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(ITEMS_MAX_WIDTH));

            if (moveEnumModeEnabled)
            {
                MoveEnumView();
            }
            else
            {
                EditEnumView();
            }

            EditorGUILayout.EndVertical();
        }



        private void DefaultView()
        {
            EditorGUILayout.LabelField(tabLabel, EditorCustomStyles.labelLargeBold);

            for (int index = 0; index < array.arraySize; index++)
            {
                tempProperty = array.GetArrayElementAtIndex(index);
                tempPropertyLabel = enumDisplayNames[tempProperty.FindPropertyRelative(enumPropertyPath).enumValueIndex];

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(tempPropertyLabel, EditorCustomStyles.labelMedium);

                EditorGUILayout.PropertyField(tempProperty.FindPropertyRelative(objectReferencePropertyPath), prefabContent);

                if (textureFieldEnabled)
                {
                    EditorGUILayout.PropertyField(tempProperty.FindPropertyRelative(texturePropertyPath), new GUIContent(textureLabel));
                }

                displayProperties = LevelEditorUtils.GetUnmarkedProperties(tempProperty);

                foreach (SerializedProperty item in displayProperties)
                {
                    EditorGUILayout.PropertyField(item);
                }

                EditorGUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(EDIT_ENUM, EditorCustomStyles.button, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                EnableEditMode();
            }

            GUILayout.EndHorizontal();
        }

        public void EnableEditMode()
        {
            editEnamModeEnabled = true;
            values = new List<EditedEnumValue>();
            errors = new List<string>();
            usedEnumIndexes = new List<int>();
            usedNames = new List<string>();
            ParseEnumFile();

            foreach (KeyValuePair<int, string> pair in enumDictionary)
            {
                values.Add(new EditedEnumValue(pair.Value, pair.Key, true));
            }
        }

        private void ParseEnumFile()
        {
            string path = LevelEditorBase.GetProjectPath() + enumFilePath;
            string allText = File.ReadAllText(path);
            int firstIndex = allText.LastIndexOf('{');
            int secondIndex = allText.IndexOf('}');
            allText = allText.Substring(firstIndex + 1, secondIndex - firstIndex - 2);
            string[] parseArray = allText.Split(',');

            enumDictionary = new Dictionary<int, string>();
            int index;
            string leftPart;
            string rightPart;

            for (int i = 0; i < parseArray.Length; i++)
            {
                index = parseArray[i].IndexOf('=');

                if (index != -1) // for last element or direct enum editing
                {
                    leftPart = parseArray[i].Substring(0, index).Trim();
                    rightPart = parseArray[i].Substring(index + 1).Trim();
                    enumDictionary.Add(int.Parse(rightPart), leftPart);
                }
            }
        }

        private void EditEnumView()
        {
            EditorGUILayout.LabelField(enumName + ENUM, EditorCustomStyles.labelLargeBold);
            ClearValidation();

            defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CUSTOM_LABEL_WIDTH;
            //action buttons
            GUILayout.BeginVertical();

            for (int i = 0; i < values.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("#" + (i + 1) + " " + (values[i].old ? "(old)" : "(new)"), GUILayout.MaxWidth(FIRST_COLUMN_WIDTH));
                values[i].name = EditorGUILayout.TextField(ENUM_NAME, values[i].name);
                GUILayout.Space(10);

                if (values[i].old)
                {
                    EditorGUILayout.LabelField(ENUM_VALUE, values[i].enumIndex.ToString());
                }
                else
                {
                    values[i].enumIndex = EditorGUILayout.IntField(ENUM_VALUE, values[i].enumIndex);
                }



                EditorGUI.BeginDisabledGroup(values[i].old);

                if (GUILayout.Button(MOVE, GUILayout.Width(40)))
                {
                    GUIUtility.keyboardControl = int.MinValue;
                    maxPosition = 0;

                    foreach (EditedEnumValue item in values)
                    {
                        maxPosition = Mathf.Max(item.enumIndex, maxPosition);
                    }

                    maxPosition += AcceptablePositionGap;
                    movableElementIndex = i;
                    moveEnumModeEnabled = true;
                }

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button(REMOVE, GUILayout.Width(20)))
                {
                    GUIUtility.keyboardControl = int.MinValue;

                    if (EditorUtility.DisplayDialog("Warning dialog", $"Are you sure that you want to remove element #{(i + 1)}?", "Ok", "Cancel"))
                    {
                        values.RemoveAt(i);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();


            for (int i = 0; i < values.Count; i++)
            {
                ValidataValue(i, values[i]);
            }

            EditorGUIUtility.labelWidth = defaultLabelWidth;

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Sort by value"))
            {
                SortValues();
            }

            GUILayout.FlexibleSpace();


            if (GUILayout.Button(ADD))
            {
                values.Add(new EditedEnumValue(string.Empty, Mathf.Max(usedEnumIndexes.ToArray()) + 1, false));
            }



            GUILayout.EndHorizontal();


            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox(errors[0], MessageType.Error);
            }


            //cancel/save section
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(CANCEL, EditorCustomStyles.buttonGreen, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                editEnamModeEnabled = false;
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(errors.Count > 0);

            if (GUILayout.Button(SAVE, EditorCustomStyles.buttonBlue, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
            {
                UpdateEnumFile();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        private void MoveEnumView()
        {
            currentIndex = 0;

            EditorGUILayout.LabelField(enumName + ENUM, EditorCustomStyles.labelLargeBold);
            ClearValidation();

            defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CUSTOM_LABEL_WIDTH;
            //action buttons
            GUILayout.BeginVertical();

            for (int index = 0; index < maxPosition; index++)
            {
                if ((currentIndex < values.Count) && (values[currentIndex].enumIndex == index))
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("#" + (currentIndex + 1) + " " + (values[currentIndex].old ? "(old)" : "(new)"), GUILayout.MaxWidth(FIRST_COLUMN_WIDTH));
                    EditorGUILayout.LabelField(ENUM_NAME, values[currentIndex].name);
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(ENUM_VALUE, values[currentIndex].enumIndex.ToString());
                    GUILayout.EndHorizontal();
                    currentIndex++;
                }
                else
                {
                    if (GUILayout.Button($"Place here (new value: {index})", EditorCustomStyles.button))
                    {
                        values[movableElementIndex].enumIndex = index;
                        SortValues();
                        moveEnumModeEnabled = false;
                    }
                }
            }

            GUILayout.EndVertical();

            EditorGUIUtility.labelWidth = defaultLabelWidth;
        }

        private void ClearValidation()
        {
            errors.Clear();
            usedNames.Clear();
            usedEnumIndexes.Clear();
        }

        private void ValidataValue(int index, EditedEnumValue editedEnumValue)
        {
            if (editedEnumValue.name.Length == 0)
            {
                errors.Add("Empty \"name\" in element  #" + (index + 1));
            }
            else if (usedNames.Contains(editedEnumValue.name))
            {
                errors.Add("You need to make \"name\" unique in element  #" + (index + 1));
            }
            else if (!regex.IsMatch(editedEnumValue.name))
            {
                errors.Add($"Name in element#{(index + 1)} should start with capital letter and use only latin letters, numbers or underscores");
            }
            else
            {
                usedNames.Add(editedEnumValue.name);
            }

            if (usedEnumIndexes.Contains(editedEnumValue.enumIndex))
            {
                errors.Add("You need to make \"value\" unique in element  #" + (index + 1));
            }
            else
            {
                usedEnumIndexes.Add(editedEnumValue.enumIndex);
            }
        }

        private void SortValues()
        {
            values.Sort((x, y) => x.enumIndex.CompareTo(y.enumIndex));
        }

        private void UpdateEnumFile()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("namespace Watermelon");
            stringBuilder.AppendLine();
            stringBuilder.Append("{");
            stringBuilder.AppendLine();
            stringBuilder.Append("\tpublic enum " + enumName);
            stringBuilder.AppendLine();
            stringBuilder.Append("\t{");
            stringBuilder.AppendLine();

            SortValues(); // To make everything follow the same order

            for (int i = 0; i < values.Count; i++)
            {
                stringBuilder.Append("\t\t");
                stringBuilder.Append(values[i].name);
                stringBuilder.Append(" = ");
                stringBuilder.Append(values[i].enumIndex);
                stringBuilder.Append(",");
                stringBuilder.AppendLine();
            }

            stringBuilder.Append("\t}");
            stringBuilder.AppendLine();
            stringBuilder.Append("}");

            string path = Application.dataPath.Replace("Assets", string.Empty) + enumFilePath;
            onBeforeEnumFileUpdate?.Invoke();
            File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(enumFilePath);
        }

        private class EditedEnumValue
        {
            public string name;
            public int enumIndex;
            public bool old;

            public EditedEnumValue(string name, int enumIndex, bool old)
            {
                this.name = name;
                this.enumIndex = enumIndex;
                this.old = old;
            }
        }
    }
}

