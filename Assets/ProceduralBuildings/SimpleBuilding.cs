using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple example of a building generator that uses the BlockSystemInterface facade.
/// Creates a basic square building with foundation, walls, and a roof.
/// </summary>
public class SimpleBuilding : MonoBehaviour
{
    [Header("Building Generator Settings")]
    [Tooltip("Reference to the block system interface")]
    public BlockSystemInterface blockSystem;

    [Tooltip("Width of the building in cells")]
    [Range(3, 20)]
    public int buildingWidth = 5;

    [Tooltip("Depth of the building in cells")]
    [Range(3, 20)]
    public int buildingDepth = 5;

    [Tooltip("Height of the building in cells")]
    [Range(2, 10)]
    public int buildingHeight = 3;

    [Tooltip("X position of the building in the grid")]
    public int startX = 0;

    [Tooltip("Z position of the building in the grid")]
    public int startZ = 0;

    [Header("Block Types")]
    [Tooltip("Name of the foundation block type")]
    public string foundationBlockName = "EmptyBlock";

    [Tooltip("Name of the wall block type")]
    public string wallBlockName = "EmptyBlock";

    [Tooltip("Name of the door block type")]
    public string doorBlockName = "BlockWithDoor";

    [Tooltip("Name of the window block type")]
    public string windowBlockName = "Block_WindowSmall";

    [Tooltip("Name of the roof block type")]
    public string roofBlockName = "Roof_C1_A_1type";

    [Header("Runtime Controls")]
    [Tooltip("Generate the building when the component starts")]
    public bool generateOnStart = true;

    [Tooltip("Clear any existing blocks before generating")]
    public bool clearExistingBlocks = true;

    [Tooltip("Add windows to the walls")]
    public bool addWindows = true;

    [Tooltip("Add a door to the front wall")]
    public bool addDoor = true;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateBuilding();
        }
    }

    /// <summary>
    /// Generate a complete building
    /// </summary>
    public void GenerateBuilding()
    {
        // Make sure the block system is valid
        if (blockSystem == null)
        {
            blockSystem = GetComponent<BlockSystemInterface>();
            if (blockSystem == null)
            {
                Debug.LogError("SimpleBuilding: No BlockSystemInterface found!");
                return;
            }
        }

        // Initialize the block system
        if (!blockSystem.Initialize())
        {
            Debug.LogError("SimpleBuilding: Failed to initialize the BlockSystemInterface!");
            return;
        }

        // Clear existing blocks if requested
        if (clearExistingBlocks)
        {
            ClearBuildingArea();
        }

        // Generate the building parts in order
        PlaceFoundation();
        PlaceWalls();
        PlaceRoof();

        Debug.Log("Building generation complete!");
    }

    /// <summary>
    /// Clear all blocks in the building area
    /// </summary>
    private void ClearBuildingArea()
    {
        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                for (int y = 0; y < buildingHeight + 1; y++) // +1 for roof
                {
                    Vector3Int position = new Vector3Int(startX + x, y, startZ + z);
                    blockSystem.ClearCell(position);
                }
            }
        }
    }

    /// <summary>
    /// Place the foundation blocks
    /// </summary>
    private void PlaceFoundation()
    {
        BuildingBlock foundationBlock = blockSystem.GetBlockByName(foundationBlockName);

        if (foundationBlock == null)
        {
            Debug.LogWarning($"SimpleBuilding: Foundation block '{foundationBlockName}' not found!");
            return;
        }

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                Vector3Int position = new Vector3Int(startX + x, 0, startZ + z);
                blockSystem.PlaceBlock(foundationBlock, position, tryAllRotations: true);
            }
        }

        Debug.Log("Foundation placed successfully");
    }

    /// <summary>
    /// Place the wall blocks
    /// </summary>
    private void PlaceWalls()
    {
        BuildingBlock wallBlock = blockSystem.GetBlockByName(wallBlockName);
        BuildingBlock windowBlock = addWindows ? blockSystem.GetBlockByName(windowBlockName) : null;
        BuildingBlock doorBlock = addDoor ? blockSystem.GetBlockByName(doorBlockName) : null;

        if (wallBlock == null)
        {
            Debug.LogWarning($"SimpleBuilding: Wall block '{wallBlockName}' not found!");
            return;
        }

        // Place walls for each floor
        for (int y = 1; y < buildingHeight; y++)
        {
            for (int x = 0; x < buildingWidth; x++)
            {
                for (int z = 0; z < buildingDepth; z++)
                {
                    // Only place blocks on the perimeter
                    bool isPerimeter = x == 0 || x == buildingWidth - 1 || z == 0 || z == buildingDepth - 1;

                    if (isPerimeter)
                    {
                        Vector3Int position = new Vector3Int(startX + x, y, startZ + z);

                        // Try to place a door on the front wall at ground level
                        if (addDoor && doorBlock != null && y == 1 && z == 0 && x == buildingWidth / 2)
                        {
                            if (!blockSystem.PlaceBlock(doorBlock, position, tryAllRotations: true))
                            {
                                // If door placement fails, place a wall instead
                                blockSystem.PlaceBlock(wallBlock, position, tryAllRotations: true);
                            }
                        }
                        // Try to place windows on the front and back walls on upper floors
                        else if (addWindows && windowBlock != null && y > 1 && (z == 0 || z == buildingDepth - 1) && x % 2 == 1)
                        {
                            if (!blockSystem.PlaceBlock(windowBlock, position, tryAllRotations: true))
                            {
                                // If window placement fails, place a wall instead
                                blockSystem.PlaceBlock(wallBlock, position, tryAllRotations: true);
                            }
                        }
                        // Place normal walls on the sides
                        else if (addWindows && windowBlock != null && y > 1 && (x == 0 || x == buildingWidth - 1) && z % 2 == 1)
                        {
                            if (!blockSystem.PlaceBlock(windowBlock, position, tryAllRotations: true))
                            {
                                // If window placement fails, place a wall instead
                                blockSystem.PlaceBlock(wallBlock, position, tryAllRotations: true);
                            }
                        }
                        // Default to walls for all other perimeter positions
                        else
                        {
                            blockSystem.PlaceBlock(wallBlock, position, tryAllRotations: true);
                        }
                    }
                }
            }
        }

        Debug.Log("Walls placed successfully");
    }

    /// <summary>
    /// Place the roof blocks
    /// </summary>
    private void PlaceRoof()
    {
        BuildingBlock roofBlock = blockSystem.GetBlockByName(roofBlockName);

        if (roofBlock == null)
        {
            Debug.LogWarning($"SimpleBuilding: Roof block '{roofBlockName}' not found!");
            return;
        }

        // Place flat roof blocks at the top level
        int roofY = buildingHeight;

        for (int x = 0; x < buildingWidth; x++)
        {
            for (int z = 0; z < buildingDepth; z++)
            {
                Vector3Int position = new Vector3Int(startX + x, roofY, startZ + z);
                blockSystem.PlaceBlock(roofBlock, position, tryAllRotations: true);
            }
        }

        Debug.Log("Roof placed successfully");
    }

    /// <summary>
    /// Draw gizmos to visualize the building area
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw a wire cube to show the building dimensions
        Vector3 center = new Vector3(
            startX + buildingWidth / 2f - 0.5f,
            buildingHeight / 2f,
            startZ + buildingDepth / 2f - 0.5f
        );

        Vector3 size = new Vector3(buildingWidth, buildingHeight, buildingDepth);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}