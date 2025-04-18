using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages a 3D grid for procedural building generation, handling placement and socket connections.
/// </summary>
public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Dimensions of the grid in cells (X, Y, Z)")]
    public Vector3Int gridDimensions = new Vector3Int(10, 5, 10);

    [Tooltip("Reference to the cell size prefab")]
    public GameObject cellSizePrefab;

    [Tooltip("World position of the bottom-left corner of the grid")]
    public Transform gridOrigin;

    [Header("Ground Settings")]
    [Tooltip("Socket type to use for ground connections")]
    public string groundSocketType = "ground";

    [Tooltip("Show ground cell configuration in the inspector")]
    public bool showGroundConfig = true;

    [Header("Placement Settings")]
    [Tooltip("Try to place blocks with random horizontal rotation")]
    public bool enableRandomRotation = false;

    [Tooltip("Try all horizontal rotations before failing placement")]
    public bool enableAutoRotation = false;

    [Header("References")]
    [Tooltip("Reference to the BuildingBlocksManager asset")]
    public BuildingBlocksManager buildingBlocksManager;

    [Tooltip("Reference to the SocketManager asset")]
    public SocketManager socketManager;

    [Header("Debug Settings")]
    [Tooltip("Show grid visualization in the scene")]
    public bool showGridGizmos = true;

    [Tooltip("Show socket information in the scene")]
    public bool showSocketGizmos = false;

    // The size of each cell based on the prefab
    private Vector3 cellSize;

    // The 3D grid of cells
    private GridCell[,,] grid;

    // Reference to current socket compatibility dictionary
    private Dictionary<string, List<string>> socketCompatibilityDict;

    // Serializable class to store ground cell configuration
    [System.Serializable]
    public class GroundCellRow
    {
        public List<bool> cells = new List<bool>();
    }

    // Ground cell configuration (which bottom cells have ground sockets)
    [SerializeField]
    private List<GroundCellRow> groundCellRows = new List<GroundCellRow>();

    /// <summary>
    /// Represents a single cell in the 3D grid
    /// </summary>
    [System.Serializable]
    public class GridCell
    {
        // Position in the grid
        public Vector3Int gridPosition;

        // World position of the cell (bottom-left corner)
        public Vector3 worldPosition;

        // Is the cell occupied by a building block
        public bool isOccupied = false;

        // Reference to the placed building block GameObject
        public GameObject placedBlockObject;

        // Reference to the building block data
        public BuildingBlock placedBlockData;

        // Socket types for each direction (in grid space, not block space)
        // Order: Right, Left, Up, Down, Forward, Back [corresponds to Direction enum: 0-5]
        public string[] sockets = new string[6];

        public GridCell(Vector3Int gridPos, Vector3 worldPos)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;

            // Initialize empty sockets
            for (int i = 0; i < 6; i++)
            {
                sockets[i] = "";
            }
        }
    }

    private void Awake()
    {
        // Validate references
        if (cellSizePrefab == null)
        {
            Debug.LogError("Cell size prefab not assigned!");
            return;
        }

        if (buildingBlocksManager == null)
        {
            Debug.LogError("BuildingBlocksManager reference not assigned!");
            return;
        }

        if (socketManager == null)
        {
            Debug.LogError("SocketManager reference not assigned!");
            return;
        }

        // Calculate cell size from the prefab
        CalculateCellSize();

        // Initialize ground cells if needed
        if (groundCellRows.Count == 0)
        {
            InitializeGroundCells();
        }

        // Initialize the grid
        InitializeGrid();

        // Get socket compatibility dictionary
        socketCompatibilityDict = socketManager.GetSocketCompDict();
    }

    /// <summary>
    /// Handles changes to inspector properties
    /// </summary>
    public void OnValidate()
    {
        // If grid dimensions changed, resize the ground cells
        if (gridDimensions.x > 0 && gridDimensions.z > 0)
        {
            ResizeGroundCells();
        }
    }

    /// <summary>
    /// Calculate the cell size based on the provided prefab
    /// </summary>
    private void CalculateCellSize()
    {
        if (cellSizePrefab != null)
        {
            // Get the bounds of the prefab to determine cell size
            Renderer renderer = cellSizePrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                cellSize = renderer.bounds.size;
                ////Debug.Log($"Cell size calculated from prefab: {cellSize}");
            }
            else
            {
                // Default size if no renderer found
                cellSize = Vector3.one;
                Debug.LogWarning("No renderer found on cellSizePrefab. Using default size of 1x1x1.");
            }
        }
        else
        {
            // Default size if no prefab assigned
            cellSize = Vector3.one;
            Debug.LogWarning("No cellSizePrefab assigned. Using default size of 1x1x1.");
        }
    }

    /// <summary>
    /// Get the current cell size
    /// </summary>
    public Vector3 GetCellSize()
    {
        return cellSize;
    }

    /// <summary>
    /// Initialize ground cell configuration
    /// </summary>
    private void InitializeGroundCells()
    {
        groundCellRows.Clear();

        for (int x = 0; x < gridDimensions.x; x++)
        {
            GroundCellRow row = new GroundCellRow();
            for (int z = 0; z < gridDimensions.z; z++)
            {
                row.cells.Add(true); // By default, all cells have ground
            }
            groundCellRows.Add(row);
        }
    }

    /// <summary>
    /// Resize ground cell configuration when grid dimensions change
    /// </summary>
    private void ResizeGroundCells()
    {
        // If empty, initialize
        if (groundCellRows.Count == 0)
        {
            InitializeGroundCells();
            return;
        }

        // Resize X dimension
        if (groundCellRows.Count != gridDimensions.x)
        {
            // If smaller, add new rows
            while (groundCellRows.Count < gridDimensions.x)
            {
                GroundCellRow newRow = new GroundCellRow();
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    newRow.cells.Add(true);
                }
                groundCellRows.Add(newRow);
            }

            // If larger, remove excess rows
            while (groundCellRows.Count > gridDimensions.x)
            {
                groundCellRows.RemoveAt(groundCellRows.Count - 1);
            }
        }

        // Resize Z dimension in each row
        for (int x = 0; x < groundCellRows.Count; x++)
        {
            // If smaller, add new cells
            while (groundCellRows[x].cells.Count < gridDimensions.z)
            {
                groundCellRows[x].cells.Add(true);
            }

            // If larger, remove excess cells
            while (groundCellRows[x].cells.Count > gridDimensions.z)
            {
                groundCellRows[x].cells.RemoveAt(groundCellRows[x].cells.Count - 1);
            }
        }
    }

    /// <summary>
    /// Check if a ground cell is enabled at the given coordinates
    /// </summary>
    public bool HasGroundCell(int x, int z)
    {
        if (x >= 0 && x < gridDimensions.x && z >= 0 && z < gridDimensions.z)
        {
            // Ensure the rows are properly sized
            if (x < groundCellRows.Count && z < groundCellRows[x].cells.Count)
            {
                return groundCellRows[x].cells[z];
            }
        }
        return false;
    }

    /// <summary>
    /// Set the ground cell status at the given coordinates
    /// </summary>
    public void SetGroundCell(int x, int z, bool hasGround)
    {
        if (x >= 0 && x < gridDimensions.x && z >= 0 && z < gridDimensions.z)
        {
            // Ensure the list is properly sized
            if (x < groundCellRows.Count && z < groundCellRows[x].cells.Count)
            {
                groundCellRows[x].cells[z] = hasGround;

                // Update the bottom cell socket if grid is initialized
                if (grid != null)
                {
                    if (hasGround)
                    {
                        grid[x, 0, z].sockets[DirectionToIndex(Direction.Down)] = groundSocketType;
                    }
                    else
                    {
                        grid[x, 0, z].sockets[DirectionToIndex(Direction.Down)] = "";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set all ground cells to the specified state
    /// </summary>
    public void SetAllGroundCells(bool hasGround)
    {
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int z = 0; z < gridDimensions.z; z++)
            {
                SetGroundCell(x, z, hasGround);
            }
        }
    }

    /// <summary>
    /// Initialize the grid with empty cells
    /// </summary>
    private void InitializeGrid()
    {
        // Initialize the grid array
        grid = new GridCell[gridDimensions.x, gridDimensions.y, gridDimensions.z];

        // Calculate the origin position
        Vector3 origin = gridOrigin != null ? gridOrigin.position : transform.position;

        // Create all the cells
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    // Calculate the world position (bottom-left corner)
                    Vector3 worldPos = origin + new Vector3(
                        x * cellSize.x,
                        y * cellSize.y,
                        z * cellSize.z
                    );

                    // Create a new cell
                    Vector3Int gridPos = new Vector3Int(x, y, z);
                    grid[x, y, z] = new GridCell(gridPos, worldPos);

                    // Set ground socket for bottom layer cells if configured
                    if (y == 0 && HasGroundCell(x, z))
                    {
                        grid[x, 0, z].sockets[DirectionToIndex(Direction.Down)] = groundSocketType;
                    }
                }
            }
        }

        //Debug.Log($"Grid initialized with dimensions {gridDimensions}");
    }

    /// <summary>
    /// Tests if a building block can be placed at a position with any rotation
    /// </summary>
    /// <param name="blockData">Block to test</param>
    /// <param name="gridPosition">Position to test</param>
    /// <param name="tryAllRotations">Whether to try all rotations</param>
    /// <param name="useRandomRotation">Whether to try random rotation first</param>
    /// <returns>Tuple with success flag and rotation that worked (0 if none worked)</returns>
    public (bool success, int rotation) TestBlockPlacement(BuildingBlock blockData, Vector3Int gridPosition,
                                                         bool tryAllRotations = true, bool useRandomRotation = false)
    {
        // Check if the position is valid
        if (!IsValidPosition(gridPosition))
        {
            Debug.LogWarning($"Invalid grid position: {gridPosition}");
            return (false, 0);
        }

        // Check if the cell is already occupied
        if (grid[gridPosition.x, gridPosition.y, gridPosition.z].isOccupied)
        {
            Debug.LogWarning($"Cell at {gridPosition} is already occupied");
            return (false, 0);
        }

        // Define the rotations to try (in degrees)
        int[] rotationsToTry;

        // Starting rotation angle
        int startRotation = 0;

        // If random rotation is enabled, choose a random starting rotation
        if (useRandomRotation)
        {
            startRotation = Random.Range(0, 4) * 90;
            //Debug.Log($"Using random starting rotation: {startRotation}°");
        }

        // If we're trying all rotations, create an array of all possible rotations
        // starting from the initial rotation (which may be random) and incrementing by 90 degrees
        if (tryAllRotations)
        {
            rotationsToTry = new int[4];
            for (int i = 0; i < 4; i++)
            {
                rotationsToTry[i] = (startRotation + (i * 90)) % 360;
            }
        }
        else
        {
            // Otherwise, just try the starting rotation
            rotationsToTry = new int[] { startRotation };
        }

        // Try each rotation until one works
        foreach (int yRotation in rotationsToTry)
        {
            // Clone the block data to prevent modifying the original
            BuildingBlock blockDataClone = CloneBuildingBlock(blockData);

            // Apply the horizontal rotation
            ApplyHorizontalYRotation(blockDataClone, yRotation);

            // Test if the block fits with this rotation
            if (AreSocketsCompatible(blockDataClone, gridPosition))
            {
                //Debug.Log($"Block placement successful at {gridPosition} with rotation {yRotation}°");
                return (true, yRotation);
            }
        }

        //Debug.Log($"Block placement failed at {gridPosition} - no valid rotation found");
        return (false, 0);
    }

    /// <summary>
    /// Place a building block in a specific grid cell with a specific rotation
    /// </summary>
    /// <param name="blockData">BuildingBlock data</param>
    /// <param name="gridPosition">Grid position</param>
    /// <param name="yRotation">Y rotation in degrees (0, 90, 180, 270)</param>
    /// <returns>True if placement succeeded</returns>
    public bool PutInCell(BuildingBlock blockData, Vector3Int gridPosition, int yRotation = 0)
    {
        // Check if the position is valid
        if (!IsValidPosition(gridPosition))
        {
            Debug.LogWarning($"Invalid grid position: {gridPosition}");
            return false;
        }

        // Check if the cell is already occupied
        if (grid[gridPosition.x, gridPosition.y, gridPosition.z].isOccupied)
        {
            Debug.LogWarning($"Cell at {gridPosition} is already occupied");
            return false;
        }

        // Clone the block data to prevent modifying the original
        BuildingBlock blockDataClone = CloneBuildingBlock(blockData);

        // Apply the specified rotation
        ApplyHorizontalYRotation(blockDataClone, yRotation);

        // Verify that the block fits with this rotation
        if (!AreSocketsCompatible(blockDataClone, gridPosition))
        {
            Debug.LogWarning($"Block doesn't fit at position {gridPosition} with rotation {yRotation}");
            return false;
        }

        // Get the cell
        GridCell cell = grid[gridPosition.x, gridPosition.y, gridPosition.z];

        // Instantiate the building block
        GameObject blockObject = Instantiate(blockData.Prefab);

        // Align and rotate the building block
        AlignBuildingBlock(blockObject, blockDataClone, cell.worldPosition, yRotation);

        // Update the cell's socket information
        UpdateCellSockets(cell, blockDataClone);

        // Update neighboring cells' sockets
        UpdateNeighborSockets(gridPosition);

        // Mark the cell as occupied and store the building block reference
        cell.isOccupied = true;
        cell.placedBlockObject = blockObject;
        cell.placedBlockData = blockDataClone; // Store the rotated block data

        Debug.Log($"Successfully placed block '{blockData.Name}' at {gridPosition} with {yRotation}° rotation");
        return true;
    }

    /// <summary>
    /// Align a building block with the bottom of a cell, accounting for its down direction
    /// </summary>
    private void AlignBuildingBlock(GameObject blockObject, BuildingBlock blockData, Vector3 cellWorldPosition, float yRotationDegrees = 0f)
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

            // Position the building block so its bottom aligns with the bottom of the cell
            blockObject.transform.position = new Vector3(
                cellWorldPosition.x + (cellSize.x / 2f), // Center X
                cellWorldPosition.y - bottomOffset,      // Align bottom
                cellWorldPosition.z + (cellSize.z / 2f)  // Center Z
            );
        }
        else
        {
            // If no renderer found, just place at cell center with a warning
            blockObject.transform.position = new Vector3(
                cellWorldPosition.x + (cellSize.x / 2f),
                cellWorldPosition.y + (cellSize.y / 2f),
                cellWorldPosition.z + (cellSize.z / 2f)
            );

            // Apply rotation
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

            Debug.LogWarning($"No renderer found on building block '{blockData.Name}' for alignment");
        }
    }

    /// <summary>
    /// Apply the appropriate rotation based on the down direction
    /// This approach uses direct transform rotation rather than quaternion multiplication
    /// </summary>
    private void ApplyDownDirectionRotation(GameObject obj, Direction downDirection)
    {
        switch (downDirection)
        {
            case Direction.Up:
                // For "Up" down direction, rotate 180 degrees around X axis
                obj.transform.Rotate(Vector3.right, 180f);
                break;

            case Direction.Down:
                // Default orientation, no additional rotation needed
                break;

            case Direction.Front:
                // For "Front" down direction, rotate 90 degrees around X axis
                obj.transform.Rotate(Vector3.right, 90f);
                // Apply 180 degree Z rotation to match socket orientation
                //obj.transform.Rotate(Vector3.forward, 180f);
                break;

            case Direction.Back:
                // For "Back" down direction, rotate -90 degrees around X axis
                obj.transform.Rotate(Vector3.right, -90f);
                // Apply 180 degree Z rotation to match socket orientation
                //obj.transform.Rotate(Vector3.forward, 180f);
                break;

            case Direction.Left:
                // For "Left" down direction, rotate -90 degrees around Z axis
                obj.transform.Rotate(Vector3.forward, -90f);
                // Apply 180 degree X rotation to match socket orientation
                //obj.transform.Rotate(Vector3.right, 180f);
                break;

            case Direction.Right:
                // For "Right" down direction, rotate 90 degrees around Z axis
                obj.transform.Rotate(Vector3.forward, 90f);
                // Apply 180 degree X rotation to match socket orientation
                //obj.transform.Rotate(Vector3.right, 180f);
                break;
        }
    }

    /// <summary>
    /// Apply a specific horizontal rotation (around Y axis) to a building block's socket data
    /// </summary>
    private void ApplyHorizontalYRotation(BuildingBlock blockData, int yRotationDegrees)
    {
        // Normalize the rotation to 0, 90, 180, or 270 degrees
        yRotationDegrees = ((yRotationDegrees % 360) + 360) % 360;

        // Skip if rotation is 0 degrees
        if (yRotationDegrees == 0)
            return;

        // Store original socket values
        string originalTop = blockData.TopSocket;
        string originalBottom = blockData.BottomSocket;
        string originalFront = blockData.FrontSocket;
        string originalBack = blockData.BackSocket;
        string originalLeft = blockData.LeftSocket;
        string originalRight = blockData.RightSocket;

        // Socket rotation logic depends on the down direction
        // For all down directions, we need to rotate the horizontal sockets correctly

        // For standard down direction (Down/Up), horizontal rotation is straightforward
        if (blockData.DownDirection == Direction.Down || blockData.DownDirection == Direction.Up)
        {
            switch (yRotationDegrees)
            {
                case 90: // 90 degrees clockwise around Y axis
                    blockData.FrontSocket = originalRight;
                    blockData.RightSocket = originalBack;
                    blockData.BackSocket = originalLeft;
                    blockData.LeftSocket = originalFront;
                    break;

                case 180: // 180 degrees around Y axis
                    blockData.FrontSocket = originalBack;
                    blockData.BackSocket = originalFront;
                    blockData.LeftSocket = originalRight;
                    blockData.RightSocket = originalLeft;
                    break;

                case 270: // 270 degrees clockwise (or 90 counterclockwise) around Y axis
                    blockData.FrontSocket = originalLeft;
                    blockData.LeftSocket = originalBack;
                    blockData.BackSocket = originalRight;
                    blockData.RightSocket = originalFront;
                    break;
            }
        }
        else if (blockData.DownDirection == Direction.Front || blockData.DownDirection == Direction.Back)
        {
            // For Front/Back down direction, Y rotation affects Top/Bottom/Left/Right
            // The top/bottom become the new sides when the building block is oriented this way
            switch (yRotationDegrees)
            {
                case 90:
                    if (blockData.DownDirection == Direction.Front)
                    {
                        blockData.TopSocket = originalRight;
                        blockData.RightSocket = originalBottom;
                        blockData.BottomSocket = originalLeft;
                        blockData.LeftSocket = originalTop;
                    }
                    else // Back
                    {
                        blockData.TopSocket = originalLeft;
                        blockData.LeftSocket = originalBottom;
                        blockData.BottomSocket = originalRight;
                        blockData.RightSocket = originalTop;
                    }
                    break;

                case 180:
                    blockData.TopSocket = originalBottom;
                    blockData.BottomSocket = originalTop;
                    blockData.LeftSocket = originalRight;
                    blockData.RightSocket = originalLeft;
                    break;

                case 270:
                    if (blockData.DownDirection == Direction.Front)
                    {
                        blockData.TopSocket = originalLeft;
                        blockData.LeftSocket = originalBottom;
                        blockData.BottomSocket = originalRight;
                        blockData.RightSocket = originalTop;
                    }
                    else // Back
                    {
                        blockData.TopSocket = originalRight;
                        blockData.RightSocket = originalBottom;
                        blockData.BottomSocket = originalLeft;
                        blockData.LeftSocket = originalTop;
                    }
                    break;
            }
        }
        else if (blockData.DownDirection == Direction.Left || blockData.DownDirection == Direction.Right)
        {
            // For Left/Right down direction, Y rotation affects Top/Bottom/Front/Back
            switch (yRotationDegrees)
            {
                case 90:
                    if (blockData.DownDirection == Direction.Left)
                    {
                        blockData.TopSocket = originalFront;
                        blockData.FrontSocket = originalBottom;
                        blockData.BottomSocket = originalBack;
                        blockData.BackSocket = originalTop;
                    }
                    else // Right
                    {
                        blockData.TopSocket = originalBack;
                        blockData.BackSocket = originalBottom;
                        blockData.BottomSocket = originalFront;
                        blockData.FrontSocket = originalTop;
                    }
                    break;

                case 180:
                    blockData.TopSocket = originalBottom;
                    blockData.BottomSocket = originalTop;
                    blockData.FrontSocket = originalBack;
                    blockData.BackSocket = originalFront;
                    break;

                case 270:
                    if (blockData.DownDirection == Direction.Left)
                    {
                        blockData.TopSocket = originalBack;
                        blockData.BackSocket = originalBottom;
                        blockData.BottomSocket = originalFront;
                        blockData.FrontSocket = originalTop;
                    }
                    else // Right
                    {
                        blockData.TopSocket = originalFront;
                        blockData.FrontSocket = originalBottom;
                        blockData.BottomSocket = originalBack;
                        blockData.BackSocket = originalTop;
                    }
                    break;
            }
        }

        //Debug.Log($"Applied horizontal Y rotation: {yRotationDegrees}° for block with down direction: {blockData.DownDirection}");
    }

    /// <summary>
    /// Apply standard Y rotation for blocks with Down or Up as DownDirection
    /// </summary>
    private void ApplyStandardYRotation(BuildingBlock blockData, int yRotationDegrees)
    {
        // Store original socket values
        string originalTop = blockData.TopSocket;
        string originalBottom = blockData.BottomSocket;
        string originalFront = blockData.FrontSocket;
        string originalBack = blockData.BackSocket;
        string originalLeft = blockData.LeftSocket;
        string originalRight = blockData.RightSocket;

        // Apply rotation based on angle
        switch (yRotationDegrees)
        {
            case 90: // 90 degrees clockwise around Y axis
                blockData.FrontSocket = originalRight;
                blockData.RightSocket = originalBack;
                blockData.BackSocket = originalLeft;
                blockData.LeftSocket = originalFront;
                break;

            case 180: // 180 degrees around Y axis
                blockData.FrontSocket = originalBack;
                blockData.BackSocket = originalFront;
                blockData.LeftSocket = originalRight;
                blockData.RightSocket = originalLeft;
                break;

            case 270: // 270 degrees clockwise (or 90 counterclockwise) around Y axis
                blockData.FrontSocket = originalLeft;
                blockData.LeftSocket = originalBack;
                blockData.BackSocket = originalRight;
                blockData.RightSocket = originalFront;
                break;
        }
    }

    /// <summary>
    /// Apply adapted Y rotation for blocks with alternative Down directions
    /// This method handles the special cases where the block is oriented differently
    /// </summary>
    private void ApplyAdaptedYRotation(BuildingBlock blockData, int yRotationDegrees)
    {
        // Store original socket values
        string originalTop = blockData.TopSocket;
        string originalBottom = blockData.BottomSocket;
        string originalFront = blockData.FrontSocket;
        string originalBack = blockData.BackSocket;
        string originalLeft = blockData.LeftSocket;
        string originalRight = blockData.RightSocket;

        // The rotation needs to be adapted based on the down direction
        switch (blockData.DownDirection)
        {
            case Direction.Front:
                // Front is down, so Y rotation affects different axes
                switch (yRotationDegrees)
                {
                    case 90:
                        blockData.TopSocket = originalLeft;
                        blockData.RightSocket = originalTop;
                        blockData.BottomSocket = originalRight;
                        blockData.LeftSocket = originalBottom;
                        break;
                    case 180:
                        blockData.TopSocket = originalBottom;
                        blockData.RightSocket = originalLeft;
                        blockData.BottomSocket = originalTop;
                        blockData.LeftSocket = originalRight;
                        break;
                    case 270:
                        blockData.TopSocket = originalRight;
                        blockData.RightSocket = originalBottom;
                        blockData.BottomSocket = originalLeft;
                        blockData.LeftSocket = originalTop;
                        break;
                }
                break;

            case Direction.Back:
                // Back is down, so Y rotation affects different axes
                switch (yRotationDegrees)
                {
                    case 90:
                        blockData.TopSocket = originalRight;
                        blockData.RightSocket = originalBottom;
                        blockData.BottomSocket = originalLeft;
                        blockData.LeftSocket = originalTop;
                        break;
                    case 180:
                        blockData.TopSocket = originalBottom;
                        blockData.RightSocket = originalLeft;
                        blockData.BottomSocket = originalTop;
                        blockData.LeftSocket = originalRight;
                        break;
                    case 270:
                        blockData.TopSocket = originalLeft;
                        blockData.RightSocket = originalTop;
                        blockData.BottomSocket = originalRight;
                        blockData.LeftSocket = originalBottom;
                        break;
                }
                break;

            case Direction.Left:
                // Left is down, so Y rotation affects different axes
                switch (yRotationDegrees)
                {
                    case 90:
                        blockData.TopSocket = originalBack;
                        blockData.FrontSocket = originalTop;
                        blockData.BottomSocket = originalFront;
                        blockData.BackSocket = originalBottom;
                        break;
                    case 180:
                        blockData.TopSocket = originalBottom;
                        blockData.FrontSocket = originalBack;
                        blockData.BottomSocket = originalTop;
                        blockData.BackSocket = originalFront;
                        break;
                    case 270:
                        blockData.TopSocket = originalFront;
                        blockData.FrontSocket = originalBottom;
                        blockData.BottomSocket = originalBack;
                        blockData.BackSocket = originalTop;
                        break;
                }
                break;

            case Direction.Right:
                // Right is down, so Y rotation affects different axes
                switch (yRotationDegrees)
                {
                    case 90:
                        blockData.TopSocket = originalFront;
                        blockData.FrontSocket = originalBottom;
                        blockData.BottomSocket = originalBack;
                        blockData.BackSocket = originalTop;
                        break;
                    case 180:
                        blockData.TopSocket = originalBottom;
                        blockData.FrontSocket = originalBack;
                        blockData.BottomSocket = originalTop;
                        blockData.BackSocket = originalFront;
                        break;
                    case 270:
                        blockData.TopSocket = originalBack;
                        blockData.FrontSocket = originalTop;
                        blockData.BottomSocket = originalFront;
                        blockData.BackSocket = originalBottom;
                        break;
                }
                break;
        }

        Debug.Log($"Applied adapted Y rotation: {yRotationDegrees}° for block with down direction: {blockData.DownDirection}");
    }

    /// <summary>
    /// Get the rotation quaternion for a specific direction
    /// </summary>
    private Quaternion GetRotationFromDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return Quaternion.Euler(180, 0, 0); // Upside down
            case Direction.Down:
                return Quaternion.identity; // Default orientation
            case Direction.Front:
                return Quaternion.Euler(-90, 0, 0);
            case Direction.Back:
                return Quaternion.Euler(90, 0, 0);
            case Direction.Left:
                return Quaternion.Euler(0, 0, 90);
            case Direction.Right:
                return Quaternion.Euler(0, 0, -90);
            default:
                return Quaternion.identity;
        }
    }



    /// <summary>
    /// Clone a BuildingBlock to prevent modifying the original
    /// </summary>
    private BuildingBlock CloneBuildingBlock(BuildingBlock original)
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
            RightSocket = original.RightSocket
        };

        return clone;
    }

    /// <summary>
    /// Convert Direction enum to int index for socket arrays
    /// </summary>
    private int DirectionToIndex(Direction direction)
    {
        switch (direction)
        {
            case Direction.Right: return 0;
            case Direction.Left: return 1;
            case Direction.Up: return 2;
            case Direction.Down: return 3;
            case Direction.Front: return 4;
            case Direction.Back: return 5;
            default: return 0;
        }
    }

    /// <summary>
    /// Convert int index to Direction enum
    /// </summary>
    private Direction IndexToDirection(int index)
    {
        switch (index)
        {
            case 0: return Direction.Right;
            case 1: return Direction.Left;
            case 2: return Direction.Up;
            case 3: return Direction.Down;
            case 4: return Direction.Front;
            case 5: return Direction.Back;
            default: return Direction.Right;
        }
    }

    /// <summary>
    /// Get the opposite direction
    /// </summary>
    private Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Right: return Direction.Left;
            case Direction.Left: return Direction.Right;
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Front: return Direction.Back;
            case Direction.Back: return Direction.Front;
            default: return direction;
        }
    }

    /// <summary>
    /// Get the grid position of the neighbor in the specified direction
    /// </summary>
    private Vector3Int GetNeighborPosition(Vector3Int position, Direction direction)
    {
        switch (direction)
        {
            case Direction.Right: return position + new Vector3Int(1, 0, 0);
            case Direction.Left: return position + new Vector3Int(-1, 0, 0);
            case Direction.Up: return position + new Vector3Int(0, 1, 0);
            case Direction.Down: return position + new Vector3Int(0, -1, 0);
            case Direction.Front: return position + new Vector3Int(0, 0, 1);
            case Direction.Back: return position + new Vector3Int(0, 0, -1);
            default: return position;
        }
    }

    /// <summary>
    /// Update a cell's socket information based on a building block
    /// </summary>
    private void UpdateCellSockets(GridCell cell, BuildingBlock blockData)
    {
        // Update socket for each direction
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            int socketIndex = DirectionToIndex(direction);
            string socketType = blockData.GetSocketForDirection(direction);
            cell.sockets[socketIndex] = socketType;
        }
    }

    /// <summary>
    /// Update socket information of neighboring cells
    /// </summary>
    private void UpdateNeighborSockets(Vector3Int position)
    {
        GridCell cell = grid[position.x, position.y, position.z];

        // Update each neighbor's socket
        for (int i = 0; i < 6; i++)
        {
            Direction direction = IndexToDirection(i);
            Vector3Int neighborPos = GetNeighborPosition(position, direction);

            // Skip if neighbor is out of bounds
            if (!IsValidPosition(neighborPos))
                continue;

            // Get the opposite direction's index
            int oppositeIndex = DirectionToIndex(GetOppositeDirection(direction));

            // Update the neighbor's socket
            GridCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
            neighbor.sockets[oppositeIndex] = cell.sockets[i];
        }
    }

    /// <summary>
    /// Check if a building block's bottom socket is compatible with the socket below it
    /// </summary>
    private bool AreSocketsCompatible(BuildingBlock blockData, Vector3Int position)
    {
        // First check all existing neighbors for compatibility
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            // Get the current block's socket for this direction
            string blockSocket = blockData.GetSocketForDirection(direction);

            // Skip directions with empty sockets (they connect to anything)
            if (string.IsNullOrEmpty(blockSocket))
                continue;

            // Get neighbor position
            Vector3Int neighborPos = GetNeighborPosition(position, direction);

            // Skip if neighbor is out of grid bounds
            if (!IsValidPosition(neighborPos))
                continue;

            // Skip if neighbor cell is not occupied
            if (!grid[neighborPos.x, neighborPos.y, neighborPos.z].isOccupied)
                continue;

            // Get the neighbor's socket that would connect to our block
            Direction oppositeDirection = GetOppositeDirection(direction);
            int oppositeIndex = DirectionToIndex(oppositeDirection);

            // Get the socket type from the neighbor
            string neighborSocket = grid[neighborPos.x, neighborPos.y, neighborPos.z].sockets[oppositeIndex];

            // Skip empty neighbor sockets
            if (string.IsNullOrEmpty(neighborSocket))
                continue;

            // Check compatibility
            if (!IsSocketCompatible(blockSocket, neighborSocket))
            {
                //Debug.Log($"Socket incompatible: {blockData.Name} at {position}, direction {direction} " +
                    //           $"socket '{blockSocket}' doesn't connect with neighbor socket '{neighborSocket}'");
                return false;
            }
        }

        // CASE 1: Ground level (y=0) - Check against ground socket
        if (position.y == 0)
        {
            // Get the building block's bottom socket
            string blockBottomSocket = blockData.GetSocketForDirection(Direction.Down);

            // Skip if the block doesn't have a bottom socket
            if (string.IsNullOrEmpty(blockBottomSocket))
            {
                return true; // No bottom socket, so no ground compatibility needed
            }

            // Get the current cell to check its ground socket
            GridCell currentCell = grid[position.x, position.y, position.z];
            string groundSocket = currentCell.sockets[DirectionToIndex(Direction.Down)];

            // If ground socket exists, check compatibility
            if (!string.IsNullOrEmpty(groundSocket))
            {
                bool compatible = IsSocketCompatible(blockBottomSocket, groundSocket);

                if (!compatible)
                {
                    Debug.LogWarning($"Ground socket incompatible: {blockData.Name} with bottom socket '{blockBottomSocket}'" +
                                   $" doesn't connect with ground socket '{groundSocket}'");
                    return false;
                }
            }
            else
            {
                // No ground socket, but block needs one
                Debug.LogWarning($"No ground socket at position {position}, but block {blockData.Name} requires one");
                return false;
            }
        }
        else
        {
            // CASE 2: Above ground - Check socket of cell below if it's occupied
            Vector3Int belowPos = new Vector3Int(position.x, position.y - 1, position.z);

            // Verify the position below is valid
            if (!IsValidPosition(belowPos))
            {
                // This shouldn't happen in normal building as we're checking a cell above ground
                Debug.LogWarning($"Position below {position} is out of bounds");
                return false;
            }

            // Get the building block's bottom socket
            string blockBottomSocket = blockData.GetSocketForDirection(Direction.Down);

            // Skip if the block doesn't have a bottom socket
            if (string.IsNullOrEmpty(blockBottomSocket))
            {
                return true; // No bottom socket, so we're fine
            }

            // Get the cell below
            GridCell cellBelow = grid[belowPos.x, belowPos.y, belowPos.z];

            // If cell below is not occupied, we don't need to check compatibility
            if (!cellBelow.isOccupied)
            {
                return false; // Block requires a bottom connection but there's nothing below
            }

            // Get the top socket of the cell below
            string belowTopSocket = cellBelow.sockets[DirectionToIndex(Direction.Up)];

            // Skip if the cell below doesn't have a top socket
            if (string.IsNullOrEmpty(belowTopSocket))
            {
                Debug.LogWarning($"Cell below has no top socket, but block {blockData.Name} requires one");
                return false;
            }

            // Check compatibility
            bool isCompatible = IsSocketCompatible(blockBottomSocket, belowTopSocket);

            if (!isCompatible)
            {
                Debug.LogWarning($"Bottom socket incompatible: {blockData.Name} with bottom socket '{blockBottomSocket}'" +
                               $" doesn't connect with cell below's top socket '{belowTopSocket}'");
                return false;
            }
        }

        // All sockets are compatible
        return true;
    }

    /// <summary>
    /// Check if two socket types are compatible
    /// </summary>
    private bool IsSocketCompatible(string socketA, string socketB)
    {
        // Check if the socketCompatibilityDict is initialized
        if (socketCompatibilityDict == null)
        {
            Debug.LogError("Socket compatibility dictionary is null!");
            return false;
        }

        // Check if the socket type exists in the dictionary
        if (!socketCompatibilityDict.ContainsKey(socketA))
        {
            Debug.LogWarning($"Socket type '{socketA}' not found in compatibility dictionary");
            return false;
        }

        // Check compatibility
        return socketCompatibilityDict[socketA].Contains(socketB);
    }

    /// <summary>
    /// Check if a grid position is valid
    /// </summary>
    public bool IsValidPosition(Vector3Int position)
    {
        return position.x >= 0 && position.x < gridDimensions.x &&
               position.y >= 0 && position.y < gridDimensions.y &&
               position.z >= 0 && position.z < gridDimensions.z;
    }

    /// <summary>
    /// Get a cell at a specific grid position
    /// </summary>
    public GridCell GetCell(Vector3Int gridPosition)
    {
        if (grid != null && IsValidPosition(gridPosition))
        {
            return grid[gridPosition.x, gridPosition.y, gridPosition.z];
        }
        return null;
    }

    /// <summary>
    /// Remove a building block from a cell and reset its sockets
    /// </summary>
    public void ClearCell(Vector3Int gridPosition)
    {
        if (!IsValidPosition(gridPosition))
            return;

        GridCell cell = grid[gridPosition.x, gridPosition.y, gridPosition.z];

        // Destroy the GameObject
        if (cell.isOccupied && cell.placedBlockObject != null)
        {
            Destroy(cell.placedBlockObject);
        }

        // Reset the cell's occupation status
        cell.isOccupied = false;
        cell.placedBlockObject = null;
        cell.placedBlockData = null;

        // Check each direction and only reset socket if neighbor is empty or out of bounds
        for (int i = 0; i < 6; i++)
        {
            Direction direction = IndexToDirection(i);
            Vector3Int neighborPos = GetNeighborPosition(gridPosition, direction);

            bool resetSocket = true;

            // Keep socket if it's a ground socket on the bottom layer
            if (gridPosition.y == 0 && i == DirectionToIndex(Direction.Down) && HasGroundCell(gridPosition.x, gridPosition.z))
            {
                cell.sockets[i] = groundSocketType;
                resetSocket = false;
            }
            // Keep socket if neighbor exists and is occupied
            else if (IsValidPosition(neighborPos) && grid[neighborPos.x, neighborPos.y, neighborPos.z].isOccupied)
            {
                resetSocket = false;
                // Socket value stays the same to maintain connection with the neighboring block
            }

            // Reset the socket if needed
            if (resetSocket)
            {
                cell.sockets[i] = "";
            }
        }

        // Update neighboring cells' sockets that face this cell
        for (int i = 0; i < 6; i++)
        {
            Direction direction = IndexToDirection(i);
            Vector3Int neighborPos = GetNeighborPosition(gridPosition, direction);

            // Skip if neighbor is out of bounds
            if (!IsValidPosition(neighborPos))
                continue;

            // Get the opposite direction's index
            Direction oppositeDirection = GetOppositeDirection(direction);
            int oppositeIndex = DirectionToIndex(oppositeDirection);

            // Only reset the neighbor's socket if it's not occupied
            GridCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
            if (!neighbor.isOccupied)
            {
                neighbor.sockets[oppositeIndex] = "";
            }
        }

        //Debug.Log($"Cleared cell at {gridPosition} while preserving sockets for occupied neighbors");
    }

    /// <summary>
    /// Draw gizmos for debugging in the editor
    /// </summary>
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Make sure we repaint when the mouse moves for hover effects
        UnityEditor.SceneView.RepaintAll();
