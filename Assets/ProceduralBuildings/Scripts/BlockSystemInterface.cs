using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Facade for the procedural building block placement system.
/// Provides simplified access to the grid and block placement functionality
/// for custom building generation algorithms.
/// </summary>
public class BlockSystemInterface : MonoBehaviour
{
    [Header("Core System References")]
    [Tooltip("Reference to the GridGenerator")]
    public GridGenerator gridGenerator;

    [Tooltip("Reference to the BuildingBlocksManager asset")]
    public BuildingBlocksManager buildingBlocksManager;

    [Tooltip("Reference to the SocketManager asset")]
    public SocketManager socketManager;

    /// <summary>
    /// Initialize the system. Call this before using any other methods.
    /// </summary>
    /// <returns>True if initialization was successful</returns>
    public bool Initialize()
    {
        // Validate references
        if (gridGenerator == null)
        {
            Debug.LogError("BlockSystemInterface: GridGenerator reference not assigned!");
            return false;
        }

        if (buildingBlocksManager == null)
        {
            Debug.LogError("BlockSystemInterface: BuildingBlocksManager reference not assigned!");
            return false;
        }

        if (socketManager == null)
        {
            Debug.LogError("BlockSystemInterface: SocketManager reference not assigned!");
            return false;
        }

        return true;
    }

    #region Grid Information

    /// <summary>
    /// Get the dimensions of the grid in cells (X, Y, Z)
    /// </summary>
    public Vector3Int GetGridDimensions()
    {
        return gridGenerator.gridDimensions;
    }

    /// <summary>
    /// Get the size of each cell in the grid
    /// </summary>
    public Vector3 GetCellSize()
    {
        return gridGenerator.GetCellSize();
    }

    /// <summary>
    /// Check if a position is within the valid grid bounds
    /// </summary>
    public bool IsValidGridPosition(Vector3Int position)
    {
        return gridGenerator.IsValidPosition(position);
    }

    /// <summary>
    /// Get a grid cell at the specified position
    /// </summary>
    public GridGenerator.GridCell GetCell(Vector3Int position)
    {
        return gridGenerator.GetCell(position);
    }

    /// <summary>
    /// Check if a grid position is occupied by a building block
    /// </summary>
    public bool IsCellOccupied(Vector3Int position)
    {
        GridGenerator.GridCell cell = GetCell(position);
        return cell != null && cell.isOccupied;
    }

    /// <summary>
    /// Get information about the building block at a grid position
    /// </summary>
    /// <param name="position">Grid position to check</param>
    /// <returns>BuildingBlock if the cell is occupied, null otherwise</returns>
    public BuildingBlock GetBlockAtPosition(Vector3Int position)
    {
        GridGenerator.GridCell cell = GetCell(position);
        if (cell != null && cell.isOccupied)
        {
            return cell.placedBlockData;
        }
        return null;
    }

    /// <summary>
    /// Get the socket type at a specific grid position and direction
    /// </summary>
    public string GetSocketAtPosition(Vector3Int position, Direction direction)
    {
        GridGenerator.GridCell cell = GetCell(position);
        if (cell != null)
        {
            int socketIndex = DirectionToIndex(direction);
            return cell.sockets[socketIndex];
        }
        return null;
    }

    /// <summary>
    /// Check if a ground cell is enabled at the given coordinates
    /// </summary>
    public bool HasGroundCell(int x, int z)
    {
        return gridGenerator.HasGroundCell(x, z);
    }

    #endregion

    #region Block Placement

    /// <summary>
    /// Place a building block in the grid at the specified position
    /// </summary>
    /// <param name="blockName">Name of the building block from BuildingBlocksManager</param>
    /// <param name="position">Grid position to place the block</param>
    /// <param name="tryAllRotations">Whether to try all rotations to find a valid placement</param>
    /// <param name="useRandomRotation">Whether to use random rotation for initial placement attempt</param>
    /// <returns>True if placement was successful</returns>
    public bool PlaceBlock(string blockName, Vector3Int position, bool tryAllRotations = false, bool useRandomRotation = false)
    {
        // Find the building block
        BuildingBlock block = buildingBlocksManager.FindBuildingBlock(blockName);
        if (block == null)
        {
            Debug.LogWarning($"BlockSystemInterface: Block '{blockName}' not found");
            return false;
        }

        return PlaceBlock(block, position, tryAllRotations, useRandomRotation);
    }

