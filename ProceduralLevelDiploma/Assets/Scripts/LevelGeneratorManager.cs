using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LevelGeneratorManager : MonoBehaviour
{
    [Header("References")]
    public ProceduralLevelGenerator levelGenerator;
    public GameObject player;
    
    [Header("UI References")]
    public Button generateButton;
    public Button clearButton;
    public Slider progressSlider;
    public TMP_InputField seedInputField;
    public Toggle useRandomSeedToggle;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currentSeedText;
    
    [Header("Generation Presets")]
    public List<GenerationPreset> presets = new List<GenerationPreset>();
    public TMP_Dropdown presetDropdown;
    
    [Header("Player Spawn")]
    public Transform playerSpawnPoint;
    public bool respawnPlayerOnGeneration = true;
    
    private bool isGenerating = false;
    
    [System.Serializable]
    public class GenerationPreset
    {
        public string name;
        public GenerationSettings settings;
    }
    
    private void Awake()
    {
        // Auto-find level generator if not assigned
        if (levelGenerator == null)
        {
            levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();
            
            if (levelGenerator != null)
            {
                Debug.Log("LevelGeneratorManager: Auto-found ProceduralLevelGenerator in scene.");
            }
            else
            {
                Debug.LogWarning("LevelGeneratorManager: No ProceduralLevelGenerator found in scene! Please assign one or use QuickLevelSetup to create the complete system.");
            }
        }
        
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO;
                Debug.Log("LevelGeneratorManager: Auto-found Player in scene.");
            }
        }
    }
    
    private void Start()
    {
        InitializeUI();
        SetupTileRules();
        
        if (levelGenerator != null)
        {
            levelGenerator.OnGenerationProgress += UpdateProgress;
            levelGenerator.OnGenerationComplete += OnGenerationComplete;
        }
    }
    
    private void InitializeUI()
    {
        // Setup buttons
        if (generateButton != null)
            generateButton.onClick.AddListener(GenerateLevel);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(ClearLevel);
        
        // Setup seed input
        if (seedInputField != null)
        {
            seedInputField.onValueChanged.AddListener(OnSeedInputChanged);
            seedInputField.text = levelGenerator?.settings.seed.ToString() ?? "0";
        }
        
        if (useRandomSeedToggle != null)
        {
            useRandomSeedToggle.onValueChanged.AddListener(OnRandomSeedToggleChanged);
            useRandomSeedToggle.isOn = levelGenerator?.settings.useRandomSeed ?? true;
        }
        
        // Setup presets
        if (presetDropdown != null && presets.Count > 0)
        {
            var options = new List<string>();
            foreach (var preset in presets)
            {
                options.Add(preset.name);
            }
            presetDropdown.AddOptions(options);
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }
        
        // Initialize progress slider
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.gameObject.SetActive(false);
        }
        
        UpdateUI();
    }
    
    private void SetupTileRules()
    {
        if (levelGenerator == null)
        {
            Debug.LogWarning("LevelGeneratorManager: levelGenerator is not assigned! Please assign it in the inspector or use QuickLevelSetup to create one.");
            return;
        }
        
        if (levelGenerator.availableTiles.Count > 0) return;
        
        // Auto-create tile rules from prefabs
        CreateTileRulesFromPrefabs();
    }
    
    private void CreateTileRulesFromPrefabs()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("LevelGeneratorManager: Cannot create tile rules - levelGenerator is null!");
            return;
        }
        
#if UNITY_EDITOR
        // Get all prefabs from the Prefabs folder
        var prefabGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        
        if (prefabGuids.Length == 0)
        {
            Debug.LogWarning("LevelGeneratorManager: No prefabs found in Assets/Prefabs folder!");
            return;
        }
        
        int createdRules = 0;
        foreach (var guid in prefabGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                CreateTileRuleForPrefab(prefab);
                createdRules++;
            }
        }
        
        Debug.Log($"LevelGeneratorManager: Created {createdRules} tile rules from prefabs.");
#else
        Debug.LogWarning("Automatic tile rule creation is only available in the Unity Editor. Please manually assign tile rules.");
