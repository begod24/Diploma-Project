using UnityEngine;

public class LevelSetupChecklist : MonoBehaviour
{
    [Header("Setup Checklist")]
    [SerializeField] private bool showInstructions = true;
    
    private void OnGUI()
    {
        if (!showInstructions) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 600));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("LEVEL GENERATION SETUP CHECKLIST", GUI.skin.box);
        GUILayout.Space(10);
        
        // Check components
        bool hasGenerator = FindObjectOfType<ProceduralLevelGenerator>() != null;
        bool hasManager = FindObjectOfType<LevelGeneratorManager>() != null;
        bool hasPlayer = GameObject.FindWithTag("Player") != null;
        bool hasExample = FindObjectOfType<LevelGenerationExample>() != null;
        
        GUILayout.Label("REQUIRED COMPONENTS:");
        DrawChecklistItem("ProceduralLevelGenerator", hasGenerator);
        DrawChecklistItem("LevelGeneratorManager", hasManager);
        DrawChecklistItem("Player with PlayerInputManager", hasPlayer);
        DrawChecklistItem("LevelGenerationExample (for testing)", hasExample);
        
        GUILayout.Space(10);
        
        // Check tile rules
        ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();
        bool hasTileRules = generator != null && generator.availableTiles.Count > 0;
        
        GUILayout.Label("TILE SETUP:");
        DrawChecklistItem("Tile Rules Created", hasTileRules);
        
        if (generator != null)
        {
            GUILayout.Label($"Available Tiles: {generator.availableTiles.Count}");
        }
        
        GUILayout.Space(10);
        
        // Quick actions
        GUILayout.Label("QUICK ACTIONS:");
        
        if (GUILayout.Button("Auto Setup Scene"))
        {
            QuickLevelSetup setup = FindObjectOfType<QuickLevelSetup>();
            if (setup == null)
            {
                GameObject go = new GameObject("Quick Setup");
                setup = go.AddComponent<QuickLevelSetup>();
            }
            setup.QuickSetupScene();
        }
        
        if (GUILayout.Button("Generate Level (G)"))
        {
            if (hasGenerator)
            {
                generator.GenerateLevel();
            }
            else
            {
                Debug.LogWarning("No ProceduralLevelGenerator found!");
            }
        }
        
        if (GUILayout.Button("Clear Level (C)"))
        {
            if (hasGenerator)
            {
                generator.ClearLevel();
            }
        }
        
        GUILayout.Space(10);
        
        // Instructions
        GUILayout.Label("MANUAL SETUP STEPS:");
        GUILayout.Label("1. Create empty GameObject");
        GUILayout.Label("2. Add SceneSetupHelper component");
        GUILayout.Label("3. Drag your prefabs to the fields");
        GUILayout.Label("4. Right-click → 'Setup Scene'");
        GUILayout.Label("5. Press G to generate level!");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Hide This Checklist"))
        {
            showInstructions = false;
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawChecklistItem(string item, bool completed)
    {
        string icon = completed ? "✓" : "✗";
        Color color = completed ? Color.green : Color.red;
        
        Color oldColor = GUI.color;
        GUI.color = color;
        GUILayout.Label($"{icon} {item}");
        GUI.color = oldColor;
    }
    
    private void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.G))
        {
            ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();
            if (generator != null)
            {
                generator.GenerateLevel();
                Debug.Log("Generating new level...");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ProceduralLevelGenerator generator = FindObjectOfType<ProceduralLevelGenerator>();
            if (generator != null)
            {
                generator.ClearLevel();
                Debug.Log("Level cleared!");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            showInstructions = !showInstructions;
        }
    }
}
