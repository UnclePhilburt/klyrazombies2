using UnityEngine;

/// <summary>
/// Configuration for the world chunk system.
/// </summary>
[CreateAssetMenu(fileName = "ChunkConfig", menuName = "Game/Chunk Config")]
public class ChunkConfig : ScriptableObject
{
    [Header("Grid Settings")]
    [Tooltip("Number of chunks in X direction")]
    public int gridSizeX = 3;

    [Tooltip("Number of chunks in Z direction")]
    public int gridSizeZ = 3;

    [Tooltip("Size of each chunk in world units (meters)")]
    public float chunkSize = 300f;

    [Header("Loading Settings")]
    [Tooltip("How many chunks around player to keep loaded (1 = 3x3 grid around player)")]
    public int loadRadius = 1;

    [Tooltip("Scene name prefix (chunks named Chunk_0_0, Chunk_0_1, etc.)")]
    public string chunkScenePrefix = "Chunk";

    [Tooltip("Name of the persistent scene (player, UI, managers)")]
    public string persistentSceneName = "Persistent";

    [Header("World Offset")]
    [Tooltip("World position of chunk (0,0) corner")]
    public Vector3 worldOrigin = Vector3.zero;

    /// <summary>
    /// Get the chunk coordinate for a world position.
    /// </summary>
    public Vector2Int WorldToChunk(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - worldOrigin;
        return new Vector2Int(
            Mathf.FloorToInt(localPos.x / chunkSize),
            Mathf.FloorToInt(localPos.z / chunkSize)
        );
    }

    /// <summary>
    /// Get the scene name for a chunk coordinate.
    /// </summary>
    public string GetChunkSceneName(int x, int z)
    {
        return $"{chunkScenePrefix}_{x}_{z}";
    }

    /// <summary>
    /// Get the scene name for a chunk coordinate.
    /// </summary>
    public string GetChunkSceneName(Vector2Int coord)
    {
        return GetChunkSceneName(coord.x, coord.y);
    }

    /// <summary>
    /// Check if a chunk coordinate is within the valid grid.
    /// </summary>
    public bool IsValidChunk(int x, int z)
    {
        return x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ;
    }

    /// <summary>
    /// Check if a chunk coordinate is within the valid grid.
    /// </summary>
    public bool IsValidChunk(Vector2Int coord)
    {
        return IsValidChunk(coord.x, coord.y);
    }

    /// <summary>
    /// Get the world bounds for a chunk.
    /// </summary>
    public Bounds GetChunkBounds(int x, int z)
    {
        Vector3 min = worldOrigin + new Vector3(x * chunkSize, 0, z * chunkSize);
        Vector3 size = new Vector3(chunkSize, 1000f, chunkSize);
        return new Bounds(min + size * 0.5f, size);
    }
}
