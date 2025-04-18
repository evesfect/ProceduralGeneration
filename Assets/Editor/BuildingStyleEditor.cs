#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(BuildingStyle))]
public class BuildingStyleEditor : Editor
{
    private BuildingStyle buildingStyle;
    private SerializedObject serializedBuildingStyle;
    private SerializedProperty blockWeightsProp;
    private SerializedProperty styleNameProp;

    // Foldout states
    private Dictionary<string, bool> blockFoldouts = new Dictionary<string, bool>();

    private void OnEnable()
    {
        buildingStyle = (BuildingStyle)target;
        serializedBuildingStyle = new SerializedObject(target);
        blockWeightsProp = serializedBuildingStyle.FindProperty("blockWeights");
        styleNameProp = serializedBuildingStyle.FindProperty("styleName");
    }

    public override void OnInspectorGUI()
    {
        serializedBuildingStyle.Update();

        // Style name
        EditorGUILayout.PropertyField(styleNameProp);

        // Block weights
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Block Weights", EditorStyles.boldLabel);

        if (blockWeightsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No building blocks defined. Create blocks in the Building Blocks Manager first.", MessageType.Info);
        }

        for (int i = 0; i < blockWeightsProp.arraySize; i++)
        {
            SerializedProperty blockProp = blockWeightsProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = blockProp.FindPropertyRelative("blockName");
            SerializedProperty weightProp = blockProp.FindPropertyRelative("weight");
            SerializedProperty enableHeightProp = blockProp.FindPropertyRelative("enableHeightFactor");
            SerializedProperty heightCurveProp = blockProp.FindPropertyRelative("heightCurve");
            SerializedProperty enableDistanceProp = blockProp.FindPropertyRelative("enableDistanceFactor");
            SerializedProperty distanceCurveProp = blockProp.FindPropertyRelative("distanceCurve");

            // Ensure the block has an entry in the foldout dictionary
            string blockName = nameProp.stringValue;
            if (!blockFoldouts.ContainsKey(blockName))
            {
                blockFoldouts[blockName] = false;
            }

            // Block foldout header with base weight
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            blockFoldouts[blockName] = EditorGUILayout.Foldout(blockFoldouts[blockName], blockName, true);

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Base Weight:", GUILayout.Width(80));
            weightProp.floatValue = EditorGUILayout.FloatField(weightProp.floatValue, GUILayout.Width(50));

            // Ensure base weight is positive
            if (weightProp.floatValue < 0)
                weightProp.floatValue = 0;

            EditorGUILayout.EndHorizontal();

            // Show curve editors if expanded
            if (blockFoldouts[blockName])
            {
                EditorGUILayout.Space(5);

                // Height factor
                EditorGUILayout.PropertyField(enableHeightProp, new GUIContent("Use Height Factor"));
                if (enableHeightProp.boolValue)
                {
                    EditorGUILayout.LabelField("Height Curve (X: 0-1 = bottom to top, Y: 0-1 = weight multiplier)");

                    // Draw a square curve editor with 1:1 aspect ratio
                    float width = EditorGUIUtility.currentViewWidth - 40;
                    float height = width;

                    // Cap the height to avoid excessively tall editors
                    height = Mathf.Min(height, 200);

                    Rect curveRect = EditorGUILayout.GetControlRect(false, height);

                    // Draw the curve editor
                    heightCurveProp.animationCurveValue = EditorGUI.CurveField(curveRect, heightCurveProp.animationCurveValue);

                    // Ensure curve is normalized to 0-1 on both axes
                    NormalizeCurve(heightCurveProp);
                }

                EditorGUILayout.Space(10);

                // Distance factor
                EditorGUILayout.PropertyField(enableDistanceProp, new GUIContent("Use Distance Factor"));
                if (enableDistanceProp.boolValue)
                {
                    EditorGUILayout.LabelField("Distance Curve (X: 0-1 = center to edge, Y: 0-1 = weight multiplier)");

                    // Draw a square curve editor with 1:1 aspect ratio
                    float width = EditorGUIUtility.currentViewWidth - 40;
                    float height = width;

                    // Cap the height to avoid excessively tall editors
                    height = Mathf.Min(height, 200);

                    Rect curveRect = EditorGUILayout.GetControlRect(false, height);

                    // Draw the curve editor
                    distanceCurveProp.animationCurveValue = EditorGUI.CurveField(curveRect, distanceCurveProp.animationCurveValue);

                    // Ensure curve is normalized to 0-1 on both axes
                    NormalizeCurve(distanceCurveProp);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        serializedBuildingStyle.ApplyModifiedProperties();
    }

    // Helper to ensure curve always has 0-1 on x and y axes
    private void NormalizeCurve(SerializedProperty curveProp)
    {
        AnimationCurve curve = curveProp.animationCurveValue;

        // Need at least one key
        if (curve.length == 0)
        {
            curve = AnimationCurve.Linear(0, 1, 1, 1);
            curveProp.animationCurveValue = curve;
            return;
        }

        bool changed = false;
        Keyframe[] keys = curve.keys;

        // Check each key and adjust if needed
        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe key = keys[i];

            // Clamp X to 0-1 range
            if (key.time < 0 || key.time > 1)
            {
                key.time = Mathf.Clamp01(key.time);
                changed = true;
            }

            // Clamp Y to 0-1 range
            if (key.value < 0 || key.value > 1)
            {
                key.value = Mathf.Clamp01(key.value);
                changed = true;
            }

            keys[i] = key;
        }

        // If changes were made, update the curve
        if (changed)
        {
            curve = new AnimationCurve(keys);
            curveProp.animationCurveValue = curve;
        }
    }
}
#endif