#endif

        if (!showGridGizmos)
            return;

        // If grid is not initialized, draw a preview based on settings
        if (grid == null)
        {
            DrawGridPreview();
            return;
        }

        // Draw the grid cells
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        // Get world position of cell center
                        Vector3 cellCenter = grid[x, y, z].worldPosition + (cellSize / 2f);

                        // Color based on whether the cell is occupied
                        Gizmos.color = grid[x, y, z].isOccupied ? Color.green : Color.cyan;

                        // Draw cell wireframe
                        Gizmos.DrawWireCube(cellCenter, cellSize * 0.9f);

                        // Draw sockets if enabled
                        if (showSocketGizmos)
                        {
                            DrawSocketGizmos(grid[x, y, z], cellCenter);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw a preview of the grid in the editor before initialization
    /// </summary>
    private void DrawGridPreview()
    {
        // Calculate the cell size if possible
        Vector3 previewCellSize = Vector3.one;
        if (cellSizePrefab != null)
        {
            Renderer renderer = cellSizePrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                previewCellSize = renderer.bounds.size;
            }
        }

        // Get the origin position
        Vector3 origin = gridOrigin != null ? gridOrigin.position : transform.position;

        // Draw the preview grid
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    Vector3 cellPosition = origin + new Vector3(
                        x * previewCellSize.x,
                        y * previewCellSize.y,
                        z * previewCellSize.z
                    );

                    Vector3 centerPos = cellPosition + (previewCellSize / 2f);
                    Gizmos.DrawWireCube(centerPos, previewCellSize * 0.9f);

                    // Highlight ground cells in the bottom layer
                    if (y == 0 && HasGroundCell(x, z))
                    {
                        Gizmos.color = new Color(0.8f, 0.6f, 0.2f, 0.3f);
                        Gizmos.DrawCube(new Vector3(centerPos.x, centerPos.y - previewCellSize.y / 2.2f, centerPos.z),
                                        new Vector3(previewCellSize.x * 0.9f, 0.05f, previewCellSize.z * 0.9f));
                        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw socket gizmos for a cell
    /// </summary>
    private void DrawSocketGizmos(GridCell cell, Vector3 cellCenter)
    {
#if UNITY_EDITOR
        // Socket visualization settings
        float socketSize = 0.1f;
        Vector3[] socketPositions = new Vector3[6]
        {
            cellCenter + new Vector3(cellSize.x / 2f, 0, 0),      // Right
            cellCenter + new Vector3(-cellSize.x / 2f, 0, 0),     // Left
            cellCenter + new Vector3(0, cellSize.y / 2f, 0),      // Up
            cellCenter + new Vector3(0, -cellSize.y / 2f, 0),     // Down
            cellCenter + new Vector3(0, 0, cellSize.z / 2f),      // Forward
            cellCenter + new Vector3(0, 0, -cellSize.z / 2f)      // Back
        };

        Color[] socketColors = new Color[6]
        {
            Color.red,       // Right
            Color.green,     // Left
            Color.blue,      // Up
            Color.yellow,    // Down
            Color.magenta,   // Front
            Color.cyan       // Back
        };

        // Direction names for labels
        string[] directionNames = new string[6]
        {
            "R", // Right
            "L", // Left
            "U", // Up
            "D", // Down
            "F", // Front
            "B"  // Back
        };

        // Get mouse position in the scene view
        Vector2 mousePos = Event.current.mousePosition;
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);

        // Create a GUIStyle for the text that supports transparency
        GUIStyle labelStyle = new GUIStyle(UnityEditor.EditorStyles.boldLabel);

        // Draw each socket
        for (int i = 0; i < 6; i++)
        {
            // Skip if socket is empty
            if (string.IsNullOrEmpty(cell.sockets[i]))
                continue;

            // Check if mouse is hovering over this socket
            bool isHovered = false;
            float distanceToMouse = UnityEditor.HandleUtility.DistancePointLine(socketPositions[i], ray.origin, ray.origin + ray.direction * 100);
            if (distanceToMouse < socketSize * 3)
            {
                isHovered = true;
            }

            // Base color for the socket (white for non-empty sockets)
            Color socketColor = Color.white;

            // Apply transparency unless hovered
            if (!isHovered)
            {
                // Make socket 60% transparent (alpha 0.4)
                socketColor.a = 0.4f;
            }

            // Draw socket sphere
            Gizmos.color = socketColor;
            Gizmos.DrawSphere(socketPositions[i], socketSize);

            // Calculate label position slightly offset from the socket
            Vector3 labelOffset = (socketPositions[i] - cellCenter).normalized * 0.2f;
            Vector3 labelPos = socketPositions[i] + labelOffset;

            // Create label text with direction and socket type
            string labelText = $"{directionNames[i]}: {cell.sockets[i]}";

            // Set text color with appropriate transparency
            Color textColor = socketColors[i];
            if (!isHovered)
            {
                textColor.a = 0.4f;
            }

            // Set the text color in the style
            labelStyle.normal.textColor = textColor;

            // Draw the text label with the custom style
            UnityEditor.Handles.Label(labelPos, labelText, labelStyle);
        }
#else
        // Draw basic socket visualization for builds
        float socketSize = 0.1f;
        Vector3[] socketPositions = new Vector3[6]
        {
            cellCenter + new Vector3(cellSize.x / 2f, 0, 0),
            cellCenter + new Vector3(-cellSize.x / 2f, 0, 0),
            cellCenter + new Vector3(0, cellSize.y / 2f, 0),
            cellCenter + new Vector3(0, -cellSize.y / 2f, 0),
            cellCenter + new Vector3(0, 0, cellSize.z / 2f),
            cellCenter + new Vector3(0, 0, -cellSize.z / 2f)
        };
        
        for (int i = 0; i < 6; i++)
        {
            if (!string.IsNullOrEmpty(cell.sockets[i]))
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(socketPositions[i], socketSize);
            }
        }
#endif
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom editor for the GridGenerator to add ground cell configuration
/// </summary>
[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    private Vector2 groundConfigScrollPosition;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get the GridGenerator instance
        GridGenerator gridGenerator = (GridGenerator)target;

        // Draw the ground configuration UI if enabled
        if (gridGenerator.showGroundConfig)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ground Cell Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure which cells in the bottom layer have ground sockets", MessageType.Info);

            // Begin scrollable area for the checkboxes
            groundConfigScrollPosition = EditorGUILayout.BeginScrollView(
                groundConfigScrollPosition,
                GUILayout.Height(300)
            );

            // Display column headers
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40); // Space for row labels
            for (int x = 0; x < gridGenerator.gridDimensions.x; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(18));
            }
            EditorGUILayout.EndHorizontal();

            // Display grid of checkboxes (z rows, x columns)
            for (int z = 0; z < gridGenerator.gridDimensions.z; z++)
            {
                EditorGUILayout.BeginHorizontal();

                // Show row header
                GUILayout.Label($"Z: {z}", GUILayout.Width(40));

                for (int x = 0; x < gridGenerator.gridDimensions.x; x++)
                {
                    bool hasGround = gridGenerator.HasGroundCell(x, z);

                    // Toggle checkbox
                    bool newValue = EditorGUILayout.Toggle(hasGround, GUILayout.Width(18));

                    // Update ground cells if changed
                    if (newValue != hasGround)
                    {
                        Undo.RecordObject(gridGenerator, "Change Ground Cell");
                        gridGenerator.SetGroundCell(x, z, newValue);
                        EditorUtility.SetDirty(gridGenerator);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Buttons to select/deselect all
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                Undo.RecordObject(gridGenerator, "Select All Ground Cells");
                gridGenerator.SetAllGroundCells(true);
                EditorUtility.SetDirty(gridGenerator);
            }

            if (GUILayout.Button("Deselect All"))
            {
                Undo.RecordObject(gridGenerator, "Deselect All Ground Cells");
                gridGenerator.SetAllGroundCells(false);
                EditorUtility.SetDirty(gridGenerator);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif