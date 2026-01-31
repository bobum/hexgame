using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HexGame.Generation;

namespace HexGame.Region;

/// <summary>
/// Central coordinator for region operations.
/// Manages the lifecycle of region loading, saving, and transitions.
///
/// Responsibilities:
/// - Generate new regions (wraps MapGenerator)
/// - Save/load regions (uses RegionSerializer)
/// - Apply loaded regions to HexGrid
/// - Coordinate region transitions
///
/// Usage:
/// - Add as autoload singleton or child of main scene
/// - Call Initialize() with HexGrid reference
/// - Use GenerateNewRegionAsync() or LoadRegionAsync() to manage regions
/// </summary>
public partial class RegionManager : Node
{
    /// <summary>
    /// Singleton instance for easy access.
    /// </summary>
    public static RegionManager? Instance { get; private set; }

    #region Signals

    /// <summary>
    /// Emitted when a region starts loading.
    /// </summary>
    [Signal]
    public delegate void RegionLoadingEventHandler(string regionName);

    /// <summary>
    /// Emitted during region operations with progress (0.0-1.0).
    /// </summary>
    [Signal]
    public delegate void RegionProgressEventHandler(string stage, float progress);

    /// <summary>
    /// Emitted when a region finishes loading successfully.
    /// </summary>
    [Signal]
    public delegate void RegionLoadedEventHandler(string regionName);

    /// <summary>
    /// Emitted when a region is unloaded.
    /// </summary>
    [Signal]
    public delegate void RegionUnloadedEventHandler(string regionName);

    /// <summary>
    /// Emitted when a region operation fails.
    /// </summary>
    [Signal]
    public delegate void RegionErrorEventHandler(string error);

    #endregion

    #region Fields

    private HexGrid? _grid;
    private RegionSerializer _serializer = new();
    private RegionData? _currentRegion;
    private CancellationTokenSource? _operationCts;
    private bool _isOperating;
    private string _regionsBasePath = "user://regions";

    #endregion

    #region Properties

    /// <summary>
    /// The currently loaded region, or null if no region is loaded.
    /// </summary>
    public RegionData? CurrentRegion => _currentRegion;

    /// <summary>
    /// Whether a region operation is in progress.
    /// </summary>
    public bool IsOperating => _isOperating;

    /// <summary>
    /// Whether the manager has been initialized with a grid reference.
    /// </summary>
    public bool IsInitialized => _grid != null;

    /// <summary>
    /// Base path for region files. Defaults to "user://regions".
    /// </summary>
    public string RegionsBasePath
    {
        get => _regionsBasePath;
        set => _regionsBasePath = value;
    }

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Instance = this;

        // Subscribe to serializer events
        _serializer.SaveProgress += OnSerializerProgress;
        _serializer.LoadProgress += OnSerializerProgress;

