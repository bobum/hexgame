using Godot;

/// <summary>
/// Orbital camera with mouse and keyboard controls.
/// WASD/Arrows: Pan
/// Mouse Wheel: Zoom
/// Middle Mouse + Drag: Pan
/// Right Mouse + Drag: Rotate
/// Q/E: Rotate
/// R/F: Tilt (pitch)
/// </summary>
public partial class MapCamera : Camera3D
{
    [Export] public float MinZoom { get; set; } = 10.0f;
    [Export] public float MaxZoom { get; set; } = 150.0f;
    [Export] public float MinPitch { get; set; } = 20.0f;
    [Export] public float MaxPitch { get; set; } = 85.0f;
    [Export] public float PanSpeed { get; set; } = 1.5f;
    [Export] public float RotateSpeed { get; set; } = 0.3f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float KeyboardPanSpeed { get; set; } = 200.0f;
    [Export] public float KeyboardRotateSpeed { get; set; } = 90.0f;

    // Orbital camera state
    private Vector3 _target = new Vector3(45, 0, 40);  // Center of 6x6 grid approximately
    private float _distance = 60.0f;
    private float _pitch = 45.0f;
    private float _yaw = 0.0f;

    // Target values for smooth interpolation
    private float _targetDistance = 60.0f;
    private float _targetPitch = 45.0f;
    private float _targetYaw = 0.0f;
    private Vector3 _targetPosition = new Vector3(45, 0, 40);

    // Input state
    private bool _isPanning = false;
    private bool _isRotating = false;
    private Vector2 _lastMousePos = Vector2.Zero;

    // Smoothing factor
    private float _smoothing = 10.0f;

    public override void _Ready()
    {
        _targetPosition = _target;
        Far = 300.0f;
        Current = true;
        UpdateCameraPosition();
        GD.Print("MapCamera ready - WASD to pan, mouse wheel to zoom, right-drag to rotate");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        HandleKeyboardInput(dt);
        ApplySmoothing(dt);
        UpdateCameraPosition();
    }

    public override void _Input(InputEvent @event)
    {
        // Mouse wheel zoom
        if (@event is InputEventMouseButton mb)
        {
            if (mb.Pressed)
            {
                if (mb.ButtonIndex == MouseButton.WheelUp)
                {
                    _targetDistance *= 0.9f;
                    _targetDistance = Mathf.Clamp(_targetDistance, MinZoom, MaxZoom);
                }
                else if (mb.ButtonIndex == MouseButton.WheelDown)
                {
                    _targetDistance *= 1.1f;
                    _targetDistance = Mathf.Clamp(_targetDistance, MinZoom, MaxZoom);
                }
                else if (mb.ButtonIndex == MouseButton.Middle)
                {
                    _isPanning = true;
                    _lastMousePos = mb.Position;
                }
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    _isRotating = true;
                    _lastMousePos = mb.Position;
                }
            }
            else
            {
                if (mb.ButtonIndex == MouseButton.Middle)
                    _isPanning = false;
                else if (mb.ButtonIndex == MouseButton.Right)
                    _isRotating = false;
            }
        }

        // Mouse motion for pan/rotate
        if (@event is InputEventMouseMotion mm)
        {
            if (_isPanning)
                HandlePan(mm.Relative);
            if (_isRotating)
                HandleRotate(mm.Relative);
        }
    }

    private void HandleKeyboardInput(float delta)
    {
        var moveDir = Vector3.Zero;
        float rotateDir = 0.0f;
        float pitchDir = 0.0f;

        // WASD / Arrow keys for panning
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
            moveDir.Z -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
            moveDir.Z += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
            moveDir.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
            moveDir.X += 1;

        // Q/E for rotation
        if (Input.IsKeyPressed(Key.Q))
            rotateDir -= 1;
        if (Input.IsKeyPressed(Key.E))
            rotateDir += 1;

        // R/F for tilt (pitch)
        if (Input.IsKeyPressed(Key.R))
            pitchDir -= 1;
        if (Input.IsKeyPressed(Key.F))
            pitchDir += 1;

        // Apply movement relative to camera yaw
        if (moveDir.Length() > 0)
        {
            moveDir = moveDir.Normalized();
            float yawRad = Mathf.DegToRad(_yaw);
            var forward = new Vector3(Mathf.Sin(yawRad), 0, Mathf.Cos(yawRad));
            var right = new Vector3(Mathf.Cos(yawRad), 0, -Mathf.Sin(yawRad));
            var movement = (forward * moveDir.Z + right * moveDir.X) * KeyboardPanSpeed * delta;
            _targetPosition += movement;
        }

        // Apply rotation
        if (rotateDir != 0)
            _targetYaw += rotateDir * KeyboardRotateSpeed * delta;

        // Apply pitch
        if (pitchDir != 0)
        {
            _targetPitch += pitchDir * KeyboardRotateSpeed * delta;
            _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);
        }
    }

    private void HandlePan(Vector2 delta)
    {
        float scale = _distance * PanSpeed * 0.01f;
        float yawRad = Mathf.DegToRad(_yaw);

        var forward = new Vector3(Mathf.Sin(yawRad), 0, Mathf.Cos(yawRad));
        var right = new Vector3(Mathf.Cos(yawRad), 0, -Mathf.Sin(yawRad));

        _targetPosition -= right * delta.X * scale;
        _targetPosition -= forward * delta.Y * scale;
    }

    private void HandleRotate(Vector2 delta)
    {
        _targetYaw -= delta.X * RotateSpeed;
        _targetPitch += delta.Y * RotateSpeed;
        _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);
    }

    private void ApplySmoothing(float delta)
    {
        float t = 1.0f - Mathf.Pow(0.001f, delta * _smoothing);

        _target = _target.Lerp(_targetPosition, t);
        _distance = Mathf.Lerp(_distance, _targetDistance, t);
        _pitch = Mathf.Lerp(_pitch, _targetPitch, t);
        _yaw = Mathf.Lerp(_yaw, _targetYaw, t);
    }

    private void UpdateCameraPosition()
    {
        float pitchRad = Mathf.DegToRad(_pitch);
        float yawRad = Mathf.DegToRad(_yaw);

        var offset = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad) * _distance,
            Mathf.Sin(pitchRad) * _distance,
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad) * _distance
        );

        GlobalPosition = _target + offset;
        LookAt(_target, Vector3.Up);
    }
}
