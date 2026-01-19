namespace HexGame.Interaction;

using HexGame.Core;

/// <summary>
/// Handles hex hover detection and highlighting.
/// Direct port of hex_hover.gd
/// </summary>
public partial class HexHover : Node3D
{
    private static readonly Color HighlightColor = new(1.0f, 0.9f, 0.2f, 0.8f);
    private const float HighlightHeight = 0.1f;
    private const float RingWidth = 0.08f;

    private MeshInstance3D? _highlightMesh;
    private HexCell? _currentCell;
    private HexGrid? _grid;
    private Camera3D? _camera;

    // C# events instead of Godot signals (can't use custom types in Godot signals)
    public event Action<HexCell>? CellHovered;
    public event Action? CellUnhovered;

    public override void _Ready()
    {
        CreateHighlightMesh();
    }

    public void Setup(HexGrid grid, Camera3D camera)
    {
        _grid = grid;
        _camera = camera;
    }

    private void CreateHighlightMesh()
    {
        var mesh = BuildHexRingMesh();
        _highlightMesh = new MeshInstance3D
        {
            Mesh = mesh
        };

        var material = new StandardMaterial3D
        {
            AlbedoColor = HighlightColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        _highlightMesh.MaterialOverride = material;
        _highlightMesh.Visible = false;

        AddChild(_highlightMesh);
    }

    private ArrayMesh BuildHexRingMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        var corners = HexMetrics.GetCorners();
        float innerScale = 1.0f - RingWidth;

        for (int i = 0; i < 6; i++)
        {
            var c1 = corners[i];
            var c2 = corners[(i + 1) % 6];

            // Outer corners
            var outer1 = new Vector3(c1.X, 0, c1.Z);
            var outer2 = new Vector3(c2.X, 0, c2.Z);

            // Inner corners (scaled down)
            var inner1 = new Vector3(c1.X * innerScale, 0, c1.Z * innerScale);
            var inner2 = new Vector3(c2.X * innerScale, 0, c2.Z * innerScale);

            // Build quad for this edge of the ring
            st.SetNormal(Vector3.Up);
            st.AddVertex(outer1);
            st.AddVertex(inner1);
            st.AddVertex(outer2);

            st.AddVertex(outer2);
            st.AddVertex(inner1);
            st.AddVertex(inner2);
        }

        return st.Commit();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            UpdateHover();
        }
    }

    private void UpdateHover()
    {
        if (_camera == null || _grid == null)
            return;

        var viewport = GetViewport();
        if (viewport == null) return;

        var mousePos = viewport.GetMousePosition();
        var rayOrigin = _camera.ProjectRayOrigin(mousePos);
        var rayDir = _camera.ProjectRayNormal(mousePos);

        var cell = RaycastToHex(rayOrigin, rayDir);

        if (cell != _currentCell)
        {
            _currentCell = cell;
            if (cell != null)
            {
                ShowHighlight(cell);
                CellHovered?.Invoke(cell);
            }
            else
            {
                HideHighlight();
                CellUnhovered?.Invoke();
            }
        }
    }

    private HexCell? RaycastToHex(Vector3 origin, Vector3 direction)
    {
        // Use physics raycasting against terrain mesh (like Three.js intersectObjects)
        var spaceState = GetWorld3D().DirectSpaceState;
        var rayEnd = origin + direction * 1000.0f;

        var query = PhysicsRayQueryParameters3D.Create(origin, rayEnd);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            var hitPoint = (Vector3)result["position"];
            return GetCellAtPosition(hitPoint);
        }

        // Fallback to sea level plane if no physics hit
        if (Mathf.Abs(direction.Y) < 0.001f)
            return null;

        float seaLevelY = HexMetrics.SeaLevel * HexMetrics.ElevationStep;
        float t = (seaLevelY - origin.Y) / direction.Y;

        if (t <= 0)
            return null;

        var fallbackHit = origin + direction * t;
        return GetCellAtPosition(fallbackHit);
    }

    private HexCell? GetCellAtPosition(Vector3 worldPos)
    {
        var coords = HexCoordinates.FromWorldPosition(worldPos);
        return _grid?.GetCell(coords.Q, coords.R);
    }

    private void ShowHighlight(HexCell cell)
    {
        if (_highlightMesh == null) return;

        var worldPos = cell.GetWorldPosition();
        _highlightMesh.Position = new Vector3(worldPos.X, worldPos.Y + HighlightHeight, worldPos.Z);
        _highlightMesh.Visible = true;
    }

    private void HideHighlight()
    {
        if (_highlightMesh == null) return;

        _highlightMesh.Visible = false;
        _currentCell = null;
    }

    public HexCell? GetHoveredCell() => _currentCell;
}
