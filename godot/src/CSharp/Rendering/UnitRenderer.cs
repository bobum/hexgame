using HexGame.Core;
using HexGame.Units;
using HexGame.Utilities;

namespace HexGame.Rendering;

/// <summary>
/// Renders units on the hex grid.
/// Manages unit visual representations with player colors and selection highlighting.
/// </summary>
public partial class UnitRenderer : RendererBase
{
    private readonly IUnitManager _unitManager;
    private readonly Dictionary<int, UnitVisual> _unitVisuals = new();
    private readonly ObjectPool<UnitVisual> _visualPool;
    private int? _selectedUnitId;

    /// <summary>
    /// Height offset for units above terrain.
    /// </summary>
    public float UnitHeightOffset { get; set; } = 0.1f;

    /// <summary>
    /// Scale for unit models.
    /// </summary>
    public float UnitScale { get; set; } = 0.5f;

    /// <summary>
    /// Creates a new unit renderer.
    /// </summary>
    /// <param name="unitManager">The unit manager to render units from.</param>
    public UnitRenderer(IUnitManager unitManager)
    {
        _unitManager = unitManager;
        _visualPool = new ObjectPool<UnitVisual>(ResetVisual);
    }

    /// <summary>
    /// Builds unit visuals for all existing units.
    /// </summary>
    protected override void DoBuild()
    {
        foreach (var unit in _unitManager.GetAllUnits())
        {
            CreateVisualForUnit(unit);
        }

        GD.Print($"UnitRenderer: Created {_unitVisuals.Count} unit visuals");
    }

    /// <summary>
    /// Updates unit positions and animations.
    /// </summary>
    public override void Update(double delta)
    {
        // Update visual positions for any units that moved
        foreach (var (unitId, visual) in _unitVisuals)
        {
            var unit = _unitManager.GetUnit(unitId);
            if (unit != null && visual.Node != null)
            {
                var targetPos = GetUnitWorldPosition(unit);
                visual.Node.GlobalPosition = visual.Node.GlobalPosition.Lerp(targetPos, (float)(delta * 10.0));
            }
        }
    }

    /// <summary>
    /// Updates visibility based on camera distance.
    /// </summary>
    public override void UpdateVisibility(Camera3D camera)
    {
        if (camera == null) return;

        var cameraPos = camera.GlobalPosition;
        float maxDist = RenderingConfig.UnitRenderDistance;

        foreach (var visual in _unitVisuals.Values)
        {
            if (visual.Node != null)
            {
                float distance = cameraPos.DistanceTo(visual.Node.GlobalPosition);
                visual.Node.Visible = distance < maxDist;
            }
        }
    }

    /// <summary>
    /// Adds a visual for a newly created unit.
    /// </summary>
    public void AddUnit(Unit unit)
    {
        if (!_unitVisuals.ContainsKey(unit.Id))
        {
            CreateVisualForUnit(unit);
        }
    }

    /// <summary>
    /// Removes a unit's visual.
    /// </summary>
    public void RemoveUnit(int unitId)
    {
        if (_unitVisuals.TryGetValue(unitId, out var visual))
        {
            if (visual.Node != null)
            {
                visual.Node.QueueFree();
            }
            _visualPool.Release(visual);
            _unitVisuals.Remove(unitId);
        }
    }

    /// <summary>
    /// Updates a unit's visual after it moved.
    /// </summary>
    public void UpdateUnit(Unit unit)
    {
        if (_unitVisuals.TryGetValue(unit.Id, out var visual) && visual.Node != null)
        {
            visual.Node.GlobalPosition = GetUnitWorldPosition(unit);
        }
    }

