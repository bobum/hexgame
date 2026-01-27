using System;
using System.Threading;

namespace HexGame.Generation;

/// <summary>
/// Generates features (vegetation, farms, urban areas, special structures) based on biomes.
/// Features are placed according to biome suitability and constraints.
///
/// Terrain Type Indices:
/// - 0: Sand (desert) - Sparse urban, ziggurat
/// - 1: Grass (plains) - All types, castle
/// - 2: Mud (jungle/swamp) - Dense plants, megaflora
/// - 3: Stone (hills) - Sparse urban/farm
/// - 4: Snow (mountains) - None/minimal
///
/// Special Feature Indices:
/// - 0: None
/// - 1: Castle (high elevation grass/stone)
/// - 2: Ziggurat (desert/sand)
/// - 3: Megaflora (high moisture jungle/mud)
/// </summary>
public class FeatureGenerator
{
    private readonly Random _rng;

    /// <summary>
    /// Creates a new FeatureGenerator with the specified random number generator.
    /// </summary>
    /// <param name="rng">Random number generator for feature placement decisions</param>
    public FeatureGenerator(Random rng)
    {
        _rng = rng;
    }

    /// <summary>
    /// Creates a new FeatureGenerator with the specified random number generator.
    /// Grid dimensions are accepted for API consistency with other generators but not used.
    /// </summary>
    /// <param name="rng">Random number generator for feature placement decisions</param>
    /// <param name="gridWidth">Grid width (unused, kept for API consistency)</param>
    /// <param name="gridHeight">Grid height (unused, kept for API consistency)</param>
    public FeatureGenerator(Random rng, int gridWidth, int gridHeight)
        : this(rng)
    {
    }

    /// <summary>
    /// Generates features on the cell data array.
    /// </summary>
    /// <param name="data">Cell data array (modified in place)</param>
    /// <param name="ct">Optional cancellation token for async generation</param>
    public void Generate(CellData[] data, CancellationToken ct = default)
    {
        if (data.Length == 0)
            return;

        // Place density features (PlantLevel, FarmLevel, UrbanLevel)
        PlaceDensityFeatures(data, ct);

        ct.ThrowIfCancellationRequested();

        // Place special features (Castle, Ziggurat, Megaflora)
        PlaceSpecialFeatures(data, ct);
    }

