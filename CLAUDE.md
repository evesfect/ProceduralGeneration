# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 procedural building generation system that uses a socket-based architecture to generate 3D buildings from modular blocks. The system uses Wave Function Collapse-like algorithms with weighted random selection and constraint satisfaction to create coherent structures.

## Development Environment

This is a Unity project - there are no traditional build/test commands. Development is done within the Unity Editor.

**Opening the project:**
- Open Unity Hub and add this project folder
- Unity 6 is required (see ProjectSettings/ProjectVersion.txt)

**Unity Editor workflows:**
- Tools > ProceduralBuildings > Socket Manager - Configure socket types and compatibility
- Tools > ProceduralBuildings > Building Blocks Manager - Add block prefabs and assign sockets
- Tools > ProceduralBuildings > Block Orientation Editor - Adjust block rotation/position corrections
- Tools > ProceduralBuildings > Block Rules Manager - Configure placement rules

## Core Architecture

### Socket System (Connection-Based Placement)

The socket system is the fundamental constraint mechanism. Blocks can only connect when their adjacent sockets are compatible:

- **SocketManager** (ScriptableObject): Defines socket types and bidirectional compatibility relationships
- Each BuildingBlock has 6 directional sockets (Up, Down, Front, Back, Left, Right)
- Socket compatibility is checked during placement via `GridGenerator.AreSocketsCompatible()`
- Ground cells have special "ground" sockets that act as boundary conditions

### Block System Interface (Facade Pattern)

**BlockSystemInterface** is the primary API for building generation algorithms. It provides:
- Grid queries: dimensions, cell occupancy, neighbor positions
- Block placement: with automatic rotation testing
- Block filtering: by socket type, compatibility
- Socket operations: compatibility checking, retrieving compatible sockets

See: Assets/ProceduralBuildings/Docs/block-system-interface-documentation.md

### Core Components

**GridGenerator** (MonoBehaviour):
- Manages the 3D grid of cells with socket information
- Handles block placement with rotation (0°, 90°, 180°, 270°)
- Tests placement validity via socket compatibility
- Maintains ground cell configuration for boundary conditions
- Uses BlockOrientationManager to correct mesh alignment issues

**BuildingGenerator** (MonoBehaviour):
- Implements the wave function collapse algorithm
- Uses frontier-based expansion: starts from a seed cell, expands to empty neighbors
- Weighted random selection using BuildingStyle curves (height and distance factors)
- Integrates BlockRulesManager for placement constraints
- Supports delayed placement mode for visualization/debugging

**BuildingBlocksManager** (ScriptableObject):
- Registry of all available building block types
- Each block has name, prefab, and 6 directional socket assignments

**BuildingStyle** (ScriptableObject):
- Defines weighted probabilities for block selection
- Supports spatial weight curves: height-based and distance-from-center
- Allows creating different architectural styles with same block library

**BlockOrientationManager** (ScriptableObject):
- Stores per-block rotation and position corrections
- Fixes alignment issues when block meshes don't match grid expectations
- Corrections applied in GridGenerator.AlignBuildingBlock()

### Block Placement Rules System

**BlockRulesManager** (ScriptableObject):
- Organizes rules into groups (global rules + per-block-group rules)
- Rules are checked before placement via `IsPlacementLegal()`
- Rules can react after placement via `OnBlockPlaced()` callback
- Example: BottomBlockerRule prevents placing blocks below certain types

**BlockRule** (ScriptableObject base class):
- Abstract base for custom placement constraints
- Implement `IsPlacementLegal()` for validation logic
- Override `OnBlockPlaced()` for post-placement effects

## Key Algorithms

### Block Placement Algorithm (BuildingGenerator.cs:91-182)

1. Start with empty block at starting position
2. Maintain frontier list of empty neighbor cells
3. For each frontier cell:
   - Find valid blocks using `FindValidBlockForPosition()` which:
     - Tests socket compatibility with all rotations
     - Applies BlockRulesManager constraints
     - Calculates spatial weights from BuildingStyle
     - Performs weighted random selection
   - Place selected block, notify rules
   - Mark cell as invalid if no valid block found
