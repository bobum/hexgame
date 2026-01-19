using Godot;
using Godot.Collections;


//# Main game UI panel with map controls and info display

//# Matches the lil-gui panel from web version with collapsible folders
[GlobalClass]
public partial class GameUI : Godot.Control
{
	[Signal]
	public delegate void RegenerateRequestedEventHandler(int width, int height, int seed_val);
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
	public delegate void NoiseParamChangedEventHandler(String param, double value);
	[Signal]
	public delegate void ShaderParamChangedEventHandler(String param, double value);
	[Signal]
	public delegate void LightingParamChangedEventHandler(String param, double value);
	[Signal]
	public delegate void FogParamChangedEventHandler(String param, double value);
	[Signal]
	public delegate void AsyncToggleChangedEventHandler(bool enabled);

	public Godot.PanelContainer Panel;
	public Godot.ScrollContainer Scroll;
	public Godot.VBoxContainer MainVbox;


	// Input controls
	public Godot.SpinBox MapWidthSpin;
	public Godot.SpinBox MapHeightSpin;
	public Godot.SpinBox SeedSpin;


	// Dynamic labels
	public Godot.Label FpsLabel;
	public Godot.Label CellCountLabel;
	public Godot.Label HoveredLabel;
	public Godot.Label DrawCallsLabel;
	public Godot.Label TrianglesLabel;
	public Godot.Label TurnLabel;
	public Godot.Label UnitCountLabel;
	public Godot.Label PoolStatsLabel;
	public Godot.Label GenerationStatusLabel;


	// Checkboxes
	public Godot.CheckBox AsyncCheckbox;


	// Sliders
	public Godot.HSlider NoiseScaleSlider;
	public Godot.HSlider OctavesSlider;
	public Godot.HSlider PersistenceSlider;
	public Godot.HSlider LacunaritySlider;
	public Godot.HSlider SeaLevelSlider;
	public Godot.HSlider MountainLevelSlider;
	public Godot.HSlider RiverSlider;
	public Godot.HSlider FlowSpeedSlider;


	// Shader controls
	public Godot.HSlider ShaderNoiseSlider;
	public Godot.HSlider ShaderNoiseScaleSlider;
	public Godot.HSlider ShaderWallDarkSlider;
	public Godot.HSlider ShaderRoughnessSlider;
	public Godot.HSlider AmbientEnergySlider;
	public Godot.HSlider LightEnergySlider;
	public Godot.HSlider FogNearSlider;
	public Godot.HSlider FogFarSlider;
	public Godot.HSlider FogDensitySlider;


	// Advanced shader controls
	public Godot.HSlider TriplanarSlider;
	public Godot.HSlider FresnelSlider;
	public Godot.HSlider SpecularSlider;
	public Godot.HSlider BlendStrengthSlider;

	public Godot.Node3D MainNode;


	public override void _Ready()
	{
		Panel = GetNode("Panel");
		Scroll = GetNode("Panel") / ScrollContainer;
		MainVbox = GetNode("Panel") / ScrollContainer / VBox;
		_BuildUi();

		// Emit shader defaults after a short delay to ensure terrain is built
		CallDeferred("_emit_shader_defaults");
	}


	public override void _Process(double _delta)
	{

		// Update FPS
		if(FpsLabel)
		{
			FpsLabel.Text = "FPS: %d" % Godot.Engine.GetFramesPerSecond();
		}


		// Update render stats
		if(DrawCallsLabel)
		{
			var draw_calls = Godot.Performance.GetMonitor(Godot.Performance.Monitor.RenderTotalDrawCallsInFrame);
			DrawCallsLabel.Text = "Draw Calls: %d" % draw_calls;
		}
		if(TrianglesLabel)
		{
			var primitives = Godot.Performance.GetMonitor(Godot.Performance.Monitor.RenderTotalPrimitivesInFrame);
			TrianglesLabel.Text = "Triangles: %d" % primitives;
		}
	}


	protected void _BuildUi()
	{

		// Title with instance ID for debugging
		var instance_id = "%04X" % (GD.Randi() % 0xFFFF);
		var title = Label.New();
		title.Text = "HexGame [%s]" % instance_id;
		title.HorizontalAlignment = HORIZONTAL_ALIGNMENT_CENTER;
		title.AddThemeFontSizeOverride("font_size", 16);
		MainVbox.AddChild(title);


		// Create collapsible folders
		_CreateMapFolder();
		_CreateTerrainFolder();
		_CreateRiversFolder();
		_CreateShaderFolder();
		_CreateFogFolder();
		_CreateUnitsFolder();
		_CreateTurnFolder();
		_CreateInfoFolder();
		_CreateControlsFolder();
	}


