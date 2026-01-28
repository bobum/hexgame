namespace HexGame.Rendering;

/// <summary>
/// Pure math functions for chunk coordinate and LOD calculations.
/// No Godot dependencies - fully testable without runtime.
/// </summary>
public static class ChunkMath
{
    /// <summary>
    /// LOD level enumeration (mirrors ChunkedRendererBase.LodLevel).
    /// </summary>
    public enum LodLevel
    {
        High,
        Medium,
        Low,
        Culled
    }

    /// <summary>
    /// Gets the chunk coordinates for a world position.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="chunkSize">Size of each chunk in world units.</param>
    /// <returns>Tuple of (chunkX, chunkZ) coordinates.</returns>
    public static (int cx, int cz) GetChunkCoords(float worldX, float worldZ, float chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));

        int cx = (int)Math.Floor(worldX / chunkSize);
        int cz = (int)Math.Floor(worldZ / chunkSize);
        return (cx, cz);
    }

    /// <summary>
    /// Gets the center world position of a chunk.
    /// </summary>
    /// <param name="cx">Chunk X coordinate.</param>
    /// <param name="cz">Chunk Z coordinate.</param>
    /// <param name="chunkSize">Size of each chunk in world units.</param>
    /// <returns>Tuple of (worldX, worldZ) for the chunk center.</returns>
    public static (float x, float z) GetChunkCenter(int cx, int cz, float chunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));

        return ((cx + 0.5f) * chunkSize, (cz + 0.5f) * chunkSize);
    }

    /// <summary>
    /// Calculates squared distance between two points (avoids sqrt for comparison).
    /// </summary>
    public static float DistanceSquared(float x1, float z1, float x2, float z2)
    {
        float dx = x2 - x1;
        float dz = z2 - z1;
        return dx * dx + dz * dz;
    }

    /// <summary>
    /// Selects the appropriate LOD level for a given distance.
    /// Uses hysteresis to prevent flickering at thresholds.
    /// </summary>
    /// <param name="distance">Distance from camera to chunk.</param>
    /// <param name="currentLod">Current LOD level of the chunk.</param>
    /// <param name="lodHighToMedium">Distance threshold for HIGH to MEDIUM.</param>
    /// <param name="lodMediumToLow">Distance threshold for MEDIUM to LOW.</param>
    /// <param name="maxRenderDistance">Maximum render distance (beyond = culled).</param>
    /// <param name="hysteresis">Hysteresis margin to prevent flickering.</param>
    /// <returns>The selected LOD level.</returns>
    public static LodLevel SelectLod(
        float distance,
        LodLevel currentLod,
        float lodHighToMedium,
        float lodMediumToLow,
        float maxRenderDistance,
        float hysteresis = 2.0f)
    {
        // Apply hysteresis based on current LOD
        // When moving to higher detail, don't apply hysteresis
        // When moving to lower detail, apply hysteresis to delay the transition
        float adjustedHysteresis = currentLod switch
        {
            LodLevel.High => 0,              // No hysteresis when going to lower detail
            LodLevel.Medium => hysteresis,   // Add margin when potentially going back to high
            LodLevel.Low => hysteresis,      // Add margin when potentially going back to medium
            _ => 0
        };

        if (distance > maxRenderDistance)
        {
            return LodLevel.Culled;
        }
        else if (distance < lodHighToMedium - (currentLod == LodLevel.High ? 0 : adjustedHysteresis))
        {
            return LodLevel.High;
        }
        else if (distance < lodMediumToLow - (currentLod == LodLevel.Medium ? 0 : adjustedHysteresis))
        {
            return LodLevel.Medium;
        }
        else
        {
            return LodLevel.Low;
        }
    }

    /// <summary>
    /// Determines if a chunk should be visible based on distance.
    /// </summary>
    public static bool IsChunkVisible(float distanceSquared, float maxRenderDistance)
    {
        return distanceSquared <= maxRenderDistance * maxRenderDistance;
    }
}