    /// <summary>
    /// Place a building block in the grid at the specified position
    /// </summary>
    /// <param name="block">BuildingBlock to place</param>
    /// <param name="position">Grid position to place the block</param>
    /// <param name="tryAllRotations">Whether to try all rotations to find a valid placement</param>
    /// <param name="useRandomRotation">Whether to use random rotation for initial placement attempt</param>
    /// <returns>True if placement was successful</returns>
    public bool PlaceBlock(BuildingBlock block, Vector3Int position, bool tryAllRotations = false, bool useRandomRotation = false)
    {
        // Cache original rotation settings
        bool originalAutoRotation = gridGenerator.enableAutoRotation;
        bool originalRandomRotation = gridGenerator.enableRandomRotation;

        // Apply temporary settings
        gridGenerator.enableAutoRotation = tryAllRotations;
        gridGenerator.enableRandomRotation = useRandomRotation;

        // Try to place the block
        bool success = gridGenerator.PutInCell(block, position);

        // Restore original settings
        gridGenerator.enableAutoRotation = originalAutoRotation;
        gridGenerator.enableRandomRotation = originalRandomRotation;

        return success;
    }

    /// <summary>
    /// Remove a building block from a cell
    /// </summary>
    public void ClearCell(Vector3Int position)
    {
        gridGenerator.ClearCell(position);
    }