    /// <summary>
    /// Sets the selected unit for highlighting.
    /// </summary>
    public void SetSelectedUnit(int? unitId)
    {
        // Deselect previous
        if (_selectedUnitId.HasValue && _unitVisuals.TryGetValue(_selectedUnitId.Value, out var prevVisual))
        {
            prevVisual.SetHighlighted(false);
        }

        _selectedUnitId = unitId;

        // Select new
        if (unitId.HasValue && _unitVisuals.TryGetValue(unitId.Value, out var newVisual))
        {
            newVisual.SetHighlighted(true);
        }
    }

    private void CreateVisualForUnit(Unit unit)
    {
        var visual = _visualPool.Acquire();
        visual.Initialize(unit, UnitScale);

        var node = visual.Node;
        if (node != null)
        {
            node.GlobalPosition = GetUnitWorldPosition(unit);
            AddChild(node);
        }

        _unitVisuals[unit.Id] = visual;
    }

    private Vector3 GetUnitWorldPosition(Unit unit)
    {
        float elevation = unit.Cell?.Elevation ?? 0;
        float height = elevation * HexMetrics.ElevationStep + UnitHeightOffset;
        return new HexCoordinates(unit.Q, unit.R).ToWorldPosition(height);
    }

    private void ResetVisual(UnitVisual visual)
    {
        visual.Reset();
    }

    public override void Cleanup()
    {
        foreach (var visual in _unitVisuals.Values)
        {
            visual.Node?.QueueFree();
        }
        _unitVisuals.Clear();
        _visualPool.Clear();
        base.Cleanup();
    }
}

/// <summary>
/// Visual representation of a single unit.
/// </summary>
public partial class UnitVisual : Node3D, IPoolable
{
    private MeshInstance3D? _mesh;
    private MeshInstance3D? _selectionRing;
    private StandardMaterial3D? _material;
    private int _playerId;

    /// <summary>
    /// Gets the node for this visual.
    /// </summary>
    public Node3D? Node => this;

    /// <summary>
    /// Initializes the visual for a specific unit.
    /// </summary>
    public void Initialize(Unit unit, float scale)
    {
        _playerId = unit.PlayerId;

        if (_mesh == null)
        {
            CreateMesh(scale);
        }

        UpdateAppearance(unit);
    }

    private void CreateMesh(float scale)
    {
        // Create unit mesh (simple cylinder for now)
        var capsule = new CapsuleMesh
        {
            Radius = 0.3f * scale,
            Height = 0.8f * scale
        };

        _material = new StandardMaterial3D
        {
            AlbedoColor = Colors.White
        };

        _mesh = new MeshInstance3D
        {
            Mesh = capsule,
            MaterialOverride = _material
        };
        _mesh.Position = new Vector3(0, capsule.Height / 2, 0);
        AddChild(_mesh);

        // Create selection ring
        var torusMesh = new TorusMesh
        {
            InnerRadius = 0.4f * scale,
            OuterRadius = 0.5f * scale
        };

        var selectionMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 1f, 0f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        _selectionRing = new MeshInstance3D
        {
            Mesh = torusMesh,
            MaterialOverride = selectionMaterial,
            Visible = false
        };
        _selectionRing.Position = new Vector3(0, 0.05f, 0);
        _selectionRing.RotateX(Mathf.Pi / 2);
        AddChild(_selectionRing);
    }

    private void UpdateAppearance(Unit unit)
    {
        if (_material != null)
        {
            _material.AlbedoColor = RenderingConfig.GetPlayerColor(unit.PlayerId);
        }
    }

    /// <summary>
    /// Sets the highlight state for selection.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (_selectionRing != null)
        {
            _selectionRing.Visible = highlighted;
        }
    }

    /// <summary>
    /// Resets the visual for pooling.
    /// </summary>
    public void Reset()
    {
        SetHighlighted(false);
        _playerId = 0;
    }

    /// <summary>
    /// Called when acquired from pool.
    /// </summary>
    public void OnAcquire() { }

    /// <summary>
    /// Called when released to pool.
    /// </summary>
    public void OnRelease()
    {
        Reset();
    }
}
