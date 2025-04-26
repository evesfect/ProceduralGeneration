using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlockRulesManager", menuName = "ProceduralBuildings/Block Rules Manager")]
public class BlockRulesManager : ScriptableObject
{
    [System.Serializable]
    public class BlockGroup
    {
        public string groupName;
        public List<string> blockNames = new List<string>();
        public List<BlockRule> rules = new List<BlockRule>();
    }

    public List<BlockGroup> blockGroups = new List<BlockGroup>();
    public List<BlockRule> globalRules = new List<BlockRule>(); // Rules that apply to all blocks

    // Check if a placement complies with all applicable rules
    public bool IsPlacementLegal(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        // Check global rules first
        foreach (BlockRule rule in globalRules)
        {
            if (!rule.enabled) continue;

            if (!rule.IsPlacementLegal(block, yRotation, position, generator))
            {
                Debug.Log($"Rule '{rule.ruleName}' prevented placement of {block.Name} at {position}");
                return false;
            }
        }

        // Find applicable block groups
        foreach (BlockGroup group in blockGroups)
        {
            if (group.blockNames.Contains(block.Name))
            {
                // Check rules for this group
                foreach (BlockRule rule in group.rules)
                {
                    if (!rule.enabled) continue;

                    if (!rule.IsPlacementLegal(block, yRotation, position, generator))
                    {
                        Debug.Log($"Rule '{rule.ruleName}' from group '{group.groupName}' prevented placement of {block.Name} at {position}");
                        return false;
                    }
                }
            }
        }

        // All rules passed
        return true;
    }

    // Notify rules after a block has been placed
    public void NotifyBlockPlaced(BuildingBlock block, int yRotation, Vector3Int position, BuildingGenerator generator)
    {
        // Notify global rules
        foreach (BlockRule rule in globalRules)
        {
            if (rule.enabled)
            {
                rule.OnBlockPlaced(block, yRotation, position, generator);
            }
        }

        // Notify group rules
        foreach (BlockGroup group in blockGroups)
        {
            if (group.blockNames.Contains(block.Name))
            {
                foreach (BlockRule rule in group.rules)
                {
                    if (rule.enabled)
                    {
                        rule.OnBlockPlaced(block, yRotation, position, generator);
                    }
                }
            }
        }
    }

    // Helper methods for managing block groups
    public BlockGroup GetOrCreateGroup(string groupName)
    {
        BlockGroup group = blockGroups.Find(g => g.groupName == groupName);
        if (group == null)
        {
            group = new BlockGroup { groupName = groupName };
            blockGroups.Add(group);
        }
        return group;
    }

    public void AddBlockToGroup(string blockName, string groupName)
    {
        BlockGroup group = GetOrCreateGroup(groupName);
        if (!group.blockNames.Contains(blockName))
        {
            group.blockNames.Add(blockName);
        }
    }

    public void AddRuleToGroup(BlockRule rule, string groupName)
    {
        BlockGroup group = GetOrCreateGroup(groupName);
        if (!group.rules.Contains(rule))
        {
            group.rules.Add(rule);
        }
    }

    public void AddGlobalRule(BlockRule rule)
    {
        if (!globalRules.Contains(rule))
        {
            globalRules.Add(rule);
        }
    }
}