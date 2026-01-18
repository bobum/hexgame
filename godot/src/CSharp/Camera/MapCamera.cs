namespace HexGame.Camera;

/// <summary>
/// Orbital camera with mouse and keyboard controls.
/// Direct port of map_camera.gd
/// </summary>
public partial class MapCamera : Camera3D
{
    #region Export Properties

    [Export] public float MinZoom { get; set; } = 5.0f;
    [Export] public float MaxZoom { get; set; } = 80.0f;
    [Export] public float MinPitch { get; set; } = 20.0f;
    [Export] public float MaxPitch { get; set; } = 85.0f;
    [Export] public float PanSpeed { get; set; } = 0.5f;
    [Export] public float RotateSpeed { get; set; } = 0.3f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float KeyboardPanSpeed { get; set; } = 20.0f;
    [Export] public float KeyboardRotateSpeed { get; set; } = 60.0f;

    #endregion

    #region State

    // Orbital camera state
    private Vector3 _target = Vector3.Zero;
    private float _distance = 30.0f;
    private float _pitch = 45.0f;  // Degrees
    private float _yaw = 0.0f;     // Degrees

    // Target values for smooth interpolation
    private float _targetDistance = 30.0f;
    private float _targetPitch = 45.0f;
    private float _targetYaw = 0.0f;
    private Vector3 _targetPosition = Vector3.Zero;

    // Input state
    private bool _isPanning = false;
    private bool _isRotating = false;
    private Vector2 _lastMousePos = Vector2.Zero;

    // Smoothing factor
    private float _smoothing = 10.0f;

    #endregion

    public override void _Ready()
    {
        _targetPosition = _target;
        Far = 200.0f;
        Current = true;  // Make this the active camera
        UpdateCameraPosition();
        UpdateNearPlane();
        GD.Print("MapCamera ready, set as current");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        HandleKeyboardInput(dt);
        ApplySmoothing(dt);
        UpdateCameraPosition();
        UpdateNearPlane();
    }

    private void UpdateNearPlane()
    {
        // Dynamic near plane: closer when zoomed in, farther when zoomed out
        float t = (_distance - MinZoom) / (MaxZoom - MinZoom);
        Near = Mathf.Lerp(0.5f, 4.0f, t);
    }

    public override void _Input(InputEvent @event)
    {
        // GD.Print($"MapCamera input: {@event.GetType().Name}");

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
                // Middle mouse button - start panning
                else if (mb.ButtonIndex == MouseButton.Middle)
                {
                    _isPanning = true;
                    _lastMousePos = mb.Position;
                }
                // Right mouse button - start rotating
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    _isRotating = true;
                    _lastMousePos = mb.Position;
                }
            }
            else
            {
                // Button released
                if (mb.ButtonIndex == MouseButton.Middle)
                {
                    _isPanning = false;
                }
                else if (mb.ButtonIndex == MouseButton.Right)
                {
                    _isRotating = false;
                }
            }
        }

        // Mouse motion for pan/rotate
        if (@event is InputEventMouseMotion mm)
        {
            if (_isPanning)
            {
                HandlePan(mm.Relative);
            }
            if (_isRotating)
            {
                HandleRotate(mm.Relative);
            }
        }
    }

    private void HandleKeyboardInput(float delta)
    {
        var moveDir = Vector3.Zero;
        float rotateDir = 0.0f;
        float pitchDir = 0.0f;
        float verticalDir = 0.0f;

        // WASD / Arrow keys for panning
        if (Godot.Input.IsKeyPressed(Key.W) || Godot.Input.IsKeyPressed(Key.Up))
            moveDir.Z -= 1;
        if (Godot.Input.IsKeyPressed(Key.S) || Godot.Input.IsKeyPressed(Key.Down))
            moveDir.Z += 1;
        if (Godot.Input.IsKeyPressed(Key.A) || Godot.Input.IsKeyPressed(Key.Left))
            moveDir.X -= 1;
        if (Godot.Input.IsKeyPressed(Key.D) || Godot.Input.IsKeyPressed(Key.Right))
            moveDir.X += 1;

        // Q/E for rotation
        if (Godot.Input.IsKeyPressed(Key.Q))
            rotateDir -= 1;
        if (Godot.Input.IsKeyPressed(Key.E))
            rotateDir += 1;

        // R/F for tilt (pitch)
        if (Godot.Input.IsKeyPressed(Key.R))
            pitchDir -= 1;
        if (Godot.Input.IsKeyPressed(Key.F))
            pitchDir += 1;

        // Z/X for vertical movement
        if (Godot.Input.IsKeyPressed(Key.Z))
            verticalDir += 1;
        if (Godot.Input.IsKeyPressed(Key.X))
            verticalDir -= 1;

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
        {
            _targetYaw += rotateDir * KeyboardRotateSpeed * delta;
        }

        // Apply pitch
        if (pitchDir != 0)
        {
            _targetPitch += pitchDir * KeyboardRotateSpeed * delta;
            _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);
        }

        // Apply vertical movement
        if (verticalDir != 0)
        {
            _targetPosition.Y += verticalDir * KeyboardPanSpeed * delta;
            _targetPosition.Y = Mathf.Clamp(_targetPosition.Y, 0, 30);
        }
    }

    private void HandlePan(Vector2 delta)
    {
        // Pan speed scales with distance
        float scale = _distance * PanSpeed * 0.01f;
        float yawRad = Mathf.DegToRad(_yaw);

        // Calculate movement in world space relative to camera orientation
        var forward = new Vector3(Mathf.Sin(yawRad), 0, Mathf.Cos(yawRad));
        var right = new Vector3(Mathf.Cos(yawRad), 0, -Mathf.Sin(yawRad));

        _targetPosition -= right * delta.X * scale;
        _targetPosition -= forward * delta.Y * scale;
    }

    private void HandleRotate(Vector2 delta)
    {
        // Horizontal drag rotates yaw
        _targetYaw -= delta.X * RotateSpeed;

        // Vertical drag adjusts pitch
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

        // Calculate camera position on orbital sphere around target
        var offset = new Vector3(
            Mathf.Sin(yawRad) * Mathf.Cos(pitchRad) * _distance,
            Mathf.Sin(pitchRad) * _distance,
            Mathf.Cos(yawRad) * Mathf.Cos(pitchRad) * _distance
        );

        GlobalPosition = _target + offset;
        LookAt(_target, Vector3.Up);
    }

    /// <summary>
    /// Set the camera target (what it looks at)
    /// </summary>
    public void SetTarget(Vector3 newTarget)
    {
        _targetPosition = newTarget;
        _target = newTarget;
    }

    /// <summary>
    /// Get current target position
    /// </summary>
    public Vector3 GetTarget() => _target;

    /// <summary>
    /// Focus on a specific world position
    /// </summary>
    public void FocusOn(Vector3 worldPos, float newDistance = -1)
    {
        _targetPosition = worldPos;
        if (newDistance > 0)
        {
            _targetDistance = Mathf.Clamp(newDistance, MinZoom, MaxZoom);
        }
    }
}
