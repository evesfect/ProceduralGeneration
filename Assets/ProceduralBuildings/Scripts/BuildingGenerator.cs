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
    private Dictionary<BuildingBlock, int> blockRotations = new Dictionary<BuildingBlock, int>();
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
            //Debug.Log("Initialized block system.");
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
                //Debug.Log("CurrentCells count is 0, terminating generation");
                break;
            }
            if (iteration < iterationLimit)
            {
                iteration++;
            }
            else
            {
                Debug.LogWarning($"Iteration limit reached: {iterationLimit}");
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
                    //Debug.Log($"Couldn't find a valid block for the cell {cell}");
                    invalidCells.Add(cell);
                    continue;
                }
                else
                {
                    BlockSystem.PlaceBlock(newBlock, cell, tryAllRotations: true, useRandomRotation: true);
                }
                
            
            }
            //Debug.Log("Completed an iteration. Values before swapping:");
            //Debug.Log($"CurrentCells count is {CurrentCells.Count}");
            //Debug.Log($"FrontierCells count is {FrontierCells.Count}");
            
            CurrentCells = new List<Vector3Int>(FrontierCells);
            FrontierCells.Clear();

            //Debug.Log($"CurrentCells count after swap is {CurrentCells.Count}");
            //Debug.Log($"FrontierCells count after swap is {FrontierCells.Count}");

        }
    }

    /// <summary>
    /// Finds candidate blocks for the cell and then chooses one using the weight values
    /// </summary>
    public BuildingBlock FindValidBlockForPosition(Vector3Int position, List<BuildingBlock> candidateBlocks,
                                                  bool tryAllRotations = true, bool useRandomRotation = false)
    {
        if (candidateBlocks == null || candidateBlocks.Count == 0)
            return null;

        List<(BuildingBlock block, int rotation, float weight)> validBlocks = new List<(BuildingBlock, int, float)>();
        float totalWeightSum = 0f;

        if (BlockSystem.IsCellOccupied(position))
        {
            Debug.LogError("Tried to find a building block for an already occupied cell");
            return null;
        }

        // Find all valid blocks and their rotations
        foreach (BuildingBlock block in candidateBlocks)
        {
            var (isValid, rotation) = BlockSystem.CheckBlockValidForPosition(
                block, position, tryAllRotations, useRandomRotation);

            if (isValid)
            {
                float weight = BStyle.GetWeight(block.Name);
                validBlocks.Add((block, rotation, weight));
                totalWeightSum += weight;
                Debug.Log($"Found valid block {block.Name} with rotation {rotation}° and weight {weight}");
            }
        }

        if (validBlocks.Count <= 0)
        {
            Debug.Log($"Couldn't find a valid block for the cell {position}");
            return null;
        }

        // Choose a block based on weights
        float randomLimiter = Random.Range(0f, totalWeightSum);
        float currentWeightSum = 0f;

        foreach (var (block, rotation, weight) in validBlocks)
        {
            currentWeightSum += weight;
            if (currentWeightSum >= randomLimiter)
            {
                Debug.Log($"{block.Name} is chosen with {weight / totalWeightSum * 100}% chance. Will be placed with rotation {rotation}°");

                // Store rotation in the block for later use
                BuildingBlock selectedBlock = CloneBlock(block);
                selectedBlock.CurrentRotation = rotation;
                return selectedBlock;
            }
        }

        Debug.LogError("Something went wrong while choosing a block for the cell.");
        return null;
    }

    // Helper to clone a block with its successful rotation
    private BuildingBlock CloneBlock(BuildingBlock original)
    {
        return new BuildingBlock
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
            CurrentRotation = original.CurrentRotation
        };
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
