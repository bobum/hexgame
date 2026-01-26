using Godot;

/// <summary>
/// Compass UI that points to "code north" (+Z in world space).
/// This matches the HexDirection coordinate system from Catlike Coding tutorials,
/// where direction names assume +Z is north (Unity convention).
/// The arrow rotates based on camera Y rotation to maintain orientation.
/// </summary>
public partial class CompassUI : Control
{
    private Label _arrow;
    private Camera3D _camera;

    public override void _Ready()
    {
        _arrow = GetNode<Label>("Arrow");
        _camera = GetViewport().GetCamera3D();
    }

    public override void _Process(double delta)
    {
        if (_camera == null)
        {
            _camera = GetViewport().GetCamera3D();
            return;
        }

        // Get camera's Y rotation (horizontal rotation)
        // Point toward +Z ("code north") to match HexDirection naming convention
        float cameraYRotation = _camera.GlobalRotation.Y;

        // Add 180 degrees to point toward +Z instead of -Z
        float arrowRotation = Mathf.RadToDeg(cameraYRotation) + 180f;
        _arrow.RotationDegrees = arrowRotation;
    }
}
