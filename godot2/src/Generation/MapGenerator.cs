using System;
using System.Threading;
using Godot;

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
/// </summary>
public class MapGenerator : IMapGenerator
{
    private HexGrid? _grid;
    private Random _rng = new();
    private int _currentSeed;

    // Async generation support
    private Thread? _workerThread;
    private volatile bool _isGenerating;
    private volatile bool _asyncComplete;

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

        GD.Print($"[MapGenerator] Starting synchronous generation with seed {_currentSeed}");
        _isGenerating = true;

        GenerationStarted?.Invoke();

        try
        {
            RunGenerationPipeline();
            GenerationCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapGenerator] Generation failed: {ex.Message}");
            GenerationCompleted?.Invoke(false);
        }
        finally
        {
            _isGenerating = false;
        }
    }

    /// <summary>
    /// Starts asynchronous map generation.
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

        GD.Print($"[MapGenerator] Starting async generation with seed {_currentSeed}");
        _isGenerating = true;
        _asyncComplete = false;

        GenerationStarted?.Invoke();

        _workerThread = new Thread(AsyncWorker);
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
    /// Applies the results of async generation.
    /// Must be called from main thread after IsGenerationComplete() returns true.
    /// </summary>
    public void FinishAsyncGeneration()
    {
        if (_workerThread != null)
        {
            _workerThread.Join();
            _workerThread = null;
        }

        // Apply any pending changes that require main thread
        // (e.g., chunk refresh, node operations)
        FinalizeGeneration();

        _isGenerating = false;
        GenerationCompleted?.Invoke(true);
    }

    /// <summary>
    /// Cancels any ongoing async generation.
    /// </summary>
    public void CancelGeneration()
    {
        if (_workerThread != null && _workerThread.IsAlive)
        {
            // Note: Can't actually cancel thread, but we'll wait and discard
            _workerThread.Join();
        }
        _workerThread = null;
        _isGenerating = false;
        _asyncComplete = false;
    }

    #endregion

    #region Generation Pipeline

    /// <summary>
    /// Async worker thread entry point.
    /// </summary>
    private void AsyncWorker()
    {
        try
        {
            RunGenerationPipeline();
            _asyncComplete = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapGenerator] Async generation failed: {ex.Message}");
            _asyncComplete = true; // Mark complete so FinishAsync can clean up
        }
    }

    /// <summary>
    /// Runs the full generation pipeline.
    /// </summary>
    private void RunGenerationPipeline()
    {
        ReportProgress("Resetting cells", 0f);
        ResetCells();

        ReportProgress("Generating land", 0.1f);
        GenerateLand();

        ReportProgress("Generating moisture", 0.4f);
        GenerateMoisture();

        ReportProgress("Assigning biomes", 0.5f);
        AssignBiomes();

        ReportProgress("Generating rivers", 0.6f);
        GenerateRivers();

        ReportProgress("Placing features", 0.8f);
        PlaceFeatures();

        ReportProgress("Complete", 1.0f);
    }

    /// <summary>
    /// Reports progress to listeners.
    /// </summary>
    private void ReportProgress(string stage, float progress)
    {
        GD.Print($"[MapGenerator] {stage} ({progress:P0})");
        GenerationProgress?.Invoke(stage, progress);
    }

    /// <summary>
    /// Finalizes generation after async completion.
    /// Called on main thread.
    /// </summary>
    private void FinalizeGeneration()
    {
        GD.Print("[MapGenerator] Finalizing generation");
        // Any main-thread operations go here
        // (Chunk refresh is handled by the caller)
    }

    #endregion

    #region Stage 1: Reset Cells

    /// <summary>
    /// Resets all cells to default underwater state.
    /// </summary>
    private void ResetCells()
    {
        if (_grid == null) return;

        // Get grid dimensions via reflection or counting
        // For now, iterate until we hit null
        int x = 0, z = 0;
        while (true)
        {
            var cell = _grid.GetCellByOffset(x, z);
            if (cell == null)
            {
                if (x == 0) break; // End of grid
                x = 0;
                z++;
                continue;
            }

            // Reset to underwater state
            cell.WaterLevel = GenerationConfig.WaterLevel;
            cell.Elevation = GenerationConfig.MinElevation;
            cell.TerrainTypeIndex = 0; // Will be set by biome assignment
            cell.RemoveRiver();
            cell.RemoveRoads();
            cell.UrbanLevel = 0;
            cell.FarmLevel = 0;
            cell.PlantLevel = 0;
            cell.SpecialIndex = 0;
            cell.Walled = false;

            x++;
        }

        GD.Print("[MapGenerator] Cells reset to underwater state");
    }

    #endregion

    #region Stage 2: Generate Land

    /// <summary>
    /// Generates land masses using chunk budget system.
    /// Catlike-style approach: randomly raise chunks of connected cells.
    /// </summary>
    private void GenerateLand()
    {
        if (_grid == null) return;

        // Count total cells to calculate land budget
        int totalCells = CountCells();
        int landBudget = (int)(totalCells * GenerationConfig.LandPercentage);

        GD.Print($"[MapGenerator] Land budget: {landBudget} of {totalCells} cells");

        int iterations = 0;
        while (landBudget > 0 && iterations < GenerationConfig.MaxChunkIterations)
        {
            iterations++;

            // Pick random starting cell
            HexCell? startCell = GetRandomCell();
            if (startCell == null) continue;

            // Grow a land chunk from this cell
            int chunkSize = _rng.Next(GenerationConfig.MinChunkSize, GenerationConfig.MaxChunkSize + 1);
            int raised = RaiseLandChunk(startCell, chunkSize);
            landBudget -= raised;
        }

        GD.Print($"[MapGenerator] Land generation complete after {iterations} iterations");
    }

    /// <summary>
    /// Raises a chunk of connected cells above water level.
    /// Returns the number of cells raised.
    /// </summary>
    private int RaiseLandChunk(HexCell center, int budget)
    {
        // TODO: Implement in Sprint 2
        // For now, just raise the center cell
        if (center.Elevation < GenerationConfig.WaterLevel)
        {
            center.Elevation = GenerationConfig.WaterLevel;
            return 1;
        }
        return 0;
    }

    #endregion

    #region Stage 3: Generate Moisture

    /// <summary>
    /// Generates moisture values using noise.
    /// </summary>
    private void GenerateMoisture()
    {
        // TODO: Implement in Sprint 3
        GD.Print("[MapGenerator] Moisture generation (stub)");
    }

    #endregion

    #region Stage 4: Assign Biomes

    /// <summary>
    /// Assigns terrain types based on elevation and moisture.
    /// </summary>
    private void AssignBiomes()
    {
        // TODO: Implement in Sprint 3
        GD.Print("[MapGenerator] Biome assignment (stub)");
    }

    #endregion

    #region Stage 5: Generate Rivers

    /// <summary>
    /// Generates rivers flowing from high to low elevation.
    /// </summary>
    private void GenerateRivers()
    {
        // TODO: Implement in Sprint 4
        GD.Print("[MapGenerator] River generation (stub)");
    }

    #endregion

    #region Stage 6: Place Features

    /// <summary>
    /// Places features (vegetation, structures) based on biome.
    /// </summary>
    private void PlaceFeatures()
    {
        // TODO: Implement in Sprint 5
        GD.Print("[MapGenerator] Feature placement (stub)");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Counts the total number of cells in the grid.
    /// </summary>
    private int CountCells()
    {
        if (_grid == null) return 0;

        int count = 0;
        int x = 0, z = 0;
        while (true)
        {
            var cell = _grid.GetCellByOffset(x, z);
            if (cell == null)
            {
                if (x == 0) break;
                x = 0;
                z++;
                continue;
            }
            count++;
            x++;
        }
        return count;
    }

    /// <summary>
    /// Gets the grid dimensions.
    /// </summary>
    private (int width, int height) GetGridDimensions()
    {
        if (_grid == null) return (0, 0);

        int width = 0, height = 0;

        // Find width (scan first row)
        while (_grid.GetCellByOffset(width, 0) != null)
        {
            width++;
        }

        // Find height (scan first column)
        while (_grid.GetCellByOffset(0, height) != null)
        {
            height++;
        }

        return (width, height);
    }

    /// <summary>
    /// Gets a random cell from the grid.
    /// </summary>
    private HexCell? GetRandomCell()
    {
        var (width, height) = GetGridDimensions();
        if (width == 0 || height == 0) return null;

        int x = _rng.Next(width);
        int z = _rng.Next(height);
        return _grid?.GetCellByOffset(x, z);
    }

    #endregion
}
