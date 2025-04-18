using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central manager for all block placement rules.
/// Handles rule registration and evaluation.
/// </summary>
[CreateAssetMenu(fileName = "RuleManager", menuName = "ProceduralBuildings/Rule Manager")]
public class RuleManager : ScriptableObject
{
    [System.Serializable]
    public class BlockRuleAssignment
    {
        public string blockName;
        public List<BlockPlacementRule> rules = new List<BlockPlacementRule>();
    }

    [Header("Global Rules")]
    [Tooltip("Rules that apply to all blocks")]
    public List<BlockPlacementRule> globalRules = new List<BlockPlacementRule>();

    [Header("Block-Specific Rules")]
    [Tooltip("Rules that apply to specific block types")]
    public List<BlockRuleAssignment> blockRules = new List<BlockRuleAssignment>();

    [Header("Debug Settings")]
    [Tooltip("Log rule evaluation results for debugging")]
    public bool enableDebugLogging = true;

    /// <summary>
    /// Evaluates all applicable rules for a specific block and position.
    /// </summary>
    /// <param name="blockData">Block to evaluate</param>
    /// <param name="position">Position to evaluate</param>
    /// <param name="blockSystem">Block system reference</param>
    /// <returns>True if placement is allowed, false if any rule prevents it</returns>
    public bool EvaluateRules(BuildingBlock blockData, Vector3Int position, int rotation, BlockSystemInterface blockSystem)
    {
        // Skip rule check if no rules exist
        if (globalRules.Count == 0 && blockRules.Count == 0)
            return true;

        // Check global rules first
        foreach (var rule in globalRules)
        {
            if (!rule.isEnabled)
                continue;

            if (!rule.EvaluatePlacement(blockData, position, rotation, blockSystem))
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"Global rule '{rule.ruleName}' blocked placement: {rule.GetFailureReason()}");
                }
                return false;
            }
        }

        // Check block-specific rules
        BlockRuleAssignment blockRuleAssignment = blockRules.Find(br => br.blockName == blockData.Name);
        if (blockRuleAssignment != null)
        {
            foreach (var rule in blockRuleAssignment.rules)
            {
                if (!rule.isEnabled)
                    continue;

                if (!rule.EvaluatePlacement(blockData, position, rotation, blockSystem))
                {
                    if (enableDebugLogging)
                    {
                        Debug.LogWarning($"Block-specific rule '{rule.ruleName}' blocked placement: {rule.GetFailureReason()}");
                    }
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Helper method to add a rule assignment for a specific block.
    /// </summary>
    public void AddRuleForBlock(string blockName, BlockPlacementRule rule)
    {
        BlockRuleAssignment assignment = blockRules.Find(br => br.blockName == blockName);

        if (assignment == null)
        {
            assignment = new BlockRuleAssignment { blockName = blockName };
            blockRules.Add(assignment);
        }

        if (!assignment.rules.Contains(rule))
        {
            assignment.rules.Add(rule);
        }
    }

    /// <summary>
    /// Helper method to remove a rule assignment for a specific block.
    /// </summary>
    public void RemoveRuleForBlock(string blockName, BlockPlacementRule rule)
    {
        BlockRuleAssignment assignment = blockRules.Find(br => br.blockName == blockName);

        if (assignment != null)
        {
            assignment.rules.Remove(rule);

            // Remove empty assignments
            if (assignment.rules.Count == 0)
            {
                blockRules.Remove(assignment);
            }
        }
    }
}