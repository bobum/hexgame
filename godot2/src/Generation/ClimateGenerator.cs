using System;
using System.Collections.Generic;
using System.Threading;

namespace HexGame.Generation;

/// <summary>
/// Generates climate data (moisture) and assigns biomes based on elevation and moisture.
/// Uses hash-based value noise with bilinear interpolation for moisture generation,
/// with coastal boost for realistic climate zones.
///
/// Biome Assignment (by TerrainTypeIndex):
/// - 0: Sand (desert, low moisture, or underwater)
/// - 1: Grass (grassland/plains/forest, medium moisture)
/// - 2: Mud (swamp/jungle, high moisture)
/// - 3: Stone (hills, high elevation)
/// - 4: Snow (mountains/peaks, very high elevation or wet hills)
/// </summary>
public class ClimateGenerator
{
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly int _seed;

    public ClimateGenerator(int gridWidth, int gridHeight, int seed)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _seed = seed;
    }

    /// <summary>
    /// Generates moisture and assigns biomes to all cells.
    /// </summary>
    /// <param name="data">Cell data array (modified in place)</param>
    /// <param name="ct">Optional cancellation token for async generation</param>
    public void Generate(CellData[] data, CancellationToken ct = default)
    {
        if (data.Length == 0 || _gridWidth == 0 || _gridHeight == 0)
            return;

        // Phase 1: Generate base moisture from noise
        var moistureMap = GenerateMoisture(data, ct);

        ct.ThrowIfCancellationRequested();

        // Phase 2: Apply coastal moisture boost
        ApplyCoastalMoistureBoost(data, moistureMap, ct);

        ct.ThrowIfCancellationRequested();

        // Phase 3: Assign biomes based on elevation and moisture
        AssignBiomes(data, moistureMap, ct);
    }

    /// <summary>
    /// Generates base moisture using hash-based value noise.
    /// </summary>
    /// <returns>Array of moisture values (0-1) for each cell</returns>
    private float[] GenerateMoisture(CellData[] data, CancellationToken ct)
    {
        var moistureMap = new float[data.Length];

        // Use seed offset for moisture to decorrelate from land generation
        int moistureSeed = _seed + GenerationConfig.MoistureSeedOffset;

        for (int i = 0; i < data.Length; i++)
        {
            // Check cancellation periodically
            if ((i & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            int x = data[i].X;
            int z = data[i].Z;

            // Generate noise-based moisture
            float moisture = GenerateNoise(x, z, moistureSeed, GenerationConfig.MoistureNoiseScale);

            // Normalize to 0-1 range
            moistureMap[i] = (moisture + 1f) / 2f;
        }

        return moistureMap;
    }

    /// <summary>
    /// Multi-octave hash-based value noise with bilinear interpolation.
    /// Produces smooth, deterministic noise without Godot dependencies.
    /// </summary>
    private float GenerateNoise(int x, int z, int seed, float scale)
    {
        float noise = 0f;
        float amplitude = 1f;
        float frequency = scale;
        float maxValue = 0f;

        for (int octave = 0; octave < 4; octave++)
        {
            float sampleX = x * frequency;
            float sampleZ = z * frequency;

            // Hash-based value noise at this octave
            float value = PseudoNoise(sampleX, sampleZ, seed + octave * 1000);
            noise += value * amplitude;
            maxValue += amplitude;

            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return noise / maxValue;
    }

    /// <summary>
    /// Hash-based value noise with bilinear interpolation.
    /// Note: Coordinates are expected to be within reasonable grid bounds.
    /// For grids larger than ~70 million cells per axis, integer overflow
    /// could occur in the floor conversion. Typical hex grids are far smaller.
    /// </summary>
    private float PseudoNoise(float x, float z, int seed)
    {
        // Safe for coordinates up to ~2 billion (int.MaxValue)
        // With MoistureNoiseScale=0.03, supports grids up to ~70 billion cells
        int ix = (int)Math.Floor(x);
        int iz = (int)Math.Floor(z);

        float fx = x - ix;
        float fz = z - iz;

        // Smoothstep for interpolation
        fx = fx * fx * (3f - 2f * fx);
        fz = fz * fz * (3f - 2f * fz);

        // Corner values
        float n00 = Hash(ix, iz, seed);
        float n10 = Hash(ix + 1, iz, seed);
        float n01 = Hash(ix, iz + 1, seed);
        float n11 = Hash(ix + 1, iz + 1, seed);

        // Bilinear interpolation
        float nx0 = Lerp(n00, n10, fx);
        float nx1 = Lerp(n01, n11, fx);

        return Lerp(nx0, nx1, fz);
    }

    /// <summary>
    /// Fast hash function returning values in range -1 to 1.
    /// Uses unchecked arithmetic for defined overflow behavior.
    /// </summary>
    private float Hash(int x, int z, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + z * 668265263 + seed * 1013904223;
            h = (h ^ (h >> 13)) * 1274126177;
            h = h ^ (h >> 16);
            return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF * 2f - 1f;
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Boosts moisture for cells adjacent to water.
    /// </summary>
    private void ApplyCoastalMoistureBoost(CellData[] data, float[] moistureMap, CancellationToken ct)
    {
        for (int i = 0; i < data.Length; i++)
        {
            // Check cancellation periodically
            if ((i & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            // Only boost land cells
            if (data[i].Elevation < GenerationConfig.WaterLevel)
                continue;

            // Check if adjacent to water
            bool isCoastal = false;
            foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(i, _gridWidth, _gridHeight))
            {
                if (data[neighborIndex].Elevation < GenerationConfig.WaterLevel)
                {
                    isCoastal = true;
                    break;
                }
            }

            if (isCoastal)
            {
                moistureMap[i] = Math.Min(1f, moistureMap[i] + GenerationConfig.CoastalMoistureBoost);
            }
        }
    }

    /// <summary>
    /// Assigns biomes (terrain types) based on elevation and moisture.
    /// Also stores moisture in CellData for river generation.
    /// </summary>
    private void AssignBiomes(CellData[] data, float[] moistureMap, CancellationToken ct)
    {
        for (int i = 0; i < data.Length; i++)
        {
            // Check cancellation periodically
            if ((i & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            data[i].Moisture = moistureMap[i];  // Store for river generation
            data[i].TerrainTypeIndex = GetBiome(data[i].Elevation, moistureMap[i]);
        }
    }

    /// <summary>
    /// Determines the terrain type based on elevation and moisture.
    /// </summary>
    /// <returns>TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow</returns>
    private static int GetBiome(int elevation, float moisture)
    {
        // Underwater cells - use sand (will be covered by water anyway)
        if (elevation < GenerationConfig.WaterLevel)
        {
            return 0; // Sand (underwater)
        }

        // Very high elevation - snow
        if (elevation >= GenerationConfig.MountainElevation)
        {
            return 4; // Snow
        }

        // High elevation (hills) - stone or snow based on moisture
        if (elevation >= GenerationConfig.HillElevation)
        {
            // Wet hills get snow (precipitation), dry hills get stone
            return moisture > GenerationConfig.ForestMoistureMax ? 4 : 3;
        }

        // Land biomes based on moisture thresholds
        // Desert: moisture < 0.2
        if (moisture < GenerationConfig.DesertMoistureMax)
        {
            return 0; // Sand (desert)
        }

        // Jungle/Swamp: moisture >= 0.8
        if (moisture >= GenerationConfig.ForestMoistureMax)
        {
            return 2; // Mud (jungle/swamp)
        }

        // Everything in between: Grassland/Plains/Forest (0.2 <= moisture < 0.8)
        // Feature placement will differentiate these with trees/plants later
        return 1; // Grass
    }
}