4. Repeat until frontier is empty or iteration limit reached

### Socket Rotation System (GridGenerator.cs:1059-1103)

When blocks rotate around Y-axis, their horizontal sockets rotate:
- 90°: Right→Front, Front→Left, Left→Back, Back→Right
- 180°: Front↔Back, Left↔Right
- 270°: Right→Back, Back→Left, Left→Front, Front→Right
- Up/Down sockets never change with Y-rotation

## Important File Locations

**Core Scripts:**
- `Assets/ProceduralBuildings/Scripts/BlockSystem/BlockSystemInterface.cs` - Main API facade
- `Assets/ProceduralBuildings/Scripts/BuildingGenerator.cs` - Generation algorithm
- `Assets/ProceduralBuildings/Scripts/BlockSystem/GridGenerator.cs` - Grid management & placement
- `Assets/ProceduralBuildings/Scripts/BlockSystem/BuildingBlocksManager.cs` - Block registry
- `Assets/ProceduralBuildings/Scripts/BlockSystem/SocketManager.cs` - Socket definitions
- `Assets/ProceduralBuildings/Scripts/BlockSystem/BuildingStyle.cs` - Weight curves
- `Assets/ProceduralBuildings/Scripts/BlockSystem/BlockRules/BlockRulesManager.cs` - Rules system
- `Assets/ProceduralBuildings/Scripts/BlockSystem/BlockOrientation/BlockOrientationManager.cs` - Alignment corrections

**Editor Tools:**
- `Assets/Editor/BuildingStyleEditor.cs` - Custom inspector for BuildingStyle
- `Assets/Editor/BlockOrientationEditorWindow.cs` - Visual block alignment tool
- `Assets/Editor/BlockRulesManagerEditor.cs` - Rules configuration UI

**Data Assets:** (ScriptableObjects stored in Assets/ProceduralBuildings/Data/)
- BuildingBlocksManager.asset
- SocketManager.asset
- BlockOrientationManager.asset
- BlockRulesManager.asset
- BuildingStyle assets

## Common Patterns

### Creating a New Building Generator

```csharp
// Use BlockSystemInterface, not direct GridGenerator access
public class MyGenerator : MonoBehaviour {
    [SerializeField] BlockSystemInterface blockSystem;

    void Start() {
        if (!blockSystem.Initialize()) return;

        // Get all blocks with compatible bottom sockets
        List<BuildingBlock> validBlocks =
            blockSystem.GetBlocksWithCompatibleSocket("ground", Direction.Down);

        // Place at position with automatic rotation testing
        blockSystem.PlaceBlock(validBlocks[0], new Vector3Int(0,0,0),
                               tryAllRotations: true);
    }
}
```

### Creating Custom Placement Rules

1. Create new ScriptableObject class inheriting from BlockRule
2. Implement `IsPlacementLegal()` - return false to prevent placement
3. Optionally override `OnBlockPlaced()` for side effects
4. Create asset via Create > ProceduralBuildings > [Your Rule]
5. Add to BlockRulesManager (global or group-specific)

### Understanding Socket Compatibility

Socket compatibility is **bidirectional**. If "wall" is compatible with "window", then "window" is automatically compatible with "wall". This is managed by SocketManager.SetCompatibility().

## Unity-Specific Notes

- This is NOT a .NET/C# project with csproj files - Unity uses its own build system
- Code changes are compiled automatically by Unity Editor
- Scene files (.unity) contain GameObject hierarchies with component references
- Prefabs (.prefab) are reusable GameObject templates for blocks
- ScriptableObjects (.asset) store data outside of scenes (sockets, blocks, styles, rules)

## Debugging Tips

- Enable `BuildingGenerator.enableDelayedPlacement` to visualize step-by-step generation
- Enable `GridGenerator.showSocketGizmos` to see socket assignments in Scene view
- Debug.Log statements appear in Unity Console window
- Grid visualization shows cyan wireframes (empty) and green (occupied)
