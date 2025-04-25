#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(BlockRulesManager))]
public class BlockRulesManagerEditor : Editor
{
    private Vector2 groupsScrollPosition;
    private Vector2 rulesScrollPosition;
    private string newGroupName = "";
    private int selectedGroupIndex = -1;
    private bool showGlobalRules = true;
    private BuildingBlocksManager buildingBlocksManager;

    private void OnEnable()
    {
        // Find BuildingBlocksManager for block references
        string[] guids = AssetDatabase.FindAssets("t:BuildingBlocksManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildingBlocksManager = AssetDatabase.LoadAssetAtPath<BuildingBlocksManager>(path);
        }
    }

    public override void OnInspectorGUI()
    {
        BlockRulesManager rulesManager = (BlockRulesManager)target;

        EditorGUILayout.LabelField("Block Rules Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Global Rules Section
        DrawGlobalRulesSection(rulesManager);

        // Block Groups Section
        DrawBlockGroupsSection(rulesManager);

        // Selected Group Details
        if (selectedGroupIndex >= 0 && selectedGroupIndex < rulesManager.blockGroups.Count)
        {
            DrawSelectedGroupDetails(rulesManager, rulesManager.blockGroups[selectedGroupIndex]);
        }
    }

    private void DrawGlobalRulesSection(BlockRulesManager rulesManager)
    {
        EditorGUILayout.LabelField("Global Rules", EditorStyles.boldLabel);
        showGlobalRules = EditorGUILayout.Foldout(showGlobalRules, "Global Rules");

        if (showGlobalRules)
        {
            EditorGUILayout.BeginVertical("box");

            // Display global rules
            rulesScrollPosition = EditorGUILayout.BeginScrollView(rulesScrollPosition, GUILayout.Height(150));

            if (rulesManager.globalRules.Count == 0)
            {
                EditorGUILayout.HelpBox("No global rules. Add rules that apply to all blocks.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < rulesManager.globalRules.Count; i++)
                {
                    DrawRuleEntry(rulesManager, rulesManager.globalRules, i);
                }
            }

            EditorGUILayout.EndScrollView();

            // Add global rule button
            if (GUILayout.Button("Add Global Rule"))
            {
                ShowAddRuleMenu((rule) => {
                    rulesManager.AddGlobalRule(rule);
                    EditorUtility.SetDirty(rulesManager);
                });
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawBlockGroupsSection(BlockRulesManager rulesManager)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Block Groups", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        groupsScrollPosition = EditorGUILayout.BeginScrollView(groupsScrollPosition, GUILayout.Height(150));

        // Display all block groups
        for (int i = 0; i < rulesManager.blockGroups.Count; i++)
        {
            BlockRulesManager.BlockGroup group = rulesManager.blockGroups[i];

            EditorGUILayout.BeginHorizontal();
            bool isSelected = (selectedGroupIndex == i);
            bool newSelected = EditorGUILayout.ToggleLeft(
                $"{group.groupName} ({group.blockNames.Count} blocks, {group.rules.Count} rules)",
                isSelected);

            if (newSelected && !isSelected)
            {
                selectedGroupIndex = i;
            }
            else if (!newSelected && isSelected)
            {
                selectedGroupIndex = -1;
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Group",
                    $"Are you sure you want to delete the group '{group.groupName}'?", "Yes", "No"))
                {
                    rulesManager.blockGroups.RemoveAt(i);
                    EditorUtility.SetDirty(rulesManager);

                    if (selectedGroupIndex == i)
                    {
                        selectedGroupIndex = -1;
                    }
                    else if (selectedGroupIndex > i)
                    {
                        selectedGroupIndex--;
                    }

                    GUILayout.EndHorizontal();
                    break;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Add Group section
        EditorGUILayout.BeginHorizontal();
        newGroupName = EditorGUILayout.TextField(newGroupName, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Add Group", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(newGroupName) &&
                !rulesManager.blockGroups.Any(g => g.groupName == newGroupName))
            {
                rulesManager.blockGroups.Add(new BlockRulesManager.BlockGroup { groupName = newGroupName });
                EditorUtility.SetDirty(rulesManager);
                newGroupName = "";
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedGroupDetails(BlockRulesManager rulesManager, BlockRulesManager.BlockGroup group)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Group: {group.groupName}", EditorStyles.boldLabel);

        // Blocks in this group
        EditorGUILayout.LabelField("Blocks in this group:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (group.blockNames.Count == 0)
        {
            EditorGUILayout.HelpBox("No blocks in this group. Add blocks to apply rules to them.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < group.blockNames.Count; i++)
            {
                string blockName = group.blockNames[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(blockName);

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    group.blockNames.RemoveAt(i);
                    EditorUtility.SetDirty(rulesManager);
                    GUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();

        // Add Block button
        if (GUILayout.Button("Add Block to Group"))
        {
            ShowAddBlockMenu(group);
        }

        // Rules for this group
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rules for this group:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (group.rules.Count == 0)
        {
            EditorGUILayout.HelpBox("No rules for this group. Add rules to restrict block placement.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < group.rules.Count; i++)
            {
                DrawRuleEntry(rulesManager, group.rules, i);
            }
        }

        EditorGUILayout.EndVertical();

        // Add Rule button
        if (GUILayout.Button("Add Rule to Group"))
        {
            ShowAddRuleMenu((rule) => {
                group.rules.Add(rule);
                EditorUtility.SetDirty(rulesManager);
            });
        }
    }

    private void DrawRuleEntry(BlockRulesManager rulesManager, List<BlockRule> rulesList, int index)
    {
        BlockRule rule = rulesList[index];

        if (rule != null)
        {
            EditorGUILayout.BeginVertical("box");

            // Rule header with name and type
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(rule.ruleName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(rule.GetType().Name, EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // Rule description
            EditorGUILayout.LabelField(rule.description, EditorStyles.wordWrappedLabel);

            // Rule controls
            EditorGUILayout.BeginHorizontal();
            rule.enabled = EditorGUILayout.Toggle("Enabled", rule.enabled);

            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                Selection.activeObject = rule;
            }

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                rulesList.RemoveAt(index);
                EditorUtility.SetDirty(rulesManager);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Missing Rule");

            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                rulesList.RemoveAt(index);
                EditorUtility.SetDirty(rulesManager);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
    }

    private void ShowAddBlockMenu(BlockRulesManager.BlockGroup group)
    {
        if (buildingBlocksManager != null)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var block in buildingBlocksManager.BuildingBlocks)
            {
                if (!group.blockNames.Contains(block.Name))
                {
                    menu.AddItem(new GUIContent(block.Name), false, () => {
                        group.blockNames.Add(block.Name);
                        EditorUtility.SetDirty(target);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent(block.Name));
                }
            }

            menu.ShowAsContext();
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "BuildingBlocksManager not found. Cannot list available blocks.", "OK");
        }
    }

    private void ShowAddRuleMenu(System.Action<BlockRule> onRuleCreated)
    {
        GenericMenu menu = new GenericMenu();

        // Option to create new rules
        menu.AddItem(new GUIContent("Create New Rule/"), false, null);

        // Get all BlockRule types for creating new rules
        var ruleTypes = from t in System.AppDomain.CurrentDomain.GetAssemblies()
                          .SelectMany(assembly => assembly.GetTypes())
                        where t.IsSubclassOf(typeof(BlockRule)) && !t.IsAbstract
                        select t;

        foreach (var type in ruleTypes)
        {
            menu.AddItem(new GUIContent("Create New Rule/" + type.Name), false, () => {
                BlockRule newRule = CreateRule(type);
                if (newRule != null)
                {
                    onRuleCreated(newRule);
                }
            });
        }

        // Add separator
        menu.AddSeparator("");

        // Add option to select existing rules
        menu.AddItem(new GUIContent("Add Existing Rule/"), false, null);

        // Find all existing rule assets
        string[] guids = AssetDatabase.FindAssets("t:BlockRule");
        bool hasExistingRules = false;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BlockRule rule = AssetDatabase.LoadAssetAtPath<BlockRule>(path);

            if (rule != null)
            {
                hasExistingRules = true;
                menu.AddItem(new GUIContent("Add Existing Rule/" + rule.ruleName + " (" + rule.GetType().Name + ")"),
                    false, () => {
                        onRuleCreated(rule);
                    });
            }
        }

        if (!hasExistingRules)
        {
            menu.AddDisabledItem(new GUIContent("Add Existing Rule/No rules found"));
        }

        menu.ShowAsContext();
    }

    private BlockRule CreateRule(System.Type ruleType)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Rule",
            ruleType.Name,
            "asset",
            "Create a new block placement rule");

        if (string.IsNullOrEmpty(path))
            return null;

        BlockRule rule = CreateInstance(ruleType) as BlockRule;
        AssetDatabase.CreateAsset(rule, path);
        AssetDatabase.SaveAssets();
        return rule;
    }
}
#endif