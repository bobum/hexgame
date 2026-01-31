using Godot;
using System;
using System.Collections.Generic;
using HexGame.Region;

namespace HexGame.UI;

/// <summary>
/// Strategic world map UI showing all discovered regions and travel options.
/// Displays as a fullscreen overlay with a stylized ocean/navigation chart aesthetic.
/// </summary>
public partial class RegionMapUI : Control
{
    #region Signals

    /// <summary>
    /// Emitted when the player requests travel to a region.
    /// </summary>
    [Signal]
    public delegate void TravelRequestedEventHandler(string targetRegionId);

    /// <summary>
    /// Emitted when the map is closed.
    /// </summary>
    [Signal]
    public delegate void MapClosedEventHandler();

    #endregion

    #region Configuration

    private const float IconSize = 48f;
    private const float ConnectionLineWidth = 2f;
    private const float MapPadding = 50f;
    private const float InfoPanelWidth = 280f;

    // Colors
    private static readonly Color OceanColor = new(0.1f, 0.2f, 0.4f);
    private static readonly Color DiscoveredRegionColor = new(0.8f, 0.7f, 0.5f);
    private static readonly Color UndiscoveredRegionColor = new(0.4f, 0.4f, 0.4f);
    private static readonly Color CurrentRegionColor = new(0.2f, 0.8f, 0.3f);
    private static readonly Color SelectedRegionColor = new(1.0f, 0.9f, 0.3f);
    private static readonly Color ConnectionColor = new(0.5f, 0.5f, 0.5f, 0.5f);
    private static readonly Color TravelableConnectionColor = new(0.3f, 0.7f, 1.0f, 0.8f);

    #endregion

    #region UI Elements

    private Panel? _background;
    private Control? _mapArea;
    private PanelContainer? _infoPanel;
    private Label? _titleLabel;
    private Label? _selectedNameLabel;
    private Label? _selectedBiomeLabel;
    private Label? _selectedDifficultyLabel;
    private Label? _selectedDescLabel;
    private Label? _travelTimeLabel;
    private Button? _travelButton;
    private Button? _closeButton;

    // Region icons on the map
    private readonly Dictionary<Guid, RegionIconNode> _regionIcons = new();

    #endregion

    #region State

    private RegionMap? _regionMap;
    private Guid _selectedRegionId;
    private Guid _currentRegionId;

    #endregion

