using HexGame.Core;

namespace HexGame.Generation;

/// <summary>
/// Procedural map generation using noise.
/// Supports both sync and async (threaded) generation.
/// </summary>
public class MapGenerator : IMapGenerator
{
    private HexGrid? _grid;
    private FastNoiseLite _noise;
    private RiverGenerator? _riverGenerator;
    private FeatureGenerator? _featureGenerator;

    // Threading support
    private Thread? _thread;
    private volatile bool _isGenerating;
    private int _pendingSeed;
    private CellData[]? _pendingCellData;
    private float _workerTimeMs;

    #region Configuration Properties

    public float NoiseScale { get; set; } = GameConstants.Generation.NoiseScale;
    public int Octaves { get; set; } = GameConstants.Generation.NoiseOctaves;
    public float Persistence { get; set; } = GameConstants.Generation.NoisePersistence;
    public float Lacunarity { get; set; } = GameConstants.Generation.NoiseLacunarity;
    public float SeaLevel { get; set; } = GameConstants.Generation.SeaLevelThreshold;
    public float MountainLevel { get; set; } = GameConstants.Generation.MountainLevelThreshold;
    public float RiverPercentage { get; set; } = GameConstants.Generation.RiverPercentage;

    public bool IsGenerating => _isGenerating;

    #endregion

    #region Events

    public event Action? GenerationStarted;
    public event Action<string, float>? GenerationProgress;
    public event Action<bool, float, float>? GenerationCompleted;

    #endregion

    public MapGenerator()
    {
        _noise = new FastNoiseLite();
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _noise.Frequency = NoiseScale;
        _noise.FractalOctaves = Octaves;
        _noise.FractalGain = Persistence;
        _noise.FractalLacunarity = Lacunarity;
    }

    #region IService Implementation

    /// <summary>
    /// Initializes the map generator service.
    /// </summary>
    public void Initialize()
    {
        // Already initialized in constructor
    }

    /// <summary>
    /// Shuts down the map generator, canceling any ongoing generation.
    /// </summary>
    public void Shutdown()
    {
        CancelGeneration();
        GenerationStarted = null;
        GenerationProgress = null;
        GenerationCompleted = null;
    }

    #endregion

    #region Synchronous Generation

    public void Generate(HexGrid grid, int seed = 0)
    {
        _grid = grid;
        int actualSeed = seed != 0 ? seed : new Random().Next();
        _noise.Seed = actualSeed;

        GenerationStarted?.Invoke();
        GenerationProgress?.Invoke("terrain", 0f);

        // Generate elevation
        GenerateElevation();
        GenerationProgress?.Invoke("moisture", 0.25f);

        // Generate moisture
        GenerateMoisture();
        GenerationProgress?.Invoke("biomes", 0.4f);

        // Assign biomes
        AssignBiomes();
        GenerationProgress?.Invoke("rivers", 0.55f);

        // Generate rivers
        GenerateRivers(actualSeed);
        GenerationProgress?.Invoke("features", 0.75f);

        // Generate features
        GenerateFeatures(actualSeed);
        GenerationProgress?.Invoke("complete", 1f);

        GenerationCompleted?.Invoke(true, 0, 0);
    }

    private void GenerateElevation()
    {
        foreach (var cell in _grid!.GetAllCells())
        {
            var worldPos = cell.GetWorldPosition();
            float noiseVal = (_noise.GetNoise2D(worldPos.X, worldPos.Z) + 1f) / 2f;

            // Convert to elevation using sea level system
            if (noiseVal < SeaLevel)
            {
                float normalized = noiseVal / SeaLevel;
                cell.Elevation = Mathf.RoundToInt(normalized * HexMetrics.SeaLevel);
            }
            else
            {
                float normalized = (noiseVal - SeaLevel) / (1f - SeaLevel);
                int landRange = HexMetrics.MaxElevation - HexMetrics.LandMinElevation;
                cell.Elevation = HexMetrics.LandMinElevation + (int)(normalized * landRange);
            }
        }
    }

    private void GenerateMoisture()
    {
        var moistureNoise = new FastNoiseLite();
        moistureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        moistureNoise.Seed = _noise.Seed + GameConstants.Generation.MoistureNoiseSeedOffset;
        moistureNoise.Frequency = GameConstants.Generation.MoistureNoiseFrequency;

        foreach (var cell in _grid!.GetAllCells())
        {
            var worldPos = cell.GetWorldPosition();
            cell.Moisture = (moistureNoise.GetNoise2D(worldPos.X, worldPos.Z) + 1f) / 2f;
        }
    }

    private void AssignBiomes()
    {
        foreach (var cell in _grid!.GetAllCells())
        {
            cell.TerrainType = GetBiome(cell.Elevation, cell.Moisture);
        }
    }

    private TerrainType GetBiome(int elevation, float moisture)
    {
        // Water
        if (elevation < HexMetrics.LandMinElevation)
        {
            return elevation < HexMetrics.SeaLevel - 2 ? TerrainType.Ocean : TerrainType.Coast;
        }

        int heightAboveSea = elevation - HexMetrics.LandMinElevation;

        // High elevation
        if (heightAboveSea >= GameConstants.Generation.MountainElevation) return TerrainType.Snow;
        if (heightAboveSea >= GameConstants.Generation.HillElevation) return TerrainType.Mountains;

        // Land biomes based on moisture
        if (moisture < GameConstants.Generation.DesertMoisture) return TerrainType.Desert;
        if (moisture < GameConstants.Generation.GrasslandMoisture) return heightAboveSea >= 2 ? TerrainType.Hills : TerrainType.Savanna;
        if (moisture < GameConstants.Generation.ForestMoisture) return TerrainType.Plains;
        if (moisture < GameConstants.Generation.JungleMoisture) return TerrainType.Forest;
        return TerrainType.Jungle;
    }

