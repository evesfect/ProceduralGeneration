using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
// Editor window to control the preview
public class BuildingBlockPreviewWindow : EditorWindow
{
    // Constants for EditorPrefs keys
    private const string PREVIEW_ENABLED_KEY = "BuildingBlockPreview_Enabled";
    private const string SELECTED_BLOCK_KEY = "BuildingBlockPreview_SelectedBlock";

    private BuildingBlocksManager buildingBlocksManager;
    private int selectedBlockIndex = 0;
    private bool previewEnabled = false;

    // Preview objects
    private GameObject previewInstance;
    private BuildingBlock selectedBlock;

    // Direction arrow settings
    private float arrowLength = 3.0f;
    private float arrowThickness = 0.3f;

    // Direction colors
    private static readonly Dictionary<Direction, Color> directionColors = new Dictionary<Direction, Color>()
    {
        { Direction.Up, Color.green },
        { Direction.Down, new Color(0.0f, 0.5f, 0.0f) }, // Dark green
        { Direction.Front, Color.blue },
        { Direction.Back, new Color(0.0f, 0.0f, 0.5f) }, // Dark blue
        { Direction.Left, Color.red },
        { Direction.Right, new Color(0.5f, 0.0f, 0.0f) }  // Dark red
    };

    // Direction vectors (in world space)
    private static readonly Dictionary<Direction, Vector3> directionVectors = new Dictionary<Direction, Vector3>()
    {
        { Direction.Up, Vector3.up },
        { Direction.Down, Vector3.down },
        { Direction.Front, Vector3.forward },
        { Direction.Back, Vector3.back },
        { Direction.Left, Vector3.left },
        { Direction.Right, Vector3.right }
    };

    // Get adjusted direction vectors based on the building block's rotation
    private Dictionary<Direction, Vector3> GetAdjustedDirectionVectors(Direction downDirection)
    {
        Dictionary<Direction, Vector3> adjustedVectors = new Dictionary<Direction, Vector3>();

        // Start with identity rotation
        Quaternion rotation = Quaternion.identity;

        // Apply the rotation to each direction vector
        foreach (var kvp in directionVectors)
        {
            adjustedVectors[kvp.Key] = rotation * kvp.Value;
        }

        return adjustedVectors;
    }

    [MenuItem("Tools/ProceduralBuildings/Building Block Preview")]
    public static void ShowWindow()
    {
        GetWindow<BuildingBlockPreviewWindow>("Building Block Preview");
    }

    private void OnEnable()
    {
        // Try to find existing building blocks manager
        string[] guids = AssetDatabase.FindAssets("t:BuildingBlocksManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildingBlocksManager = AssetDatabase.LoadAssetAtPath<BuildingBlocksManager>(path);
        }

        // Load saved preview state
        previewEnabled = EditorPrefs.GetBool(PREVIEW_ENABLED_KEY, false);

        // Find the index of the saved block name
        string savedBlockName = EditorPrefs.GetString(SELECTED_BLOCK_KEY, "");
        if (buildingBlocksManager != null && !string.IsNullOrEmpty(savedBlockName))
        {
            for (int i = 0; i < buildingBlocksManager.BuildingBlocks.Count; i++)
            {
                if (buildingBlocksManager.BuildingBlocks[i].Name == savedBlockName)
                {
                    selectedBlockIndex = i;
                    break;
                }
            }
        }

        // Register for scene view updates
        SceneView.duringSceneGui += OnSceneGUI;

        // If preview is enabled, create it
        if (previewEnabled)
        {
            UpdatePreview();
        }
    }

    private void OnDisable()
    {
        // Unregister from scene view updates
        SceneView.duringSceneGui -= OnSceneGUI;

        // Clean up preview
        DestroyPreview();
    }

