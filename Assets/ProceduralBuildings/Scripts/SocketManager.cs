using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// This class holds the socket data and will be saved as an asset
[CreateAssetMenu(fileName = "SocketManager", menuName = "ProceduralBuildings/Socket Manager")]
public class SocketManager : ScriptableObject
{
    public List<string> SocketList = new List<string>();

    // Since Unity can't serialize dictionaries directly, we'll use a serializable class
    [System.Serializable]
    public class SocketCompatibility
    {
        public string SocketType;
        public List<string> CompatibleSockets = new List<string>();
    }

    public List<SocketCompatibility> SocketCompatibilities = new List<SocketCompatibility>();

    // Helper method to get dictionary representation of compatibility data
    public Dictionary<string, List<string>> GetSocketCompDict()
    {
        Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();

        foreach (var socket in SocketList)
        {
            // Ensure every socket type has an entry in the dictionary
            if (!dict.ContainsKey(socket))
            {
                dict[socket] = new List<string>();
            }
        }

        foreach (var compat in SocketCompatibilities)
        {
            if (SocketList.Contains(compat.SocketType))
            {
                dict[compat.SocketType] = new List<string>(compat.CompatibleSockets);
            }
        }

        return dict;
    }

    // Set bi-directional compatibility between two sockets
    public void SetCompatibility(string socketA, string socketB, bool isCompatible)
    {
        if (!SocketList.Contains(socketA) || !SocketList.Contains(socketB))
            return;

        // Update socketA's compatibility list
        SocketCompatibility compatA = SocketCompatibilities.Find(c => c.SocketType == socketA);
        if (compatA == null)
        {
            compatA = new SocketCompatibility { SocketType = socketA };
            SocketCompatibilities.Add(compatA);
        }

        // Update socketB's compatibility list
        SocketCompatibility compatB = SocketCompatibilities.Find(c => c.SocketType == socketB);
        if (compatB == null)
        {
            compatB = new SocketCompatibility { SocketType = socketB };
            SocketCompatibilities.Add(compatB);
        }

        if (isCompatible)
        {
            // Add bi-directional compatibility
            if (!compatA.CompatibleSockets.Contains(socketB))
                compatA.CompatibleSockets.Add(socketB);

            if (!compatB.CompatibleSockets.Contains(socketA))
                compatB.CompatibleSockets.Add(socketA);
        }
        else
        {
            // Remove bi-directional compatibility
            compatA.CompatibleSockets.Remove(socketB);
            compatB.CompatibleSockets.Remove(socketA);
        }
    }

    // Remove socket type and clean up all references in compatibility dictionary
    public void RemoveSocketType(string socketName)
    {
        if (!SocketList.Contains(socketName))
            return;

        // Remove from socket list
        SocketList.Remove(socketName);

        // Remove socket's own compatibility entry
        SocketCompatibilities.RemoveAll(c => c.SocketType == socketName);

        // Remove from all other compatibility lists
        foreach (var compat in SocketCompatibilities)
        {
            compat.CompatibleSockets.Remove(socketName);
        }
    }
}

// Editor window to manage sockets
public class SocketManagerWindow : EditorWindow
{
    private SocketManager socketManager;
    private string newSocketName = "";
    private int selectedSocketA = 0;
    private int selectedSocketB = 0;
    private Vector2 scrollPosition;

    [MenuItem("Tools/ProceduralBuildings/Socket Manager")]
    public static void ShowWindow()
    {
        GetWindow<SocketManagerWindow>("Socket Manager");
    }

    private void OnEnable()
    {
        // Try to find existing socket manager asset
        string[] guids = AssetDatabase.FindAssets("t:SocketManager");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            socketManager = AssetDatabase.LoadAssetAtPath<SocketManager>(path);
        }

