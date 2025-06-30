using UnityEngine;

[System.Serializable]
public class SceneSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnAwake = true;
    
    [Header("Prefab References")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject ceilingPrefab;
    [SerializeField] private GameObject[] furniturePrefabs;
    [SerializeField] private GameObject playerPrefab;
    
    [Header("Scene Objects")]
    [SerializeField] private Transform levelParent;
    [SerializeField] private Light mainLight;
    [SerializeField] private Camera mainCamera;
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupScene();
        }
    }
    
    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        Debug.Log("=== STARTING SCENE SETUP ===");
        
        CreateLevelGenerationSystem();
        SetupLighting();
        SetupCamera();
        SetupPlayer();
        
        // Check if we can auto-setup prefabs, otherwise show instructions
#if UNITY_EDITOR
        Debug.Log("Auto-setting up prefabs in editor...");
        SetupPrefabsInEditor();
#else
        TagInteractables(); // Show manual setup instructions
#endif
        
        Debug.Log("=== SCENE SETUP COMPLETE ===");
        Debug.Log("You can now use the Level Generation system! Press G in play mode to generate a level.");
    }
    
    private void CreateLevelGenerationSystem()
    {
        // Create level generator
        GameObject generatorGO = new GameObject("Level Generator");
        ProceduralLevelGenerator generator = generatorGO.AddComponent<ProceduralLevelGenerator>();
        
        // Create level parent
        if (levelParent == null)
        {
            GameObject levelParentGO = new GameObject("Generated Level");
            levelParent = levelParentGO.transform;
        }
        
        generator.levelParent = levelParent;
        
        // Auto-assign prefabs if available
        generator.availableTiles.Clear();
        
        if (floorPrefab != null)
            generator.availableTiles.Add(CreateTileRule("Floor", TileType.Floor, floorPrefab));
        
        if (wallPrefab != null)
            generator.availableTiles.Add(CreateTileRule("Wall", TileType.Wall, wallPrefab));
        
        if (doorPrefab != null)
            generator.availableTiles.Add(CreateTileRule("Door", TileType.Door, doorPrefab));
        
        if (ceilingPrefab != null)
            generator.availableTiles.Add(CreateTileRule("Ceiling", TileType.Ceiling, ceilingPrefab));
        
        foreach (var furniture in furniturePrefabs)
        {
            if (furniture != null)
                generator.availableTiles.Add(CreateTileRule(furniture.name, TileType.Furniture, furniture));
        }
        
        // Create manager
        GameObject managerGO = new GameObject("Level Manager");
        LevelGeneratorManager manager = managerGO.AddComponent<LevelGeneratorManager>();
        manager.levelGenerator = generator;
        
        // Add example component for testing
        managerGO.AddComponent<LevelGenerationExample>();
        
        Debug.Log("Level generation system created successfully!");
    }
    
    private TileRule CreateTileRule(string name, TileType type, GameObject prefab)
    {
        TileRule rule = ScriptableObject.CreateInstance<TileRule>();
        rule.tileName = name;
        rule.tileType = type;
        rule.prefab = prefab;
        
        // Configure based on type
        switch (type)
        {
            case TileType.Floor:
                rule.connector.up = ConnectionType.Open;
                rule.connector.down = ConnectionType.Floor;
                rule.spawnWeight = 1f;
                rule.canPlaceOnFloor = true;
                break;
                
            case TileType.Wall:
                rule.connector.north = ConnectionType.Wall;
                rule.connector.south = ConnectionType.Wall;
                rule.connector.east = ConnectionType.Wall;
                rule.connector.west = ConnectionType.Wall;
                rule.connector.down = ConnectionType.Floor;
                rule.spawnWeight = 0.8f;
                rule.canPlaceOnFloor = true;
                break;
                
            case TileType.Door:
                rule.connector.north = ConnectionType.Door;
                rule.connector.south = ConnectionType.Door;
                rule.connector.down = ConnectionType.Floor;
                rule.spawnWeight = 0.1f;
                rule.maxInstances = 20;
                rule.canPlaceOnFloor = true;
                break;
                
            case TileType.Ceiling:
                rule.connector.down = ConnectionType.Open;
                rule.connector.up = ConnectionType.None;
                rule.spawnWeight = 0.7f;
                rule.canPlaceOnCeiling = true;
                break;
                
            case TileType.Furniture:
                rule.connector.down = ConnectionType.Floor;
                rule.spawnWeight = 0.3f;
                rule.requiresSupport = true;
                rule.canRotate = true;
                rule.canPlaceOnFloor = true;
                break;
        }
        
        return rule;
    }
    
    private void SetupLighting()
    {
        if (mainLight == null)
        {
            GameObject lightGO = new GameObject("Main Light");
            mainLight = lightGO.AddComponent<Light>();
        }
        
        mainLight.type = LightType.Directional;
        mainLight.color = Color.white;
        mainLight.intensity = 1f;
        mainLight.shadows = LightShadows.Soft;
        mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        
        // Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.3f);
    }
    
    private void SetupCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
        }
        
        mainCamera.fieldOfView = 75f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 1000f;
        
        // Add audio listener if not present
        if (mainCamera.GetComponent<AudioListener>() == null)
            mainCamera.gameObject.AddComponent<AudioListener>();
    }
    
    private void SetupPlayer()
    {
        GameObject player = null;
        
        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab);
            player.name = "Player";
        }
        else
        {
            // Create basic player
            player = new GameObject("Player");
            
            // Add capsule collider
            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0, 1f, 0);
            
            // Add character controller
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0, 1f, 0);
            
            // Move camera to player if it exists
            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(player.transform);
                mainCamera.transform.localPosition = new Vector3(0, 1.7f, 0);
                mainCamera.transform.localRotation = Quaternion.identity;
            }
        }
        
        // Add input manager
        if (player.GetComponent<PlayerInputManager>() == null)
        {
            PlayerInputManager inputManager = player.AddComponent<PlayerInputManager>();
            
            if (mainCamera != null)
                inputManager.SetCameraTransform(mainCamera.transform);
        }
        
        // Position player
        player.transform.position = new Vector3(10f, 2f, 10f);
        
        player.tag = "Player";
    }
    
    private void TagInteractables()
    {
        // Note: We can't modify prefabs at runtime, so we'll log what needs to be done manually
        // or set up the prefabs in the editor beforehand
        
        Debug.Log("=== PREFAB SETUP INSTRUCTIONS ===");
        
        if (doorPrefab != null)
        {
            if (!doorPrefab.CompareTag("Interactable"))
            {
                Debug.LogWarning($"Please manually set the tag of '{doorPrefab.name}' prefab to 'Interactable' in the Inspector.");
            }
            
            if (doorPrefab.GetComponent<InteractableDoor>() == null)
            {
                Debug.LogWarning($"Please manually add 'InteractableDoor' component to '{doorPrefab.name}' prefab in the Inspector.");
            }
        }
        
        foreach (var furniture in furniturePrefabs)
        {
            if (furniture != null)
            {
                if (!furniture.CompareTag("Interactable"))
                {
                    Debug.LogWarning($"Please manually set the tag of '{furniture.name}' prefab to 'Interactable' in the Inspector.");
                }
                
                if (furniture.GetComponent<InteractableObject>() == null)
                {
                    Debug.LogWarning($"Please manually add 'InteractableObject' component to '{furniture.name}' prefab in the Inspector.");
                }
            }
        }
        
        Debug.Log("=== END PREFAB SETUP INSTRUCTIONS ===");
    }
    
    [ContextMenu("Create Interactable Tag")]
    public void CreateInteractableTag()
    {
        // This would typically be done through the TagManager, but for runtime:
        Debug.Log("Please create an 'Interactable' tag in the Tag Manager if it doesn't exist.");
    }
    
    [ContextMenu("Auto Setup Prefabs (Editor Only)")]
    public void AutoSetupPrefabs()
    {
#if UNITY_EDITOR
        SetupPrefabsInEditor();
#else
        Debug.LogWarning("Automatic prefab setup is only available in the Unity Editor.");
        TagInteractables(); // Show manual setup instructions
#endif
    }

