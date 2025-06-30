using UnityEngine;
using System.Collections.Generic;

public class QuickLevelSetup : MonoBehaviour
{
    [Header("Quick Setup Guide")]
    [TextArea(10, 20)]
    public string setupInstructions = @"MANUAL SCENE SETUP GUIDE:

1. LEVEL GENERATOR SETUP:
   - Create empty GameObject → Name it 'Level Generator'
   - Add 'ProceduralLevelGenerator' component
   - Set levelParent to an empty GameObject named 'Generated Level'

2. TILE RULES SETUP:
   - Create TileRule ScriptableObjects for each prefab:
     * Floor: TileType.Floor, connects up=Open, down=Floor
     * Wall: TileType.Wall, all sides=Wall, down=Floor  
     * Door: TileType.Door, north/south=Door, down=Floor
     * Furniture: TileType.Furniture, down=Floor, requiresSupport=true
   - Add all TileRules to availableTiles list

3. MANAGER SETUP:
   - Create empty GameObject → Name it 'Level Manager' 
   - Add 'LevelGeneratorManager' component
   - Link levelGenerator reference

4. PLAYER SETUP:
   - Add your Player prefab or create player GameObject
   - Add 'PlayerInputManager' component
   - Set cameraTransform to player's camera

5. PREFAB SETUP:
   - Add 'Interactable' tag to door and furniture prefabs
   - Add 'InteractableDoor' component to door prefab
   - Add 'InteractableObject' component to furniture prefabs

6. TESTING:
   - Add 'LevelGenerationExample' component anywhere for keyboard controls
   - Press G in play mode to generate!";

    [Header("Auto Create Missing Components")]
    public bool createLevelGenerator = true;
    public bool createLevelManager = true; 
    public bool createPlayer = true;
    public bool setupLighting = true;

    [Header("References")]
    public GameObject[] availablePrefabs;
    public GameObject playerPrefab;

    [ContextMenu("Quick Setup Scene")]
    public void QuickSetupScene()
    {
        Debug.Log("=== QUICK LEVEL SETUP START ===");

        if (createLevelGenerator)
            SetupLevelGenerator();

        if (createLevelManager) 
            SetupLevelManager();

        if (createPlayer)
            SetupPlayer();

        if (setupLighting)
            SetupBasicLighting();

        Debug.Log("=== QUICK LEVEL SETUP COMPLETE ===");
        Debug.Log("Next steps:");
        Debug.Log("1. Assign your prefabs to the Level Generator's Available Tiles");
        Debug.Log("2. Configure tile rules (or use SceneSetupHelper for auto-config)");
        Debug.Log("3. Press G in play mode to generate a level!");
    }

    private void SetupLevelGenerator()
    {
        GameObject generatorGO = GameObject.Find("Level Generator");
        if (generatorGO == null)
        {
            generatorGO = new GameObject("Level Generator");
            Debug.Log("✓ Created Level Generator GameObject");
        }

        ProceduralLevelGenerator generator = generatorGO.GetComponent<ProceduralLevelGenerator>();
        if (generator == null)
        {
            generator = generatorGO.AddComponent<ProceduralLevelGenerator>();
            Debug.Log("✓ Added ProceduralLevelGenerator component");
        }

        // Create level parent
        GameObject levelParent = GameObject.Find("Generated Level");
        if (levelParent == null)
        {
            levelParent = new GameObject("Generated Level");
            Debug.Log("✓ Created Generated Level parent object");
        }

        generator.levelParent = levelParent.transform;

        // Configure basic settings
        generator.settings.levelSize = new Vector3Int(20, 3, 20);
        generator.settings.roomDensity = 0.5f;
        generator.settings.furnitureDensity = 0.2f;
        generator.settings.useRandomSeed = true;
        generator.generateOnStart = false; // Manual generation
        generator.showDebugGizmos = true;

        Debug.Log("✓ Level Generator configured with default settings");
    }

    private void SetupLevelManager()
    {
        GameObject managerGO = GameObject.Find("Level Manager");
        if (managerGO == null)
        {
            managerGO = new GameObject("Level Manager");
            Debug.Log("✓ Created Level Manager GameObject");
        }

        LevelGeneratorManager manager = managerGO.GetComponent<LevelGeneratorManager>();
        if (manager == null)
        {
            manager = managerGO.AddComponent<LevelGeneratorManager>();
            Debug.Log("✓ Added LevelGeneratorManager component");
        }

        // Link to level generator
        ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();
        if (generator != null)
        {
            manager.levelGenerator = generator;
            Debug.Log("✓ Linked Level Manager to Level Generator");
        }

        // Add example component for testing
        if (managerGO.GetComponent<LevelGenerationExample>() == null)
        {
            managerGO.AddComponent<LevelGenerationExample>();
            Debug.Log("✓ Added LevelGenerationExample for keyboard controls");
        }
    }

