using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Net.NetworkInformation;

[CreateAssetMenu(fileName = "RoofEdgeRule", menuName = "ProceduralBuildings/Rules/Roof Edge Rule")]
public class RoofEdgeRule : BlockRule
{
    private string incompatibleName = "Uncompatible";
    private string Full_TopName = "Full_Top";

    public override bool IsPlacementLegal(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        Debug.Log($"=== Testing rotation {yRotation} for {block.Name} ===");
        Debug.Log($"Original Front: {block.FrontSocket}, Right: {block.RightSocket}");


        BuildingBlock copyBlock = generator.CloneBlock(block);
        generator.BlockSystem.gridGenerator.ApplyHorizontalYRotation(copyBlock, yRotation);

        Debug.Log($"After rotation Front: {copyBlock.FrontSocket}, Right: {copyBlock.RightSocket}");

        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            if (copyBlock.GetSocketForDirection(direction) == incompatibleName && direction != Direction.Up && direction != Direction.Down)
            {
                Vector3Int neighborPos = generator.BlockSystem.GetNeighborPosition(position, direction);
                Vector3Int controlPosition = neighborPos - new Vector3Int(0, 1, 0);
                Debug.Log($"ControlPosition for roof at {position} is {controlPosition}");

                if (!generator.BlockSystem.gridGenerator.IsValidPosition(controlPosition) ||
                    !generator.BlockSystem.IsCellOccupied(controlPosition)) { continue; }

                BuildingBlock controlBlock = generator.BlockSystem.GetBlockAtPosition(controlPosition);
                if (controlBlock.GetSocketForDirection(Direction.Up) == Full_TopName)
                {
                    Debug.Log($"Block at {controlPosition} does not allow placement of {block.Name} at {position} with rotation {yRotation}");
                    return false;
                }
                else { Debug.Log($"Roof at {position} is compatible with {controlPosition}"); }
            }
        }


        return true;
    }

    private void OnEnable()
    {
        ruleName = "Roof Edge Rule";
        description = "Prevents placement of roofs that need to form an edge if there is a block under that edge";
    }
}
