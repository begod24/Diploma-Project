using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum TileType
{
    Empty,
    Floor,
    Wall,
    Door,
    Ceiling,
    Furniture
}

[System.Serializable]
public enum ConnectionType
{
    None,
    Floor,
    Wall,
    Door,
    Open
}

[System.Serializable]
public class TileConnector
{
    [Header("Connection Rules")]
    public ConnectionType north = ConnectionType.None;
    public ConnectionType south = ConnectionType.None;
    public ConnectionType east = ConnectionType.None;
    public ConnectionType west = ConnectionType.None;
    public ConnectionType up = ConnectionType.None;
    public ConnectionType down = ConnectionType.None;
    
    public bool CanConnect(TileConnector other, Vector3Int direction)
    {
        ConnectionType myConnection = GetConnectionInDirection(direction);
        ConnectionType otherConnection = other.GetConnectionInDirection(-direction);
        
        // Both must be compatible
        return IsCompatible(myConnection, otherConnection);
    }
    
    public ConnectionType GetConnectionInDirection(Vector3Int direction)
    {
        if (direction == Vector3Int.forward) return north;
        if (direction == Vector3Int.back) return south;
        if (direction == Vector3Int.right) return east;
        if (direction == Vector3Int.left) return west;
        if (direction == Vector3Int.up) return up;
        if (direction == Vector3Int.down) return down;
        return ConnectionType.None;
    }
    
    private bool IsCompatible(ConnectionType a, ConnectionType b)
    {
        if (a == ConnectionType.None || b == ConnectionType.None) return false;
        if (a == ConnectionType.Open || b == ConnectionType.Open) return true;
        return a == b;
    }
}

[CreateAssetMenu(fileName = "New Tile Rule", menuName = "Procedural Generation/Tile Rule")]
public class TileRule : ScriptableObject
{
    [Header("Basic Info")]
    public string tileName;
    public TileType tileType;
    public GameObject prefab;
    
    [Header("Spawn Rules")]
    [Range(0f, 1f)]
    public float spawnWeight = 1f;
    public int maxInstances = -1; // -1 for unlimited
    
    [Header("Connections")]
    public TileConnector connector;
    
    [Header("Placement Rules")]
    public bool canRotate = true;
    public bool requiresSupport = false;
    public LayerMask supportLayers = -1;
    
    [Header("Constraints")]
    public List<TileType> forbiddenNeighbors = new List<TileType>();
    public List<TileType> requiredNeighbors = new List<TileType>();
    public int minNeighborCount = 0;
    public int maxNeighborCount = 8;
    
    [Header("Multi-level Support")]
    public bool canPlaceOnFloor = true;
    public bool canPlaceOnCeiling = false;
    public bool canPlaceOnWalls = false;
    
    public bool CanPlaceAt(Vector3Int position, Dictionary<Vector3Int, TileRule> placedTiles, ProceduralLevelGenerator generator)
    {
        // Check basic placement rules
        if (requiresSupport && !HasSupport(position, placedTiles, generator))
            return false;
        
        // Check neighbor constraints
        var neighbors = GetNeighbors(position, placedTiles);
        
        // Check forbidden neighbors
        foreach (var neighbor in neighbors)
        {
            if (forbiddenNeighbors.Contains(neighbor.tileType))
                return false;
        }
        
        // Check required neighbors
        if (requiredNeighbors.Count > 0)
        {
            bool hasRequiredNeighbor = false;
            foreach (var neighbor in neighbors)
            {
                if (requiredNeighbors.Contains(neighbor.tileType))
                {
                    hasRequiredNeighbor = true;
                    break;
                }
            }
            if (!hasRequiredNeighbor) return false;
        }
        
        // Check neighbor count
        if (neighbors.Count < minNeighborCount || neighbors.Count > maxNeighborCount)
            return false;
        
        return true;
    }
    
    private List<TileRule> GetNeighbors(Vector3Int position, Dictionary<Vector3Int, TileRule> placedTiles)
    {
        List<TileRule> neighbors = new List<TileRule>();
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
                neighbors.Add(placedTiles[neighborPos]);
            }
        }
        
        return neighbors;
    }
    
    private bool HasSupport(Vector3Int position, Dictionary<Vector3Int, TileRule> placedTiles, ProceduralLevelGenerator generator)
    {
        Vector3Int supportPos = position + Vector3Int.down;
        return placedTiles.ContainsKey(supportPos) && 
               (placedTiles[supportPos].tileType == TileType.Floor || 
                placedTiles[supportPos].tileType == TileType.Wall);
    }
}
