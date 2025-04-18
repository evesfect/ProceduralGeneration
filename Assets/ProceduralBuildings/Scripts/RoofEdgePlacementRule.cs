using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rule that ensures roof blocks with incompatible sockets are placed properly at building edges.
/// </summary>
[CreateAssetMenu(fileName = "NewRoofEdgeRule", menuName = "ProceduralBuildings/Rules/Roof Edge Rule")]
public class RoofEdgePlacementRule : BlockPlacementRule
{
    [Header("Roof Socket Settings")]
    [Tooltip("Socket type that indicates an incompatible edge")]
    public string incompatibleSocketType = "incompatible";

    [Tooltip("Socket type that should NOT be under neighboring cells")]
    public string forbiddenNeighborBottomSocketType = "full_top";

    [Header("Debug")]
    [Tooltip("Enable detailed logging for debugging")]
    public bool debugLogging = true;

    private string failureReason = "";

    public override bool EvaluatePlacement(BuildingBlock blockData, Vector3Int position, int rotation, BlockSystemInterface blockSystem)
    {
        if (!isEnabled)
            return true;

        if (debugLogging)
            Debug.Log($"Evaluating roof placement rule for '{blockData.Name}' at {position}, for rotation {rotation}");

        // First, check if this block has any incompatible sockets on its sides
        List<Direction> incompatibleDirections = FindIncompatibleSideSockets(blockData);

        // If no incompatible side sockets, this rule doesn't apply
        if (incompatibleDirections.Count == 0)
            return true;

        if (debugLogging)
            Debug.Log($"Found {incompatibleDirections.Count} incompatible sides: {string.Join(", ", incompatibleDirections)}");

        // For each incompatible direction, check if it's properly placed at a building edge
        foreach (Direction BlockDirection in incompatibleDirections)
        {
            Direction direction = RotateDirection(BlockDirection, rotation);
            // Get the neighbor position in this direction
            Vector3Int neighborPos = blockSystem.GetNeighborPosition(position, direction);

            if (debugLogging)
                Debug.Log($"Checking neighbor at {neighborPos} in direction {direction}");

            // If we're at the grid edge, that's valid for a roof edge
            if (!blockSystem.IsValidGridPosition(neighborPos))
            {
                if (debugLogging)
                    Debug.Log("Neighbor is outside grid - valid edge placement");
                continue;
            }

            // If neighbor cell is occupied, that's not valid for a roof edge with incompatible socket
            if (blockSystem.IsCellOccupied(neighborPos))
            {
                failureReason = $"Roof block '{blockData.Name}' has incompatible socket facing an occupied cell";
                if (debugLogging)
                    Debug.LogWarning(failureReason);
                return false;
            }

            // Get the position below the neighbor
            Vector3Int belowNeighborPos = blockSystem.GetNeighborPosition(neighborPos, Direction.Down);

            if (debugLogging)
                Debug.Log($"Checking cell below neighbor at {belowNeighborPos}");

            // If below neighbor is outside grid, that's fine
            if (!blockSystem.IsValidGridPosition(belowNeighborPos))
            {
                if (debugLogging)
                    Debug.Log("Cell below neighbor is outside grid - valid");
                continue;
            }

            // Only check occupied cells below neighbor
            if (blockSystem.IsCellOccupied(belowNeighborPos))
            {
                // Get the actual block below the neighbor
                BuildingBlock blockBelowNeighbor = blockSystem.GetBlockAtPosition(belowNeighborPos);

                if (blockBelowNeighbor != null)
                {
                    // Get its top socket type
                    string topSocketType = blockBelowNeighbor.GetSocketForDirection(Direction.Up);

                    if (debugLogging)
                        Debug.Log($"Block below neighbor has top socket: '{topSocketType}'");

                    // Check if it has the forbidden socket type (usually "full_top")
                    if (topSocketType == forbiddenNeighborBottomSocketType)
                    {
                        failureReason = $"Roof block at '{position}' conflicts with the block at '{belowNeighborPos}'. ";
                        if (debugLogging)
                            Debug.LogWarning(failureReason);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public override string GetFailureReason()
    {
        return failureReason;
    }

    /// <summary>
    /// Find all directions where this block has an incompatible socket on its sides.
    /// Takes into account the block's down direction for proper orientation.
    /// </summary>
    private List<Direction> FindIncompatibleSideSockets(BuildingBlock blockData)
    {
        List<Direction> incompatibleDirections = new List<Direction>();

        // Check all horizontal directions (not Up/Down)
        Direction[] sideDirections = new Direction[]
        {
            Direction.Front, Direction.Back, Direction.Left, Direction.Right
        };

        foreach (Direction direction in sideDirections)
        {
            string socketType = blockData.GetSocketForDirection(direction);
            if (socketType == incompatibleSocketType)
            {
                incompatibleDirections.Add(direction);
            }
        }

        return incompatibleDirections;
    }

    public static Direction RotateDirection(Direction dir, int rotationDegrees)
    {
        // Normalize rotation to [0, 360)
        rotationDegrees = ((rotationDegrees % 360) + 360) % 360;
        int steps = rotationDegrees / 90;

        switch (dir)
        {
            case Direction.Up:
            case Direction.Down:
                // Up and Down don't rotate around Y-axis
                return dir;

            case Direction.Front:
                return RotateHorizontal(Direction.Front, steps);
            case Direction.Right:
                return RotateHorizontal(Direction.Right, steps);
            case Direction.Back:
                return RotateHorizontal(Direction.Back, steps);
            case Direction.Left:
                return RotateHorizontal(Direction.Left, steps);

            default:
                throw new System.ArgumentOutOfRangeException(nameof(dir), $"Unknown Direction: {dir}");
        }
    }

    private static Direction RotateHorizontal(Direction dir, int steps)
    {
        // Order matters: Front -> Right -> Back -> Left
        Direction[] horizontal = new Direction[]
        {
            Direction.Front,
            Direction.Right,
            Direction.Back,
            Direction.Left
        };

        int currentIndex = System.Array.IndexOf(horizontal, dir);
        int newIndex = (currentIndex + steps) % horizontal.Length;
        return horizontal[newIndex];
    }
}