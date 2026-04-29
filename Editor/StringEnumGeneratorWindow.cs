using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TechCosmos.MSE.Editor
{
    [System.Serializable]
    public class StringEntryData
    {
        public string key;
        public string value;
    }

    public class StringEnumGeneratorWindow : EditorWindow
    {
        private Vector2 leftPanelScroll;
        private Vector2 rightPanelScroll;

        // 生成相关字段
        private string enumFileName = "NewStringEnum";
        private string namespaceName = "GeneratedEnums";
        private List<StringEntryData> entries = new List<StringEntryData>();
        private string newEntryName = "";
        private string newEntryValue = "";

        // 批量编辑模式
        private bool isBatchEditMode = false;
        private Vector2 batchEditScroll;

        // 值跟随键模式
        private bool isValueFollowKey = false;

        // 已生成的枚举文件列表
        private List<GeneratedEnumInfo> generatedEnums = new List<GeneratedEnumInfo>();
        private GeneratedEnumInfo selectedEnum;
        private bool isEditing = false;
        private string editEnumName;
        private string editNamespace;
        private List<StringEntryData> editEntries = new List<StringEntryData>();
        private string editNewEntryName = "";
        private string editNewEntryValue = "";
        private bool isEditBatchMode = false;

        private class GeneratedEnumInfo
        {
            public string filePath;
            public string enumName;
            public string nameSpace;
            public List<StringEntryData> entries = new List<StringEntryData>();
        }

        [MenuItem("Tech-Cosmos/枚举字符串生成器")]
        public static void ShowWindow()
        {
            var window = GetWindow<StringEnumGeneratorWindow>("Magic String Eliminator");
            window.minSize = new Vector2(900, 600);
        }

        private void OnEnable()
        {
            LoadSettings();
            RefreshGeneratedEnums();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnDestroy()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            EditorPrefs.SetBool("MSE_IsBatchEditMode", isBatchEditMode);
            EditorPrefs.SetBool("MSE_IsValueFollowKey", isValueFollowKey);
            EditorPrefs.SetString("MSE_EnumFileName", enumFileName);
            EditorPrefs.SetString("MSE_NamespaceName", namespaceName);
        }

        private void LoadSettings()
        {
            isBatchEditMode = EditorPrefs.GetBool("MSE_IsBatchEditMode", false);
            isValueFollowKey = EditorPrefs.GetBool("MSE_IsValueFollowKey", false);
            enumFileName = EditorPrefs.GetString("MSE_EnumFileName", "NewStringEnum");
            namespaceName = EditorPrefs.GetString("MSE_NamespaceName", "GeneratedEnums");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // 左侧面板 - 生成或编辑工具
            if (isEditing)
            {
                DrawEditPanel();
            }
            else
            {
                DrawLeftPanel();
            }

            // 分割线
            EditorGUILayout.Separator();

            // 右侧面板 - 已生成的枚举
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(450));
            GUILayout.Label("Magic String Eliminator", EditorStyles.boldLabel);
            GUILayout.Label("Generate Type-Safe String Constants", EditorStyles.miniLabel);

            // 文件名设置
            GUILayout.Space(10);
            GUILayout.Label("Enum Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            enumFileName = EditorGUILayout.TextField("Enum Name", enumFileName);
            namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(enumFileName) && !IsValidIdentifier(enumFileName))
                {
                    EditorGUILayout.HelpBox("Invalid enum name! Use only letters, numbers and underscores.", MessageType.Warning);
                }
                SaveSettings();
            }

            EditorGUILayout.Space();

            // 模式切换按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Entry Mode:", GUILayout.Width(80));

            GUI.backgroundColor = !isBatchEditMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Single", GUILayout.Height(25)))
            {
                isBatchEditMode = false;
                SaveSettings();
            }

            GUI.backgroundColor = isBatchEditMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Batch Table", GUILayout.Height(25)))
            {
                isBatchEditMode = true;
                SaveSettings();
            }
            GUI.backgroundColor = Color.white;

            // 值跟随键切换按钮
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = isValueFollowKey ? Color.green : Color.gray;
            if (GUILayout.Button(isValueFollowKey ? "🔗 Key=Value" : "🔓 Key≠Value", GUILayout.Width(100), GUILayout.Height(25)))
            {
                isValueFollowKey = !isValueFollowKey;
                SaveSettings();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (!isBatchEditMode)
            {
                DrawSingleEntryMode();
            }
            else
            {
                DrawBatchEntryMode();
            }

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            // 生成按钮
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Enum", GUILayout.Height(30)))
            {
                ValidateAndGenerate();
            }
            GUI.backgroundColor = Color.white;

            // 清空按钮
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (entries.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Clear all entries?", "Yes", "No"))
                    {
                        entries.Clear();
                    }
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // 快速填充示例
            EditorGUILayout.Space();
            if (GUILayout.Button("Load Example", GUILayout.Height(20)))
            {
                LoadExampleData();
            }

            // 导入导出功能
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export CSV", GUILayout.Height(20)))
            {
                ExportToCSV(entries, enumFileName);
            }
            if (GUILayout.Button("Import CSV", GUILayout.Height(20)))
            {
                ImportFromCSV(entries);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSingleEntryMode()
        {
            GUILayout.Label("Add New Entry", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newEntryName = EditorGUILayout.TextField("Key", newEntryName);

            if (!isValueFollowKey)
            {
                newEntryValue = EditorGUILayout.TextField("Value", newEntryValue);
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(20)))
            {
                string finalValue = isValueFollowKey ? newEntryName : newEntryValue;
                AddNewEntry(newEntryName, finalValue, entries);
                newEntryName = "";
                newEntryValue = "";
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (isValueFollowKey)
            {
                EditorGUILayout.HelpBox("🔗 Key=Value mode: Value will be the same as Key", MessageType.Info);
            }

            EditorGUILayout.Space();

            GUILayout.Label($"Current Entries ({entries.Count})", EditorStyles.boldLabel);
            DrawEntryList(entries, leftPanelScroll, (updatedScroll) => leftPanelScroll = updatedScroll);
        }

        private void DrawBatchEntryMode()
        {
            GUILayout.Label($"Batch Edit Entries ({entries.Count})", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Row", GUILayout.Height(20)))
            {
                string newValue = isValueFollowKey ? "NewKey" : "NewValue";
                entries.Add(new StringEntryData { key = "NewKey", value = newValue });
            }
            if (GUILayout.Button("Add 5 Rows", GUILayout.Height(20)))
            {
                for (int i = 0; i < 5; i++)
                {
                    string key = $"NewKey{entries.Count}";
                    string value = isValueFollowKey ? key : $"NewValue{entries.Count}";
                    entries.Add(new StringEntryData { key = key, value = value });
                }
            }
            if (GUILayout.Button("Remove Last", GUILayout.Height(20)))
            {
                if (entries.Count > 0)
                    entries.RemoveAt(entries.Count - 1);
            }
            if (GUILayout.Button("Remove Empty", GUILayout.Height(20)))
            {
                entries.RemoveAll(e => string.IsNullOrEmpty(e.key) && string.IsNullOrEmpty(e.value));
            }
            EditorGUILayout.EndHorizontal();

            if (isValueFollowKey)
            {
                EditorGUILayout.HelpBox("🔗 Key=Value mode: Values will sync with Keys automatically", MessageType.Info);
            }

            EditorGUILayout.Space();

            DrawTableHeader();

            batchEditScroll = EditorGUILayout.BeginScrollView(batchEditScroll, GUILayout.Height(250));

            int removeIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                DrawTableRow(i, entries, ref removeIndex, ref moveUpIndex, ref moveDownIndex);
            }

            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0)
            {
                entries.RemoveAt(removeIndex);
            }
            if (moveUpIndex > 0)
            {
                var temp = entries[moveUpIndex];
                entries[moveUpIndex] = entries[moveUpIndex - 1];
                entries[moveUpIndex - 1] = temp;
            }
            if (moveDownIndex >= 0 && moveDownIndex < entries.Count - 1)
            {
                var temp = entries[moveDownIndex];
                entries[moveDownIndex] = entries[moveDownIndex + 1];
                entries[moveDownIndex + 1] = temp;
            }
        }

        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", EditorStyles.toolbarButton, GUILayout.Width(30));
            GUILayout.Label("Key", EditorStyles.toolbarButton, GUILayout.Width(150));

            if (!isValueFollowKey)
            {
                GUILayout.Label("Value", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Label("Value (auto)", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }

            GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableRow(int index, List<StringEntryData> list, ref int removeIndex, ref int moveUpIndex, ref int moveDownIndex)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{index + 1}", GUILayout.Width(30));

            EditorGUI.BeginChangeCheck();
            list[index].key = EditorGUILayout.TextField(list[index].key, GUILayout.Width(150));

            if (isValueFollowKey && EditorGUI.EndChangeCheck())
            {
                list[index].value = list[index].key;
            }

            if (!isValueFollowKey)
            {
                list[index].value = EditorGUILayout.TextField(list[index].value, GUILayout.ExpandWidth(true));
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(list[index].value, GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
            }

            GUI.backgroundColor = Color.gray;
            if (GUILayout.Button("↑", GUILayout.Width(20)))
            {
                moveUpIndex = index;
            }

            if (GUILayout.Button("↓", GUILayout.Width(20)))
            {
                moveDownIndex = index;
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("✎", GUILayout.Width(20)))
            {
                EditEntry(list[index]);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✕", GUILayout.Width(20)))
            {
                removeIndex = index;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(450));
            GUILayout.Label("Editing Enum", EditorStyles.boldLabel);
            GUILayout.Label($"File: {selectedEnum?.filePath}", EditorStyles.miniLabel);

            GUILayout.Space(10);
            GUILayout.Label("Enum Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            editEnumName = EditorGUILayout.TextField("Enum Name", editEnumName);
            editNamespace = EditorGUILayout.TextField("Namespace", editNamespace);
            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(editEnumName) && !IsValidIdentifier(editEnumName))
                {
                    EditorGUILayout.HelpBox("Invalid enum name!", MessageType.Warning);
                }
                SaveSettings();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Entry Mode:", GUILayout.Width(80));

            GUI.backgroundColor = !isEditBatchMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Single", GUILayout.Height(25)))
            {
                isEditBatchMode = false;
                SaveSettings();
            }

            GUI.backgroundColor = isEditBatchMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Batch Table", GUILayout.Height(25)))
            {
                isEditBatchMode = true;
                SaveSettings();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            GUI.backgroundColor = isValueFollowKey ? Color.green : Color.gray;
            if (GUILayout.Button(isValueFollowKey ? "🔗 Key=Value" : "🔓 Key≠Value", GUILayout.Width(100), GUILayout.Height(25)))
            {
                isValueFollowKey = !isValueFollowKey;
                SaveSettings();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (!isEditBatchMode)
            {
                GUILayout.Label("Add New Entry", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                editNewEntryName = EditorGUILayout.TextField("Key", editNewEntryName);

                if (!isValueFollowKey)
                {
                    editNewEntryValue = EditorGUILayout.TextField("Value", editNewEntryValue);
                }

                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("+", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    string finalValue = isValueFollowKey ? editNewEntryName : editNewEntryValue;
                    AddNewEntry(editNewEntryName, finalValue, editEntries);
                    editNewEntryName = "";
                    editNewEntryValue = "";
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (isValueFollowKey)
                {
                    EditorGUILayout.HelpBox("🔗 Key=Value mode: Value will be the same as Key", MessageType.Info);
                }

                EditorGUILayout.Space();

                GUILayout.Label($"Entries ({editEntries.Count})", EditorStyles.boldLabel);
                DrawEntryList(editEntries, leftPanelScroll, (updatedScroll) => leftPanelScroll = updatedScroll, true);
            }
            else
            {
                DrawEditBatchMode();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(editEnumName))
                {
                    EditorUtility.DisplayDialog("Error", "Enum name cannot be empty!", "OK");
                }
                else if (!IsValidIdentifier(editEnumName))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid enum name!", "OK");
                }
                else if (editEntries.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Need at least one entry!", "OK");
                }
                else
                {
                    OverrideEnumFile();
                }
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                CancelEditing();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Editing will override the existing file. Consider making a backup first.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawEditBatchMode()
        {
            GUILayout.Label($"Batch Edit Entries ({editEntries.Count})", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Row", GUILayout.Height(20)))
            {
                string newValue = isValueFollowKey ? "NewKey" : "NewValue";
                editEntries.Add(new StringEntryData { key = "NewKey", value = newValue });
            }
            if (GUILayout.Button("Add 5 Rows", GUILayout.Height(20)))
            {
                for (int i = 0; i < 5; i++)
                {
                    string key = $"NewKey{editEntries.Count}";
                    string value = isValueFollowKey ? key : $"NewValue{editEntries.Count}";
                    editEntries.Add(new StringEntryData { key = key, value = value });
                }
            }
            if (GUILayout.Button("Remove Last", GUILayout.Height(20)))
            {
                if (editEntries.Count > 0)
                    editEntries.RemoveAt(editEntries.Count - 1);
            }
            if (GUILayout.Button("Remove Empty", GUILayout.Height(20)))
            {
                editEntries.RemoveAll(e => string.IsNullOrEmpty(e.key) && string.IsNullOrEmpty(e.value));
            }
            EditorGUILayout.EndHorizontal();

            if (isValueFollowKey)
            {
                EditorGUILayout.HelpBox("🔗 Key=Value mode: Values will sync with Keys automatically", MessageType.Info);
            }

            EditorGUILayout.Space();

            DrawTableHeader();

            batchEditScroll = EditorGUILayout.BeginScrollView(batchEditScroll, GUILayout.Height(250));

            int removeIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;

            for (int i = 0; i < editEntries.Count; i++)
            {
                DrawTableRow(i, editEntries, ref removeIndex, ref moveUpIndex, ref moveDownIndex);
            }

            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0)
            {
                editEntries.RemoveAt(removeIndex);
            }
            if (moveUpIndex > 0)
            {
                var temp = editEntries[moveUpIndex];
                editEntries[moveUpIndex] = editEntries[moveUpIndex - 1];
                editEntries[moveUpIndex - 1] = temp;
            }
            if (moveDownIndex >= 0 && moveDownIndex < editEntries.Count - 1)
            {
                var temp = editEntries[moveDownIndex];
                editEntries[moveDownIndex] = editEntries[moveDownIndex + 1];
                editEntries[moveDownIndex + 1] = temp;
            }
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Generated String Enums", EditorStyles.boldLabel);
            if (GUILayout.Button("↻ Refresh", GUILayout.Width(80)))
            {
                RefreshGeneratedEnums();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

            if (generatedEnums.Count == 0)
            {
                EditorGUILayout.HelpBox("No string enums generated yet. Create one in the left panel!", MessageType.Info);
            }
            else
            {
                var groupedEnums = generatedEnums
                    .GroupBy(e => string.IsNullOrEmpty(e.nameSpace) ? "(No Namespace)" : e.nameSpace)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedEnums)
                {
                    DrawNamespaceGroup(group.Key, group.ToList());
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Total: {generatedEnums.Count} enums", EditorStyles.miniLabel);

            var namespaceCount = generatedEnums
                .Select(e => string.IsNullOrEmpty(e.nameSpace) ? "(No Namespace)" : e.nameSpace)
                .Distinct()
                .Count();
            GUILayout.Label($"Namespaces: {namespaceCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawNamespaceGroup(string namespaceName, List<GeneratedEnumInfo> enumsInNamespace)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.3f);

            string foldoutKey = $"NamespaceFoldout_{namespaceName}";
            bool isExpanded = EditorPrefs.GetBool(foldoutKey, true);

            EditorGUI.BeginChangeCheck();
            isExpanded = EditorGUILayout.Foldout(isExpanded, "", true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(foldoutKey, isExpanded);
            }

            GUIStyle namespaceStyle = new GUIStyle(EditorStyles.boldLabel);
            namespaceStyle.normal.textColor = new Color(0.2f, 0.5f, 0.8f);
            GUILayout.Label($"📁 {namespaceName}", namespaceStyle);

            GUILayout.FlexibleSpace();

            GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel);
            countStyle.normal.textColor = Color.gray;
            GUILayout.Label($"{enumsInNamespace.Count} enum{(enumsInNamespace.Count > 1 ? "s" : "")}", countStyle);

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                foreach (var enumInfo in enumsInNamespace.OrderBy(e => e.enumName))
                {
                    DrawEnumListItem(enumInfo);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawEnumListItem(GeneratedEnumInfo enumInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool isCurrentlyEditing = isEditing && selectedEnum == enumInfo;

            EditorGUILayout.BeginHorizontal();

            string icon = isCurrentlyEditing ? "✏️" : "🔤";

            GUI.backgroundColor = isCurrentlyEditing ? Color.green : Color.white;
            if (GUILayout.Button($"{icon} {enumInfo.enumName}", EditorStyles.boldLabel))
            {
                if (isCurrentlyEditing)
                {
                    CancelEditing();
                }
                else
                {
                    selectedEnum = enumInfo;
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(enumInfo.filePath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }

            if (!isCurrentlyEditing && GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                StartEditing(enumInfo);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Enum",
                    $"Delete '{enumInfo.enumName}'?\n\nThis action cannot be undone!",
                    "Delete", "Cancel"))
                {
                    DeleteEnumFile(enumInfo);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (selectedEnum == enumInfo && !isCurrentlyEditing)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Namespace:", GUILayout.Width(80));
                EditorGUILayout.LabelField(string.IsNullOrEmpty(enumInfo.nameSpace) ? "(No Namespace)" : enumInfo.nameSpace);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("File:", GUILayout.Width(40));
                EditorGUILayout.SelectableLabel(enumInfo.filePath, EditorStyles.textField, GUILayout.Height(16));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Entries: {enumInfo.entries.Count}");

                if (enumInfo.entries.Count > 0)
                {
                    EditorGUILayout.LabelField("Values:", EditorStyles.miniLabel);

                    int displayCount = Mathf.Min(enumInfo.entries.Count, 10);
                    for (int i = 0; i < displayCount; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  • {enumInfo.entries[i].key}", GUILayout.Width(150));

                        if (enumInfo.entries[i].key == enumInfo.entries[i].value)
                        {
                            GUIStyle grayStyle = new GUIStyle(EditorStyles.label);
                            grayStyle.normal.textColor = Color.gray;
                            EditorGUILayout.LabelField($"= \"{enumInfo.entries[i].value}\"", grayStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"= \"{enumInfo.entries[i].value}\"");
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (enumInfo.entries.Count > 10)
                    {
                        EditorGUILayout.LabelField($"  ... and {enumInfo.entries.Count - 10} more", EditorStyles.miniLabel);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawEntryList(List<StringEntryData> entryList, Vector2 scrollPos, Action<Vector2> setScrollPos, bool isEditMode = false)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

            for (int i = entryList.Count - 1; i >= 0; i--)
            {
                int currentIndex = i;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"{entryList[i].key}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"\"{entryList[i].value}\"", GUILayout.MinWidth(100));

                GUILayout.FlexibleSpace();

                if (isEditMode)
                {
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        if (currentIndex < entryList.Count - 1)
                        {
                            var temp = entryList[currentIndex];
                            entryList[currentIndex] = entryList[currentIndex + 1];
                            entryList[currentIndex + 1] = temp;
                        }
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        if (currentIndex > 0)
                        {
                            var temp = entryList[currentIndex];
                            entryList[currentIndex] = entryList[currentIndex - 1];
                            entryList[currentIndex - 1] = temp;
                        }
                    }

                    if (GUILayout.Button("✎", GUILayout.Width(25)))
                    {
                        EditEntry(entryList[currentIndex]);
                    }
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    entryList.RemoveAt(currentIndex);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            setScrollPos?.Invoke(scrollPos);
        }

        private void AddNewEntry(string key, string value, List<StringEntryData> targetList)
        {
            if (string.IsNullOrEmpty(key))
            {
                EditorUtility.DisplayDialog("Error", "Key cannot be empty!", "OK");
                return;
            }

            if (!IsValidIdentifier(key))
            {
                EditorUtility.DisplayDialog("Error", "Invalid key name! Use only letters, numbers and underscores.", "OK");
                return;
            }

            if (targetList.Any(e => e.key == key))
            {
                EditorUtility.DisplayDialog("Error", $"Key '{key}' already exists!", "OK");
                return;
            }

            targetList.Add(new StringEntryData
            {
                key = key,
                value = string.IsNullOrEmpty(value) ? key : value
            });
        }

        private void EditEntry(StringEntryData entry)
        {
            var editWindow = GetWindow<EditEntryWindow>(true, "Edit Entry", true);
            editWindow.Initialize(entry);
            editWindow.Show();
        }

        private void StartEditing(GeneratedEnumInfo enumInfo)
        {
            isEditing = true;
            selectedEnum = enumInfo;

            editEnumName = enumInfo.enumName;
            editNamespace = enumInfo.nameSpace;
            editEntries = enumInfo.entries.Select(e => new StringEntryData { key = e.key, value = e.value }).ToList();
        }

        private void CancelEditing()
        {
            isEditing = false;
            selectedEnum = null;
            editEntries.Clear();
        }

        private void ValidateAndGenerate()
        {
            if (string.IsNullOrEmpty(enumFileName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter an enum name!", "OK");
                return;
            }

            if (!IsValidIdentifier(enumFileName))
            {
                EditorUtility.DisplayDialog("Error", "Invalid enum name!", "OK");
                return;
            }

            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please add at least one entry!", "OK");
                return;
            }

            var duplicateKeys = entries.GroupBy(e => e.key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateKeys.Count > 0)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Duplicate keys found: {string.Join(", ", duplicateKeys)}", "OK");
                return;
            }

            var invalidKeys = entries.Where(e => !IsValidIdentifier(e.key)).Select(e => e.key).ToList();
            if (invalidKeys.Count > 0)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Invalid keys found: {string.Join(", ", invalidKeys)}", "OK");
                return;
            }

            if (IsEnumNameExists(enumFileName))
            {
                if (EditorUtility.DisplayDialog("Override?",
                    $"Enum '{enumFileName}' already exists. Do you want to override it?", "Yes", "No"))
                {
                    GenerateEnumFile();
                }
            }
            else
            {
                GenerateEnumFile();
            }
        }

        private void OverrideEnumFile()
        {
            if (selectedEnum == null) return;

            var tempEntries = entries;
            var tempEnumName = enumFileName;
            var tempNamespace = namespaceName;

            entries = editEntries;
            enumFileName = editEnumName;
            namespaceName = editNamespace;

            GenerateEnumFile();

            entries = tempEntries;
            enumFileName = tempEnumName;
            namespaceName = tempNamespace;

            CancelEditing();
            RefreshGeneratedEnums();

            EditorUtility.DisplayDialog("Success", $"Enum '{editEnumName}' has been overridden!", "OK");
        }

        private void GenerateEnumFile()
        {
            string codeContent = GenerateEnumCode();

            string directory = "Assets/Generated/Enums/";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, $"{enumFileName}.cs");

            File.WriteAllText(filePath, codeContent);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"String Enum '{enumFileName}' generated!\nPath: {filePath}\nEntries: {entries.Count}", "OK");

            RefreshGeneratedEnums();

            entries.Clear();
            enumFileName = "NewStringEnum";
        }

        private string GenerateEnumCode()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("// This file is auto-generated by Magic String Eliminator. Do not modify manually.");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    public enum {enumFileName}");
            sb.AppendLine("    {");

            for (int i = 0; i < entries.Count; i++)
            {
                string line = $"        {entries[i].key}";
                if (i < entries.Count - 1)
                    line += ",";
                sb.AppendLine(line);
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine($"    public static class {enumFileName}Extensions");
            sb.AppendLine("    {");
            sb.AppendLine($"        private static readonly Dictionary<int, string> _stringMap =");
            sb.AppendLine($"            new Dictionary<int, string>");
            sb.AppendLine("            {");

            for (int i = 0; i < entries.Count; i++)
            {
                string comma = i < entries.Count - 1 ? "," : "";
                sb.AppendLine($"                {{ (int){enumFileName}.{entries[i].key}, \"{entries[i].value}\" }}{comma}");
            }

            sb.AppendLine("            };");
            sb.AppendLine();

            sb.AppendLine($"        public static string GetString(this {enumFileName} value)");
            sb.AppendLine("        {");
            sb.AppendLine("            return _stringMap.TryGetValue((int)value, out var result) ? result : value.ToString();");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine($"        public static {enumFileName} FromString(string value)");
            sb.AppendLine("        {");
            sb.AppendLine("            foreach (var pair in _stringMap)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (pair.Value == value)");
            sb.AppendLine($"                    return ({enumFileName})pair.Key;");
            sb.AppendLine("            }");
            sb.AppendLine($"            throw new System.ArgumentException($\"No {enumFileName} with string value '{{value}}'\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private void RefreshGeneratedEnums()
        {
            generatedEnums.Clear();

            string directory = "Assets/Generated/Enums/";
            if (!Directory.Exists(directory))
                return;

            string[] files = Directory.GetFiles(directory, "*.cs");

            foreach (string file in files)
            {
                try
                {
                    var enumInfo = ParseEnumFile(file);
                    if (enumInfo != null)
                    {
                        generatedEnums.Add(enumInfo);
                    }
                }
                catch
                {
                    // 忽略解析错误
                }
            }

            generatedEnums = generatedEnums
                .OrderBy(e => string.IsNullOrEmpty(e.nameSpace) ? "(No Namespace)" : e.nameSpace)
                .ThenBy(e => e.enumName)
                .ToList();
        }

        private GeneratedEnumInfo ParseEnumFile(string filePath)
        {
            string content = File.ReadAllText(filePath);
            var info = new GeneratedEnumInfo { filePath = filePath };

            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("namespace "))
                {
                    info.nameSpace = trimmedLine.Replace("namespace ", "").TrimEnd('{').Trim();
                }

                if (trimmedLine.StartsWith("public enum "))
                {
                    info.enumName = trimmedLine.Replace("public enum ", "").TrimEnd('{').Trim();
                }
            }

            var dictMatches = System.Text.RegularExpressions.Regex.Matches(content,
                @"\(\s*int\s*\)\s*\w+\.(\w+)\s*,\s*""([^""]*)""");

            foreach (System.Text.RegularExpressions.Match match in dictMatches)
            {
                if (match.Groups.Count >= 3)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    info.entries.Add(new StringEntryData { key = key, value = value });
                }
            }

            return info;
        }

        private void DeleteEnumFile(GeneratedEnumInfo enumInfo)
        {
            if (File.Exists(enumInfo.filePath))
            {
                File.Delete(enumInfo.filePath);
                AssetDatabase.Refresh();
                generatedEnums.Remove(enumInfo);
                if (selectedEnum == enumInfo)
                {
                    selectedEnum = null;
                    CancelEditing();
                }
            }
        }

        private bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
            }

            return true;
        }

        private bool IsEnumNameExists(string enumName)
        {
            return generatedEnums.Any(e => e.enumName.Equals(enumName, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadExampleData()
        {
            enumFileName = "GameTags";
            namespaceName = "GeneratedEnums";

            entries.Clear();
            entries.Add(new StringEntryData { key = "Player", value = "Player" });
            entries.Add(new StringEntryData { key = "Enemy", value = "Enemy" });
            entries.Add(new StringEntryData { key = "Collectible", value = "Collectible" });
            entries.Add(new StringEntryData { key = "Obstacle", value = "Obstacle" });
            entries.Add(new StringEntryData { key = "Checkpoint", value = "Checkpoint" });
        }

        private void ExportToCSV(List<StringEntryData> exportEntries, string fileName)
        {
            if (exportEntries.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No entries to export!", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export to CSV", "", fileName + ".csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("Key,Value");
                    foreach (var entry in exportEntries)
                    {
                        sw.WriteLine($"\"{entry.key}\",\"{entry.value}\"");
                    }
                }
                EditorUtility.DisplayDialog("Success", $"Exported {exportEntries.Count} entries to CSV!", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to export: {e.Message}", "OK");
            }
        }

        private void ImportFromCSV(List<StringEntryData> targetList)
        {
            string path = EditorUtility.OpenFilePanel("Import from CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                List<StringEntryData> importedEntries = new List<StringEntryData>();
                using (StreamReader sr = new StreamReader(path))
                {
                    string header = sr.ReadLine();
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            string key = parts[0].Trim('"', ' ');
                            string value = parts[1].Trim('"', ' ');
                            importedEntries.Add(new StringEntryData { key = key, value = value });
                        }
                    }
                }

                if (importedEntries.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("Import",
                        $"Found {importedEntries.Count} entries. Replace current entries?", "Replace", "Append"))
                    {
                        targetList.Clear();
                        targetList.AddRange(importedEntries);
                    }
                    else
                    {
                        targetList.AddRange(importedEntries);
                    }
                    EditorUtility.DisplayDialog("Success", $"Imported {importedEntries.Count} entries!", "OK");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import: {e.Message}", "OK");
            }
        }
    }

    public class EditEntryWindow : EditorWindow
    {
        private StringEntryData entry;
        private string key;
        private string value;

        public void Initialize(StringEntryData entryToEdit)
        {
            entry = entryToEdit;
            key = entry.key;
            value = entry.value;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Edit Entry", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            key = EditorGUILayout.TextField("Key", key);
            value = EditorGUILayout.TextField("Value", value);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
            {
                if (string.IsNullOrEmpty(key))
                {
                    EditorUtility.DisplayDialog("Error", "Key cannot be empty!", "OK");
                    return;
                }

                entry.key = key;
                entry.value = value;
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
