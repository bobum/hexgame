using HexGame.Core;
using HexGame.Pathfinding;

namespace HexGame.Rendering;

/// <summary>
/// Renders selection highlights, reachable cells, and path previews.
/// </summary>
public partial class HighlightRenderer : RendererBase
{
    private MeshInstance3D? _selectionHighlight;
    private MeshInstance3D? _hoverHighlight;
    private Node3D? _reachableContainer;
    private Node3D? _pathContainer;

    private readonly Dictionary<(int Q, int R), MeshInstance3D> _reachableHighlights = new();
    private readonly List<MeshInstance3D> _pathHighlights = new();

    private StandardMaterial3D? _selectionMaterial;
    private StandardMaterial3D? _hoverMaterial;
    private StandardMaterial3D? _reachableMaterial;
    private StandardMaterial3D? _pathMaterial;

    /// <summary>
    /// Color for selected cell/unit highlight.
    /// </summary>
    public Color SelectionColor { get; set; } = new(1f, 1f, 0f, 0.5f);

    /// <summary>
    /// Color for hovered cell highlight.
    /// </summary>
    public Color HoverColor { get; set; } = new(1f, 1f, 1f, 0.3f);

    /// <summary>
    /// Color for reachable cells.
    /// </summary>
    public Color ReachableColor { get; set; } = new(0f, 0.8f, 0.2f, 0.3f);

    /// <summary>
    /// Color for path preview.
    /// </summary>
    public Color PathColor { get; set; } = new(0f, 0.5f, 1f, 0.5f);

    /// <summary>
    /// Height offset for highlights above terrain.
    /// </summary>
    public float HighlightOffset { get; set; } = 0.05f;

    protected override void DoBuild()
    {
        CreateMaterials();
        CreateHighlightMeshes();

        _reachableContainer = new Node3D { Name = "ReachableHighlights" };
        AddChild(_reachableContainer);

        _pathContainer = new Node3D { Name = "PathHighlights" };
        AddChild(_pathContainer);
    }

    private void CreateMaterials()
    {
        _selectionMaterial = CreateHighlightMaterial(SelectionColor);
        _hoverMaterial = CreateHighlightMaterial(HoverColor);
        _reachableMaterial = CreateHighlightMaterial(ReachableColor);
        _pathMaterial = CreateHighlightMaterial(PathColor);
    }

    private static StandardMaterial3D CreateHighlightMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled
        };
    }

    private void CreateHighlightMeshes()
    {
        var hexMesh = CreateHexMesh();

        _selectionHighlight = new MeshInstance3D
        {
            Mesh = hexMesh,
            MaterialOverride = _selectionMaterial,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_selectionHighlight);

        _hoverHighlight = new MeshInstance3D
        {
            Mesh = hexMesh,
            MaterialOverride = _hoverMaterial,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_hoverHighlight);
    }

    private static ArrayMesh CreateHexMesh()
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var center = Vector3.Zero;
        var corners = HexMetrics.GetCorners();

        for (int i = 0; i < 6; i++)
        {
            int nextI = (i + 1) % 6;
            surfaceTool.AddVertex(center);
            surfaceTool.AddVertex(corners[i]);
            surfaceTool.AddVertex(corners[nextI]);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    #region Selection Highlight

    /// <summary>
    /// Shows selection highlight at a cell.
    /// </summary>
    public void ShowSelection(HexCell? cell)
    {
        if (_selectionHighlight == null) return;

        if (cell == null)
        {
            _selectionHighlight.Visible = false;
            return;
        }

        var pos = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep + HighlightOffset);
        _selectionHighlight.GlobalPosition = pos;
        _selectionHighlight.Visible = true;
    }

    /// <summary>
    /// Hides the selection highlight.
    /// </summary>
    public void HideSelection()
    {
        if (_selectionHighlight != null)
        {
            _selectionHighlight.Visible = false;
        }
    }

    #endregion

    #region Hover Highlight

    /// <summary>
    /// Shows hover highlight at a cell.
    /// </summary>
    public void ShowHover(HexCell? cell)
    {
        if (_hoverHighlight == null) return;

        if (cell == null)
        {
            _hoverHighlight.Visible = false;
            return;
        }

        var pos = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep + HighlightOffset);
        _hoverHighlight.GlobalPosition = pos;
        _hoverHighlight.Visible = true;
    }

    /// <summary>
    /// Hides the hover highlight.
    /// </summary>
    public void HideHover()
    {
        if (_hoverHighlight != null)
        {
            _hoverHighlight.Visible = false;
        }
    }

    #endregion

    #region Reachable Cells

    /// <summary>
    /// Shows highlights for reachable cells.
    /// </summary>
    public void ShowReachableCells(IReadOnlyDictionary<HexCell, float>? cells)
    {
        ClearReachableCells();

        if (cells == null || _reachableContainer == null) return;

        var hexMesh = CreateHexMesh();

        foreach (var (cell, cost) in cells)
        {
            var highlight = new MeshInstance3D
            {
                Mesh = hexMesh,
                MaterialOverride = _reachableMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };

            var pos = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep + HighlightOffset);
            highlight.GlobalPosition = pos;

            _reachableContainer.AddChild(highlight);
            _reachableHighlights[(cell.Q, cell.R)] = highlight;
        }
    }

    /// <summary>
    /// Clears all reachable cell highlights.
    /// </summary>
    public void ClearReachableCells()
    {
        foreach (var highlight in _reachableHighlights.Values)
        {
            highlight.QueueFree();
        }
        _reachableHighlights.Clear();
    }

    #endregion

    #region Path Preview

    /// <summary>
    /// Shows a path preview.
    /// </summary>
    public void ShowPath(PathResult? pathResult)
    {
        ClearPath();

        if (pathResult == null || !pathResult.Reachable || _pathContainer == null) return;

        var hexMesh = CreateHexMesh();

        foreach (var cell in pathResult.Path)
        {
            var highlight = new MeshInstance3D
            {
                Mesh = hexMesh,
                MaterialOverride = _pathMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };

            var pos = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep + HighlightOffset + 0.01f);
            highlight.GlobalPosition = pos;

            _pathContainer.AddChild(highlight);
            _pathHighlights.Add(highlight);
        }
    }

    /// <summary>
    /// Clears the path preview.
    /// </summary>
    public void ClearPath()
    {
        foreach (var highlight in _pathHighlights)
        {
            highlight.QueueFree();
        }
        _pathHighlights.Clear();
    }

    #endregion

    public override void Cleanup()
    {
        ClearReachableCells();
        ClearPath();

        _selectionMaterial?.Dispose();
        _hoverMaterial?.Dispose();
        _reachableMaterial?.Dispose();
        _pathMaterial?.Dispose();

        _selectionMaterial = null;
        _hoverMaterial = null;
        _reachableMaterial = null;
        _pathMaterial = null;

        base.Cleanup();
    }
}
