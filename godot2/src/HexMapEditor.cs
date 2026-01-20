using Godot;

/// <summary>
/// Handles mouse input for editing hex cells.
/// Ported from Catlike Coding Hex Map Tutorial 5.
/// Uses a collision plane for raycasting.
/// </summary>
public partial class HexMapEditor : Node3D
{
    [Export] public NodePath HexGridPath = null!;
    [Export] public Color[] Colors = null!;

    private HexGrid _hexGrid = null!;

    private int _activeColorIndex;
    private int _activeElevation;
    private bool _applyColor;
    private bool _applyElevation = true;

    private Camera3D? _camera;
    private StaticBody3D _groundPlane = null!;
    private CollisionShape3D _groundShape = null!;

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

        // Default colors if not set
        if (Colors == null || Colors.Length == 0)
        {
            Colors = new Color[]
            {
                new Color(1f, 1f, 0f),  // Yellow
                new Color(0f, 1f, 0f),  // Green
                new Color(0f, 0f, 1f),  // Blue
                new Color(1f, 1f, 1f),  // White
            };
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                HandleInput();
            }
        }
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

        if (_applyColor)
        {
            cell.Color = Colors[_activeColorIndex];
        }
        if (_applyElevation)
        {
            cell.Elevation = _activeElevation;
        }
    }

    public void SelectColor(int index)
    {
        _applyColor = index >= 0;
        if (_applyColor)
        {
            _activeColorIndex = index;
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
}
