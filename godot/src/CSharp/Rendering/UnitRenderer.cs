namespace HexGame.Rendering;

using Godot;
using HexGame.Core;
using HexGame.Units;

/// <summary>
/// Renders units using instanced meshes for performance.
/// Direct port of unit_renderer.gd
/// </summary>
public partial class UnitRenderer : Node3D
{
    // Player colors for land units
    private static readonly Color[] PlayerColorsLand = {
        new(0.267f, 0.533f, 1.0f),   // Player 1: Blue
        new(1.0f, 0.267f, 0.267f),   // Player 2: Red
        new(0.267f, 1.0f, 0.267f),   // Player 3: Green
        new(1.0f, 0.533f, 0.267f),   // Player 4: Orange
    };

    // Player colors for naval units (yellow tones)
    private static readonly Color[] PlayerColorsNaval = {
        new(1.0f, 1.0f, 0.267f),     // Player 1: Yellow
        new(1.0f, 0.8f, 0.0f),       // Player 2: Gold
        new(0.8f, 1.0f, 0.267f),     // Player 3: Lime Yellow
        new(1.0f, 0.667f, 0.0f),     // Player 4: Amber
    };

    // Amphibious units (cyan tones)
    private static readonly Color[] PlayerColorsAmphibious = {
        new(0.267f, 1.0f, 1.0f),     // Player 1: Cyan
        new(0.0f, 0.8f, 0.8f),       // Player 2: Teal
        new(0.267f, 0.8f, 1.0f),     // Player 3: Sky Blue
        new(0.0f, 1.0f, 0.8f),       // Player 4: Aqua
    };

    private static readonly Color SelectedColor = new(1.0f, 1.0f, 1.0f);

    private const float ChunkSize = 16.0f;
    private const float MaxRenderDistance = 50.0f;

    private IUnitManager _unitManager;
    private HexGrid? _grid;

    // Cached meshes
    private Mesh? _infantryMesh;
    private Mesh? _cavalryMesh;
    private Mesh? _archerMesh;
    private Mesh? _galleyMesh;
    private Mesh? _warshipMesh;
    private Mesh? _marineMesh;

    // Chunk storage for units
    private readonly Dictionary<string, Dictionary<UnitType, MultiMeshInstance3D>> _unitChunks = new();

    // Unit ID maps for updating colors
    private readonly Dictionary<string, Dictionary<UnitType, List<int>>> _unitIdMaps = new();

    // Selected unit IDs
    private readonly HashSet<int> _selectedUnitIds = new();

    private bool _needsRebuild = true;

    public UnitRenderer(IUnitManager unitManager)
    {
        _unitManager = unitManager;
        CreateMeshes();
    }

    public void Setup(HexGrid grid)
    {
        _grid = grid;
    }

    private void CreateMeshes()
    {
        _infantryMesh = CreateInfantryMesh();
        _cavalryMesh = CreateCavalryMesh();
        _archerMesh = CreateArcherMesh();
        _galleyMesh = CreateGalleyMesh();
        _warshipMesh = CreateWarshipMesh();
        _marineMesh = CreateMarineMesh();
    }

    /// <summary>Infantry: Simple cylinder shape</summary>
    private static Mesh CreateInfantryMesh()
    {
        return new CylinderMesh
        {
            TopRadius = 0.15f,
            BottomRadius = 0.18f,
            Height = 0.5f,
            RadialSegments = 8
        };
    }

    /// <summary>Cavalry: Box shape (horse-like)</summary>
    private static Mesh CreateCavalryMesh()
    {
        return new BoxMesh
        {
            Size = new Vector3(0.5f, 0.35f, 0.25f)
        };
    }

    /// <summary>Archer: Cone shape (pointed)</summary>
    private static Mesh CreateArcherMesh()
    {
        return new CylinderMesh
        {
            TopRadius = 0.0f,
            BottomRadius = 0.15f,
            Height = 0.5f,
            RadialSegments = 6
        };
    }

