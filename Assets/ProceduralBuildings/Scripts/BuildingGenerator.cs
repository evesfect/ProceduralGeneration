using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private BlockSystemInterface BlockSystem;
    [SerializeField] private BuildingStyle buildingStyle;

    private bool isBlockSystemInitialized = false;
    [SerializeField] private string EmptyBlockName = "EmptyBlock";

    [SerializeField] private Vector3Int StartingPosition = new Vector3Int(0, 0, 0);

    void Start()
    {
        if (BlockSystem == null) {
            BlockSystem = FindAnyObjectByType<BlockSystemInterface>();
            if (BlockSystem != null )
            {
                Debug.LogError("Block System Interface could not be found.");
                return;
            }
        }

        isBlockSystemInitialized = BlockSystem.Initialize();
        if (!isBlockSystemInitialized )
        {
            Debug.LogError("Failed to initialize block system.");
        } else
        {
            Debug.Log("Initialized block system.");
        }
        GenerateBuilding();
    }

    public void GenerateBuilding()
    {
        if (!isBlockSystemInitialized )
        {
            Debug.LogError("Tried to generate building without block system initialization.");
            return;
        }

        List<BuildingBlock> BuildingBlocksList = BlockSystem.GetAllBuildingBlocks();

        Vector3Int dimensions = BlockSystem.GetGridDimensions();
        List<Vector3Int> CurrentCells = new List<Vector3Int>();
        List<Vector3Int> FrontierCells = new List<Vector3Int>();

        BuildingBlock EmptyBlock = BlockSystem.GetBlockByName(EmptyBlockName);
        if (EmptyBlock != null )
        {
            Debug.LogError($"Empty Block by name ({EmptyBlockName}) not found.");
        }

        // Place the initial block
        BlockSystem.PlaceBlock(EmptyBlock, StartingPosition, useRandomRotation: true);
        CurrentCells.Add(StartingPosition);

        while (true)
        {
            // Update FrontierCells list using CurrentCells
            foreach (Vector3Int cell in CurrentCells)
            {
                List<Vector3Int> tempList = BlockSystem.GetEmptyNeighborCellPositions(cell);
                foreach (Vector3Int position in tempList)
                {
                    if (!FrontierCells.Contains(position)) FrontierCells.Add(position);
                }
            }

            foreach (Vector3Int cell in FrontierCells)
            {
                BuildingBlock newBlock = BlockSystem.FindValidBlockForPosition(cell, BuildingBlocksList, tryAllRotations: true, useRandomRotation: true);
                
            
            }
        }
    }

    public BuildingBlock FindValidBlockForPosition(Vector3Int position, List<BuildingBlock> candidateBlocks,
                                                  bool tryAllRotations = true, bool useRandomRotation = false)
    {
        if (candidateBlocks == null || candidateBlocks.Count == 0)
            return null;

        if (BlockSystem.IsCellOccupied(position))
        {
            Debug.LogError("Tried to find a building block for an already occupied cell");
        }

        // Try each candidate block
        foreach (BuildingBlock block in candidateBlocks)
        {
            // Check if this block is valid at this position
            if (BlockSystem.IsBlockValidForPosition(block, position, tryAllRotations, useRandomRotation))
            {
                return block;
            }
        }

        return null;
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
