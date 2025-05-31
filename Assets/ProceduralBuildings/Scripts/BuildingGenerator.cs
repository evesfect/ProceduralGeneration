using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] public BlockSystemInterface BlockSystem;
    [SerializeField] private BuildingStyle BStyle;
    [SerializeField] private BlockOrientationManager OrientationManager;
    [SerializeField] private BlockRulesManager rulesManager;

    private bool isBlockSystemInitialized = false;
    [SerializeField] private string EmptyBlockName = "EmptyBlock";

    [SerializeField] private Vector3Int StartingPosition = new Vector3Int(0, 0, 0);

    [Header("Debug Settings")]
    [Tooltip("Delay between block placements (in seconds) for visualization")]
    [SerializeField] private float placementDelay = 0f;

    [Tooltip("Enable delayed placement for debugging")]
    [SerializeField] private bool enableDelayedPlacement = false;

    // New parameters for spatial weight calculations
    [Header("Spatial Weight Settings")]
    [Tooltip("The center position for distance calculations")]
    [SerializeField] private Vector2Int CenterPosition = new Vector2Int(5, 5);

    [Tooltip("Maximum distance from center used to normalize distance values")]
    [SerializeField] private float MaxDistance = 10f;

    private HashSet<Vector3Int> invalidCells = new HashSet<Vector3Int>();
    private Dictionary<BuildingBlock, int> blockRotations = new Dictionary<BuildingBlock, int>();

    void Start()
    {
        if (BlockSystem == null)
        {
            BlockSystem = FindFirstObjectByType<BlockSystemInterface>();
            if (BlockSystem == null)
            {
                Debug.LogError("Block System Interface could not be found.");
                return;
            }
        }

        if (OrientationManager == null)
        {
            OrientationManager = FindFirstObjectByType<BlockOrientationManager>();
            if (OrientationManager == null)
            {
                Debug.LogWarning("BlockOrientationManager not found. Blocks may not be correctly oriented.");
            }
        }

        // Ensure GridGenerator has access to the orientation manager
        if (OrientationManager != null && BlockSystem.gridGenerator != null)
        {
            BlockSystem.gridGenerator.blockOrientationManager = OrientationManager;
        }

        isBlockSystemInitialized = BlockSystem.Initialize();
        if (!isBlockSystemInitialized)
        {
            Debug.LogError("Failed to initialize block system.");
        }
        else
        {
            Debug.Log("Initialized block system with orientation manager.");
        }

        if (enableDelayedPlacement)
        {
            StartCoroutine(StartGenerationNextFrame());
        }
        else
        {
            GenerateBuildingImmediate();
        }
    }

    private IEnumerator StartGenerationNextFrame()
    {
        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        // Then start the generation
        StartCoroutine(GenerateBuildingWithDelay());
    }

    public void GenerateBuildingImmediate()
    {
        if (!isBlockSystemInitialized)
        {
            Debug.LogError("Tried to generate building without block system initialization.");
            return;
        }

        List<BuildingBlock> BuildingBlocksList = BlockSystem.GetAllBuildingBlocks();
        Vector3Int dimensions = BlockSystem.GetGridDimensions();
        List<Vector3Int> CurrentCells = new List<Vector3Int>();
        List<Vector3Int> FrontierCells = new List<Vector3Int>();

        BuildingBlock EmptyBlock = BlockSystem.GetBlockByName(EmptyBlockName);
        if (EmptyBlock == null)
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
                Debug.Log("Building generation complete - no more cells to process");
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
                if (newBlock == null)
                {
                    //Debug.Log($"Couldn't find a valid block for the cell {cell}");
                    invalidCells.Add(cell);
                    continue;
                }
                else
                {
                    if (placementDelay > 0)
                    {
                        System.Threading.Thread.Sleep((int)(placementDelay * 1000));
                    }
                    BlockSystem.PlaceBlock(newBlock, cell, tryAllRotations: true, useRandomRotation: true);
                    if (rulesManager != null)
                    {
                        rulesManager.NotifyBlockPlaced(newBlock, newBlock.CurrentRotation, cell, this);
                    }
                }
            }

            CurrentCells = new List<Vector3Int>(FrontierCells);
            FrontierCells.Clear();
        }
    }

    private IEnumerator GenerateBuildingWithDelay()
    {
        List<BuildingBlock> BuildingBlocksList = BlockSystem.GetAllBuildingBlocks();
        Vector3Int dimensions = BlockSystem.GetGridDimensions();
        List<Vector3Int> CurrentCells = new List<Vector3Int>();
        List<Vector3Int> FrontierCells = new List<Vector3Int>();

        BuildingBlock EmptyBlock = BlockSystem.GetBlockByName(EmptyBlockName);
        if (EmptyBlock == null)
        {
            Debug.LogError($"Empty Block by name ({EmptyBlockName}) not found.");
            yield break;
        }

        // Place the initial block
        BlockSystem.PlaceBlock(EmptyBlock, StartingPosition, useRandomRotation: true);
        Debug.Log($"Placed initial block: {EmptyBlockName} at {StartingPosition}");
        CurrentCells.Add(StartingPosition);
        yield return new WaitForSeconds(placementDelay);

        int iteration = 0;
        int iterationLimit = 100;
        while (true)
        {
            if (CurrentCells.Count == 0)
            {
                Debug.Log("Building generation complete - no more cells to process");
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
                if (newBlock == null)
                {
                    //Debug.Log($"Couldn't find a valid block for the cell {cell}");
                    invalidCells.Add(cell);
                    continue;
                }
                else
                {
                    BlockSystem.PlaceBlock(newBlock, cell, tryAllRotations: true, useRandomRotation: true);
                    //Debug.Log($"Placed block: {newBlock.Name} at {cell} with rotation {newBlock.CurrentRotation}°");
                    if (rulesManager != null)
                    {
                        rulesManager.NotifyBlockPlaced(newBlock, newBlock.CurrentRotation, cell, this);
                    }
                    yield return new WaitForSeconds(placementDelay);
                }
            }

            CurrentCells = new List<Vector3Int>(FrontierCells);
            FrontierCells.Clear();
        }
    }

    /// <summary>
    /// Finds candidate blocks for the cell and then chooses one using the weight values
    /// Enhanced to use spatial factors for weight calculation and apply placement rules
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

        // Calculate normalized height for weight adjustments
        float normalizedHeight = CalculateNormalizedHeight(position);

        // Calculate normalized distance from center for weight adjustments
        float normalizedDistance = CalculateNormalizedDistance(position);

        // Find all valid blocks and their rotations
        foreach (BuildingBlock block in candidateBlocks)
        {
            var (isValid, rotation) = BlockSystem.CheckBlockValidForPosition(
                block, position, tryAllRotations, useRandomRotation);

            if (isValid && rulesManager != null)
            {
                isValid = rulesManager.IsPlacementLegal(block, rotation, position, this);
            }

            if (isValid)
            {
                float weight = BStyle.GetWeight(block.Name, normalizedHeight, normalizedDistance);
                validBlocks.Add((block, rotation, weight));
                totalWeightSum += weight;

                string ruleInfo = rulesManager != null ? " (passes rules)" : "";
                Debug.Log($"Found valid block {block.Name} with rotation {rotation}° and weight {weight}{ruleInfo}");

            }
        }

        if (validBlocks.Count <= 0)
        {
            //Debug.Log($"Couldn't find a valid block for the cell {position}");
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
                Debug.Log($"{block.Name} is chosen with {weight / totalWeightSum * 100:F1}% chance. Will be placed with rotation {rotation}°");

                // Store rotation in the block for later use
                BuildingBlock selectedBlock = CloneBlock(block);
                selectedBlock.CurrentRotation = rotation;
                return selectedBlock;
            }
        }

        Debug.LogError("Something went wrong while choosing a block for the cell.");
        return null;
    }

    // Calculate the normalized height (0-1) for a position
    private float CalculateNormalizedHeight(Vector3Int position)
    {
        // Get total height of the building
        float maxHeight = BlockSystem.GetGridDimensions().y;

        // Ensure we don't divide by zero
        if (maxHeight <= 0) return 0;

        // Return normalized height (0 at bottom, 1 at top)
        return Mathf.Clamp01((float)position.y / maxHeight);
    }

    // Calculate the normalized distance from center (0-1) for a position
    private float CalculateNormalizedDistance(Vector3Int position)
    {
        // Calculate 2D distance from center (ignoring height)
        float distance = Vector2.Distance(
            new Vector2(position.x, position.z),
            new Vector2(CenterPosition.x, CenterPosition.y)
        );

        // Normalize by max distance (0 at center, 1 at max distance)
        return Mathf.Clamp01(distance / MaxDistance);
    }

    // Helper to clone a block with its successful rotation
    private BuildingBlock CloneBlock(BuildingBlock original)
    {
        return new BuildingBlock
        {
            Name = original.Name,
            Prefab = original.Prefab,
            TopSocket = original.TopSocket,
            BottomSocket = original.BottomSocket,
            FrontSocket = original.FrontSocket,
            BackSocket = original.BackSocket,
            LeftSocket = original.LeftSocket,
            RightSocket = original.RightSocket,
            CurrentRotation = original.CurrentRotation
        };
    }
}