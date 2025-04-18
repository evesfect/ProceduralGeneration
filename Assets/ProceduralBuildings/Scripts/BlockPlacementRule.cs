using UnityEngine;

/// <summary>
/// Base class for all block placement rules.
/// Rules define constraints on where building blocks can be placed.
/// </summary>
[CreateAssetMenu(fileName = "NewPlacementRule", menuName = "ProceduralBuildings/Rules/Base Rule")]
public abstract class BlockPlacementRule : ScriptableObject
{
    [Header("Rule Settings")]
    [Tooltip("Friendly name for this rule")]
    public string ruleName = "New Rule";

    [Tooltip("Description of what this rule does")]
    [TextArea(2, 5)]
    public string description;

    [Tooltip("Should the rule be applied to check placements")]
    public bool isEnabled = true;

    /// <summary>
    /// Evaluates whether a block can be placed at the specified position.
    /// </summary>
    /// <param name="blockData">The block to place</param>
    /// <param name="position">Position in the grid to evaluate</param>
    /// <param name="blockSystem">Reference to the block system for context</param>
    /// <returns>True if placement is allowed, false if rule prevents placement</returns>
    public abstract bool EvaluatePlacement(BuildingBlock blockData, Vector3Int position, int rotation, BlockSystemInterface blockSystem);

    /// <summary>
    /// Returns a description of why a rule failed (for debugging).
    /// </summary>
    /// <returns>User-friendly failure reason</returns>
    public abstract string GetFailureReason();
}