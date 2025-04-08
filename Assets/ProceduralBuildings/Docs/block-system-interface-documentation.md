# BlockSystemInterface - Developer Documentation

The `BlockSystemInterface` is a facade for the procedural building system that simplifies interaction with the grid and block placement functionality. This guide provides an overview of how to use this interface in your custom building generation algorithms.

## Getting Started

1. Add the `BlockSystemInterface` component to a GameObject in your scene
2. Assign references to the required components:
   - `GridGenerator`
   - `BuildingBlocksManager`
   - `SocketManager`
3. Initialize the interface before use:

```csharp
BlockSystemInterface blockSystem = GetComponent<BlockSystemInterface>();
if (blockSystem.Initialize())
{
    // Ready to use
}
```

## Core Functionality

### Grid Information

Get information about the grid and its cells:

```csharp
// Check grid dimensions
Vector3Int dimensions = blockSystem.GetGridDimensions();

// Get cell size
Vector3 cellSize = blockSystem.GetCellSize();

// Check if a position is valid
bool isValid = blockSystem.IsValidGridPosition(new Vector3Int(x, y, z));

// Check if a cell is occupied
bool isOccupied = blockSystem.IsCellOccupied(new Vector3Int(x, y, z));

// Get a specific cell
GridGenerator.GridCell cell = blockSystem.GetCell(new Vector3Int(x, y, z));

// Get block at a position
BuildingBlock block = blockSystem.GetBlockAtPosition(new Vector3Int(x, y, z));

// Get socket at a position and direction
string socket = blockSystem.GetSocketAtPosition(new Vector3Int(x, y, z), Direction.Up);

// Check if a ground cell exists
bool hasGround = blockSystem.HasGroundCell(x, z);
```

### Block Placement

Place and remove building blocks:

```csharp
// Place a block by name
bool success = blockSystem.PlaceBlock("WallBlock", new Vector3Int(x, y, z), 
                                      tryAllRotations: true, useRandomRotation: false);

// Place a block by reference
BuildingBlock block = blockSystem.GetBlockByName("WallBlock");
bool success = blockSystem.PlaceBlock(block, new Vector3Int(x, y, z));

// Clear a cell
blockSystem.ClearCell(new Vector3Int(x, y, z));

// Find a valid block for a position
List<BuildingBlock> candidateBlocks = blockSystem.GetAllBuildingBlocks();
BuildingBlock validBlock = blockSystem.FindValidBlockForPosition(
    new Vector3Int(x, y, z), candidateBlocks, tryAllRotations: true
);

// Check if a block can be placed at a position
bool canPlace = blockSystem.IsBlockValidForPosition(block, new Vector3Int(x, y, z));
```

### Block Collection Management

Find and filter building blocks:

```csharp
// Get all available blocks
List<BuildingBlock> allBlocks = blockSystem.GetAllBuildingBlocks();

// Find block by name
BuildingBlock block = blockSystem.GetBlockByName("RoofCorner");

// Get blocks with a specific socket type
List<BuildingBlock> blocks = blockSystem.GetBlocksWithSocket("window", Direction.Front);

// Get blocks with any socket in a direction
List<BuildingBlock> blocksWithSockets = blockSystem.GetBlocksWithAnySocket(Direction.Up);

// Get blocks with compatible sockets
List<BuildingBlock> compatibleBlocks = blockSystem.GetBlocksWithCompatibleSocket("floor", Direction.Down);
```

### Utility Methods

Helper methods for grid navigation and socket compatibility:

```csharp
// Get neighboring position
Vector3Int neighbor = blockSystem.GetNeighborPosition(position, Direction.Up);

// Get opposite direction
Direction opposite = blockSystem.GetOppositeDirection(Direction.Front); // Returns Direction.Back

// Check if sockets are compatible
bool compatible = blockSystem.AreSocketsCompatible("floor", "ceiling");

// Get list of compatible socket types
List<string> compatibleSockets = blockSystem.GetCompatibleSockets("door");
```

## Building Generation Example

Here's a simple example of how to create a procedural wall:

```csharp
void GenerateWall(BlockSystemInterface blockSystem, int startX, int startZ, int length, int height)
{
    // Get foundation blocks
    List<BuildingBlock> foundationBlocks = blockSystem.GetBlocksWithSocket("foundation", Direction.Down);
    
    // Get wall blocks
    List<BuildingBlock> wallBlocks = blockSystem.GetBlocksWithSocket("wall", Direction.Down);
    
    // Place foundation
    for (int x = 0; x < length; x++)
    {
        Vector3Int position = new Vector3Int(startX + x, 0, startZ);
        
        if (!blockSystem.IsCellOccupied(position))
        {
            BuildingBlock foundationBlock = foundationBlocks[Random.Range(0, foundationBlocks.Count)];
            blockSystem.PlaceBlock(foundationBlock, position, tryAllRotations: true);
        }
    }
    
    // Place walls
    for (int y = 1; y < height; y++)
    {
        for (int x = 0; x < length; x++)
        {
            Vector3Int position = new Vector3Int(startX + x, y, startZ);
            
            if (!blockSystem.IsCellOccupied(position))
            {
                BuildingBlock wallBlock = wallBlocks[Random.Range(0, wallBlocks.Count)];
                blockSystem.PlaceBlock(wallBlock, position, tryAllRotations: true);
            }
        }
    }
}
```

## Advanced Usage: Building a House

For more complex structures like houses, you can use the interface to build hierarchically:

1. Generate a foundation grid
2. Add walls along the perimeter
3. Create door and window openings
4. Add floor and ceiling blocks
5. Place roof blocks on top

Each of these steps can be implemented as a separate method using the BlockSystemInterface, and combined to create a complete building generator.

## Performance Considerations

- The `IsBlockValidForPosition` method temporarily places a block and then removes it, which can be expensive for large-scale generation
- Consider caching results of commonly used queries like `GetBlocksWithSocket` when building large structures
- For complex algorithms, batch your placement operations rather than checking individual blocks repeatedly