    /// <summary>
    /// Places density features (PlantLevel, FarmLevel, UrbanLevel) based on biome.
    /// </summary>
    private void PlaceDensityFeatures(CellData[] data, CancellationToken ct)
    {
        for (int i = 0; i < data.Length; i++)
        {
            // Check cancellation every 256 cells
            if ((i & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            ref CellData cell = ref data[i];

            if (!CanPlaceFeature(ref cell))
                continue;

            // Use feature placement chance
            if (_rng.NextDouble() >= GenerationConfig.FeaturePlacementChance)
                continue;

            cell.PlantLevel = GetPlantLevel(ref cell);
            cell.FarmLevel = GetFarmLevel(ref cell);
            cell.UrbanLevel = GetUrbanLevel(ref cell);
        }
    }

    /// <summary>
    /// Places special features (Castle, Ziggurat, Megaflora) on eligible cells.
    /// </summary>
    private void PlaceSpecialFeatures(CellData[] data, CancellationToken ct)
    {
        for (int i = 0; i < data.Length; i++)
        {
            // Check cancellation every 256 cells
            if ((i & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            ref CellData cell = ref data[i];

            if (!CanPlaceFeature(ref cell))
                continue;

            // Special features are rare
            if (_rng.NextDouble() >= GenerationConfig.SpecialFeatureChance)
                continue;

            // Determine which special feature based on biome
            int specialIndex = GetSpecialFeatureIndex(ref cell);
            if (specialIndex > 0)
            {
                cell.SpecialIndex = specialIndex;
                // Clear density features when placing special feature
                cell.PlantLevel = 0;
                cell.FarmLevel = 0;
                cell.UrbanLevel = 0;
            }
        }
    }

    /// <summary>
    /// Checks if a cell is eligible for feature placement.
    /// Cells must be above water and not have rivers.
    /// </summary>
    internal bool CanPlaceFeature(ref CellData cell)
    {
        // Skip underwater cells
        if (cell.Elevation < GenerationConfig.WaterLevel)
            return false;

        // Skip cells with rivers
        if (cell.HasIncomingRiver || cell.HasOutgoingRiver)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the plant level based on biome (TerrainTypeIndex).
    /// Higher moisture increases plant density.
    /// </summary>
    internal int GetPlantLevel(ref CellData cell)
    {
        // TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow
        switch (cell.TerrainTypeIndex)
        {
            case 0: // Sand (desert) - no plants
                return 0;

            case 1: // Grass (plains) - moderate plants (1-2)
                if (cell.Moisture > GenerationConfig.PlantHighMoistureThreshold)
                    return 2;
                else if (cell.Moisture > GenerationConfig.PlantMediumMoistureThreshold)
                    return _rng.Next(1, 3); // 1 or 2
                else
                    return 1;

            case 2: // Mud (jungle) - dense plants (2-3)
                if (cell.Moisture > GenerationConfig.JungleDensePlantThreshold)
                    return 3;
                else
                    return _rng.Next(2, 4); // 2 or 3

            case 3: // Stone (hills) - sparse plants (0-1)
                return cell.Moisture > GenerationConfig.StonePlantMoistureThreshold ? 1 : 0;

            case 4: // Snow - no plants
                return 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Gets the farm level based on biome (TerrainTypeIndex).
    /// Farms prefer moderate moisture grasslands.
    /// </summary>
    internal int GetFarmLevel(ref CellData cell)
    {
        // TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow
        switch (cell.TerrainTypeIndex)
        {
            case 0: // Sand (desert) - sparse farms (0-1)
                return _rng.NextDouble() < GenerationConfig.SparseFarmChance ? 1 : 0;

            case 1: // Grass (plains) - good farms (1-2)
                // Medium moisture is best for farms
                if (cell.Moisture > GenerationConfig.FarmOptimalMoistureMin &&
                    cell.Moisture < GenerationConfig.FarmOptimalMoistureMax)
                    return _rng.Next(1, 3); // 1 or 2
                else
                    return 1;

            case 2: // Mud (jungle) - sparse farms (0-1)
                return _rng.NextDouble() < GenerationConfig.SparseFarmChance ? 1 : 0;

            case 3: // Stone (hills) - sparse farms (0-1)
                return _rng.NextDouble() < GenerationConfig.StoneFarmChance ? 1 : 0;

            case 4: // Snow - no farms
                return 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Gets the urban level based on biome (TerrainTypeIndex).
    /// Urban areas prefer drier terrain with low plants.
    /// </summary>
    internal int GetUrbanLevel(ref CellData cell)
    {
        // TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow
        switch (cell.TerrainTypeIndex)
        {
            case 0: // Sand (desert) - sparse urban (0-1)
                return _rng.NextDouble() < GenerationConfig.DesertUrbanChance ? 1 : 0;

            case 1: // Grass (plains) - moderate urban (1-2)
                // Lower moisture favors urban development
                if (cell.Moisture < GenerationConfig.UrbanLowMoistureThreshold)
                    return _rng.Next(1, 3); // 1 or 2
                else
                    return _rng.NextDouble() < GenerationConfig.GrassUrbanChance ? 1 : 0;

            case 2: // Mud (jungle) - no urban
                return 0;

            case 3: // Stone (hills) - sparse urban (0-1)
                return _rng.NextDouble() < GenerationConfig.StoneUrbanChance ? 1 : 0;

            case 4: // Snow - no urban
                return 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Gets the special feature index based on biome and cell properties.
    /// Returns 0 if no special feature should be placed.
    /// </summary>
    internal int GetSpecialFeatureIndex(ref CellData cell)
    {
        // TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow
        switch (cell.TerrainTypeIndex)
        {
            case 0: // Sand (desert) - Ziggurat
                return 2; // Ziggurat

            case 1: // Grass (plains) - Castle (high elevation only)
                if (cell.Elevation >= GenerationConfig.CastleMinElevation)
                    return 1; // Castle
                return 0;

            case 2: // Mud (jungle) - Megaflora (high moisture only)
                if (cell.Moisture > GenerationConfig.MegafloraMoistureThreshold)
                    return 3; // Megaflora
                return 0;

            case 3: // Stone (hills) - Castle (high elevation only)
                if (cell.Elevation >= GenerationConfig.CastleMinElevation)
                    return 1; // Castle
                return 0;

            case 4: // Snow - no special features
                return 0;

            default:
                return 0;
        }
    }
}
