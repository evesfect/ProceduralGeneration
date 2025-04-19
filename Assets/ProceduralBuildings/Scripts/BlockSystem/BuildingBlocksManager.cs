using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// Building Block data class that will be serialized
[System.Serializable]
public class BuildingBlock
{
    public string Name;
    public GameObject Prefab;

    [System.Obsolete("DownDirection is being phased out in favor of BlockOrientationData")]
    public Direction DownDirection = Direction.Down;

    public int CurrentRotation = 0;

    // Socket assignments for each direction
    public string TopSocket;
    public string BottomSocket;
    public string FrontSocket;
    public string BackSocket;
    public string LeftSocket;
    public string RightSocket;

    public string GetSocketForDirection(Direction direction)
    {
        // Return the appropriate socket directly
        switch (direction)
        {
            case Direction.Up: return TopSocket;
            case Direction.Down: return BottomSocket;
            case Direction.Front: return FrontSocket;
            case Direction.Back: return BackSocket;
            case Direction.Left: return LeftSocket;
            case Direction.Right: return RightSocket;
            default: return null;
        }
    }

    public void SetSocketForDirection(Direction direction, string socketType)
    {
        // Set the appropriate socket directly
        switch (direction)
        {
            case Direction.Up: TopSocket = socketType; break;
            case Direction.Down: BottomSocket = socketType; break;
            case Direction.Front: FrontSocket = socketType; break;
            case Direction.Back: BackSocket = socketType; break;
            case Direction.Left: LeftSocket = socketType; break;
            case Direction.Right: RightSocket = socketType; break;
        }
    }
}

// Direction enum to represent the six directions of a cube
public enum Direction
{
    Up,
    Down,
    Front,
    Back,
    Left,
    Right
}

// Building Blocks Manager to handle creation and management of building blocks
[CreateAssetMenu(fileName = "BuildingBlocksManager", menuName = "ProceduralBuildings/Building Blocks Manager")]
public class BuildingBlocksManager : ScriptableObject
{
    public List<BuildingBlock> BuildingBlocks = new List<BuildingBlock>();

    // Add a new building block
    public void AddBuildingBlock(GameObject prefab)
    {
        if (prefab == null)
            return;

        // Check if building block already exists
        bool exists = BuildingBlocks.Any(bb => bb.Prefab == prefab);
        if (exists)
            return;

        BuildingBlock newBlock = new BuildingBlock
        {
            Name = prefab.name,
            Prefab = prefab,
            // Default to standard orientation
            DownDirection = Direction.Down
        };

        BuildingBlocks.Add(newBlock);
    }

    // Set socket for a batch of building blocks
    public void SetSocketsForSelectedBlocks(List<BuildingBlock> selectedBlocks, Direction direction, string socketType)
    {
        foreach (var block in selectedBlocks)
        {
            block.SetSocketForDirection(direction, socketType);
        }
    }

    // Find a building block by name
    public BuildingBlock FindBuildingBlock(string name)
    {
        return BuildingBlocks.Find(bb => bb.Name == name);
    }

    // Remove a building block
    public void RemoveBuildingBlock(BuildingBlock block)
    {
        BuildingBlocks.Remove(block);
    }
}

// Editor window to manage building blocks
public class BuildingBlocksManagerWindow : EditorWindow
{
    private BuildingBlocksManager buildingBlocksManager;
    private SocketManager socketManager;

    private List<GameObject> prefabsToAdd = new List<GameObject>();
    private Direction downDirection = Direction.Down;
    private int selectedBuildingBlockToRemove = 0;

    private Vector2 mainScrollPosition;
    private Vector2 buildingBlocksScrollPosition;
    private Vector2 socketAssignmentScrollPosition;
    private Vector2 buildingBlocksListScrollPosition;

    private List<bool> selectedBuildingBlocks = new List<bool>();
    private Dictionary<Direction, string> selectedSockets = new Dictionary<Direction, string>();

    [MenuItem("Tools/ProceduralBuildings/Building Blocks Manager")]
    public static void ShowWindow()
    {
        GetWindow<BuildingBlocksManagerWindow>("Building Blocks Manager");
    }

    private void OnEnable()
    {
        // Initialize dictionaries
        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            selectedSockets[dir] = "";
        }

