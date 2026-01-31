using Godot;
using HexGame.Generation;
using HexGame.Region;

/// <summary>
/// Handles mouse input for editing hex cells.
/// Ported from Catlike Coding Hex Map Tutorial 5.
/// Tutorial 14: Updated to use terrain type indices instead of colors.
/// Uses a collision plane for raycasting.
///
/// Keyboard controls:
/// - Spacebar: Generate new random map
/// - G: Regenerate map with same seed (for debugging)
/// - N: Create new test world (Region System)
/// - M: Open world map (Region System)
/// - T: Add test region to world (Region System)
/// - L: List saved regions (Region System)
/// </summary>
public partial class HexMapEditor : Node3D
{
    [Export] public NodePath HexGridPath = null!;
    [Export] public int TerrainTypeCount = 5;

    private HexGrid _hexGrid = null!;

    private int _activeTerrainTypeIndex;
    private int _activeElevation;
    private bool _applyTerrainType;
    private bool _applyElevation = true;

    private Camera3D? _camera;
    private StaticBody3D _groundPlane = null!;
    private CollisionShape3D _groundShape = null!;

    // Map generation
    private MapGenerator? _mapGenerator;
    private int _lastSeed;

    // Region System
    private RegionSystemController? _regionSystem;

    public override void _Ready()
    {
        _hexGrid = GetNode<HexGrid>(HexGridPath);

        // Create ground plane for raycasting
        _groundPlane = new StaticBody3D();
        _groundPlane.Name = "GroundPlane";
        AddChild(_groundPlane);

        _groundShape = new CollisionShape3D();
        var shape = new WorldBoundaryShape3D();
        shape.Plane = new Plane(Vector3.Up, 0f);
        _groundShape.Shape = shape;
        _groundPlane.AddChild(_groundShape);

        // Initialize map generator
        _mapGenerator = new MapGenerator();
        _mapGenerator.GenerationStarted += () => GD.Print("Map generation started...");
        _mapGenerator.GenerationProgress += (stage, progress) => GD.Print($"  {stage}: {progress:P0}");
        _mapGenerator.GenerationCompleted += (success) => GD.Print($"Map generation {(success ? "completed" : "failed")}");

        // Initialize Region System
        _regionSystem = new RegionSystemController { Name = "RegionSystem" };
        AddChild(_regionSystem);
        _regionSystem.Initialize(_hexGrid, "user://saves/test_world.world");
        _regionSystem.RegionChanged += (id, name) => GD.Print($"[Region] Changed to: {name}");
        GD.Print("[HexMapEditor] Region System initialized. Press N to create world, M to open map.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Handle keyboard input for map generation
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.G:
                    // Regenerate with same seed (for debugging)
                    GenerateMap(_lastSeed);
                    break;
                case Key.N:
                    // Create new test world (Region System)
                    CreateTestWorld();
                    break;
                case Key.T:
                    // Add second test region
                    AddTestRegion();
                    break;
                case Key.L:
                    // List saved regions
                    ListRegions();
                    break;
                // Note: M is handled by RegionSystemController to toggle the map
            }
        }

