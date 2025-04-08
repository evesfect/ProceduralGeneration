using UnityEngine;
using System.Collections;

/// <summary>
/// Example setup for the building generator.
/// This automatically sets up the required components and generates a building.
/// </summary>
public class BuildingGeneratorSetup : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Whether to automatically generate a building on start")]
    public bool autoGenerate = true;

    [Tooltip("Time to wait before generating (seconds)")]
    public float generateDelay = 1f;

    [Tooltip("Press this key to regenerate the building during play")]
    public KeyCode regenerateKey = KeyCode.Space;

    // Reference to our simple building generator
    private SimpleBuilding buildingGenerator;

    // Start is called before the first frame update
    void Start()
    {
        // Make sure we have a SimpleBuilding component
        buildingGenerator = GetComponent<SimpleBuilding>();
        if (buildingGenerator == null)
        {
            buildingGenerator = gameObject.AddComponent<SimpleBuilding>();
            Debug.Log("Added SimpleBuilding component");
        }

        // Make sure we have a BlockSystemInterface
        BlockSystemInterface blockSystem = GetComponent<BlockSystemInterface>();
        if (blockSystem == null)
        {
            blockSystem = gameObject.AddComponent<BlockSystemInterface>();
            Debug.Log("Added BlockSystemInterface component");
        }

        // Assign the block system to the building generator
        buildingGenerator.blockSystem = blockSystem;

        // Auto-find references if not set
        if (blockSystem.gridGenerator == null)
        {
            blockSystem.gridGenerator = FindAnyObjectByType<GridGenerator>();
            if (blockSystem.gridGenerator != null)
            {
                Debug.Log("Found GridGenerator in scene");
            }
        }

        // Try to find BuildingBlocksManager and SocketManager references from the GridGenerator
        if (blockSystem.gridGenerator != null)
        {
            if (blockSystem.buildingBlocksManager == null)
            {
                blockSystem.buildingBlocksManager = blockSystem.gridGenerator.buildingBlocksManager;
                if (blockSystem.buildingBlocksManager != null)
                {
                    Debug.Log("Found BuildingBlocksManager reference from GridGenerator");
                }
            }

            if (blockSystem.socketManager == null)
            {
                blockSystem.socketManager = blockSystem.gridGenerator.socketManager;
                if (blockSystem.socketManager != null)
                {
                    Debug.Log("Found SocketManager reference from GridGenerator");
                }
            }
        }

        // Delay generation if requested
        if (autoGenerate)
        {
            StartCoroutine(GenerateWithDelay());
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Regenerate the building when the regenerate key is pressed
        if (Input.GetKeyDown(regenerateKey))
        {
            Debug.Log("Regenerating building...");
            buildingGenerator.GenerateBuilding();
        }
    }

    // Coroutine to delay generation
    private IEnumerator GenerateWithDelay()
    {
        yield return new WaitForSeconds(generateDelay);

        Debug.Log("Auto-generating building...");
        buildingGenerator.GenerateBuilding();
    }

    // Show instructions in the game view
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Press {regenerateKey} to regenerate the building");
    }
}