    private void OnGUI()
    {
        if (buildingBlocksManager == null)
        {
            EditorGUILayout.HelpBox("Building Blocks Manager asset not found! Please create it first.", MessageType.Error);
            return;
        }

        // Header
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preview Directions of a Building Block", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Building block selection
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Select Building Block to Preview:", EditorStyles.boldLabel);

        if (buildingBlocksManager.BuildingBlocks.Count == 0)
        {
            EditorGUILayout.HelpBox("No building blocks created yet. Create some building blocks first.", MessageType.Warning);
            GUI.enabled = false;
        }
        else
        {
            // Create the dropdown options
            string[] blockNames = buildingBlocksManager.BuildingBlocks.Select(bb => bb.Name).ToArray();
            int newSelectedIndex = EditorGUILayout.Popup("Building Block:", selectedBlockIndex, blockNames);

            // If selection changed
            if (newSelectedIndex != selectedBlockIndex)
            {
                selectedBlockIndex = newSelectedIndex;

                // Save the selected block name
                if (selectedBlockIndex >= 0 && selectedBlockIndex < blockNames.Length)
                {
                    EditorPrefs.SetString(SELECTED_BLOCK_KEY, blockNames[selectedBlockIndex]);

                    // Update preview if enabled
                    if (previewEnabled)
                    {
                        UpdatePreview();
                    }
                }
            }
        }

        // Enable preview toggle
        EditorGUILayout.Space(5);
        bool newPreviewEnabled = EditorGUILayout.Toggle("Enable Preview", previewEnabled);

        // If changed, save the new state
        if (newPreviewEnabled != previewEnabled)
        {
            previewEnabled = newPreviewEnabled;
            EditorPrefs.SetBool(PREVIEW_ENABLED_KEY, previewEnabled);

            if (previewEnabled)
            {
                UpdatePreview();
            }
            else
            {
                DestroyPreview();
            }

            // Force scene view to update
            SceneView.RepaintAll();
        }

        GUI.enabled = true;

        // Direction color legend
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Direction Color Legend:", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        DrawColorLegendItem("Up", Color.green);
        DrawColorLegendItem("Down", new Color(0.0f, 0.5f, 0.0f));
        DrawColorLegendItem("Front", Color.blue);
        DrawColorLegendItem("Back", new Color(0.0f, 0.0f, 0.5f));
        DrawColorLegendItem("Left", Color.red);
        DrawColorLegendItem("Right", new Color(0.5f, 0.0f, 0.0f));
        EditorGUILayout.EndVertical();

        // Instructions
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Select a building block from the dropdown\n" +
            "2. Enable the preview\n" +
            "3. The preview will appear at world origin (0,0,0)\n" +
            "4. Arrows will show each direction with its assigned socket type\n" +
            "5. Colors match the legend above",
            MessageType.Info
        );
    }

    private void DrawColorLegendItem(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();

        // Draw color square
        Rect colorRect = EditorGUILayout.GetControlRect(false, 16, GUILayout.Width(16));
        EditorGUI.DrawRect(colorRect, color);

        // Draw label
        EditorGUILayout.LabelField(label);

        EditorGUILayout.EndHorizontal();
    }

    private void UpdatePreview()
    {
        // Clean up any existing preview
        DestroyPreview();

        // Find the selected block
        if (buildingBlocksManager == null || buildingBlocksManager.BuildingBlocks.Count == 0 ||
            selectedBlockIndex < 0 || selectedBlockIndex >= buildingBlocksManager.BuildingBlocks.Count)
        {
            return;
        }

        selectedBlock = buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

        // Check if the block has a valid prefab
        if (selectedBlock == null || selectedBlock.Prefab == null)
        {
            return;
        }

        // Create the preview at origin
        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedBlock.Prefab);
        if (previewInstance != null)
        {
            previewInstance.transform.position = Vector3.zero;

            // Apply rotation based on the DownDirection
            Quaternion rotation = Quaternion.identity;

            switch (selectedBlock.DownDirection)
            {
                case Direction.Down: // Default orientation
                    rotation = Quaternion.identity;
                    break;

                case Direction.Up: // Upside down (180° around X)
                    rotation = Quaternion.Euler(180, 0, 0);
                    break;

                case Direction.Front: // Front is down (90° around X)
                    rotation = Quaternion.Euler(90, 0, 0);
                    break;

                case Direction.Back: // Back is down (-90° around X)
                    rotation = Quaternion.Euler(-90, 0, 0);
                    break;

                case Direction.Left: // Left is down (90° around Z)
                    rotation = Quaternion.Euler(0, 0, 90);
                    break;

                case Direction.Right: // Right is down (-90° around Z)
                    rotation = Quaternion.Euler(0, 0, -90);
                    break;
            }

            previewInstance.transform.rotation = rotation;
            previewInstance.name = $"PREVIEW: {selectedBlock.Name} (Down: {selectedBlock.DownDirection})";

            // Make it not save with the scene
            previewInstance.hideFlags = HideFlags.DontSave;

            // Select the preview object
            Selection.activeGameObject = previewInstance;

            // Focus on it
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }

    private void DestroyPreview()
    {
        // Clean up existing preview
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        selectedBlock = null;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!previewEnabled || selectedBlock == null || previewInstance == null)
            return;

        // Get direction vectors adjusted for the building block's rotation
        Dictionary<Direction, Vector3> adjustedDirections = GetAdjustedDirectionVectors(selectedBlock.DownDirection);

        // Draw arrows for each direction
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            // Get the direction vector
            Vector3 dirVector = adjustedDirections[direction];

            // Get the color for this direction
            Color arrowColor = directionColors[direction];

            // Get socket type (may be empty)
            string socketType = selectedBlock.GetSocketForDirection(direction);

            // Create label based on whether a socket is assigned
            string label = string.IsNullOrEmpty(socketType) ?
                direction.ToString() :
                direction.ToString() + ": " + socketType;

            // Draw the arrow
            DrawArrow(previewInstance.transform.position, dirVector * arrowLength, arrowColor, label);
        }

        // Force the scene view to repaint continuously while preview is active
        sceneView.Repaint();
    }

    private void DrawArrow(Vector3 start, Vector3 direction, Color color, string label)
    {
        // Calculate end point
        Vector3 end = start + direction;

        // Draw the line
        Handles.color = color;
        Handles.DrawLine(start, end);

        // Draw the arrowhead (a small cube)
        Handles.DrawWireCube(end, Vector3.one * arrowThickness * 2);

        // Draw the label with better visibility but transparent background
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        // No background

        Handles.Label(end + direction.normalized * 0.2f, label, style);
    }
}
#endif