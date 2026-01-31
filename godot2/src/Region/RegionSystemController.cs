using Godot;
using System;
using System.Threading.Tasks;
using HexGame.UI;

namespace HexGame.Region;

/// <summary>
/// High-level controller that integrates all Region System components.
/// Handles keybinds, coordinates UI, and manages region transitions.
///
/// Usage:
/// - Add as a child node in your main scene
/// - Call Initialize() with HexGrid reference
/// - Add RegionMapUI and RegionTravelUI as children (auto-created if missing)
///
/// Keybinds:
/// - M: Toggle world map
/// - Escape: Close world map
/// </summary>
public partial class RegionSystemController : Node
{
    #region Signals

    /// <summary>
    /// Emitted when the player completes travel to a new region.
    /// </summary>
    [Signal]
    public delegate void RegionChangedEventHandler(string regionId, string regionName);

    #endregion

    #region Fields

    private RegionManager? _regionManager;
    private RegionMap? _worldMap;
    private RegionMapSerializer _worldMapSerializer = new();
    private RegionMapUI? _mapUI;
    private RegionTravelUI? _travelUI;
    private Label? _errorLabel;
    private string _worldMapPath = "";

    #endregion

    #region Properties

    /// <summary>
    /// The current world map data.
    /// </summary>
    public RegionMap? WorldMap => _worldMap;

    /// <summary>
    /// Whether the world map UI is currently visible.
    /// </summary>
    public bool IsMapOpen => _mapUI?.Visible ?? false;

    /// <summary>
    /// Whether a travel transition is in progress.
    /// </summary>
    public bool IsTraveling => _travelUI?.Visible ?? false;

    #endregion

    public override void _Ready()
    {
        // Find or create RegionManager
        _regionManager = RegionManager.Instance ?? GetNodeOrNull<RegionManager>("RegionManager");
        if (_regionManager == null)
        {
            _regionManager = new RegionManager { Name = "RegionManager" };
            AddChild(_regionManager);
        }

        // Subscribe to RegionManager errors for visual display
        _regionManager.RegionError += OnRegionError;

        // Find or create UI components
        SetupUIComponents();

        GD.Print("[RegionSystemController] Ready");
    }

