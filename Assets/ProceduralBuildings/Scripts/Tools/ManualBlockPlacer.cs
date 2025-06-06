using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime tool for manually placing building blocks in a grid
/// </summary>
public class ManualBlockPlacer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the GridGenerator")]
    public GridGenerator gridGenerator;

    [Tooltip("Reference to the BlockOrientationManager")]
    public BlockOrientationManager blockOrientationManager;

    [Header("UI Settings")]
    [Tooltip("Whether to show the placement UI")]
    public bool showUI = true;

    [Tooltip("Position of the UI window")]
    public Vector2 windowPosition = new Vector2(20, 20);

    [Tooltip("Size of the UI window")]
    public Vector2 windowSize = new Vector2(300, 400);

    // Private variables for UI state
    private int selectedBlockIndex = 0;
    private Vector3Int targetPosition = Vector3Int.zero;
    private bool showWindow = true;
    private Vector2 scrollPosition;
    private string[] blockNames;

    // Rotation options
    private bool useRandomRotation = false;
    private bool useAutoRotation = false;

    // Manual rotation control
    private bool useManualRotation = false;
    private int manualRotationAngle = 0;

    // UI Styles
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;

    private void Start()
    {
        // Verify we have a grid generator
        if (gridGenerator == null)
        {
            gridGenerator = FindFirstObjectByType<GridGenerator>();
            if (gridGenerator == null)
            {
                Debug.LogError("ManualBlockPlacer requires a GridGenerator in the scene!");
                enabled = false;
                return;
            }
        }

        // Try to find BlockOrientationManager if not assigned
        if (blockOrientationManager == null)
        {
            blockOrientationManager = FindFirstObjectByType<BlockOrientationManager>();
            if (blockOrientationManager == null)
            {
                Debug.LogWarning("BlockOrientationManager not found, orientation corrections will not be applied.");
            }
        }

        // Assign BlockOrientationManager to GridGenerator if needed
        if (blockOrientationManager != null && gridGenerator.blockOrientationManager == null)
        {
            gridGenerator.blockOrientationManager = blockOrientationManager;
        }

        // Initialize with GridGenerator's settings
        useRandomRotation = gridGenerator.enableRandomRotation;
        useAutoRotation = gridGenerator.enableAutoRotation;

        // Validate the grid dimensions for the sliders
        targetPosition = new Vector3Int(
            Mathf.Clamp(targetPosition.x, 0, gridGenerator.gridDimensions.x - 1),
            Mathf.Clamp(targetPosition.y, 0, gridGenerator.gridDimensions.y - 1),
            Mathf.Clamp(targetPosition.z, 0, gridGenerator.gridDimensions.z - 1)
        );

        // Check and get building blocks
        if (gridGenerator.buildingBlocksManager != null &&
            gridGenerator.buildingBlocksManager.BuildingBlocks.Count > 0)
        {
            blockNames = gridGenerator.buildingBlocksManager.BuildingBlocks
                .Select(bb => bb.Name)
                .ToArray();
        }
        else
        {
            Debug.LogWarning("No building blocks available in the BuildingBlocksManager!");
            blockNames = new string[0];
        }
    }

    private void OnGUI()
    {
        if (!showUI)
            return;

        // Initialize styles if needed
        InitializeStyles();

        // Main window
        if (showWindow)
        {
            Rect windowRect = new Rect(windowPosition, windowSize);
            windowRect = GUILayout.Window(123, windowRect, DrawBlockPlacerWindow, "Building Block Placer", titleStyle);
            windowPosition = windowRect.position; // Save position for dragging
        }
        else
        {
            // Small button to show window again
            if (GUI.Button(new Rect(10, 10, 150, 30), "Show Block Placer"))
            {
                showWindow = true;
            }
        }
    }

    private void InitializeStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.window);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 14;
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 12;
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
    }

    private void DrawBlockPlacerWindow(int windowID)
    {
        // Allow the window to be dragged
        GUI.DragWindow(new Rect(0, 0, windowSize.x, 20));

        // Start scrollable area
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // Building Block Selection
        GUILayout.Space(5);
        GUILayout.Label("Select Building Block", headerStyle);
        GUILayout.BeginVertical(boxStyle);

        if (blockNames.Length == 0)
        {
            GUILayout.Label("No building blocks available!");
        }
        else
        {
            // Make sure index is in range
            selectedBlockIndex = Mathf.Clamp(selectedBlockIndex, 0, blockNames.Length - 1);

            // Block selection dropdown
            string[] displayNames = blockNames.Select(name => $"{name}").ToArray();
            selectedBlockIndex = GUILayout.SelectionGrid(selectedBlockIndex, displayNames, 1);

            // Show selected block info
            if (selectedBlockIndex >= 0 && selectedBlockIndex < blockNames.Length)
            {
                BuildingBlock selectedBlock = gridGenerator.buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

                GUILayout.Space(5);
                GUILayout.Label("Selected Block Info:");
                GUILayout.Label($"Name: {selectedBlock.Name}");

                // Show orientation data if available
                if (blockOrientationManager != null)
                {
                    BlockOrientationData orientationData =
                        blockOrientationManager.GetOrientationData(selectedBlock.Name);

                    if (orientationData != null)
                    {
                        GUILayout.Label("Has orientation correction: " +
                            (orientationData.correctionRotation != Quaternion.identity ||
                             orientationData.correctionOffset != Vector3.zero ? "Yes" : "No"));
                    }
                }

                GUILayout.Label("Socket Types:");
                foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
                {
                    string socketType = selectedBlock.GetSocketForDirection(dir);
                    GUILayout.Label($"  {dir}: {(string.IsNullOrEmpty(socketType) ? "None" : socketType)}");
                }
            }
        }
        GUILayout.EndVertical();

        // Rotation Settings
        GUILayout.Space(10);
        GUILayout.Label("Rotation Settings", headerStyle);
        GUILayout.BeginVertical(boxStyle);

        // Random rotation toggle
        bool newRandomRotation = GUILayout.Toggle(useRandomRotation, "Use Random Rotation");
        if (newRandomRotation != useRandomRotation)
        {
            useRandomRotation = newRandomRotation;
            // If we enable random rotation, also apply to the grid generator
            if (useRandomRotation)
            {
                gridGenerator.enableRandomRotation = true;
                useManualRotation = false; // Disable manual rotation when random is enabled
            }
            else if (gridGenerator.enableRandomRotation)
            {
                gridGenerator.enableRandomRotation = false;
            }
        }

        // Auto rotation toggle (try all rotations)
        bool newAutoRotation = GUILayout.Toggle(useAutoRotation, "Try All Rotations");
        if (newAutoRotation != useAutoRotation)
        {
            useAutoRotation = newAutoRotation;
            // Sync with grid generator
            gridGenerator.enableAutoRotation = useAutoRotation;
        }

        // Manual rotation toggle and angle
        GUI.enabled = !useRandomRotation;
        bool newManualRotation = GUILayout.Toggle(useManualRotation, "Use Manual Rotation");
        if (newManualRotation != useManualRotation)
        {
            useManualRotation = newManualRotation;
            if (useManualRotation)
            {
                useRandomRotation = false;
                gridGenerator.enableRandomRotation = false;
            }
        }

        if (useManualRotation)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rotation Angle: ", GUILayout.Width(100));

            if (GUILayout.Button("0�", GUILayout.Width(40)))
                manualRotationAngle = 0;
            if (GUILayout.Button("90�", GUILayout.Width(40)))
                manualRotationAngle = 90;
            if (GUILayout.Button("180�", GUILayout.Width(40)))
                manualRotationAngle = 180;
            if (GUILayout.Button("270�", GUILayout.Width(40)))
                manualRotationAngle = 270;

            GUILayout.EndHorizontal();

            GUILayout.Label($"Current manual rotation: {manualRotationAngle}�");
        }
        GUI.enabled = true;

        GUILayout.EndVertical();

        // Grid Coordinates
        GUILayout.Space(10);
        GUILayout.Label("Grid Coordinates", headerStyle);
        GUILayout.BeginVertical(boxStyle);

        // X coordinate
        GUILayout.BeginHorizontal();
        GUILayout.Label("X:", GUILayout.Width(20));
        targetPosition.x = (int)GUILayout.HorizontalSlider(
            targetPosition.x,
            0,
            gridGenerator.gridDimensions.x - 1,
            GUILayout.ExpandWidth(true)
        );
        GUILayout.Label(targetPosition.x.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        // Y coordinate
        GUILayout.BeginHorizontal();
        GUILayout.Label("Y:", GUILayout.Width(20));
        targetPosition.y = (int)GUILayout.HorizontalSlider(
            targetPosition.y,
            0,
            gridGenerator.gridDimensions.y - 1,
            GUILayout.ExpandWidth(true)
        );
        GUILayout.Label(targetPosition.y.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        // Z coordinate
        GUILayout.BeginHorizontal();
        GUILayout.Label("Z:", GUILayout.Width(20));
        targetPosition.z = (int)GUILayout.HorizontalSlider(
            targetPosition.z,
            0,
            gridGenerator.gridDimensions.z - 1,
            GUILayout.ExpandWidth(true)
        );
        GUILayout.Label(targetPosition.z.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        // Show cell status
        GridGenerator.GridCell cell = gridGenerator.GetCell(targetPosition);
        if (cell != null && cell.isOccupied)
        {
            GUILayout.Space(5);
            GUILayout.Label($"Cell is occupied with: {cell.placedBlockData.Name}");

            // Add clear cell button
            if (GUILayout.Button("Clear Cell"))
            {
                gridGenerator.ClearCell(targetPosition);
            }
        }

        GUILayout.EndVertical();

        // Place Button
        GUILayout.Space(10);
        GUI.enabled = blockNames.Length > 0;
        if (GUILayout.Button("Place Block", GUILayout.Height(30)))
        {
            if (blockNames.Length > 0)
            {
                PlaceSelectedBlock();
            }
        }
        GUI.enabled = true;

        // Hide button
        GUILayout.Space(10);
        if (GUILayout.Button("Hide Window"))
        {
            showWindow = false;
        }

        GUILayout.EndScrollView();
    }

    private void PlaceSelectedBlock()
    {
        if (blockNames.Length == 0 || selectedBlockIndex < 0 || selectedBlockIndex >= blockNames.Length)
            return;

        // Store the current rotation settings from the grid generator
        bool originalRandomRotation = gridGenerator.enableRandomRotation;
        bool originalAutoRotation = gridGenerator.enableAutoRotation;

        // Apply our UI settings temporarily
        gridGenerator.enableRandomRotation = useRandomRotation;
        gridGenerator.enableAutoRotation = useAutoRotation;

        // Try to place the block
        BuildingBlock selectedBlock = gridGenerator.buildingBlocksManager.BuildingBlocks[selectedBlockIndex];
        bool success;

        if (useManualRotation)
        {
            // Use manual rotation
            success = gridGenerator.PutInCell(selectedBlock, targetPosition, manualRotationAngle);
        }
        else
        {
            // Use the GridGenerator's test and place logic
            var (testSuccess, rotation) = gridGenerator.TestBlockPlacement(
                selectedBlock, targetPosition, useAutoRotation, useRandomRotation);

            if (testSuccess)
            {
                success = gridGenerator.PutInCell(selectedBlock, targetPosition, rotation);
            }
            else
            {
                success = false;
            }
        }

        // Restore the original settings
        gridGenerator.enableRandomRotation = originalRandomRotation;
        gridGenerator.enableAutoRotation = originalAutoRotation;

        if (success)
        {
            Debug.Log($"Successfully placed {selectedBlock.Name} at {targetPosition}");
        }
        else
        {
            Debug.LogWarning($"Failed to place {selectedBlock.Name} at {targetPosition}");
        }
    }

    // Method to show the window (can be called from other scripts)
    public void ShowWindow()
    {
        showWindow = true;
    }

    // Helper method to visualize the selected cell
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || gridGenerator == null)
            return;

        // Highlight the selected cell
        GridGenerator.GridCell cell = gridGenerator.GetCell(targetPosition);
        if (cell != null)
        {
            Vector3 cellSize = gridGenerator.GetCellSize();
            Vector3 cellCenter = cell.worldPosition + (cellSize / 2f);

            // Draw selected cell outline
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cellCenter, cellSize * 1.05f);
        }
    }
}