# Procedural Level Generation System

This Unity project implements a comprehensive procedural level generation system with the following features:

## Features

### Player Input System
- **PlayerInputManager**: Complete input handling for player movement, interaction, and camera control
- Supports keyboard, mouse, and gamepad input
- Configurable settings for sensitivity, movement speeds, and input mappings
- Event-driven architecture for easy integration

### Procedural Generation
- **Seed-based reproducibility**: Generate the same level with the same seed
- **Simplified Wave Function Collapse**: Ensures tiles connect properly based on rules
- **Modular tile system**: Uses your existing prefabs to build levels
- **Room and corridor generation**: Creates realistic building layouts
- **Multi-level support**: Can generate multiple floors

### Interactive System
- **IInteractable interface**: Standard interaction system
- **InteractableDoor**: Animated doors with sound effects
- **InteractableObject**: Generic interactable objects
- **InteractionUI**: User interface for interactions

## Setup Instructions

### 1. Basic Setup
1. Open your Unity project
2. Ensure you have the Unity Input System package installed
3. Your prefabs should be in the `Assets/Prefabs` folder

### 2. Create Tile Rules
The system automatically creates tile rules for your prefabs based on their names:
- Names containing "floor" → Floor tiles
- Names containing "wall" → Wall tiles  
- Names containing "door" → Door tiles
- Names containing "ceiling" → Ceiling tiles
- Names containing "box", "wardrobe", "lamp" → Furniture tiles

### 3. Scene Setup
1. Create an empty GameObject and add the `ProceduralLevelGenerator` component
2. Create another GameObject and add the `LevelGeneratorManager` component
3. Link the ProceduralLevelGenerator to the LevelGeneratorManager
4. Optionally add the `LevelGenerationExample` component for quick testing

### 4. Player Setup
1. Use your existing Player prefab or create a new one
2. Add the `PlayerInputManager` component to your player
3. Assign references:
   - CharacterController component
   - Camera transform
   - Set interaction distance and layer mask

### 5. UI Setup (Optional)
1. Create a Canvas for the interaction UI
2. Add the `InteractionUI` component
3. Create UI elements for interaction prompts

## Usage

### Generating Levels
```csharp
// Get the generator
ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();

// Set a specific seed for reproducible generation
generator.SetSeed(12345);

// Generate the level
generator.GenerateLevel();
```

### Configuring Generation Settings
```csharp
GenerationSettings settings = generator.settings;
settings.levelSize = new Vector3Int(30, 3, 30);  // 30x3x30 level
settings.roomDensity = 0.6f;  // 60% of space filled with rooms
settings.furnitureDensity = 0.2f;  // 20% furniture placement
settings.useRandomSeed = false;  // Use specific seed
settings.seed = 12345;
```

### Creating Custom Interactables
```csharp
public class CustomInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Custom interaction!");
    }
    
    public void OnHighlightStart()
    {
        // Add visual highlight
    }
    
    public void OnHighlightEnd()
    {
        // Remove visual highlight
    }
    
    public string GetInteractionText()
    {
        return "Use Item";
    }
    
    public bool CanInteract()
    {
        return true;
    }
}
```

## Keyboard Controls (when using LevelGenerationExample)

- **G** - Generate new level
- **C** - Clear current level
- **R** - Generate with random seed
- **1** - Small level preset
- **2** - Medium level preset  
- **3** - Large level preset

## Player Controls

- **WASD** - Move
- **Mouse** - Look around
- **Space** - Jump
- **Left Shift** - Sprint
- **Left Ctrl** - Crouch
- **E** - Interact
- **Left Mouse** - Attack

## Customization

### Adding New Tile Types
1. Create a new TileRule ScriptableObject
2. Set the tile type, prefab, and connection rules
3. Add to the ProceduralLevelGenerator's availableTiles list

### Modifying Generation Algorithm
- Edit `ProceduralLevelGenerator.cs`
- Modify room generation in `GenerateRoomLayout()`
- Adjust corridor generation in `GenerateCorridors()`
- Customize Wave Function Collapse in `RunWFC()`

### Creating Generation Presets
Use `LevelGeneratorManager.CreateDefaultPresets()` or create custom presets:
```csharp
GenerationPreset customPreset = new GenerationPreset
{
    name = "Custom Layout",
    settings = new GenerationSettings
    {
        levelSize = new Vector3Int(20, 2, 20),
        roomDensity = 0.4f,
        furnitureDensity = 0.15f
    }
};
```

## Architecture Overview

### Core Components
- **ProceduralLevelGenerator**: Main generation logic
- **TileRule**: Defines how tiles can be placed and connected
- **LevelGeneratorManager**: UI and management layer
- **PlayerInputManager**: Handles all player input

### Data Flow
1. Generate room layout using cellular automata
2. Connect rooms with corridors  
3. Initialize Wave Function Collapse with room/corridor constraints
4. Collapse cells based on tile rules and connectivity
5. Place furniture and decorations
6. Instantiate GameObjects from tile rules

### Wave Function Collapse
The system uses a simplified WFC approach:
- Each grid position starts with all possible tiles
- Constraints propagate based on tile connection rules
- Lowest entropy positions are collapsed first
- Invalid configurations are avoided through proper rule definition

## Troubleshooting

### Common Issues
1. **No tiles generated**: Check that tile rules are properly assigned
2. **Tiles don't connect**: Verify connector settings in TileRule
3. **Performance issues**: Reduce level size or increase yield frequency
4. **Player falls through floor**: Ensure floor tiles have proper colliders

### Debug Features
- Enable `showDebugGizmos` to visualize generation bounds
- Enable `logGenerationSteps` for detailed console output
- Use Scene view to inspect generated tile positions

## Performance Considerations

- Level generation runs over multiple frames using coroutines
- Large levels (>50x50) may take several seconds to generate
- Consider using object pooling for frequently spawned prefabs
- Furniture and decoration placement can be disabled for better performance

## Extending the System

The system is designed to be modular and extensible:
- Add new tile types by extending the TileType enum
- Create custom generation algorithms by inheriting from ProceduralLevelGenerator
- Implement new interaction types using IInteractable
- Add visual effects and animations to enhance the generation process