	protected Godot.VBoxContainer _CreateFolder(String title, bool open = true)
	{
		var container = VBoxContainer.New();
		container.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		MainVbox.AddChild(container);


		// Content container (indented)
		var content = VBoxContainer.New();
		content.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		var margin = MarginContainer.New();
		margin.Name = title.Replace(" ", "") + "Margin";
		margin.AddThemeConstantOverride("margin_left", 12);
		margin.AddChild(content);


		// Header button with arrow
		var header = Button.New();
		header.Text = (( open ? "▼ " : "▶ " )) + title;
		header.Alignment = HORIZONTAL_ALIGNMENT_LEFT;
		header.Flat = true;
		header.AddThemeFontSizeOverride("font_size", 14);
		header.Pressed.Connect(() =>
		{	_ToggleFolder(header, margin, title);;);
		}
		container.AddChild(header);

		container.AddChild(margin);
		margin.Visible = open;


		// Separator
		var sep = HSeparator.New();
		container.AddChild(sep);

		return content;
	}


	protected void _ToggleFolder(Godot.Button header, Godot.MarginContainer margin, String title)
	{
		margin.Visible = !margin.Visible;
		var arrow = ( margin.Visible ? "▼ " : "▶ " );
		header.Text = arrow + title;
	}


	protected void _CreateMapFolder()
	{
		var content = _CreateFolder("Map Generation", true);


		// Grid for width/height/seed
		var grid = GridContainer.New();
		grid.Columns = 2;
		grid.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		content.AddChild(grid);


		// Width
		var width_label = Label.New();
		width_label.Text = "Width:";
		grid.AddChild(width_label);
		MapWidthSpin = SpinBox.New();
		MapWidthSpin.MinValue = 10;
		MapWidthSpin.MaxValue = 80;
		MapWidthSpin.Value = 32;
		MapWidthSpin.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		grid.AddChild(MapWidthSpin);


		// Height
		var height_label = Label.New();
		height_label.Text = "Height:";
		grid.AddChild(height_label);
		MapHeightSpin = SpinBox.New();
		MapHeightSpin.MinValue = 10;
		MapHeightSpin.MaxValue = 60;
		MapHeightSpin.Value = 32;
		MapHeightSpin.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		grid.AddChild(MapHeightSpin);


		// Seed
		var seed_label = Label.New();
		seed_label.Text = "Seed:";
		grid.AddChild(seed_label);
		SeedSpin = SpinBox.New();
		SeedSpin.MinValue = 1;
		SeedSpin.MaxValue = 99999;
		SeedSpin.Value = GD.Randi() % 100000;
		SeedSpin.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		grid.AddChild(SeedSpin);


		// Async checkbox
		AsyncCheckbox = CheckBox.New();
		AsyncCheckbox.Text = "Async Generation";
		AsyncCheckbox.ButtonPressed = true;
		// Default enabled
		AsyncCheckbox.Toggled += _on_async_toggled;
		content.AddChild(AsyncCheckbox);


		// Generation status label (hidden by default)
		GenerationStatusLabel = Label.New();
		GenerationStatusLabel.Text = "";
		GenerationStatusLabel.Visible = false;
		GenerationStatusLabel.AddThemeColorOverride("font_color", new Color(1.0, 0.8, 0.2));
		// Yellow
		content.AddChild(GenerationStatusLabel);


		// Buttons
		var btn_hbox = HBoxContainer.New();
		btn_hbox.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		content.AddChild(btn_hbox);

		var regen_btn = Button.New();
		regen_btn.Text = "Regenerate";
		regen_btn.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		regen_btn.Pressed.Connect(_on_regenerate_pressed);
		btn_hbox.AddChild(regen_btn);

		var random_btn = Button.New();
		random_btn.Text = "Random";
		random_btn.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		random_btn.Pressed.Connect(_on_random_seed_pressed);
		btn_hbox.AddChild(random_btn);
	}