#endif
    }
    
    private void CreateTileRuleForPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("LevelGeneratorManager: Attempting to create tile rule for null prefab!");
            return;
        }
        
        if (levelGenerator == null)
        {
            Debug.LogError("LevelGeneratorManager: Cannot create tile rule - levelGenerator is null!");
            return;
        }
        
        // Create a tile rule asset for this prefab
        TileRule tileRule = ScriptableObject.CreateInstance<TileRule>();
        tileRule.tileName = prefab.name;
        tileRule.prefab = prefab;
        
        // Determine tile type based on prefab name
        string prefabName = prefab.name.ToLower();
        if (prefabName.Contains("floor"))
        {
            tileRule.tileType = TileType.Floor;
            tileRule.connector.up = ConnectionType.Open;
            tileRule.connector.down = ConnectionType.Floor;
            tileRule.spawnWeight = 1f;
        }
        else if (prefabName.Contains("wall"))
        {
            tileRule.tileType = TileType.Wall;
            tileRule.connector.north = ConnectionType.Wall;
            tileRule.connector.south = ConnectionType.Wall;
            tileRule.connector.east = ConnectionType.Wall;
            tileRule.connector.west = ConnectionType.Wall;
            tileRule.connector.down = ConnectionType.Floor;
            tileRule.spawnWeight = 0.8f;
        }
        else if (prefabName.Contains("door"))
        {
            tileRule.tileType = TileType.Door;
            tileRule.connector.north = ConnectionType.Door;
            tileRule.connector.south = ConnectionType.Door;
            tileRule.connector.down = ConnectionType.Floor;
            tileRule.spawnWeight = 0.1f;
            tileRule.maxInstances = 10;
        }
        else if (prefabName.Contains("ceiling"))
        {
            tileRule.tileType = TileType.Ceiling;
            tileRule.connector.down = ConnectionType.Open;
            tileRule.connector.up = ConnectionType.None;
            tileRule.spawnWeight = 0.7f;
        }
        else if (prefabName.Contains("box") || prefabName.Contains("wardrobe") || prefabName.Contains("lamp"))
        {
            tileRule.tileType = TileType.Furniture;
            tileRule.connector.down = ConnectionType.Floor;
            tileRule.spawnWeight = 0.3f;
            tileRule.requiresSupport = true;
            tileRule.canRotate = true;
        }
        else
        {
            tileRule.tileType = TileType.Empty;
            tileRule.spawnWeight = 0.1f;
        }
        
        // Initialize the availableTiles list if it's null
        if (levelGenerator.availableTiles == null)
        {
            levelGenerator.availableTiles = new List<TileRule>();
        }
        
        levelGenerator.availableTiles.Add(tileRule);
        Debug.Log($"Created tile rule for {prefab.name} (Type: {tileRule.tileType})");
    }
    
    public void GenerateLevel()
    {
        if (isGenerating || levelGenerator == null) return;
        
        isGenerating = true;
        UpdateUI();
        
        if (statusText != null)
            statusText.text = "Generating level...";
        
        levelGenerator.GenerateLevel();
    }
    
    public void ClearLevel()
    {
        if (isGenerating || levelGenerator == null) return;
        
        levelGenerator.ClearLevel();
        
        if (statusText != null)
            statusText.text = "Level cleared";
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.gameObject.SetActive(false);
        }
    }
    
    private void UpdateProgress(float progress)
    {
        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = progress;
        }
        
        if (statusText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            statusText.text = $"Generating... {percentage}%";
        }
    }
    
    private void OnGenerationComplete()
    {
        isGenerating = false;
        UpdateUI();
        
        if (statusText != null)
            statusText.text = "Generation complete!";
        
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
            // Hide progress bar after a delay
            Invoke(nameof(HideProgressBar), 1f);
        }
        
        if (currentSeedText != null)
            currentSeedText.text = $"Current Seed: {levelGenerator.GetCurrentSeed()}";
        
        // Respawn player if needed
        if (respawnPlayerOnGeneration)
            RespawnPlayer();
    }
    
    private void HideProgressBar()
    {
        if (progressSlider != null)
            progressSlider.gameObject.SetActive(false);
    }
    
    private void RespawnPlayer()
    {
        if (player == null) return;
        
        Vector3 spawnPosition;
        
        if (playerSpawnPoint != null)
        {
            spawnPosition = playerSpawnPoint.position;
        }
        else
        {
            // Find a safe spawn position (on a floor tile)
            spawnPosition = FindSafeSpawnPosition();
        }
        
        // Disable player controller temporarily
        var characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = spawnPosition;
            characterController.enabled = true;
        }
        else
        {
            player.transform.position = spawnPosition;
        }
    }
    
    private Vector3 FindSafeSpawnPosition()
    {
        // Try to find a floor tile near the center of the level
        Vector3 centerPosition = new Vector3(
            levelGenerator.settings.levelSize.x * 0.5f,
            1f,
            levelGenerator.settings.levelSize.z * 0.5f
        );
        
        // Raycast down to find floor
        RaycastHit hit;
        if (Physics.Raycast(centerPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))
        {
            return hit.point + Vector3.up * 0.1f;
        }
        
        return centerPosition;
    }
    
    private void OnSeedInputChanged(string value)
    {
        if (levelGenerator == null) return;
        
        if (int.TryParse(value, out int seed))
        {
            levelGenerator.SetSeed(seed);
        }
    }
    
    private void OnRandomSeedToggleChanged(bool useRandom)
    {
        if (levelGenerator == null) return;
        
        levelGenerator.settings.useRandomSeed = useRandom;
        
        if (seedInputField != null)
            seedInputField.interactable = !useRandom;
    }
    
    private void OnPresetChanged(int presetIndex)
    {
        if (levelGenerator == null || presetIndex < 0 || presetIndex >= presets.Count) return;
        
        levelGenerator.settings = presets[presetIndex].settings;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (generateButton != null)
            generateButton.interactable = !isGenerating;
        
        if (clearButton != null)
            clearButton.interactable = !isGenerating;
        
        if (seedInputField != null && levelGenerator != null)
        {
            seedInputField.text = levelGenerator.settings.seed.ToString();
            seedInputField.interactable = !levelGenerator.settings.useRandomSeed && !isGenerating;
        }
        
        if (useRandomSeedToggle != null && levelGenerator != null)
        {
            useRandomSeedToggle.isOn = levelGenerator.settings.useRandomSeed;
            useRandomSeedToggle.interactable = !isGenerating;
        }
    }
    
    [ContextMenu("Create Default Presets")]
    public void CreateDefaultPresets()
    {
        presets.Clear();
        
        // Small dungeon preset
        presets.Add(new GenerationPreset
        {
            name = "Small Dungeon",
            settings = new GenerationSettings
            {
                levelSize = new Vector3Int(15, 3, 15),
                minRoomSize = 3,
                maxRoomSize = 6,
                roomDensity = 0.4f,
                furnitureDensity = 0.1f
            }
        });
        
        // Large complex preset
        presets.Add(new GenerationPreset
        {
            name = "Large Complex",
            settings = new GenerationSettings
            {
                levelSize = new Vector3Int(30, 4, 30),
                minRoomSize = 4,
                maxRoomSize = 10,
                roomDensity = 0.6f,
                furnitureDensity = 0.3f,
                corridorComplexity = 0.4f
            }
        });
        
        // Sparse layout preset
        presets.Add(new GenerationPreset
        {
            name = "Sparse Layout",
            settings = new GenerationSettings
            {
                levelSize = new Vector3Int(25, 3, 25),
                minRoomSize = 5,
                maxRoomSize = 8,
                roomDensity = 0.3f,
                furnitureDensity = 0.05f,
                corridorComplexity = 0.2f
            }
        });
    }
    
    [ContextMenu("Fix Missing References")]
    public void FixMissingReferences()
    {
        bool foundReferences = false;
        
        // Try to find level generator
        if (levelGenerator == null)
        {
            levelGenerator = FindFirstObjectByType<ProceduralLevelGenerator>();
            if (levelGenerator != null)
            {
                Debug.Log("✓ Found and assigned ProceduralLevelGenerator");
                foundReferences = true;
            }
            else
            {
                Debug.LogWarning("✗ No ProceduralLevelGenerator found in scene. Please create one first.");
            }
        }
        
        // Try to find player
        if (player == null)
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO;
                Debug.Log("✓ Found and assigned Player");
                foundReferences = true;
            }
            else
            {
                Debug.LogWarning("✗ No GameObject with 'Player' tag found in scene.");
            }
        }
        
        if (!foundReferences)
        {
            Debug.Log("All required references are already assigned or no missing components found in scene.");
        }
        
        // Setup tile rules if needed
        if (levelGenerator != null && levelGenerator.availableTiles.Count == 0)
        {
            Debug.Log("Setting up tile rules...");
            SetupTileRules();
        }
    }
    
    private void OnDestroy()
    {
        if (levelGenerator != null)
        {
            levelGenerator.OnGenerationProgress -= UpdateProgress;
            levelGenerator.OnGenerationComplete -= OnGenerationComplete;
        }
    }
}