        // Try to find existing manager assets
        string[] bbGuids = AssetDatabase.FindAssets("t:BuildingBlocksManager");
        if (bbGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(bbGuids[0]);
            buildingBlocksManager = AssetDatabase.LoadAssetAtPath<BuildingBlocksManager>(path);
        }

        string[] socketGuids = AssetDatabase.FindAssets("t:SocketManager");
        if (socketGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(socketGuids[0]);
            socketManager = AssetDatabase.LoadAssetAtPath<SocketManager>(path);
        }

        // If not found, create new ones
        if (buildingBlocksManager == null)
        {
            buildingBlocksManager = CreateInstance<BuildingBlocksManager>();

            // Create the directories if they don't exist
            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings"))
                AssetDatabase.CreateFolder("Assets", "ProceduralBuildings");

            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings/Data"))
                AssetDatabase.CreateFolder("Assets/ProceduralBuildings", "Data");

            AssetDatabase.CreateAsset(buildingBlocksManager, "Assets/ProceduralBuildings/Data/BuildingBlocksManager.asset");
            AssetDatabase.SaveAssets();
        }

        // Update the selected building blocks list
        UpdateSelectedList();
    }

    private void UpdateSelectedList()
    {
        selectedBuildingBlocks.Clear();
        for (int i = 0; i < buildingBlocksManager.BuildingBlocks.Count; i++)
        {
            selectedBuildingBlocks.Add(false);
        }
    }

    private void OnGUI()
    {
        if (buildingBlocksManager == null)
        {
            EditorGUILayout.HelpBox("Building Blocks Manager asset not found!", MessageType.Error);
            return;
        }

        if (socketManager == null)
        {
            EditorGUILayout.HelpBox("Socket Manager asset not found! Please create it first.", MessageType.Error);
            return;
        }

        // Make the entire window scrollable
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

        // Create Building Blocks Section
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Create Building Blocks", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Prefab selection
        EditorGUILayout.LabelField("Prefabs to Add:");

        buildingBlocksScrollPosition = EditorGUILayout.BeginScrollView(
            buildingBlocksScrollPosition,
            GUILayout.Height(100)
        );

        for (int i = 0; i < prefabsToAdd.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabsToAdd[i] = (GameObject)EditorGUILayout.ObjectField(prefabsToAdd[i], typeof(GameObject), false);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                prefabsToAdd.RemoveAt(i);
                GUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Prefab Slot"))
        {
            prefabsToAdd.Add(null);
        }

        // Down direction selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Down Direction:", GUILayout.Width(120));
        downDirection = (Direction)EditorGUILayout.EnumPopup(downDirection);
        EditorGUILayout.EndHorizontal();

        // Create building blocks button
        if (GUILayout.Button("Create Building Blocks"))
        {
            foreach (var prefab in prefabsToAdd)
            {
                if (prefab != null)
                {
                    buildingBlocksManager.AddBuildingBlock(prefab);
                }
            }

            prefabsToAdd.Clear();
            UpdateSelectedList();
            EditorUtility.SetDirty(buildingBlocksManager);
            AssetDatabase.SaveAssets();
        }

        // Remove Building Block Section
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Remove Building Block", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        // Building block selection dropdown
        string[] blockNames = buildingBlocksManager.BuildingBlocks.Select(bb => bb.Name).ToArray();

        if (blockNames.Length == 0)
        {
            EditorGUILayout.LabelField("No building blocks to remove");
            GUI.enabled = false;
        }
        else
        {
            selectedBuildingBlockToRemove = EditorGUILayout.Popup(selectedBuildingBlockToRemove, blockNames);
        }

        // Remove button
        if (GUILayout.Button("Remove", GUILayout.Width(80)))
        {
            if (blockNames.Length > 0 && selectedBuildingBlockToRemove >= 0 && selectedBuildingBlockToRemove < blockNames.Length)
            {
                BuildingBlock blockToRemove = buildingBlocksManager.BuildingBlocks.Find(bb => bb.Name == blockNames[selectedBuildingBlockToRemove]);
                if (blockToRemove != null)
                {
                    buildingBlocksManager.RemoveBuildingBlock(blockToRemove);
                    UpdateSelectedList();
                    EditorUtility.SetDirty(buildingBlocksManager);
                    AssetDatabase.SaveAssets();

                    // Reset the index if necessary
                    if (selectedBuildingBlockToRemove >= buildingBlocksManager.BuildingBlocks.Count)
                    {
                        selectedBuildingBlockToRemove = buildingBlocksManager.BuildingBlocks.Count - 1;
                        if (selectedBuildingBlockToRemove < 0) selectedBuildingBlockToRemove = 0;
                    }
                }
            }
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Manage Building Blocks' Sockets Section
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Manage Building Blocks' Sockets", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (buildingBlocksManager.BuildingBlocks.Count == 0)
        {
            EditorGUILayout.HelpBox("No building blocks created yet. Create some building blocks first.", MessageType.Info);
        }
        else
        {
            // Building blocks selection
            EditorGUILayout.LabelField("Select Building Blocks:");

            socketAssignmentScrollPosition = EditorGUILayout.BeginScrollView(
                socketAssignmentScrollPosition,
                GUILayout.Height(100)
            );

            for (int i = 0; i < buildingBlocksManager.BuildingBlocks.Count; i++)
            {
                BuildingBlock block = buildingBlocksManager.BuildingBlocks[i];

                EditorGUILayout.BeginHorizontal();

                selectedBuildingBlocks[i] = EditorGUILayout.ToggleLeft(
                    block.Name + " (Down: " + block.DownDirection.ToString() + ")",
                    selectedBuildingBlocks[i]
                );

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Socket selection for each direction
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Socket Assignments:");

            // Create a "None" option for the socket dropdown
            List<string> socketOptions = new List<string> { "None" };
            socketOptions.AddRange(socketManager.SocketList);

            foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(direction.ToString() + " Socket:", GUILayout.Width(120));

                int selectedIndex = 0;
                if (selectedSockets[direction] != "")
                {
                    selectedIndex = socketOptions.IndexOf(selectedSockets[direction]);
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                selectedIndex = EditorGUILayout.Popup(selectedIndex, socketOptions.ToArray());
                if (selectedIndex == 0)
                {
                    selectedSockets[direction] = "";
                }
                else
                {
                    selectedSockets[direction] = socketOptions[selectedIndex];
                }

                EditorGUILayout.EndHorizontal();
            }

            // Update sockets button
            if (GUILayout.Button("Update Sockets"))
            {
                List<BuildingBlock> blocksToUpdate = new List<BuildingBlock>();

                for (int i = 0; i < selectedBuildingBlocks.Count; i++)
                {
                    if (selectedBuildingBlocks[i])
                    {
                        blocksToUpdate.Add(buildingBlocksManager.BuildingBlocks[i]);
                    }
                }

                foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
                {
                    buildingBlocksManager.SetSocketsForSelectedBlocks(
                        blocksToUpdate,
                        direction,
                        selectedSockets[direction]
                    );
                }

                EditorUtility.SetDirty(buildingBlocksManager);
                AssetDatabase.SaveAssets();
            }

            // Display building blocks with their sockets
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Building Blocks and Their Sockets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            buildingBlocksListScrollPosition = EditorGUILayout.BeginScrollView(
                buildingBlocksListScrollPosition,
                GUILayout.Height(200)
            );

            foreach (var block in buildingBlocksManager.BuildingBlocks)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Name: " + block.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Prefab: " + (block.Prefab != null ? block.Prefab.name : "None"));
                EditorGUILayout.LabelField("Down Direction: " + block.DownDirection.ToString());

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Sockets (in world space):");

                EditorGUILayout.LabelField("• Up: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Up)) ? "None" : block.GetSocketForDirection(Direction.Up)));
                EditorGUILayout.LabelField("• Down: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Down)) ? "None" : block.GetSocketForDirection(Direction.Down)));
                EditorGUILayout.LabelField("• Front: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Front)) ? "None" : block.GetSocketForDirection(Direction.Front)));
                EditorGUILayout.LabelField("• Back: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Back)) ? "None" : block.GetSocketForDirection(Direction.Back)));
                EditorGUILayout.LabelField("• Left: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Left)) ? "None" : block.GetSocketForDirection(Direction.Left)));
                EditorGUILayout.LabelField("• Right: " + (string.IsNullOrEmpty(block.GetSocketForDirection(Direction.Right)) ? "None" : block.GetSocketForDirection(Direction.Right)));

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        // End the main scroll view
        EditorGUILayout.EndScrollView();
    }
}