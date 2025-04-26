using UnityEngine;

[CreateAssetMenu(fileName = "BottomBlockerRule", menuName = "ProceduralBuildings/Rules/Bottom Blocker")]
public class BottomBlockerRule : BlockRule
{
    public override bool IsPlacementLegal(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        // If not enabled, allow placement
        if (!enabled)
            return true;

        // Check if the position is at height 0
        if (position.y == 0)
        {
            Debug.Log($"BottomBlocker rule prevented {block.Name} from being placed at ground level (y=0)");
            return false; // Forbid placement at height 0
        }

        // Otherwise, allow placement
        return true;
    }

    // Constructor to set default values
    private void OnEnable()
    {
        ruleName = "Bottom Blocker";
        description = "Prevents blocks from being placed at ground level (height 0)";
    }
}