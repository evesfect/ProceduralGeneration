using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
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

        // Initialize the grid
        InitializeGrid();

        // Get socket compatibility dictionary
        socketCompatibilityDict = socketManager.GetSocketCompDict();
    }

    public void OnValidate()
    {
        // Recalculate cell size if needed
        if (cellSizePrefab != null)
        {
            CalculateCellSize();
        }

        // Update the socket compatibility dictionary
        if (socketManager != null)
        {
            socketCompatibilityDict = socketManager.GetSocketCompDict();
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
                Debug.Log($"Cell size calculated from prefab: {cellSize}");
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
                }
            }
        }

        Debug.Log($"Grid initialized with dimensions {gridDimensions}");
    }
    
    // Exposing cell size for other tools
    public Vector3 GetCellSize()
    {
        return cellSize;
    }

    /// <summary>
    /// Place a building block in a specific grid cell
    /// </summary>
    /// <param name="buildingBlockName">Name of the building block from BuildingBlocksManager</param>
    /// <param name="gridPosition">The grid position (x,y,z)</param>
    /// <returns>True if placement was successful, false otherwise</returns>
    public bool PutInCell(string buildingBlockName, Vector3Int gridPosition)
    {
        // Find the building block data
        BuildingBlock blockData = buildingBlocksManager.FindBuildingBlock(buildingBlockName);
        if (blockData == null || blockData.Prefab == null)
        {
            Debug.LogWarning($"Building block '{buildingBlockName}' not found or has no prefab");
            return false;
        }

        return PutInCell(blockData, gridPosition);
    }

    /// <summary>
    /// Place a building block in a specific grid cell
    /// </summary>
    /// <param name="blockData">BuildingBlock data</param>
    /// <param name="gridPosition">The grid position (x,y,z)</param>
    /// <returns>True if placement was successful, false otherwise</returns>
    public bool PutInCell(BuildingBlock blockData, Vector3Int gridPosition)
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

        // Check if sockets are compatible with neighbors
        if (!AreSocketsCompatible(blockData, gridPosition))
        {
            Debug.LogWarning($"Building block '{blockData.Name}' has incompatible sockets with neighboring cells");
            return false;
        }

        // Get the cell
        GridCell cell = grid[gridPosition.x, gridPosition.y, gridPosition.z];

        // Instantiate the building block
        GameObject blockObject = Instantiate(blockData.Prefab);

        // Align and rotate the building block
        AlignBuildingBlock(blockObject, blockData, cell.worldPosition);

        // Update the cell's socket information
        UpdateCellSockets(cell, blockData);

        // Update neighboring cells' sockets
        UpdateNeighborSockets(gridPosition);

        // Mark the cell as occupied and store the building block reference
        cell.isOccupied = true;
        cell.placedBlockObject = blockObject;
        cell.placedBlockData = blockData;

        return true;
    }

    /// <summary>
    /// Align a building block with the bottom of a cell, accounting for its down direction
    /// </summary>
    private void AlignBuildingBlock(GameObject blockObject, BuildingBlock blockData, Vector3 cellWorldPosition)
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
            blockObject.transform.rotation = GetRotationFromDirection(blockData.DownDirection);

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

            Debug.Log($"Building block '{blockData.Name}' aligned at {blockObject.transform.position}");
        }
        else
        {
            // If no renderer found, just place at cell center with a warning
            blockObject.transform.position = new Vector3(
                cellWorldPosition.x + (cellSize.x / 2f),
                cellWorldPosition.y + (cellSize.y / 2f),
                cellWorldPosition.z + (cellSize.z / 2f)
            );
            blockObject.transform.rotation = GetRotationFromDirection(blockData.DownDirection);
            Debug.LogWarning($"No renderer found on building block '{blockData.Name}' for alignment");
        }
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
    /// Get the index of the opposite direction
    /// </summary>
    private int GetOppositeIndex(int index)
    {
        switch (index)
        {
            case 0: return 1; // Right -> Left
            case 1: return 0; // Left -> Right
            case 2: return 3; // Up -> Down
            case 3: return 2; // Down -> Up
            case 4: return 5; // Front -> Back
            case 5: return 4; // Back -> Front
            default: return index;
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
            int oppositeIndex = GetOppositeIndex(i);

            // Update the neighbor's socket
            GridCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
            neighbor.sockets[oppositeIndex] = cell.sockets[i];
        }
    }

    /// <summary>
    /// Check if a building block's sockets are compatible with neighbors
    /// </summary>
    private bool AreSocketsCompatible(BuildingBlock blockData, Vector3Int position)
    {
        // For each direction, check if the socket is compatible with the neighbor
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            Vector3Int neighborPos = GetNeighborPosition(position, direction);

            // Skip if neighbor is out of bounds
            if (!IsValidPosition(neighborPos))
                continue;

            // Skip if neighbor is not occupied
            GridCell neighbor = grid[neighborPos.x, neighborPos.y, neighborPos.z];
            if (!neighbor.isOccupied)
                continue;

            // Get the socket types
            string blockSocket = blockData.GetSocketForDirection(direction);
            int oppositeIndex = DirectionToIndex(GetOppositeDirection(direction));
            string neighborSocket = neighbor.sockets[oppositeIndex];

            // Skip if either socket is empty
            if (string.IsNullOrEmpty(blockSocket) || string.IsNullOrEmpty(neighborSocket))
                continue;

            // Check compatibility
            if (!IsSocketCompatible(blockSocket, neighborSocket))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if two socket types are compatible
    /// </summary>
    private bool IsSocketCompatible(string socketA, string socketB)
    {
        // Use the socket manager's compatibility information
        if (socketCompatibilityDict.ContainsKey(socketA))
        {
            return socketCompatibilityDict[socketA].Contains(socketB);
        }

        return false;
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
        if (IsValidPosition(gridPosition))
        {
            return grid[gridPosition.x, gridPosition.y, gridPosition.z];
        }
        return null;
    }

    /// <summary>
    /// Get the total size of the grid in world units
    /// </summary>
    public Vector3 GetGridWorldSize()
    {
        return new Vector3(
            gridDimensions.x * cellSize.x,
            gridDimensions.y * cellSize.y,
            gridDimensions.z * cellSize.z
        );
    }

    /// <summary>
    /// Remove a building block from a cell and reset its sockets
    /// </summary>
    public void ClearCell(Vector3Int gridPosition)
    {
        if (!IsValidPosition(gridPosition))
            return;

        GridCell cell = grid[gridPosition.x, gridPosition.y, gridPosition.z];

        if (cell.isOccupied && cell.placedBlockObject != null)
        {
            Destroy(cell.placedBlockObject);
        }

        // Reset the cell
        cell.isOccupied = false;
        cell.placedBlockObject = null;
        cell.placedBlockData = null;

        // Reset socket info
        for (int i = 0; i < 6; i++)
        {
            cell.sockets[i] = "";
        }

        // Update neighboring cells' sockets
        UpdateNeighborSockets(gridPosition);
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
        Color.magenta,   // Forward
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
            // Check if mouse is hovering over this socket
            bool isHovered = false;
            float distanceToMouse = UnityEditor.HandleUtility.DistancePointLine(socketPositions[i], ray.origin, ray.origin + ray.direction * 100);
            if (distanceToMouse < socketSize * 3)
            {
                isHovered = true;
            }

            // Base color for the socket
            Color socketColor;

            // If socket has a type, use white; otherwise use the direction color
            if (!string.IsNullOrEmpty(cell.sockets[i]))
            {
                socketColor = Color.white;
            }
            else
            {
                socketColor = socketColors[i];
            }

            // Apply transparency unless hovered
            if (!isHovered)
            {
                // Make socket 60% transparent (alpha 0.4)
                socketColor.a = 0.4f;
            }

            // Draw socket sphere
            Gizmos.color = socketColor;
            Gizmos.DrawSphere(socketPositions[i], socketSize);

            // Display the socket type as text for non-empty sockets
            if (!string.IsNullOrEmpty(cell.sockets[i]))
            {
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
        Gizmos.color = !string.IsNullOrEmpty(cell.sockets[i]) ? Color.white : new Color(1, 1, 1, 0.4f);
        Gizmos.DrawSphere(socketPositions[i], socketSize);
    }
#endif
    }
}