    /// <summary>Galley: Small boat (elongated box)</summary>
    private static Mesh CreateGalleyMesh()
    {
        return new BoxMesh
        {
            Size = new Vector3(0.6f, 0.2f, 0.25f)
        };
    }

    /// <summary>Warship: Larger boat</summary>
    private static Mesh CreateWarshipMesh()
    {
        return new BoxMesh
        {
            Size = new Vector3(0.7f, 0.3f, 0.35f)
        };
    }

    /// <summary>Marine: Similar to infantry but distinctive</summary>
    private static Mesh CreateMarineMesh()
    {
        return new CylinderMesh
        {
            TopRadius = 0.12f,
            BottomRadius = 0.2f,
            Height = 0.45f,
            RadialSegments = 6
        };
    }

    private Mesh GetMeshForType(UnitType type) => type switch
    {
        UnitType.Infantry => _infantryMesh!,
        UnitType.Cavalry => _cavalryMesh!,
        UnitType.Archer => _archerMesh!,
        UnitType.Galley => _galleyMesh!,
        UnitType.Warship => _warshipMesh!,
        UnitType.Marine => _marineMesh!,
        _ => _infantryMesh!
    };

    private Color[] GetColorsForType(UnitType type)
    {
        if (type.IsNaval()) return PlayerColorsNaval;
        if (type.IsAmphibious()) return PlayerColorsAmphibious;
        return PlayerColorsLand;
    }

    public void Build()
    {
        ClearMeshes();

        var units = _unitManager.GetAllUnits();

        // Group units by chunk, then by type within chunk
        var chunksByType = new Dictionary<string, Dictionary<UnitType, List<Unit>>>();

        foreach (var unit in units)
        {
            var worldPos = unit.GetWorldPosition();
            int cx = (int)Mathf.Floor(worldPos.X / ChunkSize);
            int cz = (int)Mathf.Floor(worldPos.Z / ChunkSize);
            string chunkKey = $"{cx},{cz}";

            if (!chunksByType.ContainsKey(chunkKey))
            {
                chunksByType[chunkKey] = new Dictionary<UnitType, List<Unit>>();
                foreach (UnitType unitType in Enum.GetValues(typeof(UnitType)))
                {
                    chunksByType[chunkKey][unitType] = new List<Unit>();
                }
            }

            chunksByType[chunkKey][unit.UnitType].Add(unit);
        }

        // Build MultiMesh for each chunk and type
        foreach (var chunkKey in chunksByType.Keys)
        {
            _unitChunks[chunkKey] = new Dictionary<UnitType, MultiMeshInstance3D>();
            _unitIdMaps[chunkKey] = new Dictionary<UnitType, List<int>>();
            var chunkTypes = chunksByType[chunkKey];

            CreateChunkTypeMultimesh(chunkKey, UnitType.Infantry, chunkTypes[UnitType.Infantry]);
            CreateChunkTypeMultimesh(chunkKey, UnitType.Cavalry, chunkTypes[UnitType.Cavalry]);
            CreateChunkTypeMultimesh(chunkKey, UnitType.Archer, chunkTypes[UnitType.Archer]);
            CreateChunkTypeMultimesh(chunkKey, UnitType.Galley, chunkTypes[UnitType.Galley]);
            CreateChunkTypeMultimesh(chunkKey, UnitType.Warship, chunkTypes[UnitType.Warship]);
            CreateChunkTypeMultimesh(chunkKey, UnitType.Marine, chunkTypes[UnitType.Marine]);
        }

        _needsRebuild = false;
        GD.Print($"UnitRenderer: Built {units.Count} unit instances");
    }

    private void CreateChunkTypeMultimesh(string chunkKey, UnitType unitType, List<Unit> units)
    {
        if (units.Count == 0)
            return;

        var mesh = GetMeshForType(unitType);
        var colors = GetColorsForType(unitType);

        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = units.Count
        };

        var unitIds = new List<int>();

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            unitIds.Add(unit.Id);

