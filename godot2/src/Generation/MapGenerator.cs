using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace HexGame.Generation;

/// <summary>
/// Procedural map generator orchestrating terrain, climate, rivers, and features.
/// Supports both synchronous and asynchronous generation with progress reporting.
///
/// Generation Pipeline:
/// 1. ResetCells - Clear all cells to default underwater state
/// 2. GenerateLand - Raise terrain using chunk budget system
/// 3. GenerateMoisture - Apply noise-based moisture
/// 4. AssignBiomes - Set terrain types based on elevation/moisture
/// 5. GenerateRivers - Create rivers flowing downhill
/// 6. PlaceFeatures - Add vegetation/structures based on biome
///
/// Thread Safety:
/// - Async generation uses intermediate CellData structures
/// - All HexCell/HexGrid modifications happen on main thread
/// - Events are only fired from main thread
/// </summary>
public class MapGenerator : IMapGenerator
{
    private HexGrid? _grid;
    private Random _rng = new();
    private int _currentSeed;

    // Cached grid dimensions (set at start of generation)
    private int _gridWidth;
    private int _gridHeight;

    // Async generation support
    private Thread? _workerThread;
    private CancellationTokenSource? _cts;
    private volatile bool _isGenerating;
    private volatile bool _asyncComplete;
    private volatile bool _asyncFailed;
    private string? _asyncError;

    // Thread-safe intermediate data for async generation
    private CellData[]? _generatedData;

    // Shared intermediate data for sync generation pipeline
    private CellData[]? _syncData;

    // Pending progress updates (queued from worker thread, dispatched on main thread)
    private readonly Queue<(string stage, float progress)> _pendingProgress = new();
    private readonly object _progressLock = new();

    #region Events

    public event Action? GenerationStarted;
    public event Action<string, float>? GenerationProgress;
    public event Action<bool>? GenerationCompleted;

    #endregion

    #region Properties

    public bool IsGenerating => _isGenerating;

    #endregion

    #region IMapGenerator Implementation

