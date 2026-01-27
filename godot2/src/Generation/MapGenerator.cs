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
    /// Directly modifies HexCell instances.
    /// </summary>
    private void RunGenerationPipelineSync()
    {
        ReportProgressMainThread("Resetting cells", 0f);
        ResetCellsSync();

        ReportProgressMainThread("Generating land", 0.1f);
        GenerateLandSync();

        ReportProgressMainThread("Generating moisture", 0.4f);
        GenerateMoistureSync();

        ReportProgressMainThread("Assigning biomes", 0.5f);
        AssignBiomesSync();

        ReportProgressMainThread("Generating rivers", 0.6f);
        GenerateRiversSync();

        ReportProgressMainThread("Placing features", 0.8f);
        PlaceFeaturesSync();

        ReportProgressMainThread("Finalizing", 0.95f);
    }

    private void ResetCellsSync()
    {
        if (_grid == null) return;

        foreach (var cell in _grid.GetAllCells())
        {
            cell.WaterLevel = GenerationConfig.WaterLevel;
            cell.Elevation = GenerationConfig.MinElevation;
            cell.TerrainTypeIndex = 0;
            cell.RemoveRiver();
            cell.RemoveRoads();
            cell.UrbanLevel = 0;
            cell.FarmLevel = 0;
            cell.PlantLevel = 0;
            cell.SpecialIndex = 0;
            cell.Walled = false;
        }
    }

    private void GenerateLandSync()
    {
        if (_grid == null) return;

        int totalCells = _gridWidth * _gridHeight;
        GD.Print($"[MapGenerator] Starting land generation for {totalCells} cells");

        // Create intermediate data array
        var data = new CellData[totalCells];
        for (int z = 0; z < _gridHeight; z++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                int index = z * _gridWidth + x;
                data[index] = new CellData(x, z);
            }
        }

        // Use LandGenerator for chunk-based land creation
        var landGenerator = new LandGenerator(_rng, _gridWidth, _gridHeight);
        landGenerator.Generate(data, GenerationConfig.LandPercentage);

        // Apply results to grid
        int landCells = 0;
        foreach (var cellData in data)
        {
            var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
            if (cell != null)
            {
                cell.Elevation = cellData.Elevation;
                if (cellData.Elevation >= GenerationConfig.WaterLevel)
                    landCells++;
            }
        }

        GD.Print($"[MapGenerator] Land generation complete: {landCells} land cells ({100f * landCells / totalCells:F1}%)");
    }

    private void GenerateMoistureSync()
    {
        // TODO: Implement in Sprint 3
        GD.Print("[MapGenerator] Moisture generation (stub)");
    }

    private void AssignBiomesSync()
    {
        // TODO: Implement in Sprint 3
        GD.Print("[MapGenerator] Biome assignment (stub)");
    }

    private void GenerateRiversSync()
    {
        // TODO: Implement in Sprint 4
        GD.Print("[MapGenerator] River generation (stub)");
    }

    private void PlaceFeaturesSync()
    {
        // TODO: Implement in Sprint 5
        GD.Print("[MapGenerator] Feature placement (stub)");
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

        // Generate moisture
        QueueProgress("Generating moisture", 0.4f);
        GenerateMoistureAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        // Assign biomes
        QueueProgress("Assigning biomes", 0.5f);
        AssignBiomesAsync(data, ct);

        ct.ThrowIfCancellationRequested();

        QueueProgress("Finalizing", 0.9f);

        return data;
    }

    private void GenerateLandAsync(CellData[] data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Use LandGenerator for chunk-based land creation
        var landGenerator = new LandGenerator(_rng, _gridWidth, _gridHeight);
        landGenerator.Generate(data, GenerationConfig.LandPercentage);
    }

    private void GenerateMoistureAsync(CellData[] data, CancellationToken ct)
    {
        // TODO: Implement in Sprint 3
    }

    private void AssignBiomesAsync(CellData[] data, CancellationToken ct)
    {
        // TODO: Implement in Sprint 3
    }

    /// <summary>
    /// Applies generated data to the HexGrid.
    /// Must be called from main thread only.
    /// </summary>
    private void ApplyGeneratedDataToGrid()
    {
        if (_grid == null || _generatedData == null) return;

        _grid.SetChunkRefreshSuppression(true);

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
            // Rivers and roads would be applied here in later sprints
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