    public override void _Ready()
    {
        Visible = false;
        BuildUI();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        // Close on Escape or M
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape || keyEvent.Keycode == Key.M)
            {
                Hide();
                EmitSignal(SignalName.MapClosed);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Shows the region map with the specified world data.
    /// </summary>
    public void ShowMap(RegionMap regionMap)
    {
        _regionMap = regionMap;
        _currentRegionId = regionMap.CurrentRegionId;
        _selectedRegionId = _currentRegionId;

        RefreshMapDisplay();
        UpdateInfoPanel();
        Show();
    }

    /// <summary>
    /// Updates the map if it's currently visible.
    /// </summary>
    public void RefreshIfVisible()
    {
        if (Visible && _regionMap != null)
        {
            RefreshMapDisplay();
        }
    }

    private void BuildUI()
    {
        // Full screen overlay
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Ocean background
        _background = new Panel();
        _background.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat
        {
            BgColor = OceanColor
        };
        _background.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(_background);

        // Title
        _titleLabel = new Label
        {
            Text = "WORLD MAP",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, 20),
            Size = new Vector2(GetViewportRect().Size.X, 40)
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        AddChild(_titleLabel);

        // Map area (for drawing connections and placing icons)
        _mapArea = new Control();
        _mapArea.SetAnchorsPreset(LayoutPreset.FullRect);
        _mapArea.OffsetLeft = MapPadding;
        _mapArea.OffsetTop = 70;
        _mapArea.OffsetRight = -InfoPanelWidth - MapPadding;
        _mapArea.OffsetBottom = -MapPadding;
        _mapArea.Draw += OnMapAreaDraw;
        AddChild(_mapArea);

        // Info panel on the right
        BuildInfoPanel();

        // Close button
        _closeButton = new Button
        {
            Text = "X",
            Position = new Vector2(-50, 15),
            Size = new Vector2(35, 35)
        };
        _closeButton.AnchorLeft = 1;
        _closeButton.AnchorRight = 1;
        _closeButton.Pressed += OnClosePressed;
        AddChild(_closeButton);
    }

    private void BuildInfoPanel()
    {
        _infoPanel = new PanelContainer();
        _infoPanel.AnchorLeft = 1;
        _infoPanel.AnchorRight = 1;
        _infoPanel.AnchorTop = 0;
        _infoPanel.AnchorBottom = 1;
        _infoPanel.OffsetLeft = -InfoPanelWidth - 20;
        _infoPanel.OffsetTop = 70;
        _infoPanel.OffsetRight = -20;
        _infoPanel.OffsetBottom = -20;
        AddChild(_infoPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        _infoPanel.AddChild(vbox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 15);
        margin.AddThemeConstantOverride("margin_right", 15);
        margin.AddThemeConstantOverride("margin_top", 15);
        margin.AddThemeConstantOverride("margin_bottom", 15);
        margin.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(margin);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        margin.AddChild(content);

        // Selected region header
        var headerLabel = new Label { Text = "SELECTED REGION" };
        headerLabel.AddThemeFontSizeOverride("font_size", 12);
        headerLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        content.AddChild(headerLabel);

        _selectedNameLabel = new Label { Text = "None" };
        _selectedNameLabel.AddThemeFontSizeOverride("font_size", 18);
        content.AddChild(_selectedNameLabel);

        content.AddChild(new HSeparator());

        // Details
        _selectedBiomeLabel = new Label { Text = "Biome: Unknown" };
        content.AddChild(_selectedBiomeLabel);

        _selectedDifficultyLabel = new Label { Text = "Difficulty: -" };
        content.AddChild(_selectedDifficultyLabel);

        _selectedDescLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _selectedDescLabel.CustomMinimumSize = new Vector2(0, 60);
        content.AddChild(_selectedDescLabel);

        content.AddChild(new HSeparator());

        // Travel info
        _travelTimeLabel = new Label { Text = "Travel Time: --" };
        content.AddChild(_travelTimeLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.AddChild(spacer);

        // Travel button
        _travelButton = new Button
        {
            Text = "SET SAIL",
            CustomMinimumSize = new Vector2(0, 40),
            Disabled = true
        };
        _travelButton.Pressed += OnTravelPressed;
        content.AddChild(_travelButton);

        // Legend
        content.AddChild(new HSeparator());
        var legendLabel = new Label { Text = "Legend:" };
        legendLabel.AddThemeFontSizeOverride("font_size", 11);
        content.AddChild(legendLabel);

        AddLegendItem(content, CurrentRegionColor, "Current Location");
        AddLegendItem(content, DiscoveredRegionColor, "Discovered");
        AddLegendItem(content, UndiscoveredRegionColor, "Undiscovered");
    }

    private void AddLegendItem(VBoxContainer parent, Color color, string text)
    {
        var hbox = new HBoxContainer();
        parent.AddChild(hbox);

        var colorRect = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(16, 16)
        };
        hbox.AddChild(colorRect);

        var label = new Label { Text = "  " + text };
        label.AddThemeFontSizeOverride("font_size", 11);
        hbox.AddChild(label);
    }

    private void RefreshMapDisplay()
    {
        if (_regionMap == null || _mapArea == null) return;

        // Clear existing icons
        foreach (var icon in _regionIcons.Values)
        {
            icon.QueueFree();
        }
        _regionIcons.Clear();

        // Calculate bounds for discovered regions
        var mapAreaSize = _mapArea.Size;
        if (mapAreaSize.X <= 0 || mapAreaSize.Y <= 0)
        {
            // Defer until we have valid size
            CallDeferred(MethodName.RefreshMapDisplay);
            return;
        }

        // Create icons for each region
        foreach (var entry in _regionMap.Regions)
        {
            var icon = CreateRegionIcon(entry, mapAreaSize);
            _mapArea.AddChild(icon);
            _regionIcons[entry.RegionId] = icon;
        }

        // Trigger connection redraw
        _mapArea.QueueRedraw();
    }

    private RegionIconNode CreateRegionIcon(RegionMapEntry entry, Vector2 mapSize)
    {
        var icon = new RegionIconNode();
        icon.RegionId = entry.RegionId;
        icon.RegionName = entry.Name;
        icon.IsDiscovered = entry.IsDiscovered;
        icon.IsCurrent = entry.RegionId == _currentRegionId;
        icon.IsSelected = entry.RegionId == _selectedRegionId;
        icon.Biome = entry.PrimaryBiome;

        // Position based on map coordinates (with some scaling)
        // For now, use a simple grid layout if map positions aren't set
        float x = entry.MapX;
        float y = entry.MapY;

        // If no position, auto-layout
        if (x == 0 && y == 0)
        {
            var index = _regionMap!.Regions.IndexOf(entry);
            var cols = Math.Max(1, (int)Math.Sqrt(_regionMap.Regions.Count));
            x = (index % cols) * 150f + 100f;
            y = (index / cols) * 150f + 100f;
        }

        // Scale to fit map area
        icon.Position = new Vector2(
            Mathf.Clamp(x, IconSize, mapSize.X - IconSize),
            Mathf.Clamp(y, IconSize, mapSize.Y - IconSize)
        );

        icon.IconClicked += OnRegionIconClicked;

        return icon;
    }

    private void OnMapAreaDraw()
    {
        if (_regionMap == null || _mapArea == null) return;

        // Draw connections between regions
        foreach (var entry in _regionMap.Regions)
        {
            if (!_regionIcons.TryGetValue(entry.RegionId, out var fromIcon))
                continue;

            foreach (var targetId in entry.ConnectedRegionIds)
            {
                if (!_regionIcons.TryGetValue(targetId, out var toIcon))
                    continue;

                // Only draw each connection once (when from < to by guid)
                if (entry.RegionId.CompareTo(targetId) > 0)
                    continue;

                var color = ConnectionColor;

                // Highlight if this is a travelable path from current region
                if (entry.RegionId == _currentRegionId || targetId == _currentRegionId)
                {
                    color = TravelableConnectionColor;
                }

                _mapArea.DrawLine(
                    fromIcon.Position + new Vector2(IconSize / 2, IconSize / 2),
                    toIcon.Position + new Vector2(IconSize / 2, IconSize / 2),
                    color,
                    ConnectionLineWidth
                );
            }
        }
    }

    private void OnRegionIconClicked(string regionIdStr)
    {
        if (Guid.TryParse(regionIdStr, out var regionId))
        {
            SelectRegion(regionId);
        }
    }

    private void SelectRegion(Guid regionId)
    {
        _selectedRegionId = regionId;

        // Update icon visuals
        foreach (var (id, icon) in _regionIcons)
        {
            icon.IsSelected = id == _selectedRegionId;
            icon.QueueRedraw();
        }

        UpdateInfoPanel();
        _mapArea?.QueueRedraw();
    }

    private void UpdateInfoPanel()
    {
        if (_regionMap == null) return;

        var selected = _regionMap.GetRegionById(_selectedRegionId);

        if (selected == null)
        {
            _selectedNameLabel!.Text = "None";
            _selectedBiomeLabel!.Text = "Biome: -";
            _selectedDifficultyLabel!.Text = "Difficulty: -";
            _selectedDescLabel!.Text = "";
            _travelTimeLabel!.Text = "Travel Time: --";
            _travelButton!.Disabled = true;
            return;
        }

        _selectedNameLabel!.Text = selected.IsDiscovered ? selected.Name : "???";
        _selectedBiomeLabel!.Text = selected.IsDiscovered
            ? $"Biome: {selected.PrimaryBiome}"
            : "Biome: Unknown";
        _selectedDifficultyLabel!.Text = selected.IsDiscovered
            ? $"Difficulty: {"*".PadRight(selected.DifficultyRating, '*')}"
            : "Difficulty: ???";
        _selectedDescLabel!.Text = selected.IsDiscovered ? selected.Description : "";

        // Check if we can travel there
        bool canTravel = _selectedRegionId != _currentRegionId
            && _regionMap.CanTravelTo(_currentRegionId, _selectedRegionId);

        if (canTravel)
        {
            var connInfo = _regionMap.GetConnectionInfo(_currentRegionId, _selectedRegionId);
            if (connInfo != null)
            {
                int hours = (int)(connInfo.TravelTimeMinutes / 60);
                int mins = (int)(connInfo.TravelTimeMinutes % 60);
                _travelTimeLabel!.Text = hours > 0
                    ? $"Travel Time: ~{hours}h {mins}m"
                    : $"Travel Time: ~{mins}m";

                if (connInfo.DangerLevel > 0.5f)
                {
                    _travelTimeLabel.Text += " (Dangerous!)";
                }
            }
            else
            {
                _travelTimeLabel!.Text = "Travel Time: Unknown";
            }
        }
        else if (_selectedRegionId == _currentRegionId)
        {
            _travelTimeLabel!.Text = "You are here";
        }
        else
        {
            _travelTimeLabel!.Text = "No direct route";
        }

        _travelButton!.Disabled = !canTravel;
        _travelButton.Text = _selectedRegionId == _currentRegionId ? "CURRENT LOCATION" : "SET SAIL";
    }

    private void OnTravelPressed()
    {
        if (_selectedRegionId == Guid.Empty || _selectedRegionId == _currentRegionId)
            return;

        EmitSignal(SignalName.TravelRequested, _selectedRegionId.ToString());
        Hide();
    }

    private void OnClosePressed()
    {
        Hide();
        EmitSignal(SignalName.MapClosed);
    }
}

/// <summary>
/// Visual representation of a region on the world map.
/// </summary>
public partial class RegionIconNode : Control
{
    [Signal]
    public delegate void IconClickedEventHandler(string regionId);

    public Guid RegionId { get; set; }
    public string RegionName { get; set; } = "";
    public bool IsDiscovered { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsSelected { get; set; }
    public RegionBiome Biome { get; set; }

    private const float IconNodeSize = 48f;
    private const float Padding = 4f;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(IconNodeSize, IconNodeSize);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Draw()
    {
        var center = new Vector2(IconNodeSize / 2, IconNodeSize / 2);
        var radius = IconNodeSize / 2 - Padding;

        // Background circle
        Color bgColor;
        if (IsCurrent)
            bgColor = new Color(0.2f, 0.8f, 0.3f);
        else if (!IsDiscovered)
            bgColor = new Color(0.4f, 0.4f, 0.4f);
        else
            bgColor = GetBiomeColor();

        DrawCircle(center, radius, bgColor);

        // Selection ring
        if (IsSelected)
        {
            DrawArc(center, radius + 3, 0, Mathf.Tau, 32, new Color(1f, 0.9f, 0.3f), 3f);
        }

        // Current location marker
        if (IsCurrent)
        {
            DrawCircle(center, 6, Colors.White);
        }

        // Question mark for undiscovered
        if (!IsDiscovered)
        {
            // Draw "?" in center (simplified)
            DrawCircle(center + new Vector2(0, -4), 3, Colors.White);
            DrawCircle(center + new Vector2(0, 6), 2, Colors.White);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.IconClicked, RegionId.ToString());
            AcceptEvent();
        }
    }

    private Color GetBiomeColor()
    {
        return Biome switch
        {
            RegionBiome.Tropical => new Color(0.3f, 0.7f, 0.3f),
            RegionBiome.Arctic => new Color(0.8f, 0.9f, 1.0f),
            RegionBiome.Desert => new Color(0.9f, 0.8f, 0.5f),
            RegionBiome.Volcanic => new Color(0.6f, 0.2f, 0.1f),
            RegionBiome.Coastal => new Color(0.5f, 0.7f, 0.9f),
            RegionBiome.Swamp => new Color(0.3f, 0.4f, 0.2f),
            _ => new Color(0.6f, 0.7f, 0.5f) // Temperate
        };
    }
}
