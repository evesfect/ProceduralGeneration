#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor for the Rule Manager.
/// </summary>
[CustomEditor(typeof(RuleManager))]
public class RuleManagerEditor : Editor
{
    private RuleManager ruleManager;
    private SerializedProperty globalRulesProp;
    private SerializedProperty blockRulesProp;
    private SerializedProperty debugLoggingProp;

    private bool showGlobalRules = true;
    private bool showBlockRules = true;

    private void OnEnable()
    {
        ruleManager = (RuleManager)target;
        globalRulesProp = serializedObject.FindProperty("globalRules");
        blockRulesProp = serializedObject.FindProperty("blockRules");
        debugLoggingProp = serializedObject.FindProperty("enableDebugLogging");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Building Block Rules Manager", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Configure placement rules for procedural building generation", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        // Debug settings
        EditorGUILayout.PropertyField(debugLoggingProp);
        EditorGUILayout.Space(10);

        // Global Rules section
        showGlobalRules = EditorGUILayout.Foldout(showGlobalRules, "Global Rules", true);
        if (showGlobalRules)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(globalRulesProp, new GUIContent("Rules"), true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Global Rule", GUILayout.Width(150)))
            {
                ShowAddRuleMenu(true);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Block-specific Rules section
        showBlockRules = EditorGUILayout.Foldout(showBlockRules, "Block-Specific Rules", true);
        if (showBlockRules)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(blockRulesProp, new GUIContent("Block Rules"), true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Block Rule", GUILayout.Width(150)))
            {
                ShowAddBlockRuleMenu();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowAddRuleMenu(bool isGlobal)
    {
        // Create and show a dropdown menu of rule types
        GenericMenu menu = new GenericMenu();

        // Add height rule option
        menu.AddItem(new GUIContent("Height Rule"), false, () => {
            CreateAndAddRule<HeightPlacementRule>(isGlobal);
        });

        // Add more rule types here as they are implemented

        menu.ShowAsContext();
    }

    private void ShowAddBlockRuleMenu()
    {
        // Show a dialog to select a block first, then a rule type
        // This would need to find all block names from BuildingBlocksManager

        // For simplicity in this implementation, we'll just add a blank entry
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add New Block Entry"), false, () => {
            SerializedProperty blockRulesArray = blockRulesProp;
            blockRulesArray.arraySize++;
            SerializedProperty newElement = blockRulesArray.GetArrayElementAtIndex(blockRulesArray.arraySize - 1);
            SerializedProperty blockNameProp = newElement.FindPropertyRelative("blockName");
            blockNameProp.stringValue = "New Block";
            serializedObject.ApplyModifiedProperties();
        });

        menu.ShowAsContext();
    }

    private void CreateAndAddRule<T>(bool isGlobal) where T : BlockPlacementRule
    {
        // Create the rule asset
        T rule = ScriptableObject.CreateInstance<T>();
        rule.ruleName = typeof(T).Name;

        // Save the asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Rule Asset",
            typeof(T).Name,
            "asset",
            "Save the rule as an asset"
        );

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(rule, path);
        AssetDatabase.SaveAssets();

        // Add to the appropriate list
        if (isGlobal)
        {
            SerializedProperty globalRulesArray = globalRulesProp;
            globalRulesArray.arraySize++;
            globalRulesArray.GetArrayElementAtIndex(globalRulesArray.arraySize - 1).objectReferenceValue = rule;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif