# Enhanced Procedural Level Generator - Instructions

## Overview
The SimpleProceduralGenerator has been enhanced with proper colliders, improved WFC (Wave Function Collapse) logic, and better room generation. This system creates modular levels with proper collision detection and realistic room layouts.

## Key Features
- **Proper Colliders**: All generated objects have appropriate colliders for realistic physics
- **Improved WFC Logic**: Better room connections using minimum spanning tree algorithm
- **Smart Corridor Generation**: Intelligent pathfinding between rooms
- **Automatic Prefab Detection**: Finds prefabs automatically or creates test prefabs
- **Room Validation**: Ensures generated levels are properly accessible
- **Player Character Support**: Includes scripts for testing the generated level

## How to Use

### Quick Start (No Setup Required)
1. Create an empty GameObject in your scene
2. Attach the `SimpleProceduralGenerator` script to it
3. Press Play - the system will automatically create test prefabs and generate a level
4. Use keyboard shortcuts:
   - **G**: Generate new level with same seed
   - **R**: Generate new level with random seed
   - **C**: Clear current level

### Using Your Own Prefabs
1. Create prefabs in your Assets/Prefabs folder with names containing:
   - "floor" - for floor tiles
   - "wall" - for wall pieces
   - "door" - for doors
   - "ceiling" - for ceiling pieces
   - "box", "wardrobe", "lamp" - for furniture
2. The system will automatically find and use these prefabs
3. Alternatively, manually assign prefabs in the inspector

### Settings Explained
- **Level Size**: Dimensions of the generated level (X, Y, Z)
- **Seed**: Random seed for reproducible generation (0 = random)
- **Room Size**: Minimum and maximum room dimensions
- **Room Density**: How much of the level should be filled with rooms (0-1)
- **Furniture Density**: How much furniture to place in rooms (0-1)
- **Auto Find Prefabs**: Whether to automatically search for prefabs

## Generated Objects and Colliders

### Floor Tiles
- **Collider**: Solid (non-trigger) BoxCollider
- **Purpose**: Walkable surfaces
- **Y Position**: 0 (ground level)

### Walls
- **Collider**: Solid (non-trigger) BoxCollider
- **Purpose**: Block player movement, create room boundaries
- **Y Position**: 1 (above floor)
- **Placement**: Automatically placed around room perimeters

### Doors
- **Collider**: Trigger BoxCollider
- **Purpose**: Allow passage between rooms
- **Y Position**: 1 (same as walls)
- **Placement**: Intelligent placement at room entrances
- **Script**: InteractableDoor component for future interaction

### Ceilings
- **Collider**: Trigger BoxCollider (non-blocking)
- **Purpose**: Visual ceiling coverage
- **Y Position**: 2 (above walls)

### Furniture
- **Collider**: Solid (non-trigger) BoxCollider
- **Purpose**: Decorative elements that block movement
- **Y Position**: 1 (on top of floors)
- **Placement**: Random placement inside rooms

## Advanced Features

### Smart Corridor Generation
- Uses minimum spanning tree algorithm to ensure all rooms are connected
- Generates L-shaped or diagonal corridors based on room layout
- Avoids creating unnecessary corridors
- 30% chance for extra connections to create more interesting layouts

### Room Connection Logic
- Finds optimal connection points on room edges
- Considers room positions and sizes for best pathfinding
- Creates varied corridor shapes for more interesting layouts

### Validation System
- Automatically checks generated level for common issues
- Reports missing floors, walls, or doors
- Validates collider setup on all objects
- Provides warnings for potential accessibility problems

## Testing the Level

### Method 1: Use Included Player Scripts
1. Uncomment the player creation code in SimpleProceduralGenerator
2. The system will create a test player with:
   - CharacterController for movement
   - Camera for first-person view
   - Mouse look controls
   - WASD movement + Space to jump

### Method 2: Manual Player Setup
1. Create a player GameObject with CharacterController
2. Add the SimplePlayerController script for movement
3. Add a camera as child with SimpleMouseLook script
4. Position player in generated level

### Method 3: Use Unity's First Person Controller
1. Import Unity's Standard Assets or create your own player
2. Position player in one of the generated rooms
3. Test collision with walls, floors, and doors

## Troubleshooting

### No Level Generated
- Check that at least floor prefab is assigned or auto-detection is enabled
- Verify the level size settings are reasonable (not too small)
- Check console for error messages

### Player Falls Through Floor
- Ensure floor prefabs have solid (non-trigger) colliders
- Check that floor tiles are being placed (Y position = 0)
- Verify player has CharacterController, not just Rigidbody

### Can Walk Through Walls
- Ensure wall prefabs have solid (non-trigger) colliders
- Check that walls are being placed around room perimeters
- Verify wall height covers player height

### Rooms Not Connected
- Check that corridor generation is working (see console logs)
- Verify door placement is functioning
- Ensure room density isn't too low

### Performance Issues
- Reduce level size for better performance
- Lower room density and furniture density
- Use simpler prefabs with fewer polygons

## Extending the System

### Adding New Object Types
1. Create new prefab categories in PlaceObject method
2. Add appropriate collider setup in EnsureProperColliders method
3. Extend the generation logic to place new objects

### Custom WFC Rules
1. Modify the room connection logic in GenerateRoomConnections
2. Add constraints based on room types or themes
3. Implement tile-based constraints for more complex layouts

### Interactive Elements
1. Add scripts to door prefabs for opening/closing
2. Create furniture with interaction components
3. Add scripted events for level completion

## Notes
- The system is designed to work with minimal setup
- All objects are properly configured for physics and collision
- Generated levels are always accessible (no isolated rooms)
- Seed-based generation allows for reproducible results
- The system scales well for different level sizes and complexities