        // Tutorial 6: Disabled mouse interaction for programmatic-only testing
        // Uncomment to re-enable editing:
        // if (@event is InputEventMouseButton mb && mb.Pressed)
        // {
        //     if (mb.ButtonIndex == MouseButton.Left)
        //     {
        //         HandleInput();
        //     }
        // }
    }

    private void GenerateMap(int seed)
    {
        if (_mapGenerator == null || _mapGenerator.IsGenerating)
            return;

        // If seed is 0, a random seed will be used
        _lastSeed = seed != 0 ? seed : (int)GD.Randi();

        GD.Print($"Generating map with seed: {_lastSeed}");
        _mapGenerator.Generate(_hexGrid, _lastSeed);
    }

    private void HandleInput()
    {
        _camera ??= GetViewport().GetCamera3D();
        if (_camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var to = from + _camera.ProjectRayNormal(mousePos) * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 position = (Vector3)result["position"];
            EditCell(_hexGrid.GetCell(position));
        }
    }

    private void EditCell(HexCell? cell)
    {
        if (cell == null) return;

        if (_applyTerrainType)
        {
            cell.TerrainTypeIndex = _activeTerrainTypeIndex;
        }
        if (_applyElevation)
        {
            cell.Elevation = _activeElevation;
        }
    }

    public void SelectTerrainType(int index)
    {
        _applyTerrainType = index >= 0;
        if (_applyTerrainType)
        {
            _activeTerrainTypeIndex = index;
        }
    }

    public void SetElevation(int elevation)
    {
        _activeElevation = elevation;
    }

    public void SetApplyElevation(bool toggle)
    {
        _applyElevation = toggle;
    }

    #region Region System Testing

    private async void CreateTestWorld()
    {
        if (_regionSystem == null) return;

        GD.Print("[Test] Creating new test world...");
        var success = await _regionSystem.CreateNewWorldAsync(
            "Test Caribbean",
            "Port Royal",
            _hexGrid.CellCountX,
            _hexGrid.CellCountZ,
            (int)GD.Randi()
        );

        if (success)
        {
            GD.Print("[Test] World created! Press M to open the world map.");
        }
        else
        {
            GD.PrintErr("[Test] Failed to create world.");
        }
    }

    private async void AddTestRegion()
    {
        if (_regionSystem?.WorldMap == null)
        {
            GD.Print("[Test] No world exists. Press N first to create a world.");
            return;
        }

        // Get current region to connect to
        var currentRegion = _regionSystem.WorldMap.GetCurrentRegion();
        if (currentRegion == null)
        {
            GD.Print("[Test] No current region found.");
            return;
        }

        // Pick random biome and position
        var biomes = new[] { RegionBiome.Tropical, RegionBiome.Desert, RegionBiome.Volcanic, RegionBiome.Coastal };
        var names = new[] { "Tortuga", "Isla Cruces", "Devil's Triangle", "Nassau", "Havana", "Kingston" };
        var rng = new System.Random();

        var name = names[rng.Next(names.Length)] + " " + rng.Next(100);
        var biome = biomes[rng.Next(biomes.Length)];
        var mapX = currentRegion.MapX + rng.Next(-200, 200);
        var mapY = currentRegion.MapY + rng.Next(-200, 200);
        var difficulty = rng.Next(1, 5);
        var travelTime = 30f + rng.Next(120);

        GD.Print($"[Test] Adding region '{name}' ({biome})...");

        var entry = await _regionSystem.AddRegionToWorldAsync(
            name, mapX, mapY, biome, difficulty,
            seed: (int)GD.Randi(),
            connectTo: currentRegion.RegionId,
            travelTime: travelTime
        );

        if (entry != null)
        {
            GD.Print($"[Test] Added region '{name}'. Press M to see it on the map.");
        }
        else
        {
            GD.PrintErr("[Test] Failed to add region.");
        }
    }

    private void ListRegions()
    {
        if (_regionSystem?.WorldMap == null)
        {
            GD.Print("[Test] No world loaded. Press N to create one.");
            return;
        }

        var world = _regionSystem.WorldMap;
        GD.Print($"\n=== World: {world.WorldName} ===");
        GD.Print($"Regions: {world.Regions.Count}");

        foreach (var region in world.Regions)
        {
            var current = region.RegionId == world.CurrentRegionId ? " [CURRENT]" : "";
            var discovered = region.IsDiscovered ? "" : " (undiscovered)";
            GD.Print($"  - {region.Name}: {region.PrimaryBiome}, Diff {region.DifficultyRating}{current}{discovered}");

            foreach (var connId in region.ConnectedRegionIds)
            {
                var conn = world.GetRegionById(connId);
                var info = region.ConnectionDetails.Find(c => c.TargetRegionId == connId);
                if (conn != null && info != null)
                {
                    GD.Print($"      -> {conn.Name} ({info.TravelTimeMinutes:F0}min)");
                }
            }
        }
        GD.Print("");
    }

    #endregion
}
