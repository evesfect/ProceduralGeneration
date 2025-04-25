using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
public class BlockOrientationEditorWindow : EditorWindow
{
    private BuildingBlocksManager buildingBlocksManager;
    private BlockOrientationManager orientationManager;

    private int selectedBlockIndex = 0;
    private GameObject previewInstance;
    private bool showReferenceAxes = true;
    private bool showSocketPositions = true;
    private Vector2 scrollPosition;

    // Transform controls
    private Vector3 rotationEuler = Vector3.zero;
    private Vector3 positionOffset = Vector3.zero;

    // Reference frame size
    private float axisLength = 4f;
    private float socketRadius = 0.1f;

    // Colors for directions
    private static readonly Color[] directionColors = new Color[]
    {
        Color.red,    // Right (+X)
        Color.green,  // Up (+Y)
        Color.blue,   // Forward (+Z)
        new Color(0.5f, 0, 0),   // Left (-X)
        new Color(0, 0.5f, 0),   // Down (-Y)
        new Color(0, 0, 0.5f)    // Back (-Z)
    };

    [MenuItem("Tools/ProceduralBuildings/Block Orientation Editor")]
    public static void ShowWindow()
    {
        GetWindow<BlockOrientationEditorWindow>("Block Orientation Editor");
    }

    private void OnEnable()
    {
        // Find references to required assets
        FindBuildingBlocksManager();
        FindOrCreateOrientationManager();

        // Register for scene view updates
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // Clean up
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyPreview();
    }

    private void FindBuildingBlocksManager()
    {
        // Try to find existing building blocks manager
        string[] guids = AssetDatabase.FindAssets("t:BuildingBlocksManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildingBlocksManager = AssetDatabase.LoadAssetAtPath<BuildingBlocksManager>(path);
        }
    }

