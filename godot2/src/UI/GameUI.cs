using Godot;
using HexGame.Rendering;
using System.Collections.Generic;

namespace HexGame.UI;

/// <summary>
/// Main game UI panel with map controls and parameter adjustment.
/// Positioned on the right side of the screen with collapsible sections.
/// </summary>
public partial class GameUI : Control
{
    #region Signals

    [Signal]
    public delegate void RegenerateRequestedEventHandler(int width, int height, int seedVal);

    [Signal]
    public delegate void RandomSeedRequestedEventHandler();

    [Signal]
    public delegate void NoiseParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void FogParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void AsyncToggleChangedEventHandler(bool enabled);

    #endregion

    #region UI Elements

    private PanelContainer? _panel;
    private ScrollContainer? _scroll;
    private VBoxContainer? _mainVbox;

    // Input controls
    private SpinBox? _mapWidthSpin;
    private SpinBox? _mapHeightSpin;
    private SpinBox? _seedSpin;

    // Labels
    private Label? _fpsLabel;
    private Label? _cellCountLabel;
    private Label? _hoveredLabel;
    private Label? _drawCallsLabel;
    private Label? _trianglesLabel;
    private Label? _generationStatusLabel;

    // Checkboxes
    private CheckBox? _asyncCheckbox;

    // Sliders - stored with their value labels for direct reference
    private readonly Dictionary<HSlider, Label> _sliderValueLabels = new();

    // Sliders - Terrain
    private HSlider? _noiseScaleSlider;
    private HSlider? _octavesSlider;
    private HSlider? _persistenceSlider;
    private HSlider? _lacunaritySlider;
    private HSlider? _seaLevelSlider;
    private HSlider? _mountainLevelSlider;

    // Sliders - Fog
    private HSlider? _fogNearSlider;
    private HSlider? _fogFarSlider;
    private HSlider? _fogDensitySlider;

    // Direct fog control
    private Godot.Environment? _environment;

    #endregion

    public override void _Ready()
    {
        BuildUI();
        FindWorldEnvironment();
        // Don't call ApplyFogSettings() on startup - let scene file settings be used as-is
    }

    private void FindWorldEnvironment()
    {
        // Find WorldEnvironment in the scene tree
        var worldEnv = GetTree().Root.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
        if (worldEnv != null)
        {
            _environment = worldEnv.Environment;
            GD.Print("[GameUI] Found WorldEnvironment");
        }
        else
        {
            GD.PrintErr("[GameUI] WorldEnvironment not found!");
        }
    }

    private void ApplyFogSettings()
    {
        if (_environment == null) return;

        float near = (float)(_fogNearSlider?.Value ?? RenderingConfig.DefaultFogNear);
        float far = (float)(_fogFarSlider?.Value ?? RenderingConfig.DefaultFogFar);
        float density = (float)(_fogDensitySlider?.Value ?? RenderingConfig.DefaultFogDensity);

        // Match reference exactly - only update these 3 values
        // All other fog settings come from scene file
        _environment.FogDepthBegin = near;
        _environment.FogDepthEnd = far;
        _environment.FogLightEnergy = density;
    }

    public override void _Process(double delta)
    {
        UpdatePerformanceStats();
    }