	protected void _CreateTerrainFolder()
	{
		var content = _CreateFolder("Terrain", true);


		// Noise Scale (0.01 - 0.1)
		NoiseScaleSlider = _AddSlider(content, "Scale", 0.01, 0.1, 0.02, 0.001, _on_noise_scale_changed);


		// Octaves (1 - 8)
		OctavesSlider = _AddSlider(content, "Octaves", 1, 8, 4, 1, _on_octaves_changed);


		// Persistence (0.1 - 0.9)
		PersistenceSlider = _AddSlider(content, "Persist", 0.1, 0.9, 0.5, 0.05, _on_persistence_changed);


		// Lacunarity (1.5 - 3.0)
		LacunaritySlider = _AddSlider(content, "Lacunar", 1.5, 3.0, 2.0, 0.1, _on_lacunarity_changed);


		// Sea Level (0.0 - 0.8)
		SeaLevelSlider = _AddSlider(content, "Sea Level", 0.0, 0.8, 0.35, 0.01, _on_sea_level_changed);


		// Mountain Level (0.5 - 1.0)
		MountainLevelSlider = _AddSlider(content, "Mountains", 0.5, 1.0, 0.75, 0.01, _on_mountain_level_changed);
	}


	protected void _CreateRiversFolder()
	{
		var content = _CreateFolder("Rivers", true);


		// River percentage (0.0 - 0.3)
		RiverSlider = _AddSlider(content, "Density", 0.0, 0.3, 0.1, 0.01, _on_river_changed);


		// Flow Speed (0.5 - 3.0)
		FlowSpeedSlider = _AddSlider(content, "Flow Speed", 0.5, 3.0, 1.5, 0.1, _on_flow_speed_changed);
	}


	protected void _CreateShaderFolder()
	{
		var content = _CreateFolder("Shader", true);


		// Shader noise strength (0.0 - 0.5) - matches shader default
		ShaderNoiseSlider = _AddSlider(content, "Noise", 0.0, 0.5, 0.35, 0.01, _on_shader_noise_changed);


		// Shader noise scale (0.5 - 10.0) - matches shader default
		ShaderNoiseScaleSlider = _AddSlider(content, "NoiseScale", 0.5, 10.0, 3.0, 0.1, _on_shader_noise_scale_changed);


		// Wall darkening (0.0 - 0.8) - matches shader default
		ShaderWallDarkSlider = _AddSlider(content, "WallDark", 0.0, 0.8, 0.55, 0.01, _on_shader_wall_dark_changed);


		// Triplanar sharpness (1.0 - 10.0) - controls wall texture blend
		TriplanarSlider = _AddSlider(content, "Triplanar", 1.0, 10.0, 4.0, 0.5, _on_triplanar_changed);


		// Roughness (0.0 - 1.0)
		ShaderRoughnessSlider = _AddSlider(content, "Roughness", 0.0, 1.0, 0.9, 0.01, _on_shader_roughness_changed);


		// Specular strength (0.0 - 1.0)
		SpecularSlider = _AddSlider(content, "Specular", 0.0, 1.0, 0.3, 0.01, _on_specular_changed);


		// Fresnel strength (0.0 - 0.5)
		FresnelSlider = _AddSlider(content, "Fresnel", 0.0, 0.5, 0.15, 0.01, _on_fresnel_changed);


		// Blend strength (0.0 - 1.0) - terrain color blending at hex corners
		BlendStrengthSlider = _AddSlider(content, "Blend", 0.0, 1.0, 1.0, 0.05, _on_blend_strength_changed);


		// Ambient energy (0.0 - 1.0)
		AmbientEnergySlider = _AddSlider(content, "Ambient", 0.0, 1.0, 0.25, 0.01, _on_ambient_energy_changed);


		// Light energy (0.0 - 2.0)
		LightEnergySlider = _AddSlider(content, "Light", 0.0, 2.0, 1.0, 0.05, _on_light_energy_changed);
	}


	protected void _CreateFogFolder()
	{
		var content = _CreateFolder("Fog", true);


		// Fog near distance (5 - 40)
		FogNearSlider = _AddSlider(content, "Near", 5.0, 40.0, 15.0, 1.0, _on_fog_near_changed);


		// Fog far distance (30 - 120)
		FogFarSlider = _AddSlider(content, "Far", 30.0, 120.0, 50.0, 1.0, _on_fog_far_changed);


		// Fog density/energy (0.0 - 1.0)
		FogDensitySlider = _AddSlider(content, "Density", 0.0, 1.0, 0.5, 0.05, _on_fog_density_changed);
	}


