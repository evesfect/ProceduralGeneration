using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Runtime tool for visualizing rule application in the editor.
/// Places actual blocks to visualize how rules affect placement.
/// </summary>
public class RuleVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the BlockSystemInterface")]
    public BlockSystemInterface blockSystem;

    [Tooltip("Reference to the RuleManager")]
    public RuleManager ruleManager;

    [Header("Visualization Settings")]
    [Tooltip("Color for valid placements")]
    public Color validColor = new Color(0, 1, 0, 0.5f); // Semi-transparent green

    [Tooltip("Color for invalid placements")]
    public Color invalidColor = new Color(1, 0, 0, 0.5f); // Semi-transparent red

    [Tooltip("Color for related conflict cells")]
    public Color conflictColor = new Color(1, 0.5f, 0, 0.5f); // Semi-transparent orange

    [Tooltip("Size multiplier for visualization elements")]
    public float sizeMultiplier = 1.2f; // Slightly larger than the cell to show through blocks

    [Header("Test Settings")]
    [Tooltip("Position to test")]
    public Vector3Int testPosition;

    [Tooltip("Index of the block to test")]
    public int testBlockIndex = 0;

    [Tooltip("Toggle to enable rule visualization")]
    public bool enableVisualization = true;

    [Header("Rotation Settings")]
    [Tooltip("Test rotation in degrees (0, 90, 180, 270)")]
    public int testRotation = 0;

    [Tooltip("Try all rotations")]
    public bool tryAllRotations = true;

    // Private variables
    private BuildingBlock selectedBlock;
    private List<BuildingBlock> availableBlocks = new List<BuildingBlock>();
    private bool isValidPlacement = false;
    private string failureReason = "";
    private Vector3Int? conflictPosition = null;
    public int validRotation = 0; // Store the working rotation

    // References to spawned test objects
    private GameObject testBlockInstance = null;
    private GameObject highlightCube = null;
    private GameObject conflictHighlightCube = null;

    // Settings for tracking changes
    private Vector3Int lastTestPosition;
    private int lastTestBlockIndex = -1;
    private int lastTestRotation = -1;
    private bool lastTryAllRotations = false;

    private void Start()
    {
        // Auto-find references if not assigned
        if (blockSystem == null)
            blockSystem = FindAnyObjectByType<BlockSystemInterface>();

        if (ruleManager == null)
        {
            BuildingGenerator buildingGen = FindAnyObjectByType<BuildingGenerator>();
            if (buildingGen != null)
            {
                // Try to find RuleManager component on BuildingGenerator
                ruleManager = buildingGen.GetComponent<RuleManager>();

                // If not found, try to get it from a field (might need field reflection)
                if (ruleManager == null)
                {
                    Debug.LogWarning("RuleManager not found as component. You may need to assign it manually.");
                }
            }
        }

        // Get all available blocks
        if (blockSystem != null)
            availableBlocks = blockSystem.GetAllBuildingBlocks();

        // Set initial selected block
        UpdateSelectedBlock();

        // Create highlight objects
        CreateHighlightObjects();
    }

    private void CreateHighlightObjects()
    {
        // Create highlight for test position
        highlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlightCube.name = "TestPositionHighlight";

        // Remove collider to prevent physics interactions
        DestroyImmediate(highlightCube.GetComponent<Collider>());

        // Set material to transparent
        Renderer renderer = highlightCube.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;

        // Initially hide the highlight
        highlightCube.SetActive(false);

        // Create conflict highlight
        conflictHighlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        conflictHighlightCube.name = "ConflictPositionHighlight";

        // Remove collider
        DestroyImmediate(conflictHighlightCube.GetComponent<Collider>());

        // Set material
        Renderer conflictRenderer = conflictHighlightCube.GetComponent<Renderer>();
        Material conflictMat = new Material(Shader.Find("Standard"));
        conflictMat.SetFloat("_Mode", 3);
        conflictMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        conflictMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        conflictMat.SetInt("_ZWrite", 0);
        conflictMat.DisableKeyword("_ALPHATEST_ON");
        conflictMat.EnableKeyword("_ALPHABLEND_ON");
        conflictMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        conflictMat.renderQueue = 3000;
        conflictRenderer.material = conflictMat;

        // Initially hide the conflict highlight
        conflictHighlightCube.SetActive(false);
    }

    private void Update()
    {
        if (!enableVisualization)
        {
            ClearTestBlock();
            HideHighlights();
            return;
        }

        // Check for changes to settings
        bool settingsChanged =
            testPosition != lastTestPosition ||
            testBlockIndex != lastTestBlockIndex ||
            testRotation != lastTestRotation ||
            tryAllRotations != lastTryAllRotations;

        // Update test if settings changed
        if (settingsChanged)
        {
            // Update block selection if needed
            if (testBlockIndex != lastTestBlockIndex)
            {
                UpdateSelectedBlock();
            }

            // Test rule validation
            TestRuleValidation();

            // Update visualizations
            UpdateVisualizations();

            // Store current settings
            lastTestPosition = testPosition;
            lastTestBlockIndex = testBlockIndex;
            lastTestRotation = testRotation;
            lastTryAllRotations = tryAllRotations;
        }
    }

    private void UpdateSelectedBlock()
    {
        // Clear existing test block
        ClearTestBlock();

        if (testBlockIndex >= 0 && testBlockIndex < availableBlocks.Count)
        {
            selectedBlock = availableBlocks[testBlockIndex];
        }
    }

    private void TestRuleValidation()
    {
        // Reset conflict position
        conflictPosition = null;

        // Skip if missing references
        if (blockSystem == null || selectedBlock == null)
        {
            isValidPlacement = false;
            failureReason = "Missing references (blockSystem or selectedBlock)";
            return;
        }

        // Skip if the position is outside the grid
        if (!blockSystem.IsValidGridPosition(testPosition))
        {
            isValidPlacement = false;
            failureReason = "Position is outside the grid";
            return;
        }

        // Skip if the cell is already occupied
        if (blockSystem.IsCellOccupied(testPosition))
        {
            isValidPlacement = false;
            failureReason = "Cell is already occupied";
            return;
        }

        // Test the rules for this block and position
        bool isValid = TestRules(selectedBlock, testPosition, out string reason, out Vector3Int? conflict);

        isValidPlacement = isValid;
        failureReason = reason;
        conflictPosition = conflict;
    }

    private bool TestRules(BuildingBlock block, Vector3Int position, out string reason, out Vector3Int? conflictPos)
    {
        reason = "";
        conflictPos = null;

        // First, test if the block can be placed with the specified rotation
        var (isValid, workingRotation) = blockSystem.CheckBlockValidForPosition(
            block, position, tryAllRotations, false);

        if (!isValid)
        {
            reason = "Block doesn't fit this position (socket/physics constraints)";
            return false;
        }

        // Store the rotation that works
        validRotation = tryAllRotations ? workingRotation : testRotation;

        // Apply the rotation to a clone of the block for accurate rule testing
        BuildingBlock rotatedBlock = CloneBlockWithRotation(block, validRotation);

        // Skip rule testing if ruleManager is missing
        if (ruleManager == null)
            return true;

        // Test all rules in the rule manager with the rotated block
        // Test global rules
        foreach (var rule in ruleManager.globalRules)
        {
            if (!rule.isEnabled)
                continue;

            if (!rule.EvaluatePlacement(rotatedBlock, position, testRotation/4, blockSystem))
            {
                reason = $"Global rule '{rule.ruleName}' failed: {rule.GetFailureReason()}";

                // Try to extract conflict position from failure reason
                string failureReason = rule.GetFailureReason();

                // Look for any mention of a different position in the failure reason
                TryExtractConflictPosition(failureReason, out Vector3Int pos);
                conflictPos = pos;

                return false;
            }
        }

        // Test block-specific rules
        var blockRuleAssignment = ruleManager.blockRules.Find(br => br.blockName == block.Name);
        if (blockRuleAssignment != null)
        {
            foreach (var rule in blockRuleAssignment.rules)
            {
                if (!rule.isEnabled)
                    continue;

                if (!rule.EvaluatePlacement(rotatedBlock, position, testRotation/4, blockSystem))
                {
                    reason = $"Block-specific rule '{rule.ruleName}' failed: {rule.GetFailureReason()}";

                    // Try to extract conflict position
                    TryExtractConflictPosition(rule.GetFailureReason(), out Vector3Int pos);
                    conflictPos = pos;

                    return false;
                }
            }
        }

        return true;
    }

    private void UpdateVisualizations()
    {
        // Clear existing test block
        ClearTestBlock();

        // Skip if missing references or outside grid
        if (blockSystem == null || selectedBlock == null || !blockSystem.IsValidGridPosition(testPosition))
        {
            HideHighlights();
            return;
        }

        // Skip if cell is already occupied
        if (blockSystem.IsCellOccupied(testPosition))
        {
            HideHighlights();
            return;
        }

        // Update highlight for test position
        UpdateTestPositionHighlight();

        // Update conflict highlight if applicable
        UpdateConflictHighlight();

        // Place the test block if placement is valid
        if (isValidPlacement)
        {
            PlaceTestBlock();
        }
    }

    private void UpdateTestPositionHighlight()
    {
        // Get cell information
        var cell = blockSystem.GetCell(testPosition);
        if (cell == null)
        {
            highlightCube.SetActive(false);
            return;
        }

        // Get cell size and position
        Vector3 cellSize = blockSystem.GetCellSize();
        Vector3 cellCenter = cell.worldPosition + (cellSize / 2f);

        // Update position and size
        highlightCube.transform.position = cellCenter;
        highlightCube.transform.localScale = cellSize * sizeMultiplier;

        // Update color
        Renderer renderer = highlightCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = isValidPlacement ? validColor : invalidColor;
        }

        // Show the highlight
        highlightCube.SetActive(true);
    }

    private void UpdateConflictHighlight()
    {
        // Hide if no conflict
        if (!conflictPosition.HasValue || !blockSystem.IsValidGridPosition(conflictPosition.Value))
        {
            conflictHighlightCube.SetActive(false);
            return;
        }

        // Get cell information
        var cell = blockSystem.GetCell(conflictPosition.Value);
        if (cell == null)
        {
            conflictHighlightCube.SetActive(false);
            return;
        }

        // Get cell size and position
        Vector3 cellSize = blockSystem.GetCellSize();
        Vector3 cellCenter = cell.worldPosition + (cellSize / 2f);

        // Update position and size (slightly larger to see through blocks)
        conflictHighlightCube.transform.position = cellCenter;
        conflictHighlightCube.transform.localScale = cellSize * sizeMultiplier * 1.1f;

        // Update color
        Renderer renderer = conflictHighlightCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = conflictColor;
        }

        // Show the highlight
        conflictHighlightCube.SetActive(true);
    }

    private void PlaceTestBlock()
    {
        // Skip if missing prefab
        if (selectedBlock == null || selectedBlock.Prefab == null)
            return;

        // Instantiate the test block
        testBlockInstance = Instantiate(selectedBlock.Prefab);
        testBlockInstance.name = $"TEST_{selectedBlock.Name}";

        // Set the test block's position and rotation
        // Use the BlockSystem's AlignBuildingBlock functionality if available
        GridGenerator gridGen = FindAnyObjectByType<GridGenerator>();
        if (gridGen != null)
        {
            // Get cell position
            var cell = blockSystem.GetCell(testPosition);
            if (cell != null)
            {
                // Use reflection to call private AlignBuildingBlock method (or use your own alignment method)
                AlignTestBlock(testBlockInstance, selectedBlock, cell.worldPosition, validRotation);
            }
        }
        else
        {
            // Fallback basic positioning if GridGenerator not found
            var cell = blockSystem.GetCell(testPosition);
            if (cell != null)
            {
                Vector3 cellSize = blockSystem.GetCellSize();
                Vector3 cellCenter = cell.worldPosition + (cellSize / 2f);
                testBlockInstance.transform.position = cellCenter;
                testBlockInstance.transform.rotation = Quaternion.Euler(0, validRotation, 0);
            }
        }

        // Make it semi-transparent to show it's a test
        ApplyTransparency(testBlockInstance, 0.7f);
    }

    private void AlignTestBlock(GameObject blockObject, BuildingBlock blockData, Vector3 cellWorldPosition, float yRotationDegrees)
    {
        // Get the building block's renderer to find its bounds
        Renderer renderer = blockObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            // Try to find renderer in children
            renderer = blockObject.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            // Apply rotation based on the block's down direction
            Quaternion rotation = Quaternion.identity;

            switch (blockData.DownDirection)
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

            // Add Y rotation
            Quaternion yRotation = Quaternion.Euler(0, yRotationDegrees, 0);

            // Apply the combined rotation
            blockObject.transform.rotation = yRotation * rotation;

            // Force physics update to recalculate bounds after rotation
            Physics.SyncTransforms();

            // Get the new bounds after rotation
            Bounds bounds = renderer.bounds;

            // Calculate the lowest point of the model
            float bottomOffset = bounds.min.y - blockObject.transform.position.y;

            // Get cell size
            Vector3 cellSize = blockSystem.GetCellSize();

            // Position the building block so its bottom aligns with the bottom of the cell
            blockObject.transform.position = new Vector3(
                cellWorldPosition.x + (cellSize.x / 2f), // Center X
                cellWorldPosition.y - bottomOffset,      // Align bottom
                cellWorldPosition.z + (cellSize.z / 2f)  // Center Z
            );
        }
        else
        {
            // If no renderer found, just place at cell center
            Vector3 cellSize = blockSystem.GetCellSize();
            Vector3 cellCenter = cellWorldPosition + (cellSize / 2f);
            blockObject.transform.position = cellCenter;
            blockObject.transform.rotation = Quaternion.Euler(0, yRotationDegrees, 0);
        }
    }

    private void ApplyTransparency(GameObject obj, float alpha)
    {
        // Get all renderers
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Process each material
            foreach (Material material in renderer.materials)
            {
                // Enable transparency
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;

                // Set alpha
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }
    }

    private void ClearTestBlock()
    {
        // Destroy test block if it exists
        if (testBlockInstance != null)
        {
            DestroyImmediate(testBlockInstance);
            testBlockInstance = null;
        }
    }

    private void HideHighlights()
    {
        if (highlightCube != null)
            highlightCube.SetActive(false);

        if (conflictHighlightCube != null)
            conflictHighlightCube.SetActive(false);
    }

    // Helper method to clone a block and apply rotation
    private BuildingBlock CloneBlockWithRotation(BuildingBlock original, int yRotation)
    {
        BuildingBlock clone = new BuildingBlock
        {
            Name = original.Name,
            Prefab = original.Prefab,
            DownDirection = original.DownDirection,
            TopSocket = original.TopSocket,
            BottomSocket = original.BottomSocket,
            FrontSocket = original.FrontSocket,
            BackSocket = original.BackSocket,
            LeftSocket = original.LeftSocket,
            RightSocket = original.RightSocket,
            CurrentRotation = yRotation
        };

        return clone;
    }

    private bool TryExtractConflictPosition(string failureReason, out Vector3Int position)
    {
        position = Vector3Int.zero;

        // Look for patterns like (x, y, z) or [x, y, z] in the failure reason
        // This is a simple heuristic and might need adjustment based on your rule messages
        if (string.IsNullOrEmpty(failureReason))
            return false;

        try
        {
            // Try to find coordinate patterns in the text
            System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(@"\((\d+),\s*(\d+),\s*(\d+)\)|\[(\d+),\s*(\d+),\s*(\d+)\]");

            var match = regex.Match(failureReason);
            if (match.Success)
            {
                // Extract the coordinates
                int x = 0, y = 0, z = 0;

                // Check which group format matched
                if (match.Groups[1].Success) // (x,y,z) format
                {
                    x = int.Parse(match.Groups[1].Value);
                    y = int.Parse(match.Groups[2].Value);
                    z = int.Parse(match.Groups[3].Value);
                }
                else // [x,y,z] format
                {
                    x = int.Parse(match.Groups[4].Value);
                    y = int.Parse(match.Groups[5].Value);
                    z = int.Parse(match.Groups[6].Value);
                }

                position = new Vector3Int(x, y, z);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing conflict position: {e.Message}");
        }

        return false;
    }

    private void OnDestroy()
    {
        // Clean up test objects
        ClearTestBlock();

        // Clean up highlight objects
        if (highlightCube != null)
            DestroyImmediate(highlightCube);

        if (conflictHighlightCube != null)
            DestroyImmediate(conflictHighlightCube);
    }

#if UNITY_EDITOR
    // Custom editor to make the tool more user-friendly
    [CustomEditor(typeof(RuleVisualizer))]
    public class RuleVisualizerEditor : Editor
    {
        private SerializedProperty blockSystemProp;
        private SerializedProperty ruleManagerProp;
        private SerializedProperty validColorProp;
        private SerializedProperty invalidColorProp;
        private SerializedProperty conflictColorProp;
        private SerializedProperty sizeMultiplierProp;
        private SerializedProperty testPositionProp;
        private SerializedProperty testBlockIndexProp;
        private SerializedProperty enableVisualizationProp;
        private SerializedProperty testRotationProp;
        private SerializedProperty tryAllRotationsProp;

        private RuleVisualizer visualizer;
        private string[] blockNames;

        private void OnEnable()
        {
            blockSystemProp = serializedObject.FindProperty("blockSystem");
            ruleManagerProp = serializedObject.FindProperty("ruleManager");
            validColorProp = serializedObject.FindProperty("validColor");
            invalidColorProp = serializedObject.FindProperty("invalidColor");
            conflictColorProp = serializedObject.FindProperty("conflictColor");
            sizeMultiplierProp = serializedObject.FindProperty("sizeMultiplier");
            testPositionProp = serializedObject.FindProperty("testPosition");
            testBlockIndexProp = serializedObject.FindProperty("testBlockIndex");
            enableVisualizationProp = serializedObject.FindProperty("enableVisualization");
            testRotationProp = serializedObject.FindProperty("testRotation");
            tryAllRotationsProp = serializedObject.FindProperty("tryAllRotations");

            visualizer = (RuleVisualizer)target;
            UpdateBlockNames();
        }

        private void UpdateBlockNames()
        {
            if (visualizer.blockSystem != null)
            {
                var blocks = visualizer.blockSystem.GetAllBuildingBlocks();
                blockNames = new string[blocks.Count];
                for (int i = 0; i < blocks.Count; i++)
                {
                    blockNames[i] = blocks[i].Name;
                }
            }
            else
            {
                blockNames = new string[0];
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Rule Visualization Tool", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Visualize and test rule constraints in the Scene view", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            // References
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blockSystemProp);
            EditorGUILayout.PropertyField(ruleManagerProp);

            if (visualizer.blockSystem == null)
            {
                EditorGUILayout.HelpBox("BlockSystemInterface reference is required for visualization", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // Test Settings
            EditorGUILayout.LabelField("Test Settings", EditorStyles.boldLabel);

            // Enable/Disable toggle
            bool prevVisualization = visualizer.enableVisualization;
            EditorGUILayout.PropertyField(enableVisualizationProp);

            // If visualization was toggled off, clear test objects
            if (prevVisualization && !visualizer.enableVisualization)
            {
                visualizer.ClearTestBlock();
                visualizer.HideHighlights();
            }

            EditorGUI.BeginDisabledGroup(!visualizer.enableVisualization);

            EditorGUILayout.PropertyField(testPositionProp);

            // Block selection dropdown
            if (blockNames != null && blockNames.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Test Block:", GUILayout.Width(100));

                testBlockIndexProp.intValue = EditorGUILayout.Popup(testBlockIndexProp.intValue, blockNames);

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No building blocks available", MessageType.Info);
            }

            // Rotation settings
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(tryAllRotationsProp, new GUIContent("Try All Rotations"));

            // Only show rotation if not trying all rotations
            if (!visualizer.tryAllRotations)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Test Rotation:", GUILayout.Width(100));

                // Create a nice rotation selector
                if (GUILayout.Button("0°", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
                    testRotationProp.intValue = 0;
                if (GUILayout.Button("90°", EditorStyles.miniButtonMid, GUILayout.Width(30)))
                    testRotationProp.intValue = 90;
                if (GUILayout.Button("180°", EditorStyles.miniButtonMid, GUILayout.Width(30)))
                    testRotationProp.intValue = 180;
                if (GUILayout.Button("270°", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                    testRotationProp.intValue = 270;

                EditorGUILayout.EndHorizontal();
            }

            // Test results
            if (visualizer.enableVisualization)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);

                string result = visualizer.isValidPlacement ? "VALID PLACEMENT" : "INVALID PLACEMENT";
                Color resultColor = visualizer.isValidPlacement ? Color.green : Color.red;

                EditorGUILayout.BeginHorizontal();
                GUIStyle resultStyle = new GUIStyle(EditorStyles.boldLabel);
                resultStyle.normal.textColor = resultColor;
                EditorGUILayout.LabelField(result, resultStyle);
                EditorGUILayout.EndHorizontal();

                if (visualizer.isValidPlacement)
                {
                    EditorGUILayout.LabelField($"Working Rotation: {visualizer.validRotation}°");
                }

                if (!visualizer.isValidPlacement && !string.IsNullOrEmpty(visualizer.failureReason))
                {
                    EditorGUILayout.HelpBox(visualizer.failureReason, MessageType.Warning);
                }

                if (visualizer.conflictPosition.HasValue)
                {
                    EditorGUILayout.LabelField($"Conflict Position: {visualizer.conflictPosition.Value}");
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            // Visualization Settings
            EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(validColorProp);
            EditorGUILayout.PropertyField(invalidColorProp);
            EditorGUILayout.PropertyField(conflictColorProp);
            EditorGUILayout.PropertyField(sizeMultiplierProp);

            // Update the visualizer if any property changed
            if (serializedObject.ApplyModifiedProperties())
            {
                // If block system changed, update block names
                if (blockSystemProp.serializedObject.hasModifiedProperties)
                {
                    UpdateBlockNames();
                }

                // Force redraw of the scene view
                SceneView.RepaintAll();
            }

            // Button to focus scene camera on test position
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Focus Camera on Test Position"))
            {
                FocusCameraOnTestPosition();
            }

            // Button to force update visualizations
            if (GUILayout.Button("Update Visualization"))
            {
                visualizer.ClearTestBlock();
                visualizer.TestRuleValidation();
                visualizer.UpdateVisualizations();
                SceneView.RepaintAll();
            }
        }

        private void FocusCameraOnTestPosition()
        {
            if (visualizer.blockSystem != null && visualizer.blockSystem.IsValidGridPosition(visualizer.testPosition))
            {
                var cell = visualizer.blockSystem.GetCell(visualizer.testPosition);
                if (cell != null)
                {
                    Vector3 cellSize = visualizer.blockSystem.GetCellSize();
                    Vector3 cellCenter = cell.worldPosition + (cellSize / 2f);

                    // Focus scene view on this position
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView != null)
                    {
                        sceneView.Frame(new Bounds(cellCenter, cellSize * 5), false);
                    }
                }
            }
        }
    }
#endif
}