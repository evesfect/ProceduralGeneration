using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "TopBlockerRule", menuName = "ProceduralBuildings/Rules/Top Blocker")]
public class TopBlockerRule : BlockRule
{
    public override bool IsPlacementLegal(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        // If not enabled, allow placement
        if (!enabled)
            return true;

        int gridTop = generator.BlockSystem.GetGridDimensions().y - 1;
        if (position.y >= gridTop)
        {
            return false;
        }

        // Otherwise, allow placement
        return true;
    }

    // Constructor to set default values
    private void OnEnable()
    {
        ruleName = "Top Blocker";
        description = "Prevents blocks from being placed at the top level";
    }
}