        // If not found, create a new one
        if (socketManager == null)
        {
            socketManager = CreateInstance<SocketManager>();

            // Create the directories if they don't exist
            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings"))
                AssetDatabase.CreateFolder("Assets", "ProceduralBuildings");

            if (!AssetDatabase.IsValidFolder("Assets/ProceduralBuildings/Data"))
                AssetDatabase.CreateFolder("Assets/ProceduralBuildings", "Data");

            AssetDatabase.CreateAsset(socketManager, "Assets/ProceduralBuildings/Data/SocketManager.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        if (socketManager == null)
        {
            EditorGUILayout.HelpBox("Socket Manager asset not found!", MessageType.Error);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Socket Types Management Section
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Manage Socket Types", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.BeginHorizontal();
        newSocketName = EditorGUILayout.TextField(newSocketName);

        if (GUILayout.Button("Add", GUILayout.Width(100)))
        {
            if (!string.IsNullOrEmpty(newSocketName) && !socketManager.SocketList.Contains(newSocketName))
            {
                socketManager.SocketList.Add(newSocketName);

                // Make the socket compatible with itself by default
                socketManager.SetCompatibility(newSocketName, newSocketName, true);

                EditorUtility.SetDirty(socketManager);
                AssetDatabase.SaveAssets();
            }
        }

        if (GUILayout.Button("Remove", GUILayout.Width(100)))
        {
            if (!string.IsNullOrEmpty(newSocketName) && socketManager.SocketList.Contains(newSocketName))
            {
                socketManager.RemoveSocketType(newSocketName);
                EditorUtility.SetDirty(socketManager);
                AssetDatabase.SaveAssets();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Display all socket types
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Socket Types:");

        EditorGUILayout.BeginVertical("box");
        if (socketManager.SocketList.Count == 0)
        {
            EditorGUILayout.LabelField("No socket types defined yet.");
        }
        else
        {
            foreach (var socket in socketManager.SocketList)
            {
                EditorGUILayout.LabelField("• " + socket);
            }
        }
        EditorGUILayout.EndVertical();

        // Socket Compatibility Management Section
        GUILayout.Space(20);
        EditorGUILayout.LabelField("Manage Socket Compatibility", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (socketManager.SocketList.Count < 2)
        {
            EditorGUILayout.HelpBox("You need at least two socket types to manage compatibility.", MessageType.Info);
        }
        else
        {
            // Create string arrays for the popup
            string[] socketOptions = socketManager.SocketList.ToArray();

            EditorGUILayout.BeginHorizontal();
            selectedSocketA = EditorGUILayout.Popup(selectedSocketA, socketOptions, GUILayout.MinWidth(100));
            selectedSocketB = EditorGUILayout.Popup(selectedSocketB, socketOptions, GUILayout.MinWidth(100));
            EditorGUILayout.EndHorizontal();

            // Removed restriction - sockets should be compatible with themselves

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Compatibility"))
            {
                socketManager.SetCompatibility(socketOptions[selectedSocketA], socketOptions[selectedSocketB], true);
                EditorUtility.SetDirty(socketManager);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Remove Compatibility"))
            {
                socketManager.SetCompatibility(socketOptions[selectedSocketA], socketOptions[selectedSocketB], false);
                EditorUtility.SetDirty(socketManager);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            // Display compatibility dictionary
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Compatibility Dictionary:");

            EditorGUILayout.BeginVertical("box");
            Dictionary<string, List<string>> compatDict = socketManager.GetSocketCompDict();

            bool hasAnyCompatibility = false;
            foreach (var pair in compatDict)
            {
                if (pair.Value.Count > 0)
                {
                    hasAnyCompatibility = true;
                    break;
                }
            }

            if (!hasAnyCompatibility)
            {
                EditorGUILayout.LabelField("No compatibility relationships defined yet.");
            }
            else
            {
                foreach (var pair in compatDict)
                {
                    if (pair.Value.Count > 0)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField(pair.Key + " is compatible with:");

                        foreach (var compatSocket in pair.Value)
                        {
                            EditorGUILayout.LabelField("• " + compatSocket);
                        }
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }
}