	protected void _EmitShaderDefaults()
	{

		// Emit shader parameter signals with default values so shader gets initialized
		EmitSignal("ShaderParamChanged", "top_noise_strength", 0.35);
		EmitSignal("ShaderParamChanged", "top_noise_scale", 3.0);
		EmitSignal("ShaderParamChanged", "wall_darkening", 0.55);
		EmitSignal("ShaderParamChanged", "roughness_value", 0.9);
		EmitSignal("ShaderParamChanged", "triplanar_sharpness", 4.0);
		EmitSignal("ShaderParamChanged", "fresnel_strength", 0.15);
		EmitSignal("ShaderParamChanged", "specular_strength", 0.3);
	}


	protected void _CreateUnitsFolder()
	{
		var content = _CreateFolder("Units", true);


		// Unit count label
		UnitCountLabel = Label.New();
		UnitCountLabel.Text = "Land: 0  Naval: 0";
		content.AddChild(UnitCountLabel);


		// Pool stats label
		PoolStatsLabel = Label.New();
		PoolStatsLabel.Text = "Pool: 0/0 (0%)";
		content.AddChild(PoolStatsLabel);


		// Spawn buttons
		var spawn_hbox = HBoxContainer.New();
		spawn_hbox.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		content.AddChild(spawn_hbox);

		var spawn_land_btn = Button.New();
		spawn_land_btn.Text = "+10 Land";
		spawn_land_btn.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		spawn_land_btn.Pressed.Connect(_on_spawn_land_pressed);
		spawn_hbox.AddChild(spawn_land_btn);

		var spawn_naval_btn = Button.New();
		spawn_naval_btn.Text = "+5 Naval";
		spawn_naval_btn.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		spawn_naval_btn.Pressed.Connect(_on_spawn_naval_pressed);
		spawn_hbox.AddChild(spawn_naval_btn);


		// AI spawn button
		var spawn_ai_btn = Button.New();
		spawn_ai_btn.Text = "Spawn 10 AI";
		spawn_ai_btn.Pressed.Connect(_on_spawn_ai_pressed);
		content.AddChild(spawn_ai_btn);


		// Clear button
		var clear_btn = Button.New();
		clear_btn.Text = "Clear Units";
		clear_btn.Pressed.Connect(_on_clear_units_pressed);
		content.AddChild(clear_btn);
	}


	protected void _CreateTurnFolder()
	{
		var content = _CreateFolder("Turn", true);


		// Turn status label
		TurnLabel = Label.New();
		TurnLabel.Text = "Turn 1 - Player (movement)";
		content.AddChild(TurnLabel);


		// End Turn button
		var end_turn_btn = Button.New();
		end_turn_btn.Text = "End Turn";
		end_turn_btn.Pressed.Connect(_on_end_turn_pressed);
		content.AddChild(end_turn_btn);
	}


	protected void _CreateInfoFolder()
	{
		var content = _CreateFolder("Info", true);

		FpsLabel = Label.New();
		FpsLabel.Text = "FPS: 60";
		content.AddChild(FpsLabel);

		CellCountLabel = Label.New();
		CellCountLabel.Text = "Cells: 1024";
		content.AddChild(CellCountLabel);

		HoveredLabel = Label.New();
		HoveredLabel.Text = "Hovered: None";
		content.AddChild(HoveredLabel);

		DrawCallsLabel = Label.New();
		DrawCallsLabel.Text = "Draw Calls: 0";
		content.AddChild(DrawCallsLabel);

		TrianglesLabel = Label.New();
		TrianglesLabel.Text = "Triangles: 0";
		content.AddChild(TrianglesLabel);
	}


	protected void _CreateControlsFolder()
	{
		var content = _CreateFolder("Controls", false);

		var controls_label = Label.New();
		controls_label.Text = _GetControlsText();
		controls_label.AddThemeFontSizeOverride("font_size", 11);
		content.AddChild(controls_label);
	}


