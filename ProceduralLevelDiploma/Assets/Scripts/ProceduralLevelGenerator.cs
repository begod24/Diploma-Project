using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class GenerationSettings
{
    [Header("Level Size")]
    public Vector3Int levelSize = new Vector3Int(20, 3, 20);
    
    [Header("Generation Parameters")]
    [Range(0, 100)]
    public int seed = 0;
    public bool useRandomSeed = true;
    
    [Header("Room Generation")]
    public int minRoomSize = 3;
    public int maxRoomSize = 8;
    public int maxRoomAttempts = 50;
    [Range(0f, 1f)]
    public float roomDensity = 0.6f;
    
    [Header("Corridor Generation")]
    public int corridorWidth = 1;
    [Range(0f, 1f)]
    public float corridorComplexity = 0.3f;
    
    [Header("Furniture and Details")]
    [Range(0f, 1f)]
    public float furnitureDensity = 0.2f;
    [Range(0f, 1f)]
    public float decorationDensity = 0.1f;
}

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public GenerationSettings settings;
    
    [Header("Tile Rules")]
    public List<TileRule> availableTiles = new List<TileRule>();
    
    [Header("Generation Control")]
    public bool generateOnStart = true;
    public bool clearOnGenerate = true;
    public Transform levelParent;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool logGenerationSteps = false;
    
    // Internal data
    private Dictionary<Vector3Int, TileRule> placedTiles = new Dictionary<Vector3Int, TileRule>();
    private Dictionary<Vector3Int, GameObject> instantiatedObjects = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<TileRule, int> tileInstanceCounts = new Dictionary<TileRule, int>();
    private System.Random random;
    
    // Wave Function Collapse data
    private Dictionary<Vector3Int, HashSet<TileRule>> possibleTiles = new Dictionary<Vector3Int, HashSet<TileRule>>();
    private Queue<Vector3Int> propagationQueue = new Queue<Vector3Int>();
    
    // Room data
    private List<RectInt> rooms = new List<RectInt>();
    private HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
    
    public System.Action<float> OnGenerationProgress;
    public System.Action OnGenerationComplete;
    
    private void Start()
    {
        if (generateOnStart)
        {
            GenerateLevel();
        }
    }
    
    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        StartCoroutine(GenerateLevelCoroutine());
    }
    
    [ContextMenu("Clear Level")]
    public void ClearLevel()
    {
        foreach (var obj in instantiatedObjects.Values)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        placedTiles.Clear();
        instantiatedObjects.Clear();
        tileInstanceCounts.Clear();
        possibleTiles.Clear();
        rooms.Clear();
        corridors.Clear();
    }
    
    private IEnumerator GenerateLevelCoroutine()
    {
        if (clearOnGenerate)
            ClearLevel();
        
        // Initialize random seed
        int actualSeed = settings.useRandomSeed ? Random.Range(0, int.MaxValue) : settings.seed;
        random = new System.Random(actualSeed);
        
        if (logGenerationSteps)
            Debug.Log($"Starting level generation with seed: {actualSeed}");
        
        OnGenerationProgress?.Invoke(0f);
        
        // Step 1: Generate room layout
        yield return StartCoroutine(GenerateRoomLayout());
        OnGenerationProgress?.Invoke(0.2f);
        
        // Step 2: Connect rooms with corridors
        yield return StartCoroutine(GenerateCorridors());
        OnGenerationProgress?.Invoke(0.4f);
        
        // Step 3: Initialize Wave Function Collapse
        yield return StartCoroutine(InitializeWFC());
        OnGenerationProgress?.Invoke(0.5f);
        
        // Step 4: Run Wave Function Collapse
        yield return StartCoroutine(RunWFC());
        OnGenerationProgress?.Invoke(0.8f);
        
        // Step 5: Place furniture and decorations
        yield return StartCoroutine(PlaceFurnitureAndDecorations());
        OnGenerationProgress?.Invoke(0.9f);
        
        // Step 6: Instantiate objects
        yield return StartCoroutine(InstantiateObjects());
        OnGenerationProgress?.Invoke(1f);
        
        OnGenerationComplete?.Invoke();
        
        if (logGenerationSteps)
            Debug.Log("Level generation complete!");
    }
    
    private IEnumerator GenerateRoomLayout()
    {
        rooms.Clear();
        
        for (int attempt = 0; attempt < settings.maxRoomAttempts; attempt++)
        {
            int roomWidth = random.Next(settings.minRoomSize, settings.maxRoomSize + 1);
            int roomHeight = random.Next(settings.minRoomSize, settings.maxRoomSize + 1);
            
            int x = random.Next(1, settings.levelSize.x - roomWidth - 1);
            int z = random.Next(1, settings.levelSize.z - roomHeight - 1);
            
            RectInt newRoom = new RectInt(x, z, roomWidth, roomHeight);
            
            // Check if room overlaps with existing rooms
            bool overlaps = false;
            foreach (var existingRoom in rooms)
            {
                if (newRoom.Overlaps(existingRoom))
                {
                    overlaps = true;
                    break;
                }
            }
            
            if (!overlaps)
            {
                rooms.Add(newRoom);
                
                // Check if we've reached desired room density
                float currentDensity = CalculateRoomDensity();
                if (currentDensity >= settings.roomDensity)
                    break;
            }
            
            if (attempt % 10 == 0)
                yield return null; // Yield periodically
        }
        
        if (logGenerationSteps)
            Debug.Log($"Generated {rooms.Count} rooms");
    }
    
    private float CalculateRoomDensity()
    {
        int totalRoomArea = rooms.Sum(room => room.width * room.height);
        int totalLevelArea = settings.levelSize.x * settings.levelSize.z;
        return (float)totalRoomArea / totalLevelArea;
    }
    
    private IEnumerator GenerateCorridors()
    {
        corridors.Clear();
        
        // Connect each room to at least one other room
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == 0) continue; // Skip first room
            
            RectInt roomA = rooms[i];
            RectInt roomB = rooms[random.Next(0, i)]; // Connect to a previous room
            
            yield return StartCoroutine(CreateCorridor(roomA, roomB));
        }
        
        // Add some additional connections for complexity
        int additionalConnections = Mathf.RoundToInt(rooms.Count * settings.corridorComplexity);
        for (int i = 0; i < additionalConnections; i++)
        {
            if (rooms.Count < 2) break;
            
            RectInt roomA = rooms[random.Next(rooms.Count)];
            RectInt roomB = rooms[random.Next(rooms.Count)];
            
            if (roomA != roomB)
            {
                yield return StartCoroutine(CreateCorridor(roomA, roomB));
            }
        }
        
        if (logGenerationSteps)
            Debug.Log($"Generated corridors connecting {rooms.Count} rooms");
    }
    
    private IEnumerator CreateCorridor(RectInt roomA, RectInt roomB)
    {
        Vector2Int startPoint = new Vector2Int(
            roomA.x + roomA.width / 2,
            roomA.y + roomA.height / 2
        );
        
        Vector2Int endPoint = new Vector2Int(
            roomB.x + roomB.width / 2,
            roomB.y + roomB.height / 2
        );
        
        // Create L-shaped corridor
        Vector2Int current = startPoint;
        
        // Move horizontally first
        while (current.x != endPoint.x)
        {
            corridors.Add(current);
            current.x += current.x < endPoint.x ? 1 : -1;
        }
        
        // Then move vertically
        while (current.y != endPoint.y)
        {
            corridors.Add(current);
            current.y += current.y < endPoint.y ? 1 : -1;
        }
        
        corridors.Add(endPoint);
        
        yield return null;
    }
    
    private IEnumerator InitializeWFC()
    {
        possibleTiles.Clear();
        
        // Initialize all positions with all possible tiles
        for (int x = 0; x < settings.levelSize.x; x++)
        {
            for (int y = 0; y < settings.levelSize.y; y++)
            {
                for (int z = 0; z < settings.levelSize.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    possibleTiles[pos] = new HashSet<TileRule>(availableTiles);
                }
            }
            
            if (x % 5 == 0)
                yield return null;
        }
        
        // Pre-place floor tiles in rooms and corridors
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int z = room.y; z < room.y + room.height; z++)
                {
                    Vector3Int floorPos = new Vector3Int(x, 0, z);
                    PlaceTile(floorPos, GetTileByType(TileType.Floor));
                }
            }
        }
        
        foreach (var corridorPos in corridors)
        {
            Vector3Int floorPos = new Vector3Int(corridorPos.x, 0, corridorPos.y);
            PlaceTile(floorPos, GetTileByType(TileType.Floor));
        }
        
        yield return null;
    }
    
    private IEnumerator RunWFC()
    {
        int iterations = 0;
        int maxIterations = settings.levelSize.x * settings.levelSize.y * settings.levelSize.z;
        
        while (HasUnresolvedCells() && iterations < maxIterations)
        {
            Vector3Int pos = GetLowestEntropyPosition();
            if (pos == Vector3Int.one * -1) break; // No valid position found
            
            CollapseCell(pos);
            yield return StartCoroutine(PropagateConstraints());
            
            iterations++;
            if (iterations % 100 == 0)
            {
                yield return null;
                if (logGenerationSteps)
                    Debug.Log($"WFC Iteration: {iterations}");
            }
        }
        
        if (logGenerationSteps)
            Debug.Log($"WFC completed in {iterations} iterations");
    }
    
    private bool HasUnresolvedCells()
    {
        foreach (var kvp in possibleTiles)
        {
            if (!placedTiles.ContainsKey(kvp.Key) && kvp.Value.Count > 0)
                return true;
        }
        return false;
    }
    
    private Vector3Int GetLowestEntropyPosition()
    {
        Vector3Int bestPos = Vector3Int.one * -1;
        int lowestEntropy = int.MaxValue;
        
        foreach (var kvp in possibleTiles)
        {
            if (placedTiles.ContainsKey(kvp.Key)) continue;
            
            int entropy = kvp.Value.Count;
            if (entropy > 0 && entropy < lowestEntropy)
            {
                lowestEntropy = entropy;
                bestPos = kvp.Key;
            }
        }
        
        return bestPos;
    }
    
    private void CollapseCell(Vector3Int position)
    {
        if (!possibleTiles.ContainsKey(position) || possibleTiles[position].Count == 0)
            return;
        
        // Weighted random selection
        var availableOptions = possibleTiles[position].ToList();
        float totalWeight = availableOptions.Sum(tile => tile.spawnWeight);
        float randomValue = (float)random.NextDouble() * totalWeight;
        
        float currentWeight = 0f;
        TileRule selectedTile = availableOptions[0];
        
        foreach (var tile in availableOptions)
        {
            currentWeight += tile.spawnWeight;
            if (randomValue <= currentWeight)
            {
                selectedTile = tile;
                break;
            }
        }
        
        PlaceTile(position, selectedTile);
    }
    
    private void PlaceTile(Vector3Int position, TileRule tile)
    {
        if (tile == null) return;
        
        placedTiles[position] = tile;
        possibleTiles[position] = new HashSet<TileRule> { tile };
        
        // Update instance count
        if (!tileInstanceCounts.ContainsKey(tile))
            tileInstanceCounts[tile] = 0;
        tileInstanceCounts[tile]++;
        
        // Add neighbors to propagation queue
        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down
        };
        
        foreach (var dir in directions)
        {
            Vector3Int neighborPos = position + dir;
            if (IsValidPosition(neighborPos))
            {
                propagationQueue.Enqueue(neighborPos);
            }
        }
    }
    
    private IEnumerator PropagateConstraints()
    {
        int propagationCount = 0;
        
        while (propagationQueue.Count > 0)
        {
            Vector3Int position = propagationQueue.Dequeue();
            
            if (placedTiles.ContainsKey(position)) continue;
            
            var validTiles = new HashSet<TileRule>();
            
            foreach (var tile in possibleTiles[position])
            {
                if (CanPlaceTileAt(position, tile))
                {
                    validTiles.Add(tile);
                }
            }
            
            if (validTiles.Count != possibleTiles[position].Count)
            {
                possibleTiles[position] = validTiles;
                
                // Add neighbors to propagation queue
                Vector3Int[] directions = {
                    Vector3Int.forward, Vector3Int.back,
                    Vector3Int.right, Vector3Int.left,
                    Vector3Int.up, Vector3Int.down
                };
                
                foreach (var dir in directions)
                {
                    Vector3Int neighborPos = position + dir;
                    if (IsValidPosition(neighborPos) && !placedTiles.ContainsKey(neighborPos))
                    {
                        propagationQueue.Enqueue(neighborPos);
                    }
                }
            }
            
            propagationCount++;
            if (propagationCount % 50 == 0)
                yield return null;
        }
    }
    
    private bool CanPlaceTileAt(Vector3Int position, TileRule tile)
    {
        // Check instance limits
        if (tile.maxInstances > 0 && 
            tileInstanceCounts.ContainsKey(tile) && 
            tileInstanceCounts[tile] >= tile.maxInstances)
        {
            return false;
        }
        
        // Check basic placement rules
        if (!tile.CanPlaceAt(position, placedTiles, this))
            return false;
        
        // Check connector compatibility
        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down
        };
        
        foreach (var dir in directions)
        {
            Vector3Int neighborPos = position + dir;
            if (placedTiles.ContainsKey(neighborPos))
            {
                TileRule neighborTile = placedTiles[neighborPos];
                if (!tile.connector.CanConnect(neighborTile.connector, dir))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    private IEnumerator PlaceFurnitureAndDecorations()
    {
        var furnitureTiles = availableTiles.Where(t => t.tileType == TileType.Furniture).ToList();
        if (furnitureTiles.Count == 0) yield break;
        
        var floorPositions = placedTiles.Where(kvp => kvp.Value.tileType == TileType.Floor)
                                      .Select(kvp => kvp.Key).ToList();
        
        int furnitureCount = Mathf.RoundToInt(floorPositions.Count * settings.furnitureDensity);
        
        for (int i = 0; i < furnitureCount && floorPositions.Count > 0; i++)
        {
            Vector3Int floorPos = floorPositions[random.Next(floorPositions.Count)];
            Vector3Int furniturePos = floorPos + Vector3Int.up;
            
            if (!placedTiles.ContainsKey(furniturePos))
            {
                TileRule furnitureTile = furnitureTiles[random.Next(furnitureTiles.Count)];
                if (CanPlaceTileAt(furniturePos, furnitureTile))
                {
                    PlaceTile(furniturePos, furnitureTile);
                }
            }
            
            floorPositions.Remove(floorPos);
            
            if (i % 10 == 0)
                yield return null;
        }
    }
    
    private IEnumerator InstantiateObjects()
    {
        if (levelParent == null)
        {
            GameObject levelParentGO = new GameObject("Generated Level");
            levelParent = levelParentGO.transform;
        }
        
        int instantiated = 0;
        
        foreach (var kvp in placedTiles)
        {
            Vector3Int gridPos = kvp.Key;
            TileRule tile = kvp.Value;
            
            if (tile.prefab != null)
            {
                Vector3 worldPos = GridToWorldPosition(gridPos);
                Quaternion rotation = Quaternion.identity;
                
                // Add some random rotation if allowed
                if (tile.canRotate)
                {
                    rotation = Quaternion.Euler(0, random.Next(4) * 90f, 0);
                }
                
                GameObject instance = Instantiate(tile.prefab, worldPos, rotation, levelParent);
                instance.name = $"{tile.tileName}_{gridPos.x}_{gridPos.y}_{gridPos.z}";
                
                instantiatedObjects[gridPos] = instance;
            }
            
            instantiated++;
            if (instantiated % 20 == 0)
                yield return null;
        }
        
        if (logGenerationSteps)
            Debug.Log($"Instantiated {instantiated} objects");
    }
    
    private Vector3 GridToWorldPosition(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, gridPos.z);
    }
    
    private bool IsValidPosition(Vector3Int position)
    {
        return position.x >= 0 && position.x < settings.levelSize.x &&
               position.y >= 0 && position.y < settings.levelSize.y &&
               position.z >= 0 && position.z < settings.levelSize.z;
    }
    
    private TileRule GetTileByType(TileType type)
    {
        return availableTiles.FirstOrDefault(t => t.tileType == type);
    }
    
    public void SetSeed(int newSeed)
    {
        settings.seed = newSeed;
        settings.useRandomSeed = false;
    }
    
    public int GetCurrentSeed()
    {
        return settings.seed;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw generation bounds
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(settings.levelSize.x * 0.5f, settings.levelSize.y * 0.5f, settings.levelSize.z * 0.5f);
        Vector3 size = new Vector3(settings.levelSize.x, settings.levelSize.y, settings.levelSize.z);
        Gizmos.DrawWireCube(center, size);
        
        // Draw rooms
        Gizmos.color = Color.green;
        foreach (var room in rooms)
        {
            Vector3 roomCenter = new Vector3(room.x + room.width * 0.5f, 0.5f, room.y + room.height * 0.5f);
            Vector3 roomSize = new Vector3(room.width, 1f, room.height);
            Gizmos.DrawWireCube(roomCenter, roomSize);
        }
        
        // Draw corridors
        Gizmos.color = Color.blue;
        foreach (var corridor in corridors)
        {
            Vector3 corridorPos = new Vector3(corridor.x, 0.5f, corridor.y);
            Gizmos.DrawWireCube(corridorPos, Vector3.one);
        }
    }
}