    private void GenerateRivers(int seed)
    {
        if (RiverPercentage <= 0) return;

        _riverGenerator = new RiverGenerator(_grid!);
        _riverGenerator.Generate(seed, RiverPercentage);
    }

    private void GenerateFeatures(int seed)
    {
        _featureGenerator = new FeatureGenerator(_grid!);
        _featureGenerator.Generate(seed);
    }

    #endregion

    #region Asynchronous Generation

    public void GenerateAsync(HexGrid grid, int seed = 0)
    {
        if (_isGenerating)
        {
            GD.PushError("MapGenerator: Generation already in progress");
            return;
        }

        _grid = grid;
        _pendingSeed = seed != 0 ? seed : new Random().Next();
        _isGenerating = true;

        GenerationStarted?.Invoke();
        GenerationProgress?.Invoke("terrain", 0f);

        // Start background thread
        _thread = new Thread(ThreadGenerateTerrain);
        _thread.Start();
    }

    private void ThreadGenerateTerrain()
    {
        var startTime = DateTime.UtcNow;

        // Create noise generators in thread (thread-safe)
        var elevNoise = new FastNoiseLite();
        elevNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        elevNoise.Seed = _pendingSeed;
        elevNoise.Frequency = NoiseScale;
        elevNoise.FractalOctaves = Octaves;
        elevNoise.FractalGain = Persistence;
        elevNoise.FractalLacunarity = Lacunarity;

        var moistNoise = new FastNoiseLite();
        moistNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        moistNoise.Seed = _pendingSeed + GameConstants.Generation.MoistureNoiseSeedOffset;
        moistNoise.Frequency = GameConstants.Generation.MoistureNoiseFrequency;

        int width = _grid!.Width;
        int height = _grid.Height;
        var cells = new CellData[width * height];

        for (int r = 0; r < height; r++)
        {
            for (int q = 0; q < width; q++)
            {
                // Calculate world position
                float x = (q + r * 0.5f) * (HexMetrics.InnerRadius * 2f);
                float z = r * (HexMetrics.OuterRadius * 1.5f);

                // Elevation from noise
                float noiseVal = (elevNoise.GetNoise2D(x, z) + 1f) / 2f;
                int elevation;
                if (noiseVal < SeaLevel)
                {
                    float normalized = noiseVal / SeaLevel;
                    elevation = Mathf.RoundToInt(normalized * HexMetrics.SeaLevel);
                }
                else
                {
                    float normalized = (noiseVal - SeaLevel) / (1f - SeaLevel);
                    int landRange = HexMetrics.MaxElevation - HexMetrics.LandMinElevation;
                    elevation = HexMetrics.LandMinElevation + (int)(normalized * landRange);
                }

                // Moisture from noise
                float moisture = (moistNoise.GetNoise2D(x, z) + 1f) / 2f;

                // Determine terrain type
                var terrainType = GetBiome(elevation, moisture);

                cells[r * width + q] = new CellData(q, r, elevation, moisture, terrainType);
            }
        }

        _pendingCellData = cells;
        _workerTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
    }

    public bool IsGenerationComplete()
    {
        return _thread != null && !_thread.IsAlive;
    }

    public GenerationResult FinishAsyncGeneration()
    {
        if (_thread == null)
        {
            return new GenerationResult(0, 0);
        }

        _thread.Join();
        _thread = null;

        GenerationProgress?.Invoke("applying", 0.3f);

        // Apply terrain data to grid
        if (_pendingCellData != null)
        {
            foreach (var cellData in _pendingCellData)
            {
                var cell = _grid!.GetCell(cellData.Q, cellData.R);
                if (cell != null)
                {
                    cell.Elevation = cellData.Elevation;
                    cell.Moisture = cellData.Moisture;
                    cell.TerrainType = cellData.TerrainType;
                }
            }
            _pendingCellData = null;
        }

        GenerationProgress?.Invoke("rivers", 0.5f);

        // Generate rivers on main thread
        var featureStart = DateTime.UtcNow;
        GenerateRivers(_pendingSeed);

        GenerationProgress?.Invoke("features", 0.75f);

        // Generate features on main thread
        GenerateFeatures(_pendingSeed);

        float featureTimeMs = (float)(DateTime.UtcNow - featureStart).TotalMilliseconds;

        _isGenerating = false;
        GenerationProgress?.Invoke("complete", 1f);
        GenerationCompleted?.Invoke(true, _workerTimeMs, featureTimeMs);

        return new GenerationResult(_workerTimeMs, featureTimeMs);
    }

    public void CancelGeneration()
    {
        if (_thread != null && _thread.IsAlive)
        {
            // Can't actually cancel thread, but we can wait and discard results
            _thread.Join();
        }
        _thread = null;
        _isGenerating = false;
        _pendingCellData = null;
    }

    #endregion

    /// <summary>
    /// Cell data for async generation transfer.
    /// </summary>
    private readonly record struct CellData(int Q, int R, int Elevation, float Moisture, TerrainType TerrainType);
}
