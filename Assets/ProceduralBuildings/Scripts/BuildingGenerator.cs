using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private BlockSystemInterface BlockSystem;
    [SerializeField] private BuildingStyle BStyle;

    private bool isBlockSystemInitialized = false;
    [SerializeField] private string EmptyBlockName = "EmptyBlock";

    [SerializeField] private Vector3Int StartingPosition = new Vector3Int(0, 0, 0);

    private HashSet<Vector3Int> invalidCells = new HashSet<Vector3Int>();

    void Start()
    {
        if (BlockSystem == null) {
            BlockSystem = FindAnyObjectByType<BlockSystemInterface>();
            if (BlockSystem == null )
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
        Dictionary<string, float> BlockWeights = new Dictionary<string, float>();

        foreach (BuildingBlock block in BuildingBlocksList)
        {
            BlockWeights[block.Name] = BStyle.GetWeight(block.Name);       
        }

        Vector3Int dimensions = BlockSystem.GetGridDimensions();
        List<Vector3Int> CurrentCells = new List<Vector3Int>();
        List<Vector3Int> FrontierCells = new List<Vector3Int>();

        BuildingBlock EmptyBlock = BlockSystem.GetBlockByName(EmptyBlockName);
        if (EmptyBlock == null )
        {
            Debug.LogError($"Empty Block by name ({EmptyBlockName}) not found.");
        }

        // Place the initial block
        BlockSystem.PlaceBlock(EmptyBlock, StartingPosition, useRandomRotation: true);
        CurrentCells.Add(StartingPosition);
        int iteration = 0;
        int iterationLimit = 100;
        while (true)
        {
            if (CurrentCells.Count == 0)
            {
                Debug.Log("CurrentCells count is 0, terminating generation");
                break;
            }
            if (iteration < iterationLimit)
            {
                iteration++;
            }
            else
            {
                Debug.Log($"Iteration limit reached: {iterationLimit}");
                break;
            }
            
            // Update FrontierCells list using CurrentCells
            foreach (Vector3Int cell in CurrentCells)
            {
                List<Vector3Int> tempList = BlockSystem.GetEmptyNeighborCellPositions(cell);
                foreach (Vector3Int position in tempList)
                {
                    if (invalidCells.Contains(position))
                        continue;
                    if (!FrontierCells.Contains(position))
                        FrontierCells.Add(position);
                }
            }

            foreach (Vector3Int cell in FrontierCells)
            {

                if (!BlockSystem.IsValidGridPosition(cell))
                {
                    Debug.LogWarning($"Skipping invalid cell position: {cell}");
                    continue;
                }

                if (invalidCells.Contains(cell))
                {
                    continue; // Skip invalid cells
                }

                BuildingBlock newBlock = FindValidBlockForPosition(cell, BuildingBlocksList, tryAllRotations: true, useRandomRotation: true);
                if(newBlock == null)
                {
                    Debug.Log($"Couldn't find a valid block for the cell {cell}");
                    invalidCells.Add(cell);
                    continue;
                }
                else
                {
                    BlockSystem.PlaceBlock(newBlock, cell, tryAllRotations: true, useRandomRotation: true);
                }
                
            
            }
            Debug.Log("Completed an iteration. Values before swapping:");
            Debug.Log($"CurrentCells count is {CurrentCells.Count}");
            Debug.Log($"FrontierCells count is {FrontierCells.Count}");
            
            CurrentCells = new List<Vector3Int>(FrontierCells);
            FrontierCells.Clear();

            Debug.Log($"CurrentCells count after swap is {CurrentCells.Count}");
            Debug.Log($"FrontierCells count after swap is {FrontierCells.Count}");

        }
    }

    /// <summary>
    /// Finds candidate blocks for the cell and then chooses one using the weight values
    /// </summary>
    /// <param name="position"></param>
    /// <param name="candidateBlocks"></param>
    /// <param name="tryAllRotations"></param>
    /// <param name="useRandomRotation"></param>
    /// <returns></returns>
    public BuildingBlock FindValidBlockForPosition(Vector3Int position, List<BuildingBlock> candidateBlocks,
                                                  bool tryAllRotations = true, bool useRandomRotation = false)
    {
        if (candidateBlocks == null || candidateBlocks.Count == 0)
            return null;

        List<BuildingBlock> validCandidateBlocks = new List<BuildingBlock>();

        if (BlockSystem.IsCellOccupied(position))
        {
            Debug.LogError("Tried to find a building block for an already occupied cell");
        }

        float totalWeightSum = 0f;

        // Try each candidate block
        foreach (BuildingBlock block in candidateBlocks)
        {
            // Check if this block is valid at this position
            if (BlockSystem.IsBlockValidForPosition(block, position, tryAllRotations, useRandomRotation))
            {
                validCandidateBlocks.Add(block);
                totalWeightSum += BStyle.GetWeight(block.Name);
            }
        }

        if (validCandidateBlocks.Count <= 0)
        {
            Debug.Log($"Couldn't find a valid block for the cell {position}");
            return null;
        }

        float currentWeightSum = 0f;
        float randomLimiter = Random.Range(0f, totalWeightSum);
        foreach(BuildingBlock block in validCandidateBlocks)
        {
            currentWeightSum += BStyle.GetWeight(block.Name);
            if (currentWeightSum >= randomLimiter)
            {
                Debug.Log($"{block.Name} is chosen with {BStyle.GetWeight(block.Name) / totalWeightSum * 100}% chance.");
                return block;
            }
        }
        Debug.Log("Something went wrong while choosing a block for the cell.");
        return null;
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