    private void FindOrCreateOrientationManager()
    {
        // Try to find existing orientation manager
        string[] guids = AssetDatabase.FindAssets("t:BlockOrientationManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            orientationManager = AssetDatabase.LoadAssetAtPath<BlockOrientationManager>(path);
        }

        // If not found, create a new one
        if (orientationManager == null)
        {
            orientationManager = CreateInstance<BlockOrientationManager>();

            // Create directories if needed
            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings"))
                AssetDatabase.CreateFolder("Assets", "ProceduralBuildings");

            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings/Data"))
                AssetDatabase.CreateFolder("Assets/ProceduralBuildings", "Data");

            // Save the asset
            AssetDatabase.CreateAsset(orientationManager, "Assets/ProceduralBuildings/Data/BlockOrientationManager.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        if (buildingBlocksManager == null)
        {
            EditorGUILayout.HelpBox("Building Blocks Manager asset not found! Please create it first.", MessageType.Error);
            return;
        }

        if (orientationManager == null)
        {
            EditorGUILayout.HelpBox("Block Orientation Manager asset not found!", MessageType.Error);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Block Orientation Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool helps you create correction transforms for building blocks with incorrect import orientations.\n\n" +
            "1. Select a block from the dropdown\n" +
            "2. Adjust rotation and position to align the block with the standard orientation\n" +
            "3. Save when the block is correctly aligned with the reference axes",
            MessageType.Info);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Block selection
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Select Building Block:", EditorStyles.boldLabel);

        if (buildingBlocksManager.BuildingBlocks.Count == 0)
        {
            EditorGUILayout.HelpBox("No building blocks created yet. Create some building blocks first.", MessageType.Warning);
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
                LoadOrientationData();
                UpdatePreview();
            }

            // Show selected block info
            if (selectedBlockIndex >= 0 && selectedBlockIndex < buildingBlocksManager.BuildingBlocks.Count)
            {
                EditorGUILayout.Space(5);
                BuildingBlock selectedBlock = buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

                EditorGUILayout.LabelField("Selected Block Info:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Name: {selectedBlock.Name}");

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Socket Types:", EditorStyles.boldLabel);
                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    string socketType = selectedBlock.GetSocketForDirection(dir);
                    EditorGUILayout.LabelField($"  {dir}: {(string.IsNullOrEmpty(socketType) ? "None" : socketType)}");
                }
            }
        }

        // Orientation controls
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Orientation Controls", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Rotation
        EditorGUILayout.LabelField("Rotation Correction:", EditorStyles.boldLabel);
        Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation (Euler):", rotationEuler);
        if (newRotation != rotationEuler)
        {
            rotationEuler = newRotation;
            UpdatePreview();
        }

        // Position
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Position Offset:", EditorStyles.boldLabel);
        Vector3 newPosition = EditorGUILayout.Vector3Field("Position:", positionOffset);
        if (newPosition != positionOffset)
        {
            positionOffset = newPosition;
            UpdatePreview();
        }

        // Visualization options
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Visualization Options", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        bool newShowAxes = EditorGUILayout.Toggle("Show Reference Axes", showReferenceAxes);
        if (newShowAxes != showReferenceAxes)
        {
            showReferenceAxes = newShowAxes;
            SceneView.RepaintAll();
        }

        bool newShowSockets = EditorGUILayout.Toggle("Show Socket Positions", showSocketPositions);
        if (newShowSockets != showSocketPositions)
        {
            showSocketPositions = newShowSockets;
            SceneView.RepaintAll();
        }

        // Save button
        EditorGUILayout.Space(20);
        if (GUILayout.Button("Save Orientation Data", GUILayout.Height(30)))
        {
            SaveOrientationData();
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
        {
            UpdatePreview();
        }

        // Reset button
        if (GUILayout.Button("Reset Orientation", GUILayout.Height(30)))
        {
            ResetOrientation();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadOrientationData()
    {
        if (selectedBlockIndex < 0 || selectedBlockIndex >= buildingBlocksManager.BuildingBlocks.Count)
            return;

        string blockName = buildingBlocksManager.BuildingBlocks[selectedBlockIndex].Name;
        BlockOrientationData data = orientationManager.GetOrientationData(blockName);

        // Load values
        rotationEuler = data.correctionRotation.eulerAngles;
        positionOffset = data.correctionOffset;
    }

    private void SaveOrientationData()
    {
        if (selectedBlockIndex < 0 || selectedBlockIndex >= buildingBlocksManager.BuildingBlocks.Count)
            return;

        string blockName = buildingBlocksManager.BuildingBlocks[selectedBlockIndex].Name;

        // Create quaternion from euler angles
        Quaternion rotation = Quaternion.Euler(rotationEuler);

        // Save the data
        orientationManager.AddOrUpdateOrientation(blockName, rotation, positionOffset);

        // Mark as dirty
        EditorUtility.SetDirty(orientationManager);
        AssetDatabase.SaveAssets();

        Debug.Log($"Saved orientation data for block: {blockName}");
    }

    private void ResetOrientation()
    {
        rotationEuler = Vector3.zero;
        positionOffset = Vector3.zero;
        UpdatePreview();
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

        BuildingBlock selectedBlock = buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

        // Check if the block has a valid prefab
        if (selectedBlock == null || selectedBlock.Prefab == null)
        {
            return;
        }

        // Create the preview at origin
        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedBlock.Prefab);
        if (previewInstance != null)
        {
            // Store the original scale before applying any adjustments
            Vector3 originalScale = previewInstance.transform.localScale;

            previewInstance.transform.position = Vector3.zero;

            // Apply rotation
            previewInstance.transform.rotation = Quaternion.Euler(rotationEuler);

            // Apply position offset
            previewInstance.transform.position += positionOffset;

            previewInstance.name = $"PREVIEW: {selectedBlock.Name}";

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
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (previewInstance == null)
            return;

        // Draw reference coordinate system
        if (showReferenceAxes)
        {
            DrawReferenceAxes();
        }

        // Draw socket positions
        if (showSocketPositions)
        {
            DrawSocketPositions();
        }

        // Force the scene view to repaint
        sceneView.Repaint();
    }

    private void DrawReferenceAxes()
    {
        // Draw the standard orientation axes
        Handles.color = Color.red;
        Handles.DrawLine(Vector3.zero, Vector3.right * axisLength);
        Handles.Label(Vector3.right * axisLength, "+X (Right)");

        Handles.color = Color.green;
        Handles.DrawLine(Vector3.zero, Vector3.up * axisLength);
        Handles.Label(Vector3.up * axisLength, "+Y (Up)");

        Handles.color = Color.blue;
        Handles.DrawLine(Vector3.zero, Vector3.forward * axisLength);
        Handles.Label(Vector3.forward * axisLength, "+Z (Forward)");

        // Draw negative directions with different colors
        Handles.color = new Color(0.5f, 0, 0);
        Handles.DrawLine(Vector3.zero, Vector3.left * axisLength);
        Handles.Label(Vector3.left * axisLength, "-X (Left)");

        Handles.color = new Color(0, 0.5f, 0);
        Handles.DrawLine(Vector3.zero, Vector3.down * axisLength);
        Handles.Label(Vector3.down * axisLength, "-Y (Down)");

        Handles.color = new Color(0, 0, 0.5f);
        Handles.DrawLine(Vector3.zero, Vector3.back * axisLength);
        Handles.Label(Vector3.back * axisLength, "-Z (Back)");
    }

    private void DrawSocketPositions()
    {
        if (selectedBlockIndex < 0 || selectedBlockIndex >= buildingBlocksManager.BuildingBlocks.Count)
            return;

        BuildingBlock selectedBlock = buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

        // Socket positions in standard orientation
        Vector3[] socketPositions = new Vector3[]
        {
            Vector3.right * axisLength * 0.8f,   // Right (+X)
            Vector3.up * axisLength * 0.8f,      // Up (+Y)
            Vector3.forward * axisLength * 0.8f, // Forward (+Z)
            Vector3.left * axisLength * 0.8f,    // Left (-X)
            Vector3.down * axisLength * 0.8f,    // Down (-Y)
            Vector3.back * axisLength * 0.8f     // Back (-Z)
        };

        // Direction names
        string[] directionNames = new string[]
        {
            "Right", "Up", "Forward", "Left", "Down", "Back"
        };

        // Get socket types
        Direction[] directions = new Direction[]
        {
            Direction.Right, Direction.Up, Direction.Front,
            Direction.Left, Direction.Down, Direction.Back
        };

        // Draw socket positions
        for (int i = 0; i < socketPositions.Length; i++)
        {
            string socketType = selectedBlock.GetSocketForDirection(directions[i]);
            if (string.IsNullOrEmpty(socketType))
                continue;

            Handles.color = directionColors[i];
            Handles.SphereHandleCap(0, socketPositions[i], Quaternion.identity, socketRadius, EventType.Repaint);

            // Draw label with socket type
            Handles.Label(socketPositions[i], $"{directionNames[i]}: {socketType}");
        }
    }
}
#endif