        GD.Print("[RegionManager] Ready");
    }

    public override void _ExitTree()
    {
        _serializer.SaveProgress -= OnSerializerProgress;
        _serializer.LoadProgress -= OnSerializerProgress;

        _operationCts?.Cancel();
        _operationCts?.Dispose();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Initializes the manager with a HexGrid reference.
    /// Must be called before any region operations.
    /// </summary>
    public void Initialize(HexGrid grid)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        GD.Print("[RegionManager] Initialized with HexGrid");
    }

    #endregion

    #region Region Generation

    /// <summary>
    /// Generates a new region with procedural content.
    /// </summary>
    /// <param name="name">Display name for the region.</param>
    /// <param name="width">Width in cells (default: 200).</param>
    /// <param name="height">Height in cells (default: 200).</param>
    /// <param name="seed">Random seed (0 = random).</param>
    /// <returns>The generated RegionData, or null if generation failed.</returns>
    public async Task<RegionData?> GenerateNewRegionAsync(
        string name,
        int width = 0,
        int height = 0,
        int seed = 0)
    {
        if (_isOperating)
        {
            GD.PrintErr("[RegionManager] Operation already in progress");
            return null;
        }

        width = width > 0 ? width : RegionConfig.DefaultRegionWidth;
        height = height > 0 ? height : RegionConfig.DefaultRegionHeight;
        seed = seed != 0 ? seed : (int)GD.Randi();

        _isOperating = true;
        _operationCts = new CancellationTokenSource();

        try
        {
            EmitSignal(SignalName.RegionLoading, name);
            ReportProgress("Initializing", 0f);

            // Create empty region data
            var region = RegionData.CreateEmpty(name, width, height, seed);

            ReportProgress("Generating terrain", 0.1f);

            // Generate content using the generation pipeline
            var ct = _operationCts.Token;
            await Task.Run(() => GenerateRegionContent(region, ct), ct);

            ReportProgress("Complete", 1.0f);

            GD.Print($"[RegionManager] Generated region '{name}' ({width}x{height}) with seed {seed}");
            return region;
        }
        catch (OperationCanceledException)
        {
            GD.Print("[RegionManager] Generation cancelled");
            return null;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RegionManager] Generation failed: {ex.Message}");
            EmitSignal(SignalName.RegionError, ex.Message);
            return null;
        }
        finally
        {
            _isOperating = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    /// <summary>
    /// Generates terrain, rivers, roads, and features for a region.
    /// Runs on background thread.
    /// </summary>
    private void GenerateRegionContent(RegionData region, CancellationToken ct)
    {
        var rng = new Random(region.Seed);
        var width = region.Width;
        var height = region.Height;

        // Use existing generators
        ct.ThrowIfCancellationRequested();
        var landGenerator = new LandGenerator(rng, width, height);
        landGenerator.Generate(region.Cells, GenerationConfig.LandPercentage, ct);

        ct.ThrowIfCancellationRequested();
        var climateGenerator = new ClimateGenerator(width, height, region.Seed);
        climateGenerator.Generate(region.Cells, ct);

        ct.ThrowIfCancellationRequested();
        var riverGenerator = new RiverGenerator(rng, width, height);
        riverGenerator.Generate(region.Cells, ct);

        ct.ThrowIfCancellationRequested();
        var featureRng = new Random(region.Seed + GenerationConfig.FeatureSeedOffset);
        var featureGenerator = new FeatureGenerator(featureRng, width, height);
        featureGenerator.Generate(region.Cells, ct);

        ct.ThrowIfCancellationRequested();
        var roadRng = new Random(region.Seed + GenerationConfig.RoadSeedOffset);
        var roadGenerator = new RoadGenerator(roadRng, width, height);
        roadGenerator.Generate(region.Cells, ct);
    }

    #endregion

    #region Region Save/Load

    /// <summary>
    /// Saves a region to disk.
    /// </summary>
    /// <param name="region">The region to save.</param>
    /// <param name="filename">Optional filename (defaults to region ID).</param>
    /// <returns>True if save succeeded.</returns>
    public async Task<bool> SaveRegionAsync(RegionData region, string? filename = null)
    {
        if (_isOperating)
        {
            GD.PrintErr("[RegionManager] Operation already in progress");
            return false;
        }

        _isOperating = true;
        _operationCts = new CancellationTokenSource();

        try
        {
            filename ??= $"{region.RegionId}{RegionConfig.FileExtension}";
            var path = GetRegionPath(filename);

            // Ensure directory exists
            EnsureRegionDirectoryExists();

            var result = await _serializer.SaveAsync(region, path, _operationCts.Token);

            if (result)
            {
                GD.Print($"[RegionManager] Saved region '{region.Name}' to {path}");
            }

            return result;
        }
        finally
        {
            _isOperating = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    /// <summary>
    /// Saves the current region to disk.
    /// </summary>
    public async Task<bool> SaveCurrentRegionAsync()
    {
        if (_currentRegion == null)
        {
            GD.PrintErr("[RegionManager] No current region to save");
            return false;
        }

        return await SaveRegionAsync(_currentRegion);
    }

    /// <summary>
    /// Loads a region from disk.
    /// </summary>
    /// <param name="filename">The filename to load.</param>
    /// <returns>The loaded RegionData, or null if load failed.</returns>
    public async Task<RegionData?> LoadRegionFromFileAsync(string filename)
    {
        if (_isOperating)
        {
            GD.PrintErr("[RegionManager] Operation already in progress");
            return null;
        }

        _isOperating = true;
        _operationCts = new CancellationTokenSource();

        try
        {
            var path = GetRegionPath(filename);
            EmitSignal(SignalName.RegionLoading, filename);

            var region = await _serializer.LoadAsync(path, _operationCts.Token);

            if (region != null)
            {
                GD.Print($"[RegionManager] Loaded region '{region.Name}' from {path}");
            }

            return region;
        }
        finally
        {
            _isOperating = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    /// <summary>
    /// Loads region metadata only (fast, for Region Map display).
    /// </summary>
    public async Task<RegionMetadata?> LoadRegionMetadataAsync(string filename)
    {
        var path = GetRegionPath(filename);
        return await _serializer.LoadMetadataAsync(path);
    }

    #endregion

    #region Region Application

    /// <summary>
    /// Applies a region's cell data to the HexGrid.
    /// This is the main entry point for switching regions.
    /// </summary>
    /// <param name="region">The region to apply.</param>
    /// <param name="saveCurrentFirst">Whether to save the current region before switching.</param>
    /// <returns>True if the region was applied successfully.</returns>
    public async Task<bool> ApplyRegionAsync(RegionData region, bool saveCurrentFirst = true)
    {
        if (_grid == null)
        {
            GD.PrintErr("[RegionManager] Not initialized - call Initialize() first");
            EmitSignal(SignalName.RegionError, "RegionManager not initialized");
            return false;
        }

        if (_isOperating)
        {
            GD.PrintErr("[RegionManager] Operation already in progress");
            return false;
        }

        _isOperating = true;

        try
        {
            // Save current region if requested
            if (saveCurrentFirst && _currentRegion != null)
            {
                ReportProgress("Saving current region", 0f);
                await SaveCurrentRegionAsync();
                EmitSignal(SignalName.RegionUnloaded, _currentRegion.Name);
            }

            EmitSignal(SignalName.RegionLoading, region.Name);
            ReportProgress("Applying region", 0.2f);

            // Verify grid dimensions match (or resize if supported)
            if (_grid.CellCountX != region.Width || _grid.CellCountZ != region.Height)
            {
                GD.PrintErr($"[RegionManager] Grid size mismatch: grid is {_grid.CellCountX}x{_grid.CellCountZ}, region is {region.Width}x{region.Height}");
                EmitSignal(SignalName.RegionError, "Grid size mismatch");
                return false;
            }

            // Apply cell data to grid
            ApplyRegionToGrid(region);

            _currentRegion = region;

            ReportProgress("Complete", 1.0f);
            EmitSignal(SignalName.RegionLoaded, region.Name);

            GD.Print($"[RegionManager] Applied region '{region.Name}' to grid");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[RegionManager] Apply failed: {ex.Message}");
            EmitSignal(SignalName.RegionError, ex.Message);
            return false;
        }
        finally
        {
            _isOperating = false;
        }
    }

    /// <summary>
    /// Generates a new region and applies it to the grid in one operation.
    /// Convenience method for creating new game regions.
    /// </summary>
    public async Task<bool> GenerateAndApplyRegionAsync(
        string name,
        int width = 0,
        int height = 0,
        int seed = 0)
    {
        var region = await GenerateNewRegionAsync(name, width, height, seed);
        if (region == null)
        {
            return false;
        }

        return await ApplyRegionAsync(region, saveCurrentFirst: false);
    }

    /// <summary>
    /// Loads a region from file and applies it to the grid in one operation.
    /// </summary>
    public async Task<bool> LoadAndApplyRegionAsync(string filename, bool saveCurrentFirst = true)
    {
        var region = await LoadRegionFromFileAsync(filename);
        if (region == null)
        {
            return false;
        }

        return await ApplyRegionAsync(region, saveCurrentFirst);
    }

    /// <summary>
    /// Applies region cell data to the HexGrid.
    /// Uses three passes: properties, rivers, roads.
    /// </summary>
    private void ApplyRegionToGrid(RegionData region)
    {
        if (_grid == null) return;

        // Suppress chunk refreshes during bulk modifications
        _grid.SetChunkRefreshSuppression(true);

        try
        {
            // Pass 1: Apply all cell properties
            foreach (var cellData in region.Cells)
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

            // Pass 2: Apply rivers (after all elevations are set)
            int riversApplied = 0;
            foreach (var cellData in region.Cells)
            {
                if (!cellData.HasOutgoingRiver) continue;

                var cell = _grid.GetCellByOffset(cellData.X, cellData.Z);
                if (cell == null) continue;

                var direction = (HexDirection)cellData.OutgoingRiverDirection;
                var neighbor = cell.GetNeighbor(direction);
                if (neighbor != null)
                {
                    cell.SetOutgoingRiver(direction);
                    if (cell.HasOutgoingRiver) riversApplied++;
                }
            }

            // Pass 3: Apply roads (after rivers)
            int roadsApplied = 0;
            foreach (var cellData in region.Cells)
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

            GD.Print($"[RegionManager] Applied {region.Cells.Length} cells, {riversApplied} rivers, {roadsApplied} road segments");
        }
        finally
        {
            // Re-enable refreshes and trigger rebuild
            _grid.SetChunkRefreshSuppression(false);
            _grid.RefreshAllChunks();
        }
    }

    /// <summary>
    /// Extracts the current grid state into a RegionData object.
    /// Useful for saving player modifications.
    /// </summary>
    public RegionData? ExtractCurrentGridState(string name, int seed = 0)
    {
        if (_grid == null)
        {
            GD.PrintErr("[RegionManager] Not initialized");
            return null;
        }

        var region = new RegionData
        {
            Name = name,
            Width = _grid.CellCountX,
            Height = _grid.CellCountZ,
            Seed = seed,
            Cells = new CellData[_grid.CellCountX * _grid.CellCountZ]
        };

        int index = 0;
        foreach (var cell in _grid.GetAllCells())
        {
            region.Cells[index++] = new CellData(cell.Coordinates.X + cell.Coordinates.Z / 2, cell.Coordinates.Z)
            {
                Elevation = cell.Elevation,
                WaterLevel = cell.WaterLevel,
                TerrainTypeIndex = cell.TerrainTypeIndex,
                UrbanLevel = cell.UrbanLevel,
                FarmLevel = cell.FarmLevel,
                PlantLevel = cell.PlantLevel,
                SpecialIndex = cell.SpecialIndex,
                Walled = cell.Walled,
                HasIncomingRiver = cell.HasIncomingRiver,
                HasOutgoingRiver = cell.HasOutgoingRiver,
                IncomingRiverDirection = (int)cell.IncomingRiver,
                OutgoingRiverDirection = (int)cell.OutgoingRiver,
                Moisture = 0f, // Not stored in HexCell
                HasRoadNE = cell.HasRoadThroughEdge(HexDirection.NE),
                HasRoadE = cell.HasRoadThroughEdge(HexDirection.E),
                HasRoadSE = cell.HasRoadThroughEdge(HexDirection.SE),
                HasRoadSW = cell.HasRoadThroughEdge(HexDirection.SW),
                HasRoadW = cell.HasRoadThroughEdge(HexDirection.W),
                HasRoadNW = cell.HasRoadThroughEdge(HexDirection.NW)
            };
        }

        return region;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Cancels any ongoing region operation.
    /// </summary>
    public void CancelOperation()
    {
        _operationCts?.Cancel();
    }

    /// <summary>
    /// Gets the full path for a region file.
    /// </summary>
    public string GetRegionPath(string filename)
    {
        if (Path.IsPathRooted(filename) || filename.StartsWith("user://") || filename.StartsWith("res://"))
        {
            return filename;
        }

        return $"{_regionsBasePath}/{filename}";
    }

    /// <summary>
    /// Checks if a region file exists.
    /// </summary>
    public bool RegionFileExists(string filename)
    {
        var path = GetRegionPath(filename);

        if (path.StartsWith("user://") || path.StartsWith("res://"))
        {
            return Godot.FileAccess.FileExists(path);
        }

        return File.Exists(path);
    }

    /// <summary>
    /// Gets a list of all region files in the regions directory.
    /// </summary>
    public string[] GetAvailableRegions()
    {
        var regions = new System.Collections.Generic.List<string>();

        if (_regionsBasePath.StartsWith("user://"))
        {
            using var dir = DirAccess.Open(_regionsBasePath);
            if (dir != null)
            {
                dir.ListDirBegin();
                var filename = dir.GetNext();
                while (!string.IsNullOrEmpty(filename))
                {
                    if (filename.EndsWith(RegionConfig.FileExtension))
                    {
                        regions.Add(filename);
                    }
                    filename = dir.GetNext();
                }
            }
        }
        else if (Directory.Exists(_regionsBasePath))
        {
            foreach (var file in Directory.GetFiles(_regionsBasePath, $"*{RegionConfig.FileExtension}"))
            {
                regions.Add(Path.GetFileName(file));
            }
        }

        return regions.ToArray();
    }

    private void EnsureRegionDirectoryExists()
    {
        if (_regionsBasePath.StartsWith("user://"))
        {
            using var dir = DirAccess.Open("user://");
            dir?.MakeDirRecursive(_regionsBasePath.Replace("user://", ""));
        }
        else
        {
            Directory.CreateDirectory(_regionsBasePath);
        }
    }

    private void ReportProgress(string stage, float progress)
    {
        EmitSignal(SignalName.RegionProgress, stage, progress);
    }

    private void OnSerializerProgress(string stage, float progress)
    {
        ReportProgress(stage, progress);
    }

    #endregion
}
