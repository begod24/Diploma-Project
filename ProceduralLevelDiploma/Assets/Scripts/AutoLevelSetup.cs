using UnityEngine;

[System.Serializable]
public class AutoLevelSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool generateOnStart = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupSimpleGenerator();
        }
    }
    
    void SetupSimpleGenerator()
    {
        // Check if SimpleProceduralGenerator already exists
        SimpleProceduralGenerator generator = FindFirstObjectByType<SimpleProceduralGenerator>();
        
        if (generator == null)
        {
            // Create new GameObject with generator
            GameObject generatorGO = new GameObject("Procedural Level Generator");
            generator = generatorGO.AddComponent<SimpleProceduralGenerator>();
            
            Debug.Log("✓ Created SimpleProceduralGenerator");
        }
        
        // Auto-assign prefabs from your prefabs folder
        AssignPrefabsToGenerator(generator);
        
        // Optionally generate level immediately
        if (generateOnStart && generator != null)
        {
            Debug.Log("Starting automatic level generation...");
            generator.GenerateNewLevel();
        }
        
        Debug.Log("=== AUTO SETUP COMPLETE ===");
        Debug.Log("Controls: G = Generate, R = New Seed, C = Clear");
    }
    
    void AssignPrefabsToGenerator(SimpleProceduralGenerator generator)
    {
        if (generator == null) return;
        
        Debug.Log("Auto-assigning prefabs to generator...");
        
        // This will be handled by the generator's auto-find system
        // The generator will automatically look for prefabs with matching names
        
        Debug.Log("✓ Prefab auto-assignment enabled in generator");
    }
    
    [ContextMenu("Setup Level Generator")]
    public void ManualSetup()
    {
        SetupSimpleGenerator();
    }
}
