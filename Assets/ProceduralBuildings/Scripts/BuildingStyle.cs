using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuildingStyle", menuName = "ProceduralBuildings/Building Style")]
public class BuildingStyle : ScriptableObject
{
    [System.Serializable]
    public class BlockWeight
    {
        public string blockName;
        public float weight = 1.0f;
    }

    public string styleName = "Default Style";
    public List<BlockWeight> blockWeights = new List<BlockWeight>();

    // This will run when the asset is created
    private void OnEnable()
    {
        // Only initialize if this is a new, empty asset
        if (blockWeights == null || blockWeights.Count == 0)
        {
            // Find BuildingBlocksManager
            BuildingBlocksManager blockManager = FindBuildingBlocksManager();
            if (blockManager != null)
            {
                InitializeWithBlocks(blockManager);
            }
        }
    }

    // Helper to find the BuildingBlocksManager asset
    private BuildingBlocksManager FindBuildingBlocksManager()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BuildingBlocksManager");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<BuildingBlocksManager>(path);
        }
#endif
        return null;
    }

    // Initialize with blocks from BuildingBlocksManager
    public void InitializeWithBlocks(BuildingBlocksManager blockManager)
    {
        if (blockManager == null) return;

        blockWeights.Clear();

        foreach (var block in blockManager.BuildingBlocks)
        {
            blockWeights.Add(new BlockWeight
            {
                blockName = block.Name,
                weight = 1.0f
            });
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Get weight for a block by name
    public float GetWeight(string blockName)
    {
        foreach (var entry in blockWeights)
        {
            if (entry.blockName == blockName)
                return entry.weight;
        }
        return 1.0f; // Default weight if not found
    }
}