    private void BuildUI()
    {
        // Position on right side of screen
        AnchorLeft = 1;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = -RenderingConfig.ControlPanelWidth - RenderingConfig.PanelMargin;
        OffsetTop = RenderingConfig.PanelMargin;
        OffsetRight = -RenderingConfig.PanelMargin;
        OffsetBottom = -RenderingConfig.PanelMargin;

        // Create panel background
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        // Scroll container for content
        _scroll = new ScrollContainer();
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _panel.AddChild(_scroll);

        // Main vertical layout
        _mainVbox = new VBoxContainer();
        _mainVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainVbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.AddChild(_mainVbox);

        // Title
        var title = new Label
        {
            Text = "HexGame Controls",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        _mainVbox.AddChild(title);

        // Create collapsible sections
        CreateMapFolder();
        CreateTerrainFolder();
        CreateFogFolder();
        CreateInfoFolder();
        CreateControlsFolder();
    }

    private VBoxContainer CreateFolder(string title, bool open = true)
    {
        var container = new VBoxContainer();
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainVbox!.AddChild(container);

        // Content container with margin
        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var margin = new MarginContainer();
        margin.Name = title.Replace(" ", "") + "Margin";
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddChild(content);

        // Header button with arrow
        var header = new Button
        {
            Text = (open ? "- " : "+ ") + title,
            Alignment = HorizontalAlignment.Left,
            Flat = true
        };
        header.AddThemeFontSizeOverride("font_size", 14);
        header.Pressed += () => ToggleFolder(header, margin, title);
        container.AddChild(header);

        container.AddChild(margin);
        margin.Visible = open;

        // Separator
        var sep = new HSeparator();
        container.AddChild(sep);

        return content;
    }

    private void ToggleFolder(Button header, MarginContainer margin, string title)
    {
        margin.Visible = !margin.Visible;
        var arrow = margin.Visible ? "- " : "+ ";
        header.Text = arrow + title;
    }

    private void CreateMapFolder()
    {
        var content = CreateFolder("Map Generation", true);

        // Grid for width/height/seed
        var grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        content.AddChild(grid);

        // Width
        grid.AddChild(new Label { Text = "Width:" });
        _mapWidthSpin = new SpinBox
        {
            MinValue = RenderingConfig.MinMapWidth,
            MaxValue = RenderingConfig.MaxMapWidth,
            Value = RenderingConfig.DefaultMapWidth,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(_mapWidthSpin);

        // Height
        grid.AddChild(new Label { Text = "Height:" });
        _mapHeightSpin = new SpinBox
        {
            MinValue = RenderingConfig.MinMapHeight,
            MaxValue = RenderingConfig.MaxMapHeight,
            Value = RenderingConfig.DefaultMapHeight,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(_mapHeightSpin);

        // Seed
        grid.AddChild(new Label { Text = "Seed:" });
        _seedSpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 99999,
            Value = GD.Randi() % 100000,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(_seedSpin);

        // Async checkbox
        _asyncCheckbox = new CheckBox
        {
            Text = "Async Generation",
            ButtonPressed = true
        };
        _asyncCheckbox.Toggled += OnAsyncToggled;
        content.AddChild(_asyncCheckbox);

        // Generation status label
        _generationStatusLabel = new Label
        {
            Text = "",
            Visible = false
        };
        _generationStatusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f));
        content.AddChild(_generationStatusLabel);

        // Buttons
        var btnHbox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        content.AddChild(btnHbox);

        var regenBtn = new Button
        {
            Text = "Regenerate",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        regenBtn.Pressed += OnRegeneratePressed;
        btnHbox.AddChild(regenBtn);

        var randomBtn = new Button
        {
            Text = "Random",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        randomBtn.Pressed += OnRandomSeedPressed;
        btnHbox.AddChild(randomBtn);
    }

    private void CreateTerrainFolder()
    {
        var content = CreateFolder("Terrain", true);

        _noiseScaleSlider = AddSlider(content, "Scale", 0.01f, 0.1f, 0.02f, 0.001f, OnNoiseScaleChanged);
        _octavesSlider = AddSlider(content, "Octaves", 1, 8, 4, 1, OnOctavesChanged);
        _persistenceSlider = AddSlider(content, "Persist", 0.1f, 0.9f, 0.5f, 0.05f, OnPersistenceChanged);
        _lacunaritySlider = AddSlider(content, "Lacunar", 1.5f, 3.0f, 2.0f, 0.1f, OnLacunarityChanged);
        _seaLevelSlider = AddSlider(content, "Sea Level", 0.0f, 0.8f, 0.35f, 0.01f, OnSeaLevelChanged);
        _mountainLevelSlider = AddSlider(content, "Mountains", 0.5f, 1.0f, 0.75f, 0.01f, OnMountainLevelChanged);
    }

    private void CreateFogFolder()
    {
        var content = CreateFolder("Fog", true);

        _fogNearSlider = AddSlider(content, "Near", RenderingConfig.FogNearMin, RenderingConfig.FogNearMax,
            RenderingConfig.DefaultFogNear, 1.0f, OnFogNearChanged);
        _fogFarSlider = AddSlider(content, "Far", RenderingConfig.FogFarMin, RenderingConfig.FogFarMax,
            RenderingConfig.DefaultFogFar, 1.0f, OnFogFarChanged);
        _fogDensitySlider = AddSlider(content, "Density", 0.0f, 1.0f,
            RenderingConfig.DefaultFogDensity, 0.05f, OnFogDensityChanged);
    }

    private void CreateInfoFolder()
    {
        var content = CreateFolder("Info", true);

        _fpsLabel = new Label { Text = "FPS: 60" };
        content.AddChild(_fpsLabel);

        _cellCountLabel = new Label { Text = "Cells: 1024" };
        content.AddChild(_cellCountLabel);

        _hoveredLabel = new Label { Text = "Hovered: None" };
        content.AddChild(_hoveredLabel);

        _drawCallsLabel = new Label { Text = "Draw Calls: 0" };
        content.AddChild(_drawCallsLabel);

        _trianglesLabel = new Label { Text = "Triangles: 0" };
        content.AddChild(_trianglesLabel);
    }

    private void CreateControlsFolder()
    {
        var content = CreateFolder("Controls", false);

        var controlsLabel = new Label { Text = GetControlsText() };
        controlsLabel.AddThemeFontSizeOverride("font_size", 11);
        content.AddChild(controlsLabel);
    }

    /// <summary>
    /// Creates a slider with label and value display.
    /// Stores the value label reference for direct updates (no child index assumptions).
    /// </summary>
    private HSlider AddSlider(VBoxContainer parent, string labelText, float minVal, float maxVal,
        float defaultVal, float step, Godot.Range.ValueChangedEventHandler callback)
    {
        var hbox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(hbox);

        var label = new Label
        {
            Text = labelText,
            CustomMinimumSize = new Vector2(70, 0)
        };
        hbox.AddChild(label);

        var slider = new HSlider
        {
            MinValue = minVal,
            MaxValue = maxVal,
            Value = defaultVal,
            Step = step,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        slider.ValueChanged += callback;
        hbox.AddChild(slider);

        var valueLabel = new Label
        {
            Text = step >= 1 ? $"{(int)defaultVal}" : $"{defaultVal:F2}",
            CustomMinimumSize = new Vector2(40, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        hbox.AddChild(valueLabel);

        // Store direct reference to value label for updates
        _sliderValueLabels[slider] = valueLabel;

        return slider;
    }

    /// <summary>
    /// Updates the value label for a slider using stored reference.
    /// </summary>
    private void UpdateSliderLabel(HSlider slider, double value, bool isInt = false)
    {
        if (_sliderValueLabels.TryGetValue(slider, out var label))
        {
            label.Text = isInt ? $"{(int)value}" : $"{value:F2}";
        }
    }

    private void UpdatePerformanceStats()
    {
        if (_fpsLabel != null)
        {
            _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }

        if (_drawCallsLabel != null)
        {
            var drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
            _drawCallsLabel.Text = $"Draw Calls: {(int)drawCalls}";
        }

        if (_trianglesLabel != null)
        {
            var primitives = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
            _trianglesLabel.Text = $"Triangles: {(int)primitives}";
        }
    }

    #region Public Methods

    /// <summary>
    /// Sets the current seed value in the UI.
    /// </summary>
    public void SetSeed(int seedVal)
    {
        if (_seedSpin != null)
        {
            _seedSpin.Value = Math.Abs(seedVal) % 100000;
        }
    }

    /// <summary>
    /// Updates the cell count display.
    /// </summary>
    public void SetCellCount(int count)
    {
        if (_cellCountLabel != null)
        {
            _cellCountLabel.Text = $"Cells: {count}";
        }
    }

    /// <summary>
    /// Updates the hovered hex display.
    /// </summary>
    public void SetHoveredHex(int q, int r, string terrain)
    {
        if (_hoveredLabel != null)
        {
            _hoveredLabel.Text = $"Hovered: ({q}, {r}) {terrain}";
        }
    }

    /// <summary>
    /// Clears the hovered hex display.
    /// </summary>
    public void ClearHoveredHex()
    {
        if (_hoveredLabel != null)
        {
            _hoveredLabel.Text = "Hovered: None";
        }
    }

    /// <summary>
    /// Shows a generation status message.
    /// </summary>
    public void ShowGenerationStatus(string message)
    {
        if (_generationStatusLabel != null)
        {
            _generationStatusLabel.Text = message;
            _generationStatusLabel.Visible = true;
        }
    }

    /// <summary>
    /// Hides the generation status message.
    /// </summary>
    public void HideGenerationStatus()
    {
        if (_generationStatusLabel != null)
        {
            _generationStatusLabel.Visible = false;
            _generationStatusLabel.Text = "";
        }
    }

    #endregion

    #region Event Handlers

    private void OnRegeneratePressed()
    {
        if (_mapWidthSpin == null || _mapHeightSpin == null || _seedSpin == null) return;
        int width = (int)_mapWidthSpin.Value;
        int height = (int)_mapHeightSpin.Value;
        int seedVal = (int)_seedSpin.Value;
        EmitSignal(SignalName.RegenerateRequested, width, height, seedVal);
    }

    private void OnRandomSeedPressed()
    {
        if (_seedSpin != null)
        {
            _seedSpin.Value = GD.Randi() % 100000;
        }
        EmitSignal(SignalName.RandomSeedRequested);
    }

    private void OnAsyncToggled(bool enabled)
    {
        EmitSignal(SignalName.AsyncToggleChanged, enabled);
    }

    private void OnNoiseScaleChanged(double value)
    {
        if (_noiseScaleSlider != null) UpdateSliderLabel(_noiseScaleSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "noise_scale", (float)value);
    }

    private void OnOctavesChanged(double value)
    {
        if (_octavesSlider != null) UpdateSliderLabel(_octavesSlider, value, true);
        EmitSignal(SignalName.NoiseParamChanged, "octaves", (float)value);
    }

    private void OnPersistenceChanged(double value)
    {
        if (_persistenceSlider != null) UpdateSliderLabel(_persistenceSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "persistence", (float)value);
    }

    private void OnLacunarityChanged(double value)
    {
        if (_lacunaritySlider != null) UpdateSliderLabel(_lacunaritySlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "lacunarity", (float)value);
    }

    private void OnSeaLevelChanged(double value)
    {
        if (_seaLevelSlider != null) UpdateSliderLabel(_seaLevelSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "sea_level", (float)value);
    }

    private void OnMountainLevelChanged(double value)
    {
        if (_mountainLevelSlider != null) UpdateSliderLabel(_mountainLevelSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "mountain_level", (float)value);
    }

    private void OnFogNearChanged(double value)
    {
        if (_fogNearSlider != null) UpdateSliderLabel(_fogNearSlider, value, true);
        if (_environment != null) _environment.FogDepthBegin = (float)value;
    }

    private void OnFogFarChanged(double value)
    {
        if (_fogFarSlider != null) UpdateSliderLabel(_fogFarSlider, value, true);
        if (_environment != null) _environment.FogDepthEnd = (float)value;
    }

    private void OnFogDensityChanged(double value)
    {
        if (_fogDensitySlider != null) UpdateSliderLabel(_fogDensitySlider, value);
        if (_environment != null) _environment.FogLightEnergy = (float)value;
    }

    #endregion

    private static string GetControlsText()
    {
        return @"Camera:
WASD/Arrows: Pan
Q/E: Rotate
R/F: Tilt
Z/X: Up/Down
Scroll: Zoom

Map:
Space: New Map
G: Regenerate";
    }
}
