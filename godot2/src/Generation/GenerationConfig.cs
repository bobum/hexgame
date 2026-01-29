namespace HexGame.Generation;

/// <summary>
/// Centralized configuration constants for procedural map generation.
/// Tuning these values affects the generated terrain characteristics.
/// </summary>
public static class GenerationConfig
{
    #region Land Generation

    /// <summary>
    /// Target percentage of cells that should be land (0.0 to 1.0).
    /// </summary>
    public const float LandPercentage = 0.5f;

    /// <summary>
    /// Minimum number of cells in a land chunk when raising terrain.
    /// </summary>
    public const int MinChunkSize = 3;

    /// <summary>
    /// Maximum number of cells in a land chunk when raising terrain.
    /// </summary>
    public const int MaxChunkSize = 8;

    /// <summary>
    /// Probability that a chunk expansion continues to a neighbor (0.0 to 1.0).
    /// Higher values create more contiguous landmasses.
    /// </summary>
    public const float ChunkExpansionChance = 0.7f;

    /// <summary>
    /// Probability that a cell is raised above water level (0.0 to 1.0).
    /// Creates elevation variation in land areas.
    /// Higher values create more hills/mountains and longer rivers.
    /// </summary>
    public const float ElevationRaiseChance = 0.5f;

    /// <summary>
    /// Number of additional passes to raise land cell elevations.
    /// Each pass iterates through all land cells and may raise them.
    /// More passes = taller mountains and longer rivers.
    /// </summary>
    public const int ElevationPasses = 4;

    /// <summary>
    /// Safety limit for chunk generation iterations.
    /// </summary>
    public const int MaxChunkIterations = 10000;

    #endregion

    #region Erosion

    /// <summary>
    /// Minimum ratio of land neighbors for a land cell to remain land.
    /// Land cells with fewer land neighbors are eroded to water.
    /// </summary>
    public const float ErosionLandThreshold = 0.3f;

    /// <summary>
    /// Minimum ratio of land neighbors for a water cell to become land.
    /// Water cells with more land neighbors are filled in.
    /// </summary>
    public const float ErosionWaterThreshold = 0.7f;

    #endregion

    #region Elevation

    /// <summary>
    /// Minimum allowed cell elevation.
    /// </summary>
    public const int MinElevation = -2;

    /// <summary>
    /// Maximum allowed cell elevation.
    /// </summary>
    public const int MaxElevation = 8;

    /// <summary>
    /// Water level threshold. Cells below this elevation are underwater.
    /// Cells at WaterLevel or above are considered land.
    /// </summary>
    public const int WaterLevel = 1;

    /// <summary>
    /// Elevation threshold for mountain terrain type.
    /// </summary>
    public const int MountainElevation = 6;

    /// <summary>
    /// Elevation threshold for hill terrain type.
    /// </summary>
    public const int HillElevation = 4;

    #endregion

    #region Moisture/Climate

    /// <summary>
    /// Noise frequency for moisture generation.
    /// Lower values create larger moisture zones.
    /// </summary>
    public const float MoistureNoiseScale = 0.03f;

    /// <summary>
    /// Seed offset for moisture noise to decorrelate from elevation.
    /// </summary>
    public const int MoistureSeedOffset = 1000;

    /// <summary>
    /// Moisture boost for cells adjacent to water.
    /// </summary>
    public const float CoastalMoistureBoost = 0.2f;

    #endregion

    #region Biome Thresholds

    /// <summary>
    /// Maximum moisture for desert biome (TerrainType=Sand).
    /// Moisture below this threshold results in desert terrain.
    /// </summary>
    public const float DesertMoistureMax = 0.2f;

    /// <summary>
    /// Maximum moisture for grassland/savanna biome.
    /// Reserved for future feature placement differentiation.
    /// Currently all moisture 0.2-0.8 maps to Grass terrain type.
    /// </summary>
    public const float GrasslandMoistureMax = 0.4f;

    /// <summary>
    /// Maximum moisture for plains biome.
    /// Reserved for future feature placement differentiation.
    /// Currently all moisture 0.2-0.8 maps to Grass terrain type.
    /// </summary>
    public const float PlainsMoistureMax = 0.6f;

    /// <summary>
    /// Maximum moisture for forest biome (TerrainType=Grass).
    /// Moisture at or above this threshold results in jungle/swamp (Mud).
    /// Used by ClimateGenerator for biome boundaries.
    /// </summary>
    public const float ForestMoistureMax = 0.8f;

    // Above ForestMoistureMax is jungle/swamp (TerrainType=Mud)

    #endregion

    #region River Generation

    /// <summary>
    /// Target percentage of land cells that should have rivers (0.0 to 1.0).
    /// </summary>
    public const float RiverPercentage = 0.05f;

    /// <summary>
    /// Minimum length for a river to be created.
    /// Shorter rivers are discarded.
    /// </summary>
    public const int MinRiverLength = 3;

    /// <summary>
    /// Seed offset for river generation to decorrelate from terrain.
    /// </summary>
    public const int RiverSeedOffset = 7777;

