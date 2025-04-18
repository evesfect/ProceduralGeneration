using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rule that prevents regular building blocks from being placed if they would
/// create an invalid situation for roof blocks with incompatible sockets.
/// </summary>
[CreateAssetMenu(fileName = "NewFullBlockRule", menuName = "ProceduralBuildings/Rules/FullBlockRule")]
public class RegularBlockRoofRule : BlockPlacementRule
{
    [Header("Socket Settings")]
    [Tooltip("Socket type to check for in the cell above")]
    public string incompatibleSocketType = "incompatible";
    public string full_topSocketType = "Full_Top";

    [Tooltip("Part of name to identify roof blocks")]
    public string roofBlockNameContains = "Roof";

    [Header("Debug")]
    [Tooltip("Enable detailed logging for debugging")]
    public bool debugLogging = true;

    private string failureReason = "";

    public override bool EvaluatePlacement(BuildingBlock blockData, Vector3Int position, int rotation, BlockSystemInterface blockSystem)
    {
        if (!isEnabled)
            return true;

        string topSocketType = blockData.GetSocketForDirection(Direction.Up);
        if (topSocketType != full_topSocketType)
        {
            return true;
        }

        if (debugLogging)
        {
            Debug.Log($"Block {blockData.Name} has {full_topSocketType} as its top socket, so rule is applied");
        }

        // Get the cell above this position
        Vector3Int abovePos = blockSystem.GetNeighborPosition(position, Direction.Up);

        if (debugLogging)
            Debug.Log($"Checking cell above position {position}: {abovePos}");

        // If there's no cell above, we're fine
        if (!blockSystem.IsValidGridPosition(abovePos))
            return true;

        // The cell above should be empty for this rule to apply
        if (blockSystem.IsCellOccupied(abovePos))
            return true;

        // Get the cell's socket information
        var aboveCell = blockSystem.GetCell(abovePos);
        if (aboveCell == null)
            return true;

        // Check each direction for incompatible sockets in the cell above
        Direction[] directions = new Direction[]
        {
            Direction.Front, Direction.Back, Direction.Left, Direction.Right
        };

        foreach (Direction direction in directions)
        {
            // Check if the socket in this direction is incompatible
            string socketType = blockSystem.GetSocketAtPosition(abovePos, direction);

            if (debugLogging)
                Debug.Log($"Cell above has socket type '{socketType}' in direction {direction}");

            if (socketType != incompatibleSocketType)
                continue;

            // Found incompatible socket, check the neighboring cell in this direction
            Vector3Int neighborPos = blockSystem.GetNeighborPosition(abovePos, direction);

            if (debugLogging)
                Debug.Log($"Checking neighbor at {neighborPos} for roof block");

            // Skip if outside grid
            if (!blockSystem.IsValidGridPosition(neighborPos))
                continue;

            // Check if the neighbor cell is occupied
            if (!blockSystem.IsCellOccupied(neighborPos))
                continue;

            // Check if the block in this neighbor cell is a roof block
            BuildingBlock neighborBlock = blockSystem.GetBlockAtPosition(neighborPos);
            if (neighborBlock != null && neighborBlock.Name.Contains(roofBlockNameContains))
            {
                failureReason = $"Block '{blockData.Name}' cannot be placed here because it would create an invalid situation for roof block '{neighborBlock.Name}'";

                if (debugLogging)
                    Debug.LogWarning(failureReason);

                return false;
            }
        }

        return true;
    }

    public override string GetFailureReason()
    {
        return failureReason;
    }
}