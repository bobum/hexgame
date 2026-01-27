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
    /// </summary>
    public const float ElevationRaiseChance = 0.3f;

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
    /// Water level threshold. Cells at or below this are underwater.
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
    /// Maximum moisture for desert biome.
    /// </summary>
    public const float DesertMoistureMax = 0.2f;

    /// <summary>
    /// Maximum moisture for grassland/savanna biome.
    /// </summary>
    public const float GrasslandMoistureMax = 0.4f;

    /// <summary>
    /// Maximum moisture for plains biome.
    /// </summary>
    public const float PlainsMoistureMax = 0.6f;

    /// <summary>
    /// Maximum moisture for forest biome.
    /// </summary>
    public const float ForestMoistureMax = 0.8f;

    // Above ForestMoistureMax is jungle

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
}
