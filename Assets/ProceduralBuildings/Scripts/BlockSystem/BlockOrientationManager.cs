using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BlockOrientationManager", menuName = "ProceduralBuildings/Block Orientation Manager")]
public class BlockOrientationManager : ScriptableObject
{
    public List<BlockOrientationData> orientationData = new List<BlockOrientationData>();
    private Dictionary<string, BlockOrientationData> orientationLookup;

    private void OnEnable()
    {
        // Initialize the lookup dictionary
        RebuildLookupDictionary();
    }

    public void RebuildLookupDictionary()
    {
        orientationLookup = new Dictionary<string, BlockOrientationData>();
        foreach (var data in orientationData)
        {
            if (!string.IsNullOrEmpty(data.blockName))
            {
                orientationLookup[data.blockName] = data;
            }
        }
    }

    public BlockOrientationData GetOrientationData(string blockName)
    {
        // Ensure lookup is initialized
        if (orientationLookup == null)
        {
            RebuildLookupDictionary();
        }

        // Try to get the data
        if (orientationLookup.TryGetValue(blockName, out BlockOrientationData data))
        {
            return data;
        }

        // Return default orientation data if not found
        return new BlockOrientationData { blockName = blockName };
    }

    public void AddOrUpdateOrientation(string blockName, Quaternion rotation, Vector3 offset)
    {
        // Find existing entry
        BlockOrientationData existingData = orientationData.Find(d => d.blockName == blockName);

        if (existingData != null)
        {
            // Update existing
            existingData.correctionRotation = rotation;
            existingData.correctionOffset = offset;
        }
        else
        {
            // Add new
            orientationData.Add(new BlockOrientationData
            {
                blockName = blockName,
                correctionRotation = rotation,
                correctionOffset = offset
            });
        }

        // Update the lookup dictionary
        RebuildLookupDictionary();
    }
}