            var cell = _grid?.GetCell(unit.Q, unit.R);
            var worldPos = unit.GetWorldPosition();
            int elevation = cell?.Elevation ?? 0;

            // Naval units float on water surface, land units on terrain
            bool isOnWater = cell != null && cell.Elevation < HexMetrics.SeaLevel;
            if (unitType.IsNaval() || (unitType.IsAmphibious() && isOnWater))
            {
                worldPos.Y = HexMetrics.SeaLevel * HexMetrics.ElevationStep + 0.1f;
            }
            else
            {
                worldPos.Y = elevation * HexMetrics.ElevationStep + 0.25f;
            }

            var transform = Transform3D.Identity;
            transform.Origin = worldPos;
            multimesh.SetInstanceTransform(i, transform);

            var color = colors[unit.PlayerId % colors.Length];
            if (_selectedUnitIds.Contains(unit.Id))
            {
                color = SelectedColor;
            }
            multimesh.SetInstanceColor(i, color);
        }

        _unitIdMaps[chunkKey][unitType] = unitIds;

        var instance = new MultiMeshInstance3D
        {
            Multimesh = multimesh
        };

        var material = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex
        };
        instance.MaterialOverride = material;

        AddChild(instance);
        _unitChunks[chunkKey][unitType] = instance;
    }

    private void ClearMeshes()
    {
        foreach (var chunkMeshes in _unitChunks.Values)
        {
            foreach (var mm in chunkMeshes.Values)
            {
                mm?.QueueFree();
            }
        }
        _unitChunks.Clear();
        _unitIdMaps.Clear();
    }

    public void SetSelectedUnits(int[] ids)
    {
        _selectedUnitIds.Clear();
        foreach (var id in ids)
        {
            _selectedUnitIds.Add(id);
        }
        ApplySelectionColors();
    }

    private void ApplySelectionColors()
    {
        foreach (var chunkKey in _unitChunks.Keys)
        {
            if (!_unitIdMaps.ContainsKey(chunkKey)) continue;

            foreach (UnitType unitType in Enum.GetValues(typeof(UnitType)))
            {
                if (!_unitChunks[chunkKey].TryGetValue(unitType, out var instance)) continue;
                if (!_unitIdMaps[chunkKey].TryGetValue(unitType, out var unitIds)) continue;
                if (instance?.Multimesh == null) continue;

                var colors = GetColorsForType(unitType);
                var mm = instance.Multimesh;

                for (int i = 0; i < unitIds.Count; i++)
                {
                    var unitId = unitIds[i];
                    var unit = _unitManager.GetUnit(unitId);
                    if (unit == null) continue;

                    Color color;
                    if (_selectedUnitIds.Contains(unitId))
                    {
                        color = SelectedColor;
                    }
                    else
                    {
                        color = colors[unit.PlayerId % colors.Length];
                    }

                    mm.SetInstanceColor(i, color);
                }
            }
        }
    }

    public void Update()
    {
        if (_needsRebuild)
        {
            Build();
        }
    }

    public void UpdateVisibility(Camera3D camera)
    {
        if (camera == null)
            return;

        var cameraPos = camera.GlobalPosition;
        var cameraXz = new Vector3(cameraPos.X, 0, cameraPos.Z);
        float maxDistSq = MaxRenderDistance * MaxRenderDistance;

        foreach (var chunkKey in _unitChunks.Keys)
        {
            var parts = chunkKey.Split(',');
            int cx = int.Parse(parts[0]);
            int cz = int.Parse(parts[1]);
            float centerX = (cx + 0.5f) * ChunkSize;
            float centerZ = (cz + 0.5f) * ChunkSize;

            float dx = centerX - cameraXz.X;
            float dz = centerZ - cameraXz.Z;
            bool visible = (dx * dx + dz * dz) <= maxDistSq;

            foreach (var mm in _unitChunks[chunkKey].Values)
            {
                if (mm != null)
                {
                    mm.Visible = visible;
                }
            }
        }
    }

    public void MarkDirty()
    {
        _needsRebuild = true;
    }
}
