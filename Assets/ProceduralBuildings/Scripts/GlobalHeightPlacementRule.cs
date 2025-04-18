using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rule that restricts block placement based on height constraints.
/// </summary>
[CreateAssetMenu(fileName = "NewGlobalHeightRule", menuName = "ProceduralBuildings/Rules/Global Height Rule")]
public class GlobalHeightPlacementRule : BlockPlacementRule
{
    public enum HeightRestrictionType
    {
        DisallowTopFloor,          // Block can't be placed on the top floor
        DisallowBottomFloor,       // Block can't be placed on the bottom floor
        RestrictToBottomFloor,     // Block can only be placed on the bottom floor
        RestrictToTopFloor,        // Block can only be placed on the top floor
        RestrictToRange,           // Block must be within min/max height range
        DisallowSpecificHeight     // Block can't be placed at specific heights
    }

    [Header("Height Restriction Settings")]
    [Tooltip("Type of height restriction to apply")]
    public HeightRestrictionType restrictionType = HeightRestrictionType.DisallowTopFloor;

    [Tooltip("Minimum allowed height (used for range restriction)")]
    public int minHeight = 0;

    [Tooltip("Maximum allowed height (used for range restriction)")]
    public int maxHeight = 10;

    [Tooltip("Specific heights to disallow (used for DisallowSpecificHeight type)")]
    public List<int> disallowedHeights = new List<int>();

    public string RoofIdentifier = "Roof";

    private string failureReason = "";

    public override bool EvaluatePlacement(BuildingBlock blockData, Vector3Int position, int rotation, BlockSystemInterface blockSystem)
    {
        if (!isEnabled)
            return true;

        if (blockData.Name.Contains(RoofIdentifier))
            return true;

        Vector3Int gridDimensions = blockSystem.GetGridDimensions();
        int currentHeight = position.y;
        int maxGridHeight = gridDimensions.y - 1;

        switch (restrictionType)
        {
            case HeightRestrictionType.DisallowTopFloor:
                if (currentHeight == maxGridHeight)
                {
                    failureReason = $"Block '{blockData.Name}' cannot be placed on the top floor (y={maxGridHeight})";
                    return false;
                }
                break;

            case HeightRestrictionType.DisallowBottomFloor:
                if (currentHeight == 0)
                {
                    failureReason = $"Block '{blockData.Name}' cannot be placed on the bottom floor (y=0)";
                    return false;
                }
                break;

            case HeightRestrictionType.RestrictToBottomFloor:
                if (currentHeight != 0)
                {
                    failureReason = $"Block '{blockData.Name}' can only be placed on the bottom floor (y=0)";
                    return false;
                }
                break;

            case HeightRestrictionType.RestrictToTopFloor:
                if (currentHeight != maxGridHeight)
                {
                    failureReason = $"Block '{blockData.Name}' can only be placed on the top floor (y={maxGridHeight})";
                    return false;
                }
                break;

            case HeightRestrictionType.RestrictToRange:
                if (currentHeight < minHeight || currentHeight > maxHeight)
                {
                    failureReason = $"Block '{blockData.Name}' must be placed between heights {minHeight} and {maxHeight} (current: {currentHeight})";
                    return false;
                }
                break;

            case HeightRestrictionType.DisallowSpecificHeight:
                if (disallowedHeights.Contains(currentHeight))
                {
                    failureReason = $"Block '{blockData.Name}' cannot be placed at height {currentHeight}";
                    return false;
                }
                break;
        }

        return true;
    }

    public override string GetFailureReason()
    {
        return failureReason;
    }
}