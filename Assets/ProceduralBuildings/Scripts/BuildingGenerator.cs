using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private BlockSystemInterface BlockSystem;

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
        if (BlockSystem.IsCellOccupied(StartingPosition))
        {
            Debug.Log($"Starting Block Placed at {StartingPosition}");
        }
        BlockSystem.GetEmptyNeighborCellPositions(StartingPosition);
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