    private void OnRegionError(string error)
    {
        ShowError(error);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.M:
                    if (!IsTraveling)
                    {
                        ToggleMap();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes the controller with a HexGrid and optional world data.
    /// </summary>
    /// <param name="grid">The HexGrid to manage.</param>
    /// <param name="worldMapPath">Optional path to load/save world map data.</param>
    public void Initialize(HexGrid grid, string worldMapPath = "")
    {
        _regionManager?.Initialize(grid);
        _worldMapPath = worldMapPath;

        if (!string.IsNullOrEmpty(worldMapPath))
        {
            _ = LoadWorldMapAsync(worldMapPath);
        }
    }

    /// <summary>
    /// Creates a new world with an initial region.
    /// </summary>
    public async Task<bool> CreateNewWorldAsync(
        string worldName,
        string startingRegionName,
        int regionWidth = 0,
        int regionHeight = 0,
        int seed = 0)
    {
        if (_regionManager == null) return false;

        // Generate starting region
        var region = await _regionManager.GenerateNewRegionAsync(
            startingRegionName,
            regionWidth,
            regionHeight,
            seed
        );

        if (region == null)
        {
            GD.PrintErr("[RegionSystemController] Failed to generate starting region");
            return false;
        }

        // Create world map
        var startEntry = RegionMapEntry.FromRegionData(region);
        startEntry.PrimaryBiome = RegionBiome.Coastal;
        startEntry.Description = "Where your journey begins.";
        startEntry.DifficultyRating = 1;

        _worldMap = RegionMap.CreateNew(worldName, startEntry);

        // Apply region to grid
        var applied = await _regionManager.ApplyRegionAsync(region, saveCurrentFirst: false);
        if (!applied)
        {
            GD.PrintErr("[RegionSystemController] Failed to apply starting region");
            return false;
        }

        // Save region and world map
        await _regionManager.SaveRegionAsync(region, startEntry.FilePath);
        await SaveWorldMapAsync();

        GD.Print($"[RegionSystemController] Created new world '{worldName}' with starting region '{startingRegionName}'");
        return true;
    }

    /// <summary>
    /// Adds a new region to the world and optionally connects it to existing regions.
    /// </summary>
    public async Task<RegionMapEntry?> AddRegionToWorldAsync(
        string name,
        float mapX,
        float mapY,
        RegionBiome biome,
        int difficulty,
        int seed = 0,
        Guid? connectTo = null,
        float travelTime = 60f,
        int width = 0,
        int height = 0)
    {
        if (_regionManager == null || _worldMap == null) return null;

        // Use current region dimensions if not specified
        if (width <= 0 || height <= 0)
        {
            var currentRegion = _worldMap.GetCurrentRegion();
            if (currentRegion != null)
            {
                width = currentRegion.Width;
                height = currentRegion.Height;
            }
        }

        // Generate region with matching dimensions
        var region = await _regionManager.GenerateNewRegionAsync(name, width, height, seed);
        if (region == null) return null;

        // Create map entry
        var entry = RegionMapEntry.FromRegionData(region, mapX, mapY);
        entry.PrimaryBiome = biome;
        entry.DifficultyRating = difficulty;

        _worldMap.AddRegion(entry);

        // Connect to existing region if specified
        if (connectTo.HasValue)
        {
            _worldMap.ConnectRegions(connectTo.Value, entry.RegionId, travelTime);
        }

        // Save the region file
        await _regionManager.SaveRegionAsync(region, entry.FilePath);

        // Save updated world map
        await SaveWorldMapAsync();

        // Refresh map UI if it's open
        _mapUI?.RefreshIfVisible();

        GD.Print($"[RegionSystemController] Added region '{name}' to world");
        return entry;
    }

    /// <summary>
    /// Initiates travel to a connected region.
    /// </summary>
    public async Task<bool> TravelToRegionAsync(Guid targetRegionId)
    {
        if (_worldMap == null || _regionManager == null || _travelUI == null)
        {
            ShowError("Travel system not initialized");
            return false;
        }

        var fromEntry = _worldMap.GetCurrentRegion();
        var toEntry = _worldMap.GetRegionById(targetRegionId);

        if (fromEntry == null || toEntry == null)
        {
            ShowError("Invalid travel: missing region data");
            return false;
        }

        if (!_worldMap.CanTravelTo(fromEntry.RegionId, targetRegionId))
        {
            ShowError("Cannot travel: regions not connected");
            return false;
        }

        GD.Print($"[RegionSystemController] Starting travel from '{fromEntry.Name}' to '{toEntry.Name}'");
        GD.Print($"[RegionSystemController] Target file: {toEntry.FilePath}");
        GD.Print($"[RegionSystemController] Full path: {_regionManager.GetRegionPath(toEntry.FilePath)}");
        GD.Print($"[RegionSystemController] File exists: {_regionManager.RegionFileExists(toEntry.FilePath)}");

        // Close map if open
        _mapUI?.Hide();

        // Set up progress handler (store reference for cleanup)
        void OnProgress(string stage, float progress)
        {
            _travelUI?.UpdateProgress(stage, progress);
        }
        _regionManager.RegionProgress += OnProgress;

        bool loadSuccess = false;
        try
        {
            // Show travel UI and load region
            await _travelUI.ShowTravelAsync(fromEntry, toEntry, async () =>
            {
                try
                {
                    return await _regionManager.LoadAndApplyRegionAsync(toEntry.FilePath, saveCurrentFirst: true);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[RegionSystemController] Load failed: {ex.Message}");
                    return false;
                }
            });

            // Check if travel was cancelled
            if (_travelUI.Visible)
            {
                GD.Print("[RegionSystemController] Travel was cancelled");
                return false;
            }

            loadSuccess = true;
        }
        finally
        {
            // Always unsubscribe from progress events
            _regionManager.RegionProgress -= OnProgress;
        }

        if (!loadSuccess)
        {
            return false;
        }

        // Update world map state
        _worldMap.SetCurrentRegion(targetRegionId);
        await SaveWorldMapAsync();

        EmitSignal(SignalName.RegionChanged, targetRegionId.ToString(), toEntry.Name);

        GD.Print($"[RegionSystemController] Traveled to '{toEntry.Name}'");
        return true;
    }

    /// <summary>
    /// Toggles the world map visibility.
    /// </summary>
    public void ToggleMap()
    {
        GD.Print($"[RegionSystemController] ToggleMap called: _mapUI={_mapUI != null}, _worldMap={_worldMap != null}");

        if (_mapUI == null)
        {
            GD.PrintErr("[RegionSystemController] ToggleMap: _mapUI is null!");
            return;
        }

        if (_worldMap == null)
        {
            ShowError("No world loaded. Press N first.");
            return;
        }

        if (_mapUI.Visible)
        {
            _mapUI.Hide();
            GD.Print("[RegionSystemController] Map hidden");
        }
        else
        {
            _mapUI.ShowMap(_worldMap);
            GD.Print("[RegionSystemController] Map shown");
        }
    }

    /// <summary>
    /// Opens the world map.
    /// </summary>
    public void ShowMap()
    {
        if (_mapUI != null && _worldMap != null)
        {
            _mapUI.ShowMap(_worldMap);
        }
    }

    /// <summary>
    /// Closes the world map.
    /// </summary>
    public void HideMap()
    {
        _mapUI?.Hide();
    }

    /// <summary>
    /// Loads world map data from disk.
    /// </summary>
    public async Task<bool> LoadWorldMapAsync(string path)
    {
        _worldMapPath = path;
        var loaded = await _worldMapSerializer.LoadAsync(path);

        if (loaded != null)
        {
            _worldMap = loaded;
            GD.Print($"[RegionSystemController] Loaded world map '{loaded.WorldName}' with {loaded.Regions.Count} regions");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Saves world map data to disk.
    /// </summary>
    public async Task<bool> SaveWorldMapAsync()
    {
        if (_worldMap == null || string.IsNullOrEmpty(_worldMapPath))
        {
            return false;
        }

        return await _worldMapSerializer.SaveAsync(_worldMap, _worldMapPath);
    }

    /// <summary>
    /// Saves the current region (called automatically on travel, can be called manually).
    /// </summary>
    public async Task<bool> SaveCurrentRegionAsync()
    {
        if (_regionManager == null) return false;
        return await _regionManager.SaveCurrentRegionAsync();
    }

    private void SetupUIComponents()
    {
        // Find or create RegionMapUI
        _mapUI = GetNodeOrNull<RegionMapUI>("RegionMapUI");
        if (_mapUI == null)
        {
            _mapUI = new RegionMapUI { Name = "RegionMapUI" };
            AddChild(_mapUI);
        }
        _mapUI.TravelRequested += OnMapTravelRequested;
        _mapUI.MapClosed += OnMapClosed;

        // Find or create RegionTravelUI
        _travelUI = GetNodeOrNull<RegionTravelUI>("RegionTravelUI");
        if (_travelUI == null)
        {
            _travelUI = new RegionTravelUI { Name = "RegionTravelUI" };
            AddChild(_travelUI);
        }
        _travelUI.TravelCancelled += OnTravelCancelled;
        _travelUI.TravelCompleted += OnTravelCompleted;

        // Create error notification label
        _errorLabel = new Label
        {
            Name = "ErrorLabel",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _errorLabel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _errorLabel.AddThemeFontSizeOverride("font_size", 24);
        _errorLabel.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f));

        var errorPanel = new PanelContainer { Name = "ErrorPanel" };
        errorPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.8f),
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        errorPanel.AddThemeStyleboxOverride("panel", panelStyle);
        errorPanel.AddChild(_errorLabel);
        errorPanel.Visible = false;
        AddChild(errorPanel);
    }

    private async void ShowError(string message)
    {
        if (_errorLabel?.GetParent() is Control panel)
        {
            _errorLabel.Text = message;
            panel.Visible = true;
            await ToSignal(GetTree().CreateTimer(3.0f), Godot.Timer.SignalName.Timeout);
            panel.Visible = false;
        }
        GD.PrintErr($"[RegionSystem] {message}");
    }

    private void OnMapTravelRequested(string targetRegionIdStr)
    {
        if (Guid.TryParse(targetRegionIdStr, out var targetId))
        {
            _ = TravelToRegionAsync(targetId);
        }
    }

    private void OnMapClosed()
    {
        GD.Print("[RegionSystemController] Map closed");
    }

    private void OnTravelCancelled()
    {
        GD.Print("[RegionSystemController] Travel cancelled");
    }

    private void OnTravelCompleted()
    {
        GD.Print("[RegionSystemController] Travel completed");
    }
}
