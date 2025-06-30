using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class SimpleProceduralGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private Vector3Int levelSize = new Vector3Int(20, 3, 20);
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;
    
    [Header("Room Settings")]
    [SerializeField] private int minRoomSize = 4;
    [SerializeField] private int maxRoomSize = 8;
    [SerializeField] private float roomDensity = 0.5f;
    [SerializeField] private float furnitureDensity = 0.2f;
    
    [Header("Prefab References - Assign in Inspector")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject ceilingPrefab;
    [SerializeField] private GameObject[] furniturePrefabs;
    
    [Header("Auto-Find Prefabs")]
    [SerializeField] private bool autoFindPrefabs = true;
    
    // Internal data
    private System.Random random;
    private Dictionary<Vector3Int, GameObject> placedObjects = new Dictionary<Vector3Int, GameObject>();
    private List<RectInt> rooms = new List<RectInt>();
    private HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
    private Transform levelParent;
    
    void Start()
    {
        StartCoroutine(GenerateLevel());
    }
    
    IEnumerator GenerateLevel()
    {
        Debug.Log("=== STARTING PROCEDURAL LEVEL GENERATION ===");
        
        // Initialize
        InitializeGeneration();
        yield return null;
        
        // Auto-find prefabs if needed
        if (autoFindPrefabs)
        {
            AutoFindPrefabs();
            yield return null;
        }
        
        // Validate prefabs
        if (!ValidatePrefabs())
        {
            Debug.LogError("Cannot generate level - missing required prefabs!");
            yield break;
        }
        
        // Clear existing level
        ClearLevel();
        yield return null;
        
        // Generate room layout
        Debug.Log("Generating rooms...");
        GenerateRooms();
        yield return null;
        
        // Connect rooms with corridors
        Debug.Log("Generating corridors...");
        GenerateCorridors();
        yield return null;
        
        // Place floor tiles
        Debug.Log("Placing floors...");
        yield return StartCoroutine(PlaceFloors());
        
        // Place walls
        Debug.Log("Placing walls...");
        yield return StartCoroutine(PlaceWalls());
        
        // Place doors
        Debug.Log("Placing doors...");
        yield return StartCoroutine(PlaceDoors());
        
        // Place ceilings
        if (ceilingPrefab != null)
        {
            Debug.Log("Placing ceilings...");
            yield return StartCoroutine(PlaceCeilings());
        }
        
        // Place furniture
        if (furniturePrefabs.Length > 0)
        {
            Debug.Log("Placing furniture...");
            yield return StartCoroutine(PlaceFurniture());
        }
        
        Debug.Log("=== LEVEL GENERATION COMPLETE ===");
        Debug.Log($"Generated level with seed: {seed}");
        Debug.Log($"Rooms: {rooms.Count}, Objects placed: {placedObjects.Count}");
        
        // Validate the generated level
        ValidateGeneratedLevel();
        
        // Create a simple player for testing (commented out for now - manually add to scene if needed)
        // CreateTestPlayer();
    }
    
    void ValidateGeneratedLevel()
    {
        int floorCount = 0;
        int wallCount = 0;
        int doorCount = 0;
        int ceilingCount = 0;
        int furnitureCount = 0;
        
        foreach (var obj in placedObjects.Values)
        {
            if (obj.name.Contains("Floor")) floorCount++;
            else if (obj.name.Contains("Wall")) wallCount++;
            else if (obj.name.Contains("Door")) doorCount++;
            else if (obj.name.Contains("Ceiling")) ceilingCount++;
            else if (obj.name.Contains("Furniture")) furnitureCount++;
        }
        
        Debug.Log("=== LEVEL VALIDATION ===");
        Debug.Log($"Floors: {floorCount}, Walls: {wallCount}, Doors: {doorCount}");
        Debug.Log($"Ceilings: {ceilingCount}, Furniture: {furnitureCount}");
        
        // Check for common issues
        if (floorCount == 0)
            Debug.LogWarning("⚠ No floors were generated!");
        
        if (wallCount == 0)
            Debug.LogWarning("⚠ No walls were generated - rooms won't be enclosed!");
        
        if (doorCount == 0 && rooms.Count > 1)
            Debug.LogWarning("⚠ No doors were generated - rooms may not be accessible!");
        
        // Check collider setup
        int objectsWithColliders = 0;
        int objectsWithTriggers = 0;
        
        foreach (var obj in placedObjects.Values)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                objectsWithColliders++;
                if (col.isTrigger) objectsWithTriggers++;
            }
        }
        
        Debug.Log($"Objects with colliders: {objectsWithColliders}/{placedObjects.Count}");
        Debug.Log($"Trigger colliders: {objectsWithTriggers}");
        
        if (objectsWithColliders < placedObjects.Count)
        {
            Debug.LogWarning($"⚠ {placedObjects.Count - objectsWithColliders} objects missing colliders!");
        }
        
        Debug.Log("✓ Level validation complete");
    }
    
    void InitializeGeneration()
    {
        // Set seed
        if (useRandomSeed)
            seed = Random.Range(0, int.MaxValue);
        
        random = new System.Random(seed);
        
        // Create level parent
        if (levelParent == null)
        {
            GameObject parentGO = new GameObject("Generated Level");
            levelParent = parentGO.transform;
        }
        
        // Clear collections
        rooms.Clear();
        corridors.Clear();
        placedObjects.Clear();
    }
    
    void AutoFindPrefabs()
    {
        if (!autoFindPrefabs) return;
        
        Debug.Log("Auto-finding prefabs...");
        
        try
        {
            // Try to find prefabs by name
            if (floorPrefab == null)
            {
                floorPrefab = FindPrefabByName("floor");
                if (floorPrefab != null) Debug.Log($"✓ Found floor prefab: {floorPrefab.name}");
            }
            
            if (wallPrefab == null)
            {
                wallPrefab = FindPrefabByName("wall");
                if (wallPrefab != null) Debug.Log($"✓ Found wall prefab: {wallPrefab.name}");
            }
            
            if (doorPrefab == null)
            {
                doorPrefab = FindPrefabByName("door");
                if (doorPrefab != null) Debug.Log($"✓ Found door prefab: {doorPrefab.name}");
            }
            
            if (ceilingPrefab == null)
            {
                ceilingPrefab = FindPrefabByName("ceiling");
                if (ceilingPrefab != null) Debug.Log($"✓ Found ceiling prefab: {ceilingPrefab.name}");
            }
            
            // Find furniture
            if (furniturePrefabs == null || furniturePrefabs.Length == 0)
            {
                List<GameObject> furniture = new List<GameObject>();
                
                GameObject box = FindPrefabByName("box");
                if (box != null) 
                {
                    furniture.Add(box);
                    Debug.Log($"✓ Found furniture: {box.name}");
                }
                
                GameObject wardrobe = FindPrefabByName("wardrobe");
                if (wardrobe != null) 
                {
                    furniture.Add(wardrobe);
                    Debug.Log($"✓ Found furniture: {wardrobe.name}");
                }
                
                GameObject lamp = FindPrefabByName("lamp");
                if (lamp != null) 
                {
                    furniture.Add(lamp);
                    Debug.Log($"✓ Found furniture: {lamp.name}");
                }
                
                furniturePrefabs = furniture.ToArray();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in AutoFindPrefabs: {e.Message}");
            autoFindPrefabs = false; // Disable auto-find to prevent repeated errors
        }
        
        Debug.Log($"Prefab search complete - Floor: {floorPrefab != null}, Wall: {wallPrefab != null}, Door: {doorPrefab != null}, Furniture: {furniturePrefabs?.Length ?? 0}");
    }
    
    GameObject FindPrefabByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        
        try
        {
#if UNITY_EDITOR
            // In editor, try to find prefabs in the Assets/Prefabs folder
            string[] prefabGuids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            
            foreach (string guid in prefabGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null && prefab.name.ToLower().Contains(name.ToLower()))
                {
                    Debug.Log($"Found prefab: {prefab.name} for search term: {name}");
                    return prefab;
                }
            }
#endif
            
            // Fallback: try to find in scene objects (inactive objects)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.name.ToLower().Contains(name.ToLower()))
                {
                    // Check if it's a prefab asset or scene object
                    if (UnityEditor.AssetDatabase.Contains(obj))
                    {
                        Debug.Log($"Found prefab asset: {obj.name} for search term: {name}");
                        return obj;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error finding prefab '{name}': {e.Message}");
        }
        
        Debug.LogWarning($"Could not find prefab containing '{name}' in name. Please assign manually in inspector.");
        return null;
    }
    
    bool ValidatePrefabs()
    {
        if (floorPrefab == null)
        {
            Debug.LogWarning("Floor prefab is missing! Creating a simple test prefab...");
            CreateTestPrefabs();
        }
        
        if (floorPrefab == null)
        {
            Debug.LogError("Floor prefab is still null after attempting to create test prefabs!");
            return false;
        }
        
        if (wallPrefab == null)
        {
            Debug.LogWarning("Wall prefab not found - walls will not be generated.");
        }
        
        Debug.Log($"✓ Prefab validation complete - Floor: {floorPrefab?.name}, Wall: {wallPrefab?.name}, Door: {doorPrefab?.name}");
        return true;
    }
    
    void ClearLevel()
    {
        if (levelParent != null)
        {
            foreach (Transform child in levelParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        placedObjects.Clear();
    }
    
    void GenerateRooms()
    {
        int attempts = 50;
        
        for (int i = 0; i < attempts; i++)
        {
            int roomWidth = random.Next(minRoomSize, maxRoomSize + 1);
            int roomHeight = random.Next(minRoomSize, maxRoomSize + 1);
            
            int x = random.Next(2, levelSize.x - roomWidth - 2);
            int z = random.Next(2, levelSize.z - roomHeight - 2);
            
            RectInt newRoom = new RectInt(x, z, roomWidth, roomHeight);
            
            // Check overlap
            bool overlaps = false;
            foreach (var room in rooms)
            {
                if (newRoom.Overlaps(room))
                {
                    overlaps = true;
                    break;
                }
            }
            
            if (!overlaps)
            {
                rooms.Add(newRoom);
                
                // Check density
                float currentDensity = CalculateRoomDensity();
                if (currentDensity >= roomDensity)
                    break;
            }
        }
        
        Debug.Log($"Generated {rooms.Count} rooms");
    }
    
    float CalculateRoomDensity()
    {
        int totalRoomArea = 0;
        foreach (var room in rooms)
        {
            totalRoomArea += room.width * room.height;
        }
        return (float)totalRoomArea / (levelSize.x * levelSize.z);
    }
    
    void GenerateCorridors()
    {
        if (rooms.Count < 2) return;
        
        // Use improved corridor generation with WFC-style constraints
        // Connect rooms using minimum spanning tree approach for better connectivity
        List<RoomConnection> connections = GenerateRoomConnections();
        
        foreach (var connection in connections)
        {
            CreateSmartCorridor(connection.roomA, connection.roomB);
        }
        
        Debug.Log($"Generated {connections.Count} corridors connecting {rooms.Count} rooms");
    }
    
    List<RoomConnection> GenerateRoomConnections()
    {
        List<RoomConnection> allConnections = new List<RoomConnection>();
        List<RoomConnection> selectedConnections = new List<RoomConnection>();
        
        // Generate all possible connections between rooms
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                float distance = Vector2.Distance(
                    new Vector2(rooms[i].x + rooms[i].width * 0.5f, rooms[i].y + rooms[i].height * 0.5f),
                    new Vector2(rooms[j].x + rooms[j].width * 0.5f, rooms[j].y + rooms[j].height * 0.5f)
                );
                
                allConnections.Add(new RoomConnection
                {
                    roomA = rooms[i],
                    roomB = rooms[j],
                    distance = distance
                });
            }
        }
        
        // Sort by distance (shortest first)
        allConnections.Sort((a, b) => a.distance.CompareTo(b.distance));
        
        // Use simple minimum spanning tree to ensure all rooms are connected
        HashSet<int> connectedRooms = new HashSet<int>();
        
        foreach (var connection in allConnections)
        {
            int indexA = rooms.IndexOf(connection.roomA);
            int indexB = rooms.IndexOf(connection.roomB);
            
            // Add connection if it connects new rooms or if we want extra connectivity
            if (!connectedRooms.Contains(indexA) || !connectedRooms.Contains(indexB) ||
                (selectedConnections.Count < rooms.Count && random.NextDouble() < 0.3)) // 30% chance for extra connections
            {
                selectedConnections.Add(connection);
                connectedRooms.Add(indexA);
                connectedRooms.Add(indexB);
            }
            
            // Stop when we have enough connections
            if (connectedRooms.Count >= rooms.Count && selectedConnections.Count >= rooms.Count - 1)
                break;
        }
        
        return selectedConnections;
    }
    
    void CreateSmartCorridor(RectInt roomA, RectInt roomB)
    {
        // Find best connection points on room edges
        Vector2Int startPoint = FindBestConnectionPoint(roomA, roomB);
        Vector2Int endPoint = FindBestConnectionPoint(roomB, roomA);
        
        // Create corridor with better pathfinding
        CreateCorridorPath(startPoint, endPoint);
    }
    
    Vector2Int FindBestConnectionPoint(RectInt fromRoom, RectInt toRoom)
    {
        Vector2Int fromCenter = new Vector2Int(
            fromRoom.x + fromRoom.width / 2,
            fromRoom.y + fromRoom.height / 2
        );
        
        Vector2Int toCenter = new Vector2Int(
            toRoom.x + toRoom.width / 2,
            toRoom.y + toRoom.height / 2
        );
        
        Vector2Int direction = toCenter - fromCenter;
        
        // Find the edge point closest to the target room
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal connection
            int x = direction.x > 0 ? fromRoom.x + fromRoom.width - 1 : fromRoom.x;
            int y = Mathf.Clamp(toCenter.y, fromRoom.y, fromRoom.y + fromRoom.height - 1);
            return new Vector2Int(x, y);
        }
        else
        {
            // Vertical connection
            int x = Mathf.Clamp(toCenter.x, fromRoom.x, fromRoom.x + fromRoom.width - 1);
            int y = direction.y > 0 ? fromRoom.y + fromRoom.height - 1 : fromRoom.y;
            return new Vector2Int(x, y);
        }
    }
    
    void CreateCorridorPath(Vector2Int start, Vector2Int end)
    {
        // Use L-shaped or straight corridor based on room layout
        Vector2Int current = start;
        HashSet<Vector2Int> corridorPath = new HashSet<Vector2Int>();
        
        // Decide corridor shape based on distance and layout
        bool useElbow = random.NextDouble() < 0.7; // 70% chance for L-shaped corridor
        
        if (useElbow)
        {
            // L-shaped corridor
            bool horizontalFirst = random.NextDouble() < 0.5;
            
            if (horizontalFirst)
            {
                // Horizontal first, then vertical
                while (current.x != end.x)
                {
                    corridorPath.Add(current);
                    current.x += current.x < end.x ? 1 : -1;
                }
                while (current.y != end.y)
                {
                    corridorPath.Add(current);
                    current.y += current.y < end.y ? 1 : -1;
                }
            }
            else
            {
                // Vertical first, then horizontal
                while (current.y != end.y)
                {
                    corridorPath.Add(current);
                    current.y += current.y < end.y ? 1 : -1;
                }
                while (current.x != end.x)
                {
                    corridorPath.Add(current);
                    current.x += current.x < end.x ? 1 : -1;
                }
            }
        }
        else
        {
            // Straight diagonal corridor (stepped)
            while (current != end)
            {
                corridorPath.Add(current);
                
                if (current.x != end.x && current.y != end.y)
                {
                    // Move diagonally by choosing random direction
                    if (random.NextDouble() < 0.5)
                        current.x += current.x < end.x ? 1 : -1;
                    else
                        current.y += current.y < end.y ? 1 : -1;
                }
                else if (current.x != end.x)
                {
                    current.x += current.x < end.x ? 1 : -1;
                }
                else if (current.y != end.y)
                {
                    current.y += current.y < end.y ? 1 : -1;
                }
            }
        }
        
        corridorPath.Add(end);
        
        // Add corridor tiles to main corridor set
        foreach (var tile in corridorPath)
        {
            corridors.Add(tile);
        }
    }
    
    // Helper class for room connections
    [System.Serializable]
    public class RoomConnection
    {
        public RectInt roomA;
        public RectInt roomB;
        public float distance;
    }
    
    IEnumerator PlaceFloors()
    {
        int placed = 0;
        
        // Place floors in rooms
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int z = room.y; z < room.y + room.height; z++)
                {
                    Vector3Int pos = new Vector3Int(x, 0, z);
                    PlaceObject(pos, floorPrefab, "Floor");
                    placed++;
                    
                    if (placed % 20 == 0) yield return null;
                }
            }
        }
        
        // Place floors in corridors
        foreach (var corridor in corridors)
        {
            Vector3Int pos = new Vector3Int(corridor.x, 0, corridor.y);
            PlaceObject(pos, floorPrefab, "Floor");
            placed++;
            
            if (placed % 20 == 0) yield return null;
        }
        
        Debug.Log($"Placed {placed} floor tiles");
    }
    
    IEnumerator PlaceWalls()
    {
        if (wallPrefab == null) yield break;
        
        int placed = 0;
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
        
        // Collect all floor positions from rooms
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int z = room.y; z < room.y + room.height; z++)
                {
                    floorPositions.Add(new Vector2Int(x, z));
                }
            }
        }
        
        // Add corridor floors
        foreach (var corridor in corridors)
        {
            floorPositions.Add(corridor);
        }
        
        // Generate walls around rooms (proper room enclosure)
        foreach (var room in rooms)
        {
            // Place walls around room perimeter
            for (int x = room.x - 1; x <= room.x + room.width; x++)
            {
                for (int z = room.y - 1; z <= room.y + room.height; z++)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    
                    // Only place wall if:
                    // 1. Position is not a floor
                    // 2. Position is adjacent to a floor (room boundary)
                    // 3. Position is within level bounds
                    if (!floorPositions.Contains(pos) && 
                        IsAdjacentToFloor(pos, floorPositions) &&
                        IsWithinBounds(pos))
                    {
                        wallPositions.Add(pos);
                    }
                }
            }
        }
        
        // Also add walls around corridors
        foreach (var corridor in corridors)
        {
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };
            
            foreach (var dir in directions)
            {
                Vector2Int wallPos2D = corridor + dir;
                
                if (!floorPositions.Contains(wallPos2D) && 
                    IsWithinBounds(wallPos2D))
                {
                    wallPositions.Add(wallPos2D);
                }
            }
        }
        
        // Place all walls
        foreach (var wallPos2D in wallPositions)
        {
            Vector3Int wallPos = new Vector3Int(wallPos2D.x, 1, wallPos2D.y);
            
            if (!placedObjects.ContainsKey(wallPos))
            {
                PlaceObject(wallPos, wallPrefab, "Wall");
                placed++;
                
                if (placed % 10 == 0) yield return null;
            }
        }
        
        Debug.Log($"Placed {placed} walls around {rooms.Count} rooms and corridors");
    }
    
    bool IsAdjacentToFloor(Vector2Int pos, HashSet<Vector2Int> floorPositions)
    {
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };
        
        foreach (var dir in directions)
        {
            if (floorPositions.Contains(pos + dir))
            {
                return true;
            }
        }
        return false;
    }
    
    bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < levelSize.x && 
               pos.y >= 0 && pos.y < levelSize.z;
    }
    
    IEnumerator PlaceDoors()
    {
        if (doorPrefab == null) yield break;
        
        int placed = 0;
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        List<Vector3Int> doorCandidates = new List<Vector3Int>();
        
        // Collect all floor positions
        foreach (var room in rooms)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int z = room.y; z < room.y + room.height; z++)
                {
                    floorPositions.Add(new Vector2Int(x, z));
                }
            }
        }
        
        foreach (var corridor in corridors)
        {
            floorPositions.Add(corridor);
        }
        
        // Find door candidates (walls that connect rooms to corridors or other rooms)
        foreach (var room in rooms)
        {
            // Check room perimeter for door placement opportunities
            for (int x = room.x; x < room.x + room.width; x++)
            {
                // Top and bottom walls
                CheckDoorCandidate(new Vector2Int(x, room.y - 1), floorPositions, doorCandidates);
                CheckDoorCandidate(new Vector2Int(x, room.y + room.height), floorPositions, doorCandidates);
            }
            
            for (int z = room.y; z < room.y + room.height; z++)
            {
                // Left and right walls
                CheckDoorCandidate(new Vector2Int(room.x - 1, z), floorPositions, doorCandidates);
                CheckDoorCandidate(new Vector2Int(room.x + room.width, z), floorPositions, doorCandidates);
            }
        }
        
        // Place doors at suitable locations
        int doorsToPlace = Mathf.Max(1, rooms.Count); // At least one door per room
        int doorsPlaced = 0;
        
        // Shuffle door candidates for random placement
        for (int i = 0; i < doorCandidates.Count && doorsPlaced < doorsToPlace; i++)
        {
            int randomIndex = random.Next(doorCandidates.Count - i) + i;
            var temp = doorCandidates[i];
            doorCandidates[i] = doorCandidates[randomIndex];
            doorCandidates[randomIndex] = temp;
        }
        
        foreach (var doorPos in doorCandidates)
        {
            if (doorsPlaced >= doorsToPlace) break;
            
            // Remove existing wall if present
            if (placedObjects.ContainsKey(doorPos))
            {
                DestroyImmediate(placedObjects[doorPos]);
                placedObjects.Remove(doorPos);
            }
            
            // Place door
            PlaceObject(doorPos, doorPrefab, "Door");
            placed++;
            doorsPlaced++;
            
            yield return null;
        }
        
        Debug.Log($"Placed {placed} doors connecting rooms and corridors");
    }
    
    void CheckDoorCandidate(Vector2Int wallPos2D, HashSet<Vector2Int> floorPositions, List<Vector3Int> doorCandidates)
    {
        Vector3Int wallPos3D = new Vector3Int(wallPos2D.x, 1, wallPos2D.y);
        
        // Check if there's a wall at this position
        if (placedObjects.ContainsKey(wallPos3D))
        {
            // Check if this wall separates a room from a corridor or another room
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            bool hasFloorOnOneSide = false;
            bool hasFloorOnOtherSide = false;
            
            foreach (var dir in directions)
            {
                Vector2Int checkPos1 = wallPos2D + dir;
                Vector2Int checkPos2 = wallPos2D - dir;
                
                if (floorPositions.Contains(checkPos1) && !floorPositions.Contains(checkPos2))
                {
                    hasFloorOnOneSide = true;
                }
                else if (!floorPositions.Contains(checkPos1) && floorPositions.Contains(checkPos2))
                {
                    hasFloorOnOtherSide = true;
                }
            }
            
            // This wall is a good door candidate if it separates floor from non-floor
            if (hasFloorOnOneSide || hasFloorOnOtherSide)
            {
                doorCandidates.Add(wallPos3D);
            }
        }
    }
    
    IEnumerator PlaceCeilings()
    {
        int placed = 0;
        
        // Place ceilings above floors
        foreach (var kvp in placedObjects)
        {
            if (kvp.Value.name.Contains("Floor"))
            {
                Vector3Int ceilingPos = kvp.Key + Vector3Int.up * 2; // Two units up
                PlaceObject(ceilingPos, ceilingPrefab, "Ceiling");
                placed++;
                
                if (placed % 20 == 0) yield return null;
            }
        }
        
        Debug.Log($"Placed {placed} ceilings");
    }
    
    IEnumerator PlaceFurniture()
    {
        if (furniturePrefabs.Length == 0) yield break;
        
        int placed = 0;
        var floorPositions = placedObjects.Where(kvp => 
            kvp.Value.name.Contains("Floor")).Select(kvp => kvp.Key).ToList();
        
        int furnitureCount = Mathf.RoundToInt(floorPositions.Count * furnitureDensity);
        
        for (int i = 0; i < furnitureCount && floorPositions.Count > 0; i++)
        {
            int index = random.Next(floorPositions.Count);
            Vector3Int floorPos = floorPositions[index];
            floorPositions.RemoveAt(index);
            
            Vector3Int furniturePos = floorPos + Vector3Int.up;
            
            // Make sure position is free
            if (!placedObjects.ContainsKey(furniturePos))
            {
                GameObject furniturePrefab = furniturePrefabs[random.Next(furniturePrefabs.Length)];
                PlaceObject(furniturePos, furniturePrefab, "Furniture");
                placed++;
                
                if (placed % 10 == 0) yield return null;
            }
        }
        
        Debug.Log($"Placed {placed} furniture pieces");
    }
    
    void PlaceObject(Vector3Int gridPos, GameObject prefab, string category)
    {
        if (prefab == null || placedObjects.ContainsKey(gridPos)) return;
        
        Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, gridPos.z);
        
        // Random rotation for some objects
        Quaternion rotation = Quaternion.identity;
        if (category == "Furniture" || category == "Door")
        {
            rotation = Quaternion.Euler(0, random.Next(4) * 90f, 0);
        }
        
        GameObject instance = Instantiate(prefab, worldPos, rotation, levelParent);
        instance.name = $"{category}_{gridPos.x}_{gridPos.y}_{gridPos.z}";
        
        // Ensure proper colliders are set up
        EnsureProperColliders(instance, category);
        
        placedObjects[gridPos] = instance;
    }
    
    void EnsureProperColliders(GameObject obj, string category)
    {
        // Get or add collider component
        Collider collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            // Add appropriate collider type
            switch (category.ToLower())
            {
                case "floor":
                case "wall":
                case "door":
                    collider = obj.AddComponent<BoxCollider>();
                    break;
                case "furniture":
                    // Try to preserve existing collider type, or add box collider
                    if (obj.GetComponent<BoxCollider>() == null && 
                        obj.GetComponent<SphereCollider>() == null && 
                        obj.GetComponent<CapsuleCollider>() == null)
                    {
                        collider = obj.AddComponent<BoxCollider>();
                    }
                    break;
                default:
                    collider = obj.AddComponent<BoxCollider>();
                    break;
            }
        }
        
        // Configure collider properties based on category
        if (collider != null)
        {
            switch (category.ToLower())
            {
                case "floor":
                    // Floor should be solid for walking
                    collider.isTrigger = false;
                    break;
                case "wall":
                    // Walls should block movement
                    collider.isTrigger = false;
                    break;
                case "door":
                    // Doors should allow passage but can be interactable
                    collider.isTrigger = true;
                    // Ensure door has interaction component
                    if (obj.GetComponent<InteractableDoor>() == null)
                    {
                        obj.AddComponent<InteractableDoor>();
                    }
                    break;
                case "ceiling":
                    // Ceiling doesn't need solid collision
                    collider.isTrigger = true;
                    break;
                case "furniture":
                    // Furniture should block movement
                    collider.isTrigger = false;
                    break;
                default:
                    collider.isTrigger = false;
                    break;
            }
        }
    }
    
    // Public methods for runtime control
    [ContextMenu("Generate New Level")]
    public void GenerateNewLevel()
    {
        StopAllCoroutines();
        StartCoroutine(GenerateLevel());
    }
    
    [ContextMenu("Generate With New Seed")]
    public void GenerateWithNewSeed()
    {
        useRandomSeed = true;
        GenerateNewLevel();
    }
    
    [ContextMenu("Clear Level")]
    public void ClearCurrentLevel()
    {
        ClearLevel();
    }
    
    [ContextMenu("Create Test Prefabs")]
    public void CreateTestPrefabs()
    {
        // Create simple test prefabs if none are assigned
        if (floorPrefab == null)
        {
            floorPrefab = CreateSimplePrefab("Floor", new Color(0.6f, 0.4f, 0.2f), new Vector3(1, 0.1f, 1));
        }
        
        if (wallPrefab == null)
        {
            wallPrefab = CreateSimplePrefab("Wall", Color.gray, new Vector3(1, 2, 1));
        }
        
        if (doorPrefab == null)
        {
            doorPrefab = CreateSimplePrefab("Door", Color.blue, new Vector3(1, 2, 0.2f));
        }
        
        Debug.Log("✓ Created test prefabs. You can now generate a level!");
    }
    
    GameObject CreateSimplePrefab(string name, Color color, Vector3 size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.localScale = size;
        
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }
        
        // Ensure proper colliders based on object type
        BoxCollider collider = cube.GetComponent<BoxCollider>();
        if (collider != null)
        {
            switch (name.ToLower())
            {
                case "floor":
                    // Floor should not block movement, but can be walked on
                    collider.isTrigger = false;
                    break;
                case "wall":
                    // Walls should block movement
                    collider.isTrigger = false;
                    break;
                case "door":
                    // Doors can be interactable (for now, not blocking)
                    collider.isTrigger = true;
                    // Add door interaction component
                    var doorScript = cube.AddComponent<InteractableDoor>();
                    break;
                case "ceiling":
                    // Ceiling doesn't need collision
                    collider.isTrigger = true;
                    break;
                default:
                    // Furniture and other objects
                    collider.isTrigger = false;
                    break;
            }
        }
        
        // Disable the cube in scene so it acts as a prefab
        cube.SetActive(false);
        
        Debug.Log($"Created simple {name} prefab with proper colliders");
        return cube;
    }
    
    void CreateTestPlayer()
    {
        // Only create player if one doesn't exist
        if (Object.FindFirstObjectByType<CharacterController>() != null) return;
        
        // Find a good spawn position (center of first room, or level center)
        Vector3 spawnPos = Vector3.zero;
        if (rooms.Count > 0)
        {
            var firstRoom = rooms[0];
            spawnPos = new Vector3(
                firstRoom.x + firstRoom.width * 0.5f,
                1.5f, // Above floor level
                firstRoom.y + firstRoom.height * 0.5f
            );
        }
        else
        {
            spawnPos = new Vector3(levelSize.x * 0.5f, 1.5f, levelSize.z * 0.5f);
        }
        
        // Create player capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Test Player";
        player.transform.position = spawnPos;
        player.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        
        // Set player color
        Renderer playerRenderer = player.GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            playerRenderer.material = mat;
        }
        
        // Replace default collider with CharacterController for better movement
        DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = Vector3.zero;
        
        // Add simple movement script
        // player.AddComponent<SimplePlayerController>();
        
        // Add camera
        GameObject cameraObj = new GameObject("Player Camera");
        cameraObj.transform.parent = player.transform;
        cameraObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        
        // Add simple mouse look
        // cameraObj.AddComponent<SimpleMouseLook>();
        
        Debug.Log($"✓ Created test player at position {spawnPos}");
    }
    
    void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateNewLevel();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateWithNewSeed();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearCurrentLevel();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw level bounds
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(levelSize.x * 0.5f, levelSize.y * 0.5f, levelSize.z * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(levelSize.x, levelSize.y, levelSize.z));
        
        // Draw rooms
        Gizmos.color = Color.green;
        foreach (var room in rooms)
        {
            Vector3 roomCenter = new Vector3(room.x + room.width * 0.5f, 0.5f, room.y + room.height * 0.5f);
            Gizmos.DrawWireCube(roomCenter, new Vector3(room.width, 1f, room.height));
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