    private void SetupPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player == null && playerPrefab != null)
        {
            player = Instantiate(playerPrefab);
            player.name = "Player";
            Debug.Log("✓ Created Player from prefab");
        }
        else if (player == null)
        {
            // Create basic player
            player = new GameObject("Player");
            player.tag = "Player";

            // Add character controller
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0, 1f, 0);

            // Create camera
            GameObject cameraGO = new GameObject("Player Camera");
            cameraGO.transform.SetParent(player.transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.7f, 0);
            cameraGO.tag = "MainCamera";
            
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cameraGO.AddComponent<AudioListener>();

            Debug.Log("✓ Created basic Player with camera");
        }

        // Add input manager
        PlayerInputManager inputManager = player.GetComponent<PlayerInputManager>();
        if (inputManager == null)
        {
            inputManager = player.AddComponent<PlayerInputManager>();
            
            // Find camera
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                inputManager.SetCameraTransform(playerCamera.transform);
            }
            
            Debug.Log("✓ Added PlayerInputManager to player");
        }

        // Position player
        player.transform.position = new Vector3(10f, 2f, 10f);
    }

    private void SetupBasicLighting()
    {
        // Check if directional light exists
        Light[] lights = FindObjectsOfType<Light>();
        bool hasDirectionalLight = false;
        
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                hasDirectionalLight = true;
                break;
            }
        }

        if (!hasDirectionalLight)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            Debug.Log("✓ Created Directional Light");
        }

        // Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.3f);

        Debug.Log("✓ Configured ambient lighting");
    }

    [ContextMenu("Create Example Tile Rules")]
    public void CreateExampleTileRules()
    {
        if (availablePrefabs == null || availablePrefabs.Length == 0)
        {
            Debug.LogWarning("Please assign prefabs to availablePrefabs array first!");
            return;
        }

        ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();
        if (generator == null)
        {
            Debug.LogError("No ProceduralLevelGenerator found! Create one first.");
            return;
        }

        generator.availableTiles.Clear();

        foreach (GameObject prefab in availablePrefabs)
        {
            if (prefab == null) continue;

            TileRule rule = CreateTileRuleForPrefab(prefab);
            generator.availableTiles.Add(rule);
            Debug.Log($"✓ Created tile rule for {prefab.name}");
        }

        Debug.Log($"✓ Created {generator.availableTiles.Count} tile rules");
    }

    private TileRule CreateTileRuleForPrefab(GameObject prefab)
    {
        TileRule rule = ScriptableObject.CreateInstance<TileRule>();
        rule.tileName = prefab.name;
        rule.prefab = prefab;

        string prefabName = prefab.name.ToLower();

        if (prefabName.Contains("floor"))
        {
            rule.tileType = TileType.Floor;
            rule.connector.up = ConnectionType.Open;
            rule.connector.down = ConnectionType.Floor;
            rule.spawnWeight = 1f;
        }
        else if (prefabName.Contains("wall"))
        {
            rule.tileType = TileType.Wall;
            rule.connector.north = ConnectionType.Wall;
            rule.connector.south = ConnectionType.Wall;
            rule.connector.east = ConnectionType.Wall;
            rule.connector.west = ConnectionType.Wall;
            rule.connector.down = ConnectionType.Floor;
            rule.spawnWeight = 0.8f;
        }
        else if (prefabName.Contains("door"))
        {
            rule.tileType = TileType.Door;
            rule.connector.north = ConnectionType.Door;
            rule.connector.south = ConnectionType.Door;
            rule.connector.down = ConnectionType.Floor;
            rule.spawnWeight = 0.1f;
            rule.maxInstances = 15;
        }
        else if (prefabName.Contains("ceiling"))
        {
            rule.tileType = TileType.Ceiling;
            rule.connector.down = ConnectionType.Open;
            rule.connector.up = ConnectionType.None;
            rule.spawnWeight = 0.7f;
        }
        else if (prefabName.Contains("box") || prefabName.Contains("wardrobe") || prefabName.Contains("lamp"))
        {
            rule.tileType = TileType.Furniture;
            rule.connector.down = ConnectionType.Floor;
            rule.spawnWeight = 0.3f;
            rule.requiresSupport = true;
            rule.canRotate = true;
        }
        else
        {
            // Default to empty/decoration
            rule.tileType = TileType.Empty;
            rule.spawnWeight = 0.1f;
        }

        return rule;
    }

    private void OnValidate()
    {
        // Update instructions based on current setup
        if (Application.isPlaying) return;

        bool hasGenerator = FindObjectOfType<ProceduralLevelGenerator>() != null;
        bool hasManager = FindObjectOfType<LevelGeneratorManager>() != null;
        bool hasPlayer = GameObject.FindWithTag("Player") != null;

        string status = "CURRENT SCENE STATUS:\n";
        status += hasGenerator ? "✓ Level Generator Found\n" : "✗ Level Generator Missing\n";
        status += hasManager ? "✓ Level Manager Found\n" : "✗ Level Manager Missing\n";
        status += hasPlayer ? "✓ Player Found\n" : "✗ Player Missing\n";
        status += "\n" + setupInstructions;

        setupInstructions = status;
    }
}