    /// <summary>
    /// Generates a map synchronously (blocking).
    /// </summary>
    public void Generate(HexGrid grid, int seed = 0)
    {
        if (_isGenerating)
        {
            GD.PrintErr("MapGenerator: Generation already in progress");
            return;
        }

        _grid = grid;
        _currentSeed = seed != 0 ? seed : (int)GD.Randi();
        _rng = new Random(_currentSeed);
        CacheGridDimensions();

        GD.Print($"[MapGenerator] Starting synchronous generation with seed {_currentSeed}");
        _isGenerating = true;

        GenerationStarted?.Invoke();

        try
        {
            // Suppress chunk refreshes during bulk modifications
            _grid.SetChunkRefreshSuppression(true);

            RunGenerationPipelineSync();

            _grid.SetChunkRefreshSuppression(false);
            _grid.RefreshAllChunks();

            GenerationCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapGenerator] Generation failed: {ex.Message}");
            _grid.SetChunkRefreshSuppression(false);
            GenerationCompleted?.Invoke(false);
        }
        finally
        {
            _isGenerating = false;
            _grid = null; // Release reference to allow GC
        }
    }

    /// <summary>
    /// Starts asynchronous map generation.
    /// Uses intermediate data structures - HexGrid is only modified on main thread.
    /// </summary>
    public void GenerateAsync(HexGrid grid, int seed = 0)
    {
        if (_isGenerating)
        {
            GD.PrintErr("MapGenerator: Generation already in progress");
            return;
        }

        _grid = grid;
        _currentSeed = seed != 0 ? seed : (int)GD.Randi();
        _rng = new Random(_currentSeed);
        CacheGridDimensions();

        GD.Print($"[MapGenerator] Starting async generation with seed {_currentSeed}");
        _isGenerating = true;
        _asyncComplete = false;
        _asyncFailed = false;
        _asyncError = null;
        _generatedData = null;

        _cts = new CancellationTokenSource();

        GenerationStarted?.Invoke();

        _workerThread = new Thread(() => AsyncWorker(_cts.Token));
        _workerThread.Start();
    }

    /// <summary>
    /// Checks if async generation has completed.
    /// </summary>
    public bool IsGenerationComplete()
    {
        return _asyncComplete;
    }

    /// <summary>
    /// Applies the results of async generation to the grid.
    /// Must be called from main thread after IsGenerationComplete() returns true.
    /// </summary>
    public void FinishAsyncGeneration()
    {
        if (_workerThread != null)
        {
            _workerThread.Join();
            _workerThread = null;
        }

        _cts?.Dispose();
        _cts = null;

        // Dispatch any pending progress updates
        DispatchPendingProgress();

        if (_asyncFailed)
        {
            GD.PrintErr($"[MapGenerator] Async generation failed: {_asyncError}");
            _isGenerating = false;
            _grid = null;
            GenerationCompleted?.Invoke(false);
            return;
        }

        // Apply generated data to grid (main thread only)
        if (_generatedData != null && _grid != null)
        {
            ApplyGeneratedDataToGrid();
        }

        ReportProgressMainThread("Complete", 1.0f);

        _isGenerating = false;
        _grid = null; // Release reference to allow GC
        _generatedData = null;
        GenerationCompleted?.Invoke(true);
    }

    /// <summary>
    /// Cancels any ongoing async generation.
    /// Non-blocking - signals cancellation and returns immediately.
    /// </summary>
    public void CancelGeneration()
    {
        _cts?.Cancel();

        // Don't block waiting for thread - let it finish naturally
        // The thread checks cancellation token and exits early
        _isGenerating = false;
        _asyncComplete = true;
        _generatedData = null;
    }

    #endregion

    // CellData is defined in LandGenerator.cs and shared across generators

    #region Synchronous Generation Pipeline

    /// <summary>
    /// Runs the full generation pipeline synchronously.
    /// Uses intermediate CellData array, then applies to HexGrid at the end.
    /// </summary>
    private void RunGenerationPipelineSync()
    {
        ReportProgressMainThread("Initializing", 0f);
        InitializeSyncData();

        ReportProgressMainThread("Generating land", 0.1f);
        GenerateLandSync();

        ReportProgressMainThread("Generating climate", 0.4f);
        GenerateClimateSync();

        ReportProgressMainThread("Generating rivers", 0.6f);
        GenerateRiversSync();

        ReportProgressMainThread("Placing features", 0.75f);
        PlaceFeaturesSync();

        ReportProgressMainThread("Generating roads", 0.85f);
        GenerateRoadsSync();

        ReportProgressMainThread("Applying to grid", 0.9f);
        ApplySyncDataToGrid();

        ReportProgressMainThread("Finalizing", 0.95f);
        _syncData = null; // Release memory
    }

    /// <summary>
    /// Initializes the intermediate data array for sync generation.
    /// </summary>
    private void InitializeSyncData()
    {
        int totalCells = _gridWidth * _gridHeight;
        _syncData = new CellData[totalCells];

        for (int z = 0; z < _gridHeight; z++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                int index = z * _gridWidth + x;
                _syncData[index] = new CellData(x, z);
            }
        }

        GD.Print($"[MapGenerator] Initialized {totalCells} cells for generation");
    }

    /// <summary>
    /// Generates land using chunk-based algorithm.
    /// </summary>
    private void GenerateLandSync()
    {
        if (_syncData == null) return;

        var landGenerator = new LandGenerator(_rng, _gridWidth, _gridHeight);
        landGenerator.Generate(_syncData, GenerationConfig.LandPercentage);

        int landCells = 0;
        foreach (var cell in _syncData)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                landCells++;
        }

        GD.Print($"[MapGenerator] Land: {landCells} cells ({100f * landCells / _syncData.Length:F1}%)");
    }

    /// <summary>
    /// Generates climate (moisture) and assigns biomes.
    /// </summary>
    private void GenerateClimateSync()
    {
        if (_syncData == null) return;

        var climateGenerator = new ClimateGenerator(_gridWidth, _gridHeight, _currentSeed);
        climateGenerator.Generate(_syncData);

        GD.Print("[MapGenerator] Climate and biomes assigned");
    }

    private void GenerateRiversSync()
    {
        if (_syncData == null) return;

        // Debug: count land cells and check moisture
        int landCells = 0;
        int cellsWithMoisture = 0;
        float maxMoisture = 0f;
        int maxElevation = 0;
        foreach (var cell in _syncData)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
            {
                landCells++;
                if (cell.Moisture > 0) cellsWithMoisture++;
                if (cell.Moisture > maxMoisture) maxMoisture = cell.Moisture;
                if (cell.Elevation > maxElevation) maxElevation = cell.Elevation;
            }
        }
        GD.Print($"[RiverDebug] Land cells: {landCells}, with moisture: {cellsWithMoisture}, maxMoisture: {maxMoisture:F2}, maxElevation: {maxElevation}");

        var riverGenerator = new RiverGenerator(_rng, _gridWidth, _gridHeight);

        // Debug: check sources before generation
        var sources = riverGenerator.FindRiverSources(_syncData);
        GD.Print($"[RiverDebug] Found {sources.Count} candidate river sources");

        if (sources.Count > 0)
        {
            // Show fitness of first few sources
            for (int i = 0; i < Math.Min(3, sources.Count); i++)
            {
                var idx = sources[i];
                var cell = _syncData[idx];
                float fitness = (float)(cell.Elevation - GenerationConfig.WaterLevel) /
                               (GenerationConfig.MaxElevation - GenerationConfig.WaterLevel) * cell.Moisture;
                GD.Print($"[RiverDebug]   Source {idx}: elevation={cell.Elevation}, moisture={cell.Moisture:F2}, fitness={fitness:F2}");
            }
        }

        riverGenerator.Generate(_syncData);

        int riverCells = 0;
        var riverLocations = new System.Collections.Generic.List<string>();
        foreach (var cell in _syncData)
        {
            if (cell.HasOutgoingRiver || cell.HasIncomingRiver)
            {
                riverCells++;
                // Convert offset coords to cube coords
                int x = cell.X;
                int z = cell.Z;
                int cubeX = x - z / 2;
                int cubeZ = z;
                int cubeY = -cubeX - cubeZ;
                string riverType = cell.HasOutgoingRiver && cell.HasIncomingRiver ? "through" :
                                   cell.HasOutgoingRiver ? "source" : "end";
                riverLocations.Add($"({cubeX},{cubeY},{cubeZ}) [{riverType}]");
            }
        }

        GD.Print($"[MapGenerator] Rivers: {riverCells} cells with rivers");
        if (riverLocations.Count > 0)
        {
            GD.Print($"[RiverCells] Locations (cube coords):");
            foreach (var loc in riverLocations)
            {
                GD.Print($"  {loc}");
            }
        }
    }

    private void PlaceFeaturesSync()
    {
        if (_syncData == null) return;

        var featureRng = new Random(_currentSeed + GenerationConfig.FeatureSeedOffset);
        var featureGenerator = new FeatureGenerator(featureRng, _gridWidth, _gridHeight);
        featureGenerator.Generate(_syncData);

        GD.Print("[MapGenerator] Features placed");
    }

    private void GenerateRoadsSync()
    {
        if (_syncData == null) return;

        var roadRng = new Random(_currentSeed + GenerationConfig.RoadSeedOffset);
        var roadGenerator = new RoadGenerator(roadRng, _gridWidth, _gridHeight);
        roadGenerator.Generate(_syncData);

        GD.Print("[MapGenerator] Roads generated");
    }

    /// <summary>
    /// Applies generated data to the HexGrid.
    /// Uses two passes: first set all cell properties, then apply rivers.
    /// This ensures neighbor elevations are correct when validating rivers.
    /// </summary>
    private void ApplySyncDataToGrid()
    {
        if (_grid == null || _syncData == null) return;

        // Pass 1: Apply all cell properties (elevation, terrain, etc.)
        foreach (var cellData in _syncData)
        {
            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            cell.WaterLevel = cellData.WaterLevel;
            cell.Elevation = cellData.Elevation;
            cell.TerrainTypeIndex = cellData.TerrainTypeIndex;
            cell.UrbanLevel = cellData.UrbanLevel;
            cell.FarmLevel = cellData.FarmLevel;
            cell.PlantLevel = cellData.PlantLevel;
            cell.SpecialIndex = cellData.SpecialIndex;
            cell.Walled = cellData.Walled;
            cell.RemoveRiver();
            cell.RemoveRoads();
        }

        // Pass 2: Apply rivers (now that all elevations are set correctly)
        int riversApplied = 0;
        int riversFailed = 0;
        foreach (var cellData in _syncData)
        {
            if (!cellData.HasOutgoingRiver) continue;

            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            var direction = (HexDirection)cellData.OutgoingRiverDirection;
            var neighbor = cell.GetNeighbor(direction);
            if (neighbor != null)
            {
                bool hadRiverBefore = cell.HasOutgoingRiver;
                cell.SetOutgoingRiver(direction);
                if (cell.HasOutgoingRiver && !hadRiverBefore)
                {
                    riversApplied++;
                }
                else if (!cell.HasOutgoingRiver)
                {
                    riversFailed++;
                    if (riversFailed <= 5)
                    {
                        GD.Print($"[RiverApply] Failed: cell({cellData.X},{cellData.Z}) elev={cell.Elevation} -> neighbor elev={neighbor.Elevation}, dir={direction}");
                    }
                }
            }
        }

        // Pass 3: Apply roads (after rivers, so we can check bridge validity)
        int roadsApplied = 0;
        foreach (var cellData in _syncData)
        {
            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            for (int dir = 0; dir < 6; dir++)
            {
                if (cellData.HasRoadInDirection(dir))
                {
                    cell.AddRoad((HexDirection)dir);
                    roadsApplied++;
                }
            }
        }

        GD.Print($"[MapGenerator] Applied data to grid: {riversApplied} rivers, {riversFailed} failed, {roadsApplied} road segments");
    }

    #endregion

    #region Asynchronous Generation Pipeline

    /// <summary>
    /// Async worker thread entry point.
    /// Generates to intermediate CellData array - NO Godot node access.
    /// </summary>
    private void AsyncWorker(CancellationToken ct)
    {
        try
        {
            _generatedData = RunGenerationPipelineAsync(ct);
            _asyncComplete = true;
        }
        catch (OperationCanceledException)
        {
            _asyncComplete = true;
            _asyncFailed = false; // Cancellation is not a failure
        }
        catch (Exception ex)
        {
            _asyncError = ex.Message;
            _asyncFailed = true;
            _asyncComplete = true;
        }
    }

    /// <summary>
    /// Runs the generation pipeline to intermediate data.
    /// Thread-safe - no Godot node access.
    /// </summary>
    private CellData[] RunGenerationPipelineAsync(CancellationToken ct)
    {
        int totalCells = _gridWidth * _gridHeight;
        var data = new CellData[totalCells];

        // Initialize all cells to underwater state
        QueueProgress("Resetting cells", 0f);
        for (int z = 0; z < _gridHeight; z++)
        {
            ct.ThrowIfCancellationRequested();
            for (int x = 0; x < _gridWidth; x++)
            {
                int index = z * _gridWidth + x;
                data[index] = new CellData(x, z);
            }
        }

        ct.ThrowIfCancellationRequested();

        // Generate land
        QueueProgress("Generating land", 0.1f);
        GenerateLandAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        // Generate climate (moisture + biomes)
        QueueProgress("Generating climate", 0.4f);
        GenerateClimateAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        // Generate rivers
        QueueProgress("Generating rivers", 0.6f);
        GenerateRiversAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        // Place features
        QueueProgress("Placing features", 0.75f);
        PlaceFeaturesAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        // Generate roads
        QueueProgress("Generating roads", 0.85f);
        GenerateRoadsAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        QueueProgress("Finalizing", 0.9f);

        return data;
    }

    private void GenerateLandAsync(CellData[] data, CancellationToken ct)
    {
        // Use LandGenerator for chunk-based land creation with cancellation support
        var landGenerator = new LandGenerator(_rng, _gridWidth, _gridHeight);
        landGenerator.Generate(data, GenerationConfig.LandPercentage, ct);
    }

    private void GenerateClimateAsync(CellData[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use ClimateGenerator for moisture and biome assignment
        var climateGenerator = new ClimateGenerator(_gridWidth, _gridHeight, _currentSeed);
        climateGenerator.Generate(data, ct);
    }

    private void GenerateRiversAsync(CellData[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use RiverGenerator for river creation
        var riverGenerator = new RiverGenerator(_rng, _gridWidth, _gridHeight);
        riverGenerator.Generate(data, ct);
    }

    private void PlaceFeaturesAsync(CellData[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use FeatureGenerator for feature placement with dedicated seed
        var featureRng = new Random(_currentSeed + GenerationConfig.FeatureSeedOffset);
        var featureGenerator = new FeatureGenerator(featureRng, _gridWidth, _gridHeight);
        featureGenerator.Generate(data, ct);
    }

    private void GenerateRoadsAsync(CellData[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use RoadGenerator for road creation with dedicated seed
        var roadRng = new Random(_currentSeed + GenerationConfig.RoadSeedOffset);
        var roadGenerator = new RoadGenerator(roadRng, _gridWidth, _gridHeight);
        roadGenerator.Generate(data, ct);
    }

    /// <summary>
    /// Applies generated data to the HexGrid.
    /// Must be called from main thread only.
    /// Uses two passes: first set all cell properties, then apply rivers.
    /// </summary>
    private void ApplyGeneratedDataToGrid()
    {
        if (_grid == null || _generatedData == null) return;

        _grid.SetChunkRefreshSuppression(true);

        // Pass 1: Apply all cell properties (elevation, terrain, etc.)
        foreach (var cellData in _generatedData)
        {
            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            cell.WaterLevel = cellData.WaterLevel;
            cell.Elevation = cellData.Elevation;
            cell.TerrainTypeIndex = cellData.TerrainTypeIndex;
            cell.UrbanLevel = cellData.UrbanLevel;
            cell.FarmLevel = cellData.FarmLevel;
            cell.PlantLevel = cellData.PlantLevel;
            cell.SpecialIndex = cellData.SpecialIndex;
            cell.Walled = cellData.Walled;
            cell.RemoveRiver();
            cell.RemoveRoads();
        }

        // Pass 2: Apply rivers (now that all elevations are set correctly)
        foreach (var cellData in _generatedData)
        {
            if (!cellData.HasOutgoingRiver) continue;

            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            var direction = (HexDirection)cellData.OutgoingRiverDirection;
            var neighbor = cell.GetNeighbor(direction);
            if (neighbor != null)
            {
                cell.SetOutgoingRiver(direction);
            }
        }

        // Pass 3: Apply roads (after rivers, so we can check bridge validity)
        foreach (var cellData in _generatedData)
        {
            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell == null) continue;

            for (int dir = 0; dir < 6; dir++)
            {
                if (cellData.HasRoadInDirection(dir))
                {
                    cell.AddRoad((HexDirection)dir);
                }
            }
        }

        _grid.SetChunkRefreshSuppression(false);
        _grid.RefreshAllChunks();
    }

    #endregion

    #region Progress Reporting

    /// <summary>
    /// Reports progress from main thread (sync generation).
    /// </summary>
    private void ReportProgressMainThread(string stage, float progress)
    {
        GD.Print($"[MapGenerator] {stage} ({progress:P0})");
        GenerationProgress?.Invoke(stage, progress);
    }

    /// <summary>
    /// Queues progress update from worker thread.
    /// Will be dispatched on main thread via DispatchPendingProgress().
    /// </summary>
    private void QueueProgress(string stage, float progress)
    {
        lock (_progressLock)
        {
            _pendingProgress.Enqueue((stage, progress));
        }
    }

    /// <summary>
    /// Dispatches all pending progress updates.
    /// Must be called from main thread.
    /// </summary>
    private void DispatchPendingProgress()
    {
        lock (_progressLock)
        {
            while (_pendingProgress.Count > 0)
            {
                var (stage, progress) = _pendingProgress.Dequeue();
                GD.Print($"[MapGenerator] {stage} ({progress:P0})");
                GenerationProgress?.Invoke(stage, progress);
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Caches grid dimensions at start of generation.
    /// </summary>
    private void CacheGridDimensions()
    {
        if (_grid == null)
        {
            _gridWidth = 0;
            _gridHeight = 0;
            return;
        }

        _gridWidth = _grid.CellCountX;
        _gridHeight = _grid.CellCountZ;
    }

    #endregion
}