	protected Godot.HSlider _AddSlider(Godot.VBoxContainer parent, String label_text, double min_val, double max_val, double default_val, double step, Callable callback)
	{
		var hbox = HBoxContainer.New();
		hbox.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		parent.AddChild(hbox);

		var label = Label.New();
		label.Text = label_text;
		label.CustomMinimumSize.X = 70;
		hbox.AddChild(label);

		var slider = HSlider.New();
		slider.MinValue = min_val;
		slider.MaxValue = max_val;
		slider.Value = default_val;
		slider.Step = step;
		slider.SizeFlagsHorizontal = Control.SizeFlags.SizeExpandFill;
		slider.ValueChanged.Connect(callback);
		hbox.AddChild(slider);

		var value_label = Label.New();
		value_label.Text = ( step >= 1 ? "%d" % Int(default_val) : "%.2f" % default_val );
		value_label.CustomMinimumSize.X = 40;
		value_label.HorizontalAlignment = HORIZONTAL_ALIGNMENT_RIGHT;
		hbox.AddChild(value_label);

		return slider;
	}


	public void SetMainNode(Godot.Node3D node)
	{
		MainNode = node;
		_UpdateCellCount();
	}


	public void SetSeed(int seed_val)
	{
		if(SeedSpin)
		{
			SeedSpin.Value = seed_val % 100000;
		}
	}


	public void SetHoveredHex(int q, int r, String terrain)
	{
		if(HoveredLabel)
		{
			HoveredLabel.Text = "Hovered: (%d, %d) %s" % new Array{q, r, terrain, };
		}
	}


	public void ClearHoveredHex()
	{
		if(HoveredLabel)
		{
			HoveredLabel.Text = "Hovered: None";
		}
	}


	public void SetTurnStatus(String status)
	{
		if(TurnLabel)
		{
			TurnLabel.Text = status;
		}
	}


	public void SetUnitCounts(int land, int naval)
	{
		if(UnitCountLabel)
		{
			UnitCountLabel.Text = "Land: %d  Naval: %d" % new Array{land, naval, };
		}
	}


	public void SetPoolStats(int active, int created, double reuse_rate)
	{
		if(PoolStatsLabel)
		{
			PoolStatsLabel.Text = "Pool: %d/%d (%d%%)" % new Array{active, created, Int(reuse_rate * 100), };
		}
	}


	protected void _UpdateCellCount()
	{
		if(MainNode && CellCountLabel)
		{
			var width = ( MainNode.Contains("map_width") ? MainNode.MapWidth : 32 );
			var height = ( MainNode.Contains("map_height") ? MainNode.MapHeight : 32 );
			CellCountLabel.Text = "Cells: %d" % (width * height);
		}
	}


	protected void _OnRegeneratePressed()
	{
		var width = Int(MapWidthSpin.Value);
		var height = Int(MapHeightSpin.Value);
		var seed_val = Int(SeedSpin.Value);
		EmitSignal("RegenerateRequested", width, height, seed_val);
		_UpdateCellCount();
	}


	protected void _OnRandomSeedPressed()
	{
		SeedSpin.Value = GD.Randi() % 100000;
		EmitSignal("RandomSeedRequested");
	}


	protected void _OnEndTurnPressed()
	{
		EmitSignal("EndTurnRequested");
	}


	protected void _OnSpawnLandPressed()
	{
		EmitSignal("SpawnLandRequested", 10);
	}


	protected void _OnSpawnNavalPressed()
	{
		EmitSignal("SpawnNavalRequested", 5);
	}


	protected void _OnSpawnAiPressed()
	{
		EmitSignal("SpawnAiRequested", 5, 5);
	}


	protected void _OnClearUnitsPressed()
	{
		EmitSignal("ClearUnitsRequested");
	}


	protected void _OnNoiseScaleChanged(double value)
	{
		_UpdateSliderLabel(NoiseScaleSlider, value);
		EmitSignal("NoiseParamChanged", "noise_scale", value);
	}


	protected void _OnOctavesChanged(double value)
	{
		_UpdateSliderLabel(OctavesSlider, value, true);
		EmitSignal("NoiseParamChanged", "octaves", value);
	}


	protected void _OnPersistenceChanged(double value)
	{
		_UpdateSliderLabel(PersistenceSlider, value);
		EmitSignal("NoiseParamChanged", "persistence", value);
	}


	protected void _OnLacunarityChanged(double value)
	{
		_UpdateSliderLabel(LacunaritySlider, value);
		EmitSignal("NoiseParamChanged", "lacunarity", value);
	}


	protected void _OnSeaLevelChanged(double value)
	{
		_UpdateSliderLabel(SeaLevelSlider, value);
		EmitSignal("NoiseParamChanged", "sea_level", value);
	}


