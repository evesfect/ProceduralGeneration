using UnityEngine;

// Base abstract class for all block placement rules
[System.Serializable]
public abstract class BlockRule : ScriptableObject
{
    public string ruleName = "Unnamed Rule";
    public string description = "Rule description";
    public int priority = 0; // Higher priority rules are checked first
    public bool enabled = true;

    // Abstract method that all rules must implement
    public abstract bool IsPlacementLegal(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator);

    // Optional method for rules that need to process after a block is placed
    public virtual void OnBlockPlaced(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        // Default implementation does nothing
    }
}