    /// <summary>
    /// Try to find a valid building block for a specific grid position
    /// </summary>
    /// <param name="position">Grid position to check</param>
    /// <param name="candidateBlocks">List of building blocks to try</param>
    /// <param name="tryAllRotations">Whether to try all rotations for each block</param>
    /// <param name="useRandomRotation">Whether to use random rotation for initial attempt</param>
    /// <returns>The first valid building block or null if none found</returns>
    public BuildingBlock FindValidBlockForPosition(Vector3Int position, List<BuildingBlock> candidateBlocks,
                                                  bool tryAllRotations = true, bool useRandomRotation = false)
    {
        if (candidateBlocks == null || candidateBlocks.Count == 0)
            return null;

        // If the cell is already occupied, it's not valid
        if (IsCellOccupied(position))
            return null;

        // Try each candidate block
        foreach (BuildingBlock block in candidateBlocks)
        {
            // Check if this block is valid at this position
            if (IsBlockValidForPosition(block, position, tryAllRotations, useRandomRotation))
            {
                return block;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a building block can be placed at a specific position
    /// </summary>
    /// <param name="block">Building block to check</param>
    /// <param name="position">Grid position to check</param>
    /// <param name="tryAllRotations">Whether to try all rotations</param>
    /// <param name="useRandomRotation">Whether to use random rotation</param>
    /// <returns>True if the block can be placed at the position</returns>
    public bool IsBlockValidForPosition(BuildingBlock block, Vector3Int position,
                                       bool tryAllRotations = true, bool useRandomRotation = false)
    {
        // If the cell is already occupied, it's not valid
        if (IsCellOccupied(position))
            return false;

        // We need to check if the block would fit, but without actually placing it
        // This is a bit tricky without implementing the internal logic of GridGenerator

        // Easiest approach: simulate the placement in a temp grid, then clear the cell
        bool originalAutoRotation = gridGenerator.enableAutoRotation;
        bool originalRandomRotation = gridGenerator.enableRandomRotation;

        gridGenerator.enableAutoRotation = tryAllRotations;
        gridGenerator.enableRandomRotation = useRandomRotation;

        bool canPlace = gridGenerator.PutInCell(block, position);

        if (canPlace)
        {
            // Clear the cell if we successfully placed it
            gridGenerator.ClearCell(position);
        }

        // Restore original settings
        gridGenerator.enableAutoRotation = originalAutoRotation;
        gridGenerator.enableRandomRotation = originalRandomRotation;

        return canPlace;
    }

    #endregion

    #region Block Collection Management

    /// <summary>
    /// Get all available building blocks
    /// </summary>
    public List<BuildingBlock> GetAllBuildingBlocks()
    {
        return buildingBlocksManager.BuildingBlocks;
    }

    /// <summary>
    /// Find a building block by name
    /// </summary>
    public BuildingBlock GetBlockByName(string name)
    {
        return buildingBlocksManager.FindBuildingBlock(name);
    }

    /// <summary>
    /// Get building blocks that have a specific socket type in a specific direction
    /// </summary>
    public List<BuildingBlock> GetBlocksWithSocket(string socketType, Direction direction)
    {
        List<BuildingBlock> result = new List<BuildingBlock>();

        foreach (BuildingBlock block in buildingBlocksManager.BuildingBlocks)
        {
            string blockSocket = block.GetSocketForDirection(direction);
            if (blockSocket == socketType)
            {
                result.Add(block);
            }
        }

        return result;
    }

    /// <summary>
    /// Get all blocks that have a non-empty socket in the specified direction
    /// </summary>
    public List<BuildingBlock> GetBlocksWithAnySocket(Direction direction)
    {
        List<BuildingBlock> result = new List<BuildingBlock>();

        foreach (BuildingBlock block in buildingBlocksManager.BuildingBlocks)
        {
            string blockSocket = block.GetSocketForDirection(direction);
            if (!string.IsNullOrEmpty(blockSocket))
            {
                result.Add(block);
            }
        }

        return result;
    }

    /// <summary>
    /// Filter blocks that have a compatible socket with the specified socket type
    /// </summary>
    public List<BuildingBlock> GetBlocksWithCompatibleSocket(string socketType, Direction direction)
    {
        List<BuildingBlock> result = new List<BuildingBlock>();
        Dictionary<string, List<string>> compatDict = socketManager.GetSocketCompDict();

        // If the socket type doesn't exist, return empty list
        if (!compatDict.ContainsKey(socketType))
            return result;

        // Get list of compatible socket types
        List<string> compatibleSockets = compatDict[socketType];

        foreach (BuildingBlock block in buildingBlocksManager.BuildingBlocks)
        {
            string blockSocket = block.GetSocketForDirection(direction);
            if (!string.IsNullOrEmpty(blockSocket) && compatibleSockets.Contains(blockSocket))
            {
                result.Add(block);
            }
        }

        return result;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get the grid position in a specific direction from a starting position
    /// </summary>
    public Vector3Int GetNeighborPosition(Vector3Int position, Direction direction)
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
    /// Get the opposite of a direction
    /// </summary>
    public Direction GetOppositeDirection(Direction direction)
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
    /// Check if two socket types are compatible
    /// </summary>
    public bool AreSocketsCompatible(string socketA, string socketB)
    {
        Dictionary<string, List<string>> compatDict = socketManager.GetSocketCompDict();

        // Check if the socket types exist in the dictionary
        if (!compatDict.ContainsKey(socketA))
            return false;

        // Check compatibility
        return compatDict[socketA].Contains(socketB);
    }

    /// <summary>
    /// Get all socket types that are compatible with a specified socket type
    /// </summary>
    public List<string> GetCompatibleSockets(string socketType)
    {
        Dictionary<string, List<string>> compatDict = socketManager.GetSocketCompDict();

        if (compatDict.ContainsKey(socketType))
        {
            return new List<string>(compatDict[socketType]);
        }

        return new List<string>();
    }


    /// <summary>
    /// Returns a list of the positions of the empty neighbouring cells from a position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public List<Vector3Int> GetEmptyNeighborCellPositions(Vector3Int position)
    {
        List<Vector3Int> CellList = new List<Vector3Int>();

        Vector3Int dimensions = GetGridDimensions();

        if ((position.x - 1) >= 0)
        {
            Vector3Int tempPosition = new Vector3Int(position.x - 1, position.y, position.z);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }

        if ((position.x + 1) < dimensions.x)
        {
            Vector3Int tempPosition = new Vector3Int(position.x + 1, position.y, position.z);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }

        if ((position.z - 1) >= 0)
        {
            Vector3Int tempPosition = new Vector3Int(position.x, position.y, position.z - 1);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }

        if ((position.z + 1) < dimensions.z)
        {
            Vector3Int tempPosition = new Vector3Int(position.x, position.y, position.z + 1);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }

        if ((position.y - 1) >= 0)
        {
            Vector3Int tempPosition = new Vector3Int(position.x, position.y - 1, position.z);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }

        if ((position.y + 1) < dimensions.y)
        {
            Vector3Int tempPosition = new Vector3Int(position.x, position.y + 1, position.z);
            if (!IsCellOccupied(tempPosition))
            {
                CellList.Add(tempPosition);
            }
        }
        
        foreach(Vector3Int npos in CellList)
        {
            Debug.Log($"Empty neighbour cell found at location {npos}.");
        }

        return CellList;
    }

    #endregion
}