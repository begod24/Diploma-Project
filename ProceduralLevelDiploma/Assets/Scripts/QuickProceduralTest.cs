using UnityEngine;

/// <summary>
/// Quick setup script for testing the procedural level generator
/// Add this to an empty GameObject and press play to instantly test the system
/// </summary>
public class QuickProceduralTest : MonoBehaviour
{
    [Header("Quick Test Settings")]
    [SerializeField] private bool createPlayerOnStart = true;
    [SerializeField] private bool addLighting = true;
    [SerializeField] private Vector3Int testLevelSize = new Vector3Int(15, 3, 15);
    
    void Start()
    {
        // Set up the procedural generator
        SetupProceduralGenerator();
        
        // Add basic lighting if needed
        if (addLighting)
        {
            SetupLighting();
        }
        
        // Create a player if requested
        if (createPlayerOnStart)
        {
            Invoke("CreateTestPlayer", 2f); // Wait for level generation to complete
        }
    }
    
    void SetupProceduralGenerator()
    {
        // Check if we already have a procedural generator
        SimpleProceduralGenerator existing = FindObjectOfType<SimpleProceduralGenerator>();
        if (existing != null)
        {
            Debug.Log("✓ Procedural generator already exists in scene");
            return;
        }
        
        // Create procedural generator
        GameObject generatorGO = new GameObject("Procedural Level Generator");
        SimpleProceduralGenerator generator = generatorGO.AddComponent<SimpleProceduralGenerator>();
        
        // Configure for quick testing
        generator.GetType().GetField("levelSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(generator, testLevelSize);
        generator.GetType().GetField("useRandomSeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(generator, true);
        generator.GetType().GetField("autoFindPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(generator, true);
        
        Debug.Log("✓ Created procedural generator with quick test settings");
    }
    
    void SetupLighting()
    {
        // Check if we have adequate lighting
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
            // Create directional light
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            
            Debug.Log("✓ Created directional light for testing");
        }
        
        // Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.3f);
    }
    
    void CreateTestPlayer()
    {
        // Only create if no player exists
        if (FindObjectOfType<CharacterController>() != null)
        {
            Debug.Log("Player already exists in scene");
            return;
        }
        
        // Find a spawn position (center of first generated room)
        SimpleProceduralGenerator generator = FindObjectOfType<SimpleProceduralGenerator>();
        Vector3 spawnPos = new Vector3(testLevelSize.x * 0.5f, 2f, testLevelSize.z * 0.5f);
        
        if (generator != null)
        {
            // Try to find a better spawn position on an actual floor
            GameObject[] floors = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject obj in floors)
            {
                if (obj.name.Contains("Floor"))
                {
                    spawnPos = obj.transform.position + Vector3.up * 1.5f;
                    break;
                }
            }
        }
        
        // Create basic player capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Test Player";
        player.transform.position = spawnPos;
        player.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        
        // Set player color to bright red for visibility
        Renderer renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            renderer.material = mat;
        }
        
        // Replace capsule collider with character controller
        DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = Vector3.zero;
        
        // Add the player controller script if it exists
        if (FindObjectOfType<SimplePlayerController>() == null)
        {
            try
            {
                player.AddComponent<SimplePlayerController>();
            }
            catch
            {
                Debug.LogWarning("SimplePlayerController not found - add movement script manually");
            }
        }
        
        // Create camera
        GameObject cameraGO = new GameObject("Player Camera");
        cameraGO.transform.parent = player.transform;
        cameraGO.transform.localPosition = new Vector3(0, 0.6f, 0);
        
        Camera cam = cameraGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        
        // Disable any existing main camera
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera c in cameras)
        {
            if (c != cam && c.CompareTag("MainCamera"))
            {
                c.gameObject.SetActive(false);
            }
        }
        
        // Add mouse look if script exists
        try
        {
            cameraGO.AddComponent<SimpleMouseLook>();
        }
        catch
        {
            Debug.LogWarning("SimpleMouseLook not found - add manually for mouse look");
        }
        
        Debug.Log($"✓ Created test player at {spawnPos}");
        Debug.Log("Controls: WASD to move, Mouse to look, Space to jump, Escape to toggle cursor");
    }
    
    void Update()
    {
        // Quick test controls
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // Show help
            Debug.Log("=== QUICK TEST CONTROLS ===");
            Debug.Log("F1: Show this help");
            Debug.Log("F2: Create new player");
            Debug.Log("G: Generate new level");
            Debug.Log("R: Generate with new seed");
            Debug.Log("C: Clear level");
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CreateTestPlayer();
        }
    }
}