	protected void _OnMountainLevelChanged(double value)
	{
		_UpdateSliderLabel(MountainLevelSlider, value);
		EmitSignal("NoiseParamChanged", "mountain_level", value);
	}


	protected void _OnRiverChanged(double value)
	{
		_UpdateSliderLabel(RiverSlider, value);
		EmitSignal("NoiseParamChanged", "river_percentage", value);
	}


	protected void _OnFlowSpeedChanged(double value)
	{
		_UpdateSliderLabel(FlowSpeedSlider, value);
		EmitSignal("NoiseParamChanged", "flow_speed", value);
	}


	protected void _OnShaderNoiseChanged(double value)
	{
		_UpdateSliderLabel(ShaderNoiseSlider, value);
		EmitSignal("ShaderParamChanged", "top_noise_strength", value);
	}


	protected void _OnShaderNoiseScaleChanged(double value)
	{
		_UpdateSliderLabel(ShaderNoiseScaleSlider, value);
		EmitSignal("ShaderParamChanged", "top_noise_scale", value);
	}


	protected void _OnShaderWallDarkChanged(double value)
	{
		_UpdateSliderLabel(ShaderWallDarkSlider, value);
		EmitSignal("ShaderParamChanged", "wall_darkening", value);
	}


	protected void _OnShaderRoughnessChanged(double value)
	{
		_UpdateSliderLabel(ShaderRoughnessSlider, value);
		EmitSignal("ShaderParamChanged", "roughness_value", value);
	}


	protected void _OnTriplanarChanged(double value)
	{
		_UpdateSliderLabel(TriplanarSlider, value);
		EmitSignal("ShaderParamChanged", "triplanar_sharpness", value);
	}


	protected void _OnSpecularChanged(double value)
	{
		_UpdateSliderLabel(SpecularSlider, value);
		EmitSignal("ShaderParamChanged", "specular_strength", value);
	}


	protected void _OnFresnelChanged(double value)
	{
		_UpdateSliderLabel(FresnelSlider, value);
		EmitSignal("ShaderParamChanged", "fresnel_strength", value);
	}


	protected void _OnBlendStrengthChanged(double value)
	{
		_UpdateSliderLabel(BlendStrengthSlider, value);
		EmitSignal("ShaderParamChanged", "blend_strength", value);
	}


	protected void _OnAmbientEnergyChanged(double value)
	{
		_UpdateSliderLabel(AmbientEnergySlider, value);
		EmitSignal("LightingParamChanged", "ambient_energy", value);
	}


	protected void _OnLightEnergyChanged(double value)
	{
		_UpdateSliderLabel(LightEnergySlider, value);
		EmitSignal("LightingParamChanged", "light_energy", value);
	}


	protected void _OnFogNearChanged(double value)
	{
		_UpdateSliderLabel(FogNearSlider, value, true);
		EmitSignal("FogParamChanged", "fog_near", value);
	}


	protected void _OnFogFarChanged(double value)
	{
		_UpdateSliderLabel(FogFarSlider, value, true);
		EmitSignal("FogParamChanged", "fog_far", value);
	}


	protected void _OnFogDensityChanged(double value)
	{
		_UpdateSliderLabel(FogDensitySlider, value);
		EmitSignal("FogParamChanged", "fog_density", value);
	}


	protected void _UpdateSliderLabel(Godot.HSlider slider, double value, bool is_int = false)
	{
		if(slider && slider.GetParent())
		{
			var parent = slider.GetParent();
			if(parent.GetChildCount() > 2)
			{
				var label = parent.GetChild(2);
				if(label)
				{
					label.Text = ( is_int ? "%d" % Int(value) : "%.2f" % value );
				}
			}
		}
	}


	protected void _OnAsyncToggled(bool enabled)
	{
		EmitSignal("AsyncToggleChanged", enabled);
	}


	//# Show generation status message
	public void ShowGenerationStatus(String message)
	{
		if(GenerationStatusLabel)
		{
			GenerationStatusLabel.Text = message;
			GenerationStatusLabel.Visible = true;
		}
	}


	//# Hide generation status message
	public void HideGenerationStatus()
	{
		if(GenerationStatusLabel)
		{
			GenerationStatusLabel.Visible = false;
			GenerationStatusLabel.Text = "";
		}
	}


	protected String _GetControlsText()
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