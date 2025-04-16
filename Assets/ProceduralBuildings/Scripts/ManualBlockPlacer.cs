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

    // Testing variables
    private List<PlacementTest> placementTests = new List<PlacementTest>();

    // Placement test result struct for debugging
    private class PlacementTest
    {
        public string blockName;
        public Vector3Int position;
        public bool success;
        public int rotation;
        public bool usedRandomRotation;
        public bool usedAutoRotation;

        public override string ToString()
        {
            return $"{blockName} at {position}: {(success ? "Success" : "Failed")} - Rotation: {rotation}° " +
                   $"(Random: {usedRandomRotation}, Auto: {usedAutoRotation})";
        }
    }

    // UI Styles
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle testResultStyle;

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

        if (testResultStyle == null)
        {
            testResultStyle = new GUIStyle(GUI.skin.label);
            testResultStyle.normal.textColor = Color.green;
            testResultStyle.fontSize = 10;
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
                GUILayout.Label($"Down Direction: {selectedBlock.DownDirection}");

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

            if (GUILayout.Button("0°", GUILayout.Width(40)))
                manualRotationAngle = 0;
            if (GUILayout.Button("90°", GUILayout.Width(40)))
                manualRotationAngle = 90;
            if (GUILayout.Button("180°", GUILayout.Width(40)))
                manualRotationAngle = 180;
            if (GUILayout.Button("270°", GUILayout.Width(40)))
                manualRotationAngle = 270;

            GUILayout.EndHorizontal();

            GUILayout.Label($"Current manual rotation: {manualRotationAngle}°");
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

        // Test placement without placing
        if (GUILayout.Button("Test Block Placement (Highlights Valid Cells)"))
        {
            if (blockNames.Length > 0)
            {
                TestSelectedBlockPlacement();
            }
        }

        // Rotation test - Place blocks with different rotation settings
        if (GUILayout.Button("Run Rotation Test"))
        {
            if (blockNames.Length > 0)
            {
                RunRotationTest();
            }
        }
        GUI.enabled = true;

        // Display test results
        if (placementTests.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Rotation Test Results:", headerStyle);
            GUILayout.BeginVertical(boxStyle);

            foreach (var test in placementTests)
            {
                GUIStyle style = new GUIStyle(testResultStyle);
                style.normal.textColor = test.success ? Color.green : Color.red;
                GUILayout.Label(test.ToString(), style);
            }

            if (GUILayout.Button("Clear Test Results"))
            {
                placementTests.Clear();
            }

            GUILayout.EndVertical();
        }

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

    private void TestSelectedBlockPlacement()
    {
        if (blockNames.Length == 0 || selectedBlockIndex < 0 || selectedBlockIndex >= blockNames.Length)
            return;

        BuildingBlock selectedBlock = gridGenerator.buildingBlocksManager.BuildingBlocks[selectedBlockIndex];

        // Test the block placement without actually placing it
        var (success, rotation) = gridGenerator.TestBlockPlacement(
            selectedBlock, targetPosition, useAutoRotation, useRandomRotation);

        if (success)
        {
            Debug.Log($"Block {selectedBlock.Name} can be placed at {targetPosition} with rotation {rotation}°");
            // Add to test results
            placementTests.Add(new PlacementTest
            {
                blockName = selectedBlock.Name,
                position = targetPosition,
                success = true,
                rotation = rotation,
                usedRandomRotation = useRandomRotation,
                usedAutoRotation = useAutoRotation
            });
        }
        else
        {
            Debug.LogWarning($"Block {selectedBlock.Name} cannot be placed at {targetPosition}");
            // Add to test results
            placementTests.Add(new PlacementTest
            {
                blockName = selectedBlock.Name,
                position = targetPosition,
                success = false,
                rotation = 0,
                usedRandomRotation = useRandomRotation,
                usedAutoRotation = useAutoRotation
            });
        }
    }

    private void RunRotationTest()
    {
        if (blockNames.Length == 0 || selectedBlockIndex < 0 || selectedBlockIndex >= blockNames.Length)
            return;

        BuildingBlock selectedBlock = gridGenerator.buildingBlocksManager.BuildingBlocks[selectedBlockIndex];
        placementTests.Clear();

        // Clear the test cell first
        gridGenerator.ClearCell(targetPosition);

        // Test 1: Random rotation with auto rotation (should succeed if any rotation works)
        var (randomAutoSuccess, randomAutoRotation) = gridGenerator.TestBlockPlacement(
            selectedBlock, targetPosition, true, true);

        placementTests.Add(new PlacementTest
        {
            blockName = selectedBlock.Name,
            position = targetPosition,
            success = randomAutoSuccess,
            rotation = randomAutoRotation,
            usedRandomRotation = true,
            usedAutoRotation = true
        });

        // Test 2: Specific rotations (0, 90, 180, 270)
        for (int angle = 0; angle < 360; angle += 90)
        {
            // Clear the test cell first
            gridGenerator.ClearCell(targetPosition);

            // Clone the block data to prevent modifying the original
            BuildingBlock blockClone = new BuildingBlock
            {
                Name = selectedBlock.Name,
                Prefab = selectedBlock.Prefab,
                DownDirection = selectedBlock.DownDirection,
                TopSocket = selectedBlock.TopSocket,
                BottomSocket = selectedBlock.BottomSocket,
                FrontSocket = selectedBlock.FrontSocket,
                BackSocket = selectedBlock.BackSocket,
                LeftSocket = selectedBlock.LeftSocket,
                RightSocket = selectedBlock.RightSocket
            };

            // Apply the specified rotation
            bool success = gridGenerator.PutInCell(blockClone, targetPosition, angle);

            placementTests.Add(new PlacementTest
            {
                blockName = selectedBlock.Name,
                position = targetPosition,
                success = success,
                rotation = angle,
                usedRandomRotation = false,
                usedAutoRotation = false
            });
        }

        Debug.Log("Rotation test completed. Check the results in the UI.");
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