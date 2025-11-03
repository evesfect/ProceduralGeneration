# Procedural Building Generation System

**A socket-based modular building generation system for Unity**

Created by [evesfect](https://github.com/evesfect)

---

## Overview

Procedural Building Generation System enables the creation of complex 3D structures from modular building blocks using a constraint-based socket system. The system uses Wave Function Collapse-like algorithms with weighted random selection to generate coherent buildings that respect architectural rules and socket compatibility constraints.

Buildings are constructed by expanding from a seed position, testing block compatibility at each step, and selecting blocks based on configurable weight curves that can vary by height and distance from center.

---

## Features

-   **Socket-Based Constraint System** - Blocks connect only when adjacent sockets are compatible
-   **Wave Function Collapse Algorithm** - Frontier-based expansion ensures valid structures
-   **Weighted Random Selection** - Control block probability with height and distance curves
-   **Automatic Rotation Testing** - System tries all 90° rotations to find valid placements
-   **Custom Placement Rules** - Extensible rule system for architectural constraints
-   **BuildingStyle Profiles** - Create different architectural styles with the same block library
-   **Visual Editor Tools** - Intuitive Unity Editor interfaces for configuration
-   **Real-time or Delayed Generation** - Generate instantly or visualize step-by-step
-   **Orientation Correction System** - Automatically fixes mesh alignment issues

---

### Use Cases

-   **Procedural city generation** - Generate varied buildings with consistent architectural styles
-   **Modular level design** - Quickly prototype building layouts using block libraries
-   **Dynamic construction systems** - Build structures at runtime in response to player actions

---

### Performance Notes

-   Generation is CPU-bound; iteration limit prevents infinite loops (default: 100 iterations)
-   Socket compatibility checks use dictionary lookups (O(1) average case)
-   Weighted random selection is O(n) where n = number of valid candidate blocks
-   Delayed placement mode adds visualization delay but doesn't affect algorithm performance

---

## Technical Implementation

The system uses a multi-layered architecture combining constraint satisfaction, weighted random selection, and rule-based validation:

### 1. Socket Compatibility System

The foundation of the constraint system:

-   **SocketManager** defines socket types and bidirectional compatibility relationships
-   Each **BuildingBlock** has 6 directional sockets (Up, Down, Front, Back, Left, Right)
-   **GridGenerator** tests socket compatibility during placement via `AreSocketsCompatible()`
-   Ground cells act as boundary conditions with special "ground" sockets
-   Socket rotation handled automatically when blocks rotate around Y-axis

### 2. Wave Function Collapse Algorithm

**BuildingGenerator** implements a frontier-based expansion algorithm:

1.  Place initial "empty" block at starting position
2.  Maintain frontier list of empty neighbor cells adjacent to placed blocks
3.  For each frontier cell:
    -   Find valid blocks via `FindValidBlockForPosition()`:
        -   Test socket compatibility with all 4 rotations (0°, 90°, 180°, 270°)
        -   Apply **BlockRulesManager** constraints
        -   Calculate spatial weights from **BuildingStyle** curves
        -   Perform weighted random selection
    -   Place selected block, notify rules system
    -   Mark cell as invalid if no valid block found
4.  Expand frontier to include new empty neighbors
5.  Repeat until frontier exhausted or iteration limit reached

### 3. Weighted Selection System

**BuildingStyle** ScriptableObjects control block probabilities:

-   Base weight per block type
-   **Height curve** - Modulates weight based on normalized Y position (0=bottom, 1=top)
-   **Distance curve** - Modulates weight based on normalized distance from center
-   Final weight = `baseWeight × heightMultiplier × distanceMultiplier`

This enables architectural patterns like:

-   Foundations at bottom (high weight at Y=0)
-   Roofs at top (high weight at Y=1)
-   Decorative elements at perimeter (high weight at distance=1)

### 4. Block Placement Rules

**BlockRulesManager** organizes validation rules into groups:

-   **Global rules** - Apply to all blocks
-   **Group rules** - Apply to specific block groups
-   Rules implement `IsPlacementLegal()` to prevent invalid placements
-   Rules can implement `OnBlockPlaced()` for post-placement effects (e.g., prevent placing blocks below windows)

### 5. Grid and Orientation Management

**GridGenerator** maintains the 3D grid:

-   Tracks cell occupancy and socket information
-   Handles block rotation (socket remapping for 90°/180°/270° rotations)
-   Uses **BlockOrientationManager** to apply per-block rotation/position corrections for mesh alignment

---

## Requirements

-   **Unity Version:** Unity 6 (developed on 6000.0.34f1)
-   **Render Pipeline:** Universal Render Pipeline (URP) recommended but not required

---

## Setup

### Initial Configuration

1.  **Create Socket Types** (Tools > ProceduralBuildings > Socket Manager)
    
    -   Define socket types (e.g., "ground", "floor", "wall", "window", "roof")
    -   Set compatibility relationships (e.g., "floor" ↔ "ceiling")
2.  **Add Building Blocks** (Tools > ProceduralBuildings > Building Blocks Manager)
    
    -   Drag block prefabs into the manager
    -   Assign socket types to each block's six faces
    -   Ensure meshes are properly aligned (Y-down should face down)
3.  **Fix Block Orientations** (Tools > ProceduralBuildings > Block Orientation Editor) *if needed*
    
    -   Visually adjust blocks that don't align to grid
    -   Set rotation and position corrections per block
4.  **Create Building Style** (Create > ProceduralBuildings > Building Style)
    
    -   Assign weights to each block type
    -   Configure height and distance curves for architectural variation
5.  **Setup Rules** (Create > ProceduralBuildings > Block Rules Manager) *optional*
    
    -   Create rule assets (e.g., BottomBlockerRule)
    -   Organize into global rules or block groups

### Scene Setup

1.  Create empty GameObject, add **BlockSystemInterface** component
2.  Assign references:
    -   **GridGenerator** (add to same or child GameObject)
    -   **BuildingBlocksManager** asset
    -   **BlockOrientationManager** asset
    -   **SocketManager** asset
3.  Configure **GridGenerator**:
    -   Set grid dimensions (e.g., 10×5×10)
    -   Assign cell size prefab (any prefab with renderer for size reference)
    -   Set ground socket type (default: "ground")
    -   Configure which bottom cells have ground sockets
4.  Create GameObject with **BuildingGenerator** component
5.  Assign references:
    -   **BlockSystemInterface** from step 1
    -   **BuildingStyle** asset
    -   **BlockOrientationManager** asset
    -   **BlockRulesManager** asset (optional)
6.  Configure generation parameters:
    -   Starting position (default: 0,0,0)
    -   Center position for distance calculations
    -   Max distance for normalization
    -   Enable delayed placement for visualization (optional)

---

## Usage

### Using the BlockSystemInterface (Recommended)

The **BlockSystemInterface** provides a clean facade for custom generators:

```csharp
public class MyBuildingGenerator : MonoBehaviour{    [SerializeField] private BlockSystemInterface blockSystem;    void Start()    {        // Initialize the system        if (!blockSystem.Initialize())        {            Debug.LogError("Failed to initialize block system");            return;        }        // Get grid information        Vector3Int dimensions = blockSystem.GetGridDimensions();        Debug.Log($"Grid size: {dimensions}");        // Find blocks compatible with ground        List<BuildingBlock> foundationBlocks =            blockSystem.GetBlocksWithCompatibleSocket("ground", Direction.Down);        // Place a block (tries all rotations automatically)        Vector3Int position = new Vector3Int(5, 0, 5);        bool success = blockSystem.PlaceBlock(            foundationBlocks[0],            position,            tryAllRotations: true,            useRandomRotation: true        );        if (success)        {            Debug.Log("Block placed successfully!");        }    }}
```

### Using BuildingGenerator

The included **BuildingGenerator** implements a custom generation algorithm, you can define buildings with 2 dimension functions and limiters:

```csharp
// Runtime generationBuildingGenerator generator = FindObjectOfType<BuildingGenerator>();generator.GenerateBuildingImmediate();// Delayed generation (for visualization)// Set enableDelayedPlacement = true in inspector// Generation starts automatically on Start()
```

### Creating Custom Placement Rules

```csharp
using UnityEngine;[CreateAssetMenu(fileName = "NoFloatingBlocksRule",                 menuName = "ProceduralBuildings/Rules/No Floating Blocks")]public class NoFloatingBlocksRule : BlockRule{    public override bool IsPlacementLegal(        BuildingBlock block,        int yRotation,        Vector3Int position,        BuildingGenerator generator)    {        // Prevent placing blocks that float in the air        if (position.y > 0)        {            Vector3Int below = new Vector3Int(position.x, position.y - 1, position.z);            if (!generator.BlockSystem.IsCellOccupied(below))            {                return false; // Cell below is empty - would float            }        }        return true;    }}
```

Then:

1.  Create asset: Create > ProceduralBuildings > Rules > No Floating Blocks
2.  Add to BlockRulesManager (as global rule or in block groups)

### Custom Generator Algorithm - example

```csharp
public class CustomGenerator : MonoBehaviour{    [SerializeField] private BlockSystemInterface blockSystem;    [SerializeField] private BuildingStyle style;    public void GenerateCustomStructure()    {        blockSystem.Initialize();        // Example: Build a simple tower        Vector3Int basePosition = new Vector3Int(5, 0, 5);        int height = 10;        for (int y = 0; y < height; y++)        {            Vector3Int pos = new Vector3Int(basePosition.x, y, basePosition.z);            // Get blocks valid for this position            List<BuildingBlock> candidateBlocks = blockSystem.GetAllBuildingBlocks();            // Find a valid block (checks sockets and tries rotations)            foreach (BuildingBlock block in candidateBlocks)            {                if (blockSystem.IsBlockValidForPosition(block, pos, tryAllRotations: true))                {                    blockSystem.PlaceBlock(block, pos, tryAllRotations: true);                    break;                }            }        }    }}
```

---

## Editor Tools

-   **Socket Manager** - Define and configure socket types and compatibility
-   **Building Blocks Manager** - Add prefabs and assign sockets
-   **Block Orientation Editor** - Visually adjust block alignment with interactive preview
-   **Building Style Editor** - Configure weight curves with visual feedback
-   **Block Rules Manager** - Organize and configure placement rules

Access via: **Tools > ProceduralBuildings > [Tool Name]**

---

## Credits

Created by **evesfect**

Built with Unity 6000.0.34f1 LTS and Universal Render Pipeline.
