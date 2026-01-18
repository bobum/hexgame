namespace HexGame.UI;

/// <summary>
/// Main game UI panel with map controls and info display.
/// Direct port of game_ui.gd
/// </summary>
public partial class GameUI : Control
{
    #region Signals

    [Signal]
    public delegate void RegenerateRequestedEventHandler(int width, int height, int seedVal);

    [Signal]
    public delegate void RandomSeedRequestedEventHandler();

    [Signal]
    public delegate void EndTurnRequestedEventHandler();

    [Signal]
    public delegate void SpawnLandRequestedEventHandler(int count);

    [Signal]
    public delegate void SpawnNavalRequestedEventHandler(int count);

    [Signal]
    public delegate void SpawnAiRequestedEventHandler(int land, int naval);

    [Signal]
    public delegate void ClearUnitsRequestedEventHandler();

    [Signal]
    public delegate void NoiseParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void ShaderParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void LightingParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void FogParamChangedEventHandler(string param, float value);

    [Signal]
    public delegate void AsyncToggleChangedEventHandler(bool enabled);

    [Signal]
    public delegate void WaterParamChangedEventHandler(string param, float value);

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
    private Label? _turnLabel;
    private Label? _unitCountLabel;
    private Label? _poolStatsLabel;
    private Label? _generationStatusLabel;

    // Checkboxes
    private CheckBox? _asyncCheckbox;

    // Sliders
    private HSlider? _noiseScaleSlider;
    private HSlider? _octavesSlider;
    private HSlider? _persistenceSlider;
    private HSlider? _lacunaritySlider;
    private HSlider? _seaLevelSlider;
    private HSlider? _mountainLevelSlider;
    private HSlider? _riverSlider;
    private HSlider? _flowSpeedSlider;
    private HSlider? _shaderNoiseSlider;
    private HSlider? _shaderNoiseScaleSlider;
    private HSlider? _shaderWallDarkSlider;
    private HSlider? _shaderRoughnessSlider;
    private HSlider? _ambientEnergySlider;
    private HSlider? _lightEnergySlider;
    private HSlider? _fogNearSlider;
    private HSlider? _fogFarSlider;
    private HSlider? _fogDensitySlider;
    private HSlider? _triplanarSlider;
    private HSlider? _fresnelSlider;
    private HSlider? _specularSlider;
    private HSlider? _blendStrengthSlider;

    // Water sliders
    private HSlider? _waterHeightSlider;

    private Node3D? _mainNode;

    #endregion

    public override void _Ready()
    {
        GD.Print("GameUI _Ready called");
        BuildUI();
        CallDeferred(MethodName.EmitShaderDefaults);
        CallDeferred(MethodName.DebugPanelSize);
    }

    private void DebugPanelSize()
    {
        GD.Print($"GameUI size: {Size}, position: {Position}");
        GD.Print($"GameUI anchors: L={AnchorLeft}, R={AnchorRight}, T={AnchorTop}, B={AnchorBottom}");
        GD.Print($"GameUI offsets: L={OffsetLeft}, R={OffsetRight}, T={OffsetTop}, B={OffsetBottom}");
        GD.Print($"Panel size: {_panel?.Size}, visible: {_panel?.Visible}");
        GD.Print($"MainVbox child count: {_mainVbox?.GetChildCount()}");
    }