    /// <summary>
    /// Minimum fitness score for a cell to be a river source.
    /// Score = elevation factor * moisture.
    /// </summary>
    public const float RiverSourceMinFitness = 0.25f;

    /// <summary>
    /// Weight multiplier for steeper downhill paths when tracing rivers.
    /// </summary>
    public const float RiverSteepnessWeight = 3.0f;

    /// <summary>
    /// Probability of river flowing to equal-elevation neighbor when no downhill exists.
    /// </summary>
    public const float RiverFlatFlowChance = 0.3f;

    /// <summary>
    /// Safety limit for river tracing iterations.
    /// </summary>
    public const int MaxRiverTraceSteps = 100;

    #endregion

    #region Feature Placement

    /// <summary>
    /// Seed offset for feature placement to decorrelate from terrain.
    /// </summary>
    public const int FeatureSeedOffset = 2000;

    /// <summary>
    /// Base probability for placing any feature on eligible cells.
    /// </summary>
    public const float FeaturePlacementChance = 0.7f;

    /// <summary>
    /// Probability of placing a special feature on eligible cells.
    /// </summary>
    public const float SpecialFeatureChance = 0.02f;

    /// <summary>
    /// Minimum elevation for castle placement.
    /// Set equal to HillElevation so castles appear on hills and mountains.
    /// </summary>
    public const int CastleMinElevation = HillElevation;

    /// <summary>
    /// Moisture threshold for high plant density on grass terrain (level 2).
    /// </summary>
    public const float PlantHighMoistureThreshold = 0.6f;

    /// <summary>
    /// Moisture threshold for medium plant density on grass terrain (level 1-2).
    /// </summary>
    public const float PlantMediumMoistureThreshold = 0.3f;

    /// <summary>
    /// Moisture threshold for dense plants on mud/jungle terrain (level 3).
    /// </summary>
    public const float JungleDensePlantThreshold = 0.7f;

    /// <summary>
    /// Moisture threshold for sparse plants on stone terrain.
    /// </summary>
    public const float StonePlantMoistureThreshold = 0.5f;

    /// <summary>
    /// Probability of sparse farms on desert/mud terrain.
    /// </summary>
    public const float SparseFarmChance = 0.3f;

    /// <summary>
    /// Minimum moisture for optimal farm placement on grass terrain.
    /// </summary>
    public const float FarmOptimalMoistureMin = 0.4f;

    /// <summary>
    /// Maximum moisture for optimal farm placement on grass terrain.
    /// </summary>
    public const float FarmOptimalMoistureMax = 0.7f;

    /// <summary>
    /// Probability of sparse farms on stone terrain.
    /// </summary>
    public const float StoneFarmChance = 0.4f;

    /// <summary>
    /// Probability of sparse urban on desert terrain.
    /// </summary>
    public const float DesertUrbanChance = 0.15f;

    /// <summary>
    /// Moisture threshold below which grass terrain favors urban development.
    /// </summary>
    public const float UrbanLowMoistureThreshold = 0.4f;

    /// <summary>
    /// Probability of moderate urban on grass terrain with higher moisture.
    /// </summary>
    public const float GrassUrbanChance = 0.2f;

    /// <summary>
    /// Probability of sparse urban on stone terrain.
    /// </summary>
    public const float StoneUrbanChance = 0.1f;

    /// <summary>
    /// Moisture threshold for megaflora placement on mud terrain.
    /// </summary>
    public const float MegafloraMoistureThreshold = 0.7f;

    #endregion

    #region Weighted Selection

    /// <summary>
    /// Score threshold for high-priority selection.
    /// </summary>
    public const float WeightedSelectionHighThreshold = 0.75f;

    /// <summary>
    /// Score threshold for medium-priority selection.
    /// </summary>
    public const float WeightedSelectionMediumThreshold = 0.5f;

    /// <summary>
    /// Weight for high-priority candidates.
    /// </summary>
    public const float WeightHighPriority = 4.0f;

    /// <summary>
    /// Weight for medium-priority candidates.
    /// </summary>
    public const float WeightMediumPriority = 2.0f;

    /// <summary>
    /// Weight for low-priority candidates.
    /// </summary>
    public const float WeightLowPriority = 1.0f;

    #endregion

    #region Road Generation

    /// <summary>
    /// Seed offset for road generation to decorrelate from features.
    /// </summary>
    public const int RoadSeedOffset = 3000;

    /// <summary>
    /// Maximum path length when connecting settlements.
    /// </summary>
    public const int MaxRoadPathLength = 50;

    /// <summary>
    /// Maximum distance to search for nearby settlements to connect.
    /// </summary>
    public const int MaxSettlementConnectionDistance = 15;

    /// <summary>
    /// Minimum urban level for a cell to be considered a settlement.
    /// Level 2+ significantly reduces settlement count for faster road generation.
    /// </summary>
    public const int MinUrbanLevelForSettlement = 2;

    #endregion
}