#if UNITY_EDITOR
    private void SetupPrefabsInEditor()
    {
        bool madeChanges = false;
        
        // Setup door prefab
        if (doorPrefab != null)
        {
            if (SetupPrefabInEditor(doorPrefab, "Interactable", typeof(InteractableDoor)))
                madeChanges = true;
        }
        
        // Setup furniture prefabs
        foreach (var furniture in furniturePrefabs)
        {
            if (furniture != null)
            {
                if (SetupPrefabInEditor(furniture, "Interactable", typeof(InteractableObject)))
                    madeChanges = true;
            }
        }
        
        if (madeChanges)
        {
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("Prefab setup completed! All interactable prefabs have been configured.");
        }
        else
        {
            Debug.Log("All prefabs are already properly configured.");
        }
    }
    
    private bool SetupPrefabInEditor(GameObject prefab, string tag, System.Type componentType)
    {
        string prefabPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"Could not find asset path for {prefab.name}");
            return false;
        }
        
        bool madeChanges = false;
        
        // Load the prefab for editing
        GameObject prefabInstance = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            // Set tag if needed
            if (!prefabInstance.CompareTag(tag))
            {
                prefabInstance.tag = tag;
                madeChanges = true;
                Debug.Log($"Set tag '{tag}' on {prefab.name}");
            }
            
            // Add component if needed
            if (prefabInstance.GetComponent(componentType) == null)
            {
                prefabInstance.AddComponent(componentType);
                madeChanges = true;
                Debug.Log($"Added {componentType.Name} component to {prefab.name}");
            }
            
            // Save changes
            if (madeChanges)
            {
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            }
        }
        finally
        {
            // Always unload the prefab contents
            UnityEditor.PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
        
        return madeChanges;
    }
#endif
}
