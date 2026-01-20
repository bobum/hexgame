using Godot;

/// <summary>
/// Camera controller with swivel/stick hierarchy for hex map.
/// Ported from Catlike Coding Hex Map Tutorial 5.
///
/// Hierarchy:
/// - HexMapCamera (this node) - position on map, handles movement
///   - Swivel (Node3D) - rotation X for tilt
///     - Stick (Node3D) - position Z for zoom, Camera3D attached
/// </summary>
public partial class HexMapCamera : Node3D
{
    [Export] public float StickMinZoom = -250f;
    [Export] public float StickMaxZoom = -45f;
    [Export] public float SwivelMinZoom = 90f;
    [Export] public float SwivelMaxZoom = 45f;
    [Export] public float MoveSpeedMinZoom = 400f;
    [Export] public float MoveSpeedMaxZoom = 100f;
    [Export] public float RotationSpeed = 180f;

    private Node3D _swivel = null!;
    private Node3D _stick = null!;
    private float _zoom = 1f;
    private float _rotationAngle;

    public override void _Ready()
    {
        _swivel = GetNode<Node3D>("Swivel");
        _stick = _swivel.GetNode<Node3D>("Stick");

        // Initialize swivel and stick to current zoom level
        AdjustZoom(0f);
    }

    public override void _Process(double delta)
    {
        float zoomDelta = Input.GetAxis("ui_page_down", "ui_page_up");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta * (float)delta);
        }

        float rotationDelta = Input.GetAxis("rotate_left", "rotate_right");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta * (float)delta);
        }

        float xDelta = Input.GetAxis("ui_left", "ui_right");
        float zDelta = Input.GetAxis("ui_up", "ui_down");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta, (float)delta);
        }
    }

    private void AdjustZoom(float delta)
    {
        _zoom = Mathf.Clamp(_zoom + delta, 0f, 1f);

        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, _zoom);
        _stick.Position = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, _zoom);
        _swivel.RotationDegrees = new Vector3(angle, 0f, 0f);
    }

    private void AdjustRotation(float delta)
    {
        _rotationAngle += delta * RotationSpeed;
        if (_rotationAngle < 0f)
        {
            _rotationAngle += 360f;
        }
        else if (_rotationAngle >= 360f)
        {
            _rotationAngle -= 360f;
        }
        RotationDegrees = new Vector3(0f, _rotationAngle, 0f);
    }

    private void AdjustPosition(float xDelta, float zDelta, float delta)
    {
        Vector3 direction = new Vector3(xDelta, 0f, zDelta).Normalized();
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, _zoom) * damping * delta;

        // Transform direction by camera rotation
        Vector3 movement = direction.Rotated(Vector3.Up, Mathf.DegToRad(_rotationAngle));
        Position += movement * distance;
    }
}