    public override void _Process(double delta)
    {
        // Update FPS
        if (_fpsLabel != null)
        {
            _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }

        // Update render stats
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

    private void BuildUI()
    {
        // Position this control directly on the right side (like PerformanceMonitor does)
        AnchorLeft = 1;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = -260;
        OffsetTop = 10;
        OffsetRight = -10;
        OffsetBottom = -10;

        // Create panel as background
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        _scroll = new ScrollContainer();
        _scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _panel.AddChild(_scroll);

        _mainVbox = new VBoxContainer();
        _mainVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainVbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.AddChild(_mainVbox);

        // Title
        var instanceId = $"{GD.Randi() % 0xFFFF:X4}";
        var title = new Label
        {
            Text = $"HexGame [{instanceId}]",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        _mainVbox.AddChild(title);

        // Create collapsible folders
        CreateMapFolder();
        CreateTerrainFolder();
        CreateRiversFolder();
        CreateWaterFolder();
        CreateShaderFolder();
        CreateFogFolder();
        CreateUnitsFolder();
        CreateTurnFolder();
        CreateInfoFolder();
        CreateControlsFolder();
    }

    private VBoxContainer CreateFolder(string title, bool open = true)
    {
        var container = new VBoxContainer();
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainVbox!.AddChild(container);

        // Content container (indented)
        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var margin = new MarginContainer();
        margin.Name = title.Replace(" ", "") + "Margin";
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddChild(content);

        // Header button with arrow
        var header = new Button
        {
            Text = (open ? "▼ " : "▶ ") + title,
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
        var arrow = margin.Visible ? "▼ " : "▶ ";
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
        var widthLabel = new Label { Text = "Width:" };
        grid.AddChild(widthLabel);
        _mapWidthSpin = new SpinBox
        {
            MinValue = 10,
            MaxValue = 80,
            Value = 32,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(_mapWidthSpin);

        // Height
        var heightLabel = new Label { Text = "Height:" };
        grid.AddChild(heightLabel);
        _mapHeightSpin = new SpinBox
        {
            MinValue = 10,
            MaxValue = 60,
            Value = 32,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(_mapHeightSpin);

        // Seed
        var seedLabel = new Label { Text = "Seed:" };
        grid.AddChild(seedLabel);
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

    private void CreateRiversFolder()
    {
        var content = CreateFolder("Rivers", true);

        _riverSlider = AddSlider(content, "Density", 0.0f, 0.3f, 0.1f, 0.01f, OnRiverChanged);
        _flowSpeedSlider = AddSlider(content, "Flow Speed", 0.5f, 3.0f, 1.5f, 0.1f, OnFlowSpeedChanged);
    }

    private void CreateWaterFolder()
    {
        var content = CreateFolder("Water", true);

        _waterHeightSlider = AddSlider(content, "Height", -0.5f, 0.5f, 0.12f, 0.01f, OnWaterHeightChanged);
    }

    private void CreateShaderFolder()
    {
        var content = CreateFolder("Shader", true);

        _shaderNoiseSlider = AddSlider(content, "Noise", 0.0f, 0.5f, 0.35f, 0.01f, OnShaderNoiseChanged);
        _shaderNoiseScaleSlider = AddSlider(content, "NoiseScale", 0.5f, 10.0f, 3.0f, 0.1f, OnShaderNoiseScaleChanged);
        _shaderWallDarkSlider = AddSlider(content, "WallDark", 0.0f, 0.8f, 0.55f, 0.01f, OnShaderWallDarkChanged);
        _triplanarSlider = AddSlider(content, "Triplanar", 1.0f, 10.0f, 4.0f, 0.5f, OnTriplanarChanged);
        _shaderRoughnessSlider = AddSlider(content, "Roughness", 0.0f, 1.0f, 0.9f, 0.01f, OnShaderRoughnessChanged);
        _specularSlider = AddSlider(content, "Specular", 0.0f, 1.0f, 0.3f, 0.01f, OnSpecularChanged);
        _fresnelSlider = AddSlider(content, "Fresnel", 0.0f, 0.5f, 0.15f, 0.01f, OnFresnelChanged);
        _blendStrengthSlider = AddSlider(content, "Blend", 0.0f, 1.0f, 1.0f, 0.05f, OnBlendStrengthChanged);
        _ambientEnergySlider = AddSlider(content, "Ambient", 0.0f, 1.0f, 0.25f, 0.01f, OnAmbientEnergyChanged);
        _lightEnergySlider = AddSlider(content, "Light", 0.0f, 2.0f, 1.0f, 0.05f, OnLightEnergyChanged);
    }

    private void CreateFogFolder()
    {
        var content = CreateFolder("Fog", true);

        _fogNearSlider = AddSlider(content, "Near", 5.0f, 40.0f, 15.0f, 1.0f, OnFogNearChanged);
        _fogFarSlider = AddSlider(content, "Far", 30.0f, 120.0f, 50.0f, 1.0f, OnFogFarChanged);
        _fogDensitySlider = AddSlider(content, "Density", 0.0f, 1.0f, 0.5f, 0.05f, OnFogDensityChanged);
    }

    private void CreateUnitsFolder()
    {
        var content = CreateFolder("Units", true);

        _unitCountLabel = new Label { Text = "Land: 0  Naval: 0" };
        content.AddChild(_unitCountLabel);

        _poolStatsLabel = new Label { Text = "Pool: 0/0 (0%)" };
        content.AddChild(_poolStatsLabel);

        // Spawn buttons
        var spawnHbox = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        content.AddChild(spawnHbox);

        var spawnLandBtn = new Button
        {
            Text = "+10 Land",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        spawnLandBtn.Pressed += OnSpawnLandPressed;
        spawnHbox.AddChild(spawnLandBtn);

        var spawnNavalBtn = new Button
        {
            Text = "+5 Naval",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        spawnNavalBtn.Pressed += OnSpawnNavalPressed;
        spawnHbox.AddChild(spawnNavalBtn);

        // AI spawn button
        var spawnAiBtn = new Button { Text = "Spawn 10 AI" };
        spawnAiBtn.Pressed += OnSpawnAiPressed;
        content.AddChild(spawnAiBtn);

        // Clear button
        var clearBtn = new Button { Text = "Clear Units" };
        clearBtn.Pressed += OnClearUnitsPressed;
        content.AddChild(clearBtn);
    }

    private void CreateTurnFolder()
    {
        var content = CreateFolder("Turn", true);

        _turnLabel = new Label { Text = "Turn 1 - Player (movement)" };
        content.AddChild(_turnLabel);

        var endTurnBtn = new Button { Text = "End Turn" };
        endTurnBtn.Pressed += OnEndTurnPressed;
        content.AddChild(endTurnBtn);
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

    private HSlider AddSlider(VBoxContainer parent, string labelText, float minVal, float maxVal, float defaultVal, float step, Godot.Range.ValueChangedEventHandler callback)
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

        return slider;
    }

    private void EmitShaderDefaults()
    {
        EmitSignal(SignalName.ShaderParamChanged, "top_noise_strength", 0.35f);
        EmitSignal(SignalName.ShaderParamChanged, "top_noise_scale", 3.0f);
        EmitSignal(SignalName.ShaderParamChanged, "wall_darkening", 0.55f);
        EmitSignal(SignalName.ShaderParamChanged, "roughness_value", 0.9f);
        EmitSignal(SignalName.ShaderParamChanged, "triplanar_sharpness", 4.0f);
        EmitSignal(SignalName.ShaderParamChanged, "fresnel_strength", 0.15f);
        EmitSignal(SignalName.ShaderParamChanged, "specular_strength", 0.3f);
    }

    #region Public Methods

    public void SetMainNode(Node3D node)
    {
        _mainNode = node;
        UpdateCellCount();
    }

    public void SetSeed(int seedVal)
    {
        if (_seedSpin != null)
        {
            _seedSpin.Value = seedVal % 100000;
        }
    }

    public void SetHoveredHex(int q, int r, string terrain)
    {
        if (_hoveredLabel != null)
        {
            _hoveredLabel.Text = $"Hovered: ({q}, {r}) {terrain}";
        }
    }

    public void ClearHoveredHex()
    {
        if (_hoveredLabel != null)
        {
            _hoveredLabel.Text = "Hovered: None";
        }
    }

    public void SetTurnStatus(string status)
    {
        if (_turnLabel != null)
        {
            _turnLabel.Text = status;
        }
    }

    public void SetUnitCounts(int land, int naval)
    {
        if (_unitCountLabel != null)
        {
            _unitCountLabel.Text = $"Land: {land}  Naval: {naval}";
        }
    }

    public void SetPoolStats(int active, int created, float reuseRate)
    {
        if (_poolStatsLabel != null)
        {
            _poolStatsLabel.Text = $"Pool: {active}/{created} ({(int)(reuseRate * 100)}%)";
        }
    }

    public void ShowGenerationStatus(string message)
    {
        if (_generationStatusLabel != null)
        {
            _generationStatusLabel.Text = message;
            _generationStatusLabel.Visible = true;
        }
    }

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

    private void UpdateCellCount()
    {
        if (_mainNode != null && _cellCountLabel != null)
        {
            int width = _mainNode.HasMeta("map_width") ? (int)_mainNode.GetMeta("map_width") : 32;
            int height = _mainNode.HasMeta("map_height") ? (int)_mainNode.GetMeta("map_height") : 32;
            _cellCountLabel.Text = $"Cells: {width * height}";
        }
    }

    private void OnRegeneratePressed()
    {
        if (_mapWidthSpin == null || _mapHeightSpin == null || _seedSpin == null) return;
        int width = (int)_mapWidthSpin.Value;
        int height = (int)_mapHeightSpin.Value;
        int seedVal = (int)_seedSpin.Value;
        EmitSignal(SignalName.RegenerateRequested, width, height, seedVal);
        UpdateCellCount();
    }

    private void OnRandomSeedPressed()
    {
        if (_seedSpin != null)
        {
            _seedSpin.Value = GD.Randi() % 100000;
        }
        EmitSignal(SignalName.RandomSeedRequested);
    }

    private void OnEndTurnPressed()
    {
        EmitSignal(SignalName.EndTurnRequested);
    }

    private void OnSpawnLandPressed()
    {
        GD.Print("OnSpawnLandPressed called - emitting signal");
        EmitSignal(SignalName.SpawnLandRequested, 10);
    }

    private void OnSpawnNavalPressed()
    {
        GD.Print("OnSpawnNavalPressed called - emitting signal");
        EmitSignal(SignalName.SpawnNavalRequested, 5);
    }

    private void OnSpawnAiPressed()
    {
        EmitSignal(SignalName.SpawnAiRequested, 5, 5);
    }

    private void OnClearUnitsPressed()
    {
        EmitSignal(SignalName.ClearUnitsRequested);
    }

    private void OnAsyncToggled(bool enabled)
    {
        EmitSignal(SignalName.AsyncToggleChanged, enabled);
    }

    private void UpdateSliderLabel(HSlider slider, double value, bool isInt = false)
    {
        var parent = slider.GetParent();
        if (parent != null && parent.GetChildCount() > 2)
        {
            var label = parent.GetChild(2) as Label;
            if (label != null)
            {
                label.Text = isInt ? $"{(int)value}" : $"{value:F2}";
            }
        }
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

    private void OnRiverChanged(double value)
    {
        if (_riverSlider != null) UpdateSliderLabel(_riverSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "river_percentage", (float)value);
    }

    private void OnFlowSpeedChanged(double value)
    {
        if (_flowSpeedSlider != null) UpdateSliderLabel(_flowSpeedSlider, value);
        EmitSignal(SignalName.NoiseParamChanged, "flow_speed", (float)value);
    }

    private void OnWaterHeightChanged(double value)
    {
        if (_waterHeightSlider != null) UpdateSliderLabel(_waterHeightSlider, value);
        EmitSignal(SignalName.WaterParamChanged, "height_offset", (float)value);
    }

    private void OnShaderNoiseChanged(double value)
    {
        if (_shaderNoiseSlider != null) UpdateSliderLabel(_shaderNoiseSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "top_noise_strength", (float)value);
    }

    private void OnShaderNoiseScaleChanged(double value)
    {
        if (_shaderNoiseScaleSlider != null) UpdateSliderLabel(_shaderNoiseScaleSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "top_noise_scale", (float)value);
    }

    private void OnShaderWallDarkChanged(double value)
    {
        if (_shaderWallDarkSlider != null) UpdateSliderLabel(_shaderWallDarkSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "wall_darkening", (float)value);
    }

    private void OnTriplanarChanged(double value)
    {
        if (_triplanarSlider != null) UpdateSliderLabel(_triplanarSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "triplanar_sharpness", (float)value);
    }

    private void OnShaderRoughnessChanged(double value)
    {
        if (_shaderRoughnessSlider != null) UpdateSliderLabel(_shaderRoughnessSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "roughness_value", (float)value);
    }

    private void OnSpecularChanged(double value)
    {
        if (_specularSlider != null) UpdateSliderLabel(_specularSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "specular_strength", (float)value);
    }

    private void OnFresnelChanged(double value)
    {
        if (_fresnelSlider != null) UpdateSliderLabel(_fresnelSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "fresnel_strength", (float)value);
    }

    private void OnBlendStrengthChanged(double value)
    {
        if (_blendStrengthSlider != null) UpdateSliderLabel(_blendStrengthSlider, value);
        EmitSignal(SignalName.ShaderParamChanged, "blend_strength", (float)value);
    }

    private void OnAmbientEnergyChanged(double value)
    {
        if (_ambientEnergySlider != null) UpdateSliderLabel(_ambientEnergySlider, value);
        EmitSignal(SignalName.LightingParamChanged, "ambient_energy", (float)value);
    }

    private void OnLightEnergyChanged(double value)
    {
        if (_lightEnergySlider != null) UpdateSliderLabel(_lightEnergySlider, value);
        EmitSignal(SignalName.LightingParamChanged, "light_energy", (float)value);
    }

    private void OnFogNearChanged(double value)
    {
        if (_fogNearSlider != null) UpdateSliderLabel(_fogNearSlider, value, true);
        EmitSignal(SignalName.FogParamChanged, "fog_near", (float)value);
    }

    private void OnFogFarChanged(double value)
    {
        if (_fogFarSlider != null) UpdateSliderLabel(_fogFarSlider, value, true);
        EmitSignal(SignalName.FogParamChanged, "fog_far", (float)value);
    }

    private void OnFogDensityChanged(double value)
    {
        if (_fogDensitySlider != null) UpdateSliderLabel(_fogDensitySlider, value);
        EmitSignal(SignalName.FogParamChanged, "fog_density", (float)value);
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
Middle: Pan
Right: Rotate

Selection:
Click: Select
Ctrl+Click: Add
Box: Multi-select

Map:
Space: New Map
G: Regenerate";
    }
}
