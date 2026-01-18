using HexGame.Core;

namespace HexGame.Input;

/// <summary>
/// Controls camera movement, rotation, and zoom for the strategy game view.
/// Supports panning, zooming, and rotation around a focus point.
/// </summary>
public partial class CameraController : Node3D
{
    #region Export Properties

    /// <summary>
    /// Camera pan speed in units per second.
    /// </summary>
    [Export]
    public float PanSpeed { get; set; } = 20f;

    /// <summary>
    /// Camera zoom speed.
    /// </summary>
    [Export]
    public float ZoomSpeed { get; set; } = 2f;

    /// <summary>
    /// Camera rotation speed in radians per second.
    /// </summary>
    [Export]
    public float RotationSpeed { get; set; } = 1f;

    /// <summary>
    /// Minimum zoom distance.
    /// </summary>
    [Export]
    public float MinZoom { get; set; } = 5f;

    /// <summary>
    /// Maximum zoom distance.
    /// </summary>
    [Export]
    public float MaxZoom { get; set; } = 50f;

    /// <summary>
    /// Minimum pitch angle in degrees.
    /// </summary>
    [Export]
    public float MinPitch { get; set; } = 20f;

    /// <summary>
    /// Maximum pitch angle in degrees.
    /// </summary>
    [Export]
    public float MaxPitch { get; set; } = 80f;

    /// <summary>
    /// Smoothing factor for camera movement (0 = instant, 1 = very smooth).
    /// </summary>
    [Export]
    public float Smoothing { get; set; } = 0.9f;

    #endregion

    #region State

    private Camera3D? _camera;
    private Vector3 _targetPosition;
    private float _targetZoom;
    private float _targetRotation;
    private float _targetPitch;
    private float _currentZoom;
    private float _currentRotation;
    private float _currentPitch;

    private Vector2? _mapBoundsMin;
    private Vector2? _mapBoundsMax;

    /// <summary>
    /// Gets the camera node.
    /// </summary>
    public Camera3D? Camera => _camera;

    /// <summary>
    /// Gets the current focus position.
    /// </summary>
    public Vector3 FocusPosition => _targetPosition;

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public float CurrentZoom => _currentZoom;

    #endregion

    public override void _Ready()
    {
        // Find or create camera
        _camera = GetNodeOrNull<Camera3D>("Camera3D");
        if (_camera == null)
        {
            _camera = new Camera3D();
            AddChild(_camera);
        }

        // Initialize to defaults
        _targetZoom = (MinZoom + MaxZoom) / 2f;
        _currentZoom = _targetZoom;
        _targetPitch = Mathf.DegToRad((MinPitch + MaxPitch) / 2f);
        _currentPitch = _targetPitch;
        _targetRotation = 0f;
        _currentRotation = 0f;
        _targetPosition = Vector3.Zero;

        UpdateCameraPosition();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Smooth interpolation
        float lerpFactor = 1f - Mathf.Pow(Smoothing, dt * 60f);

        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, lerpFactor);
        _currentRotation = Mathf.LerpAngle(_currentRotation, _targetRotation, lerpFactor);
        _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, lerpFactor);
        GlobalPosition = GlobalPosition.Lerp(_targetPosition, lerpFactor);

        UpdateCameraPosition();
    }

    #region Camera Control

    /// <summary>
    /// Pans the camera by a screen-space delta.
    /// </summary>
    public void Pan(Vector2 screenDelta)
    {
        // Convert screen delta to world movement
        float worldScale = _currentZoom * 0.01f;

        // Calculate movement in camera-relative space
        var forward = new Vector3(Mathf.Sin(_currentRotation), 0, Mathf.Cos(_currentRotation));
        var right = new Vector3(forward.Z, 0, -forward.X);

        var movement = (right * screenDelta.X + forward * screenDelta.Y) * worldScale;
        _targetPosition += movement;

        ClampToBounds();
    }

    /// <summary>
    /// Pans the camera in world units.
    /// </summary>
    public void PanWorld(Vector2 worldDelta)
    {
        var forward = new Vector3(Mathf.Sin(_currentRotation), 0, Mathf.Cos(_currentRotation));
        var right = new Vector3(forward.Z, 0, -forward.X);

        _targetPosition += right * worldDelta.X + forward * worldDelta.Y;
        ClampToBounds();
    }

    /// <summary>
    /// Zooms the camera by a delta amount.
    /// </summary>
    public void Zoom(float delta)
    {
        _targetZoom = Mathf.Clamp(_targetZoom + delta * ZoomSpeed, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Sets the zoom level directly.
    /// </summary>
    public void SetZoom(float zoom)
    {
        _targetZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Rotates the camera around the focus point.
    /// </summary>
    public void Rotate(float delta)
    {
        _targetRotation += delta * RotationSpeed;
    }

    /// <summary>
    /// Sets the rotation angle directly.
    /// </summary>
    public void SetRotation(float angle)
    {
        _targetRotation = angle;
    }

    /// <summary>
    /// Adjusts the camera pitch (tilt).
    /// </summary>
    public void AdjustPitch(float delta)
    {
        float minRad = Mathf.DegToRad(MinPitch);
        float maxRad = Mathf.DegToRad(MaxPitch);
        _targetPitch = Mathf.Clamp(_targetPitch + delta, minRad, maxRad);
    }

    /// <summary>
    /// Focuses the camera on a world position.
    /// </summary>
    public void FocusOn(Vector3 worldPosition)
    {
        _targetPosition = new Vector3(worldPosition.X, 0, worldPosition.Z);
        ClampToBounds();
    }

    /// <summary>
    /// Focuses the camera on a hex cell.
    /// </summary>
    public void FocusOnCell(HexCell cell)
    {
        var worldPos = cell.Coordinates.ToWorldPosition(0);
        FocusOn(worldPos);
    }

    /// <summary>
    /// Focuses the camera on hex coordinates.
    /// </summary>
    public void FocusOnCoords(int q, int r)
    {
        var worldPos = new HexCoordinates(q, r).ToWorldPosition(0);
        FocusOn(worldPos);
    }

    /// <summary>
    /// Instantly moves the camera to the target position (no smoothing).
    /// </summary>
    public void JumpTo(Vector3 worldPosition)
    {
        _targetPosition = new Vector3(worldPosition.X, 0, worldPosition.Z);
        GlobalPosition = _targetPosition;
        ClampToBounds();
    }

    #endregion

    #region Bounds

    /// <summary>
    /// Sets the map bounds for camera clamping.
    /// </summary>
    public void SetMapBounds(Vector2 min, Vector2 max)
    {
        _mapBoundsMin = min;
        _mapBoundsMax = max;
        ClampToBounds();
    }

    /// <summary>
    /// Sets map bounds from a hex grid.
    /// </summary>
    public void SetMapBoundsFromGrid(HexGrid grid)
    {
        // Calculate world bounds from grid
        var minCoords = new HexCoordinates(0, 0).ToWorldPosition(0);
        var maxCoords = new HexCoordinates(grid.Width - 1, grid.Height - 1).ToWorldPosition(0);

        float padding = HexMetrics.OuterRadius * 2;
        _mapBoundsMin = new Vector2(minCoords.X - padding, minCoords.Z - padding);
        _mapBoundsMax = new Vector2(maxCoords.X + padding, maxCoords.Z + padding);
    }

    /// <summary>
    /// Clears the map bounds (allows camera to move anywhere).
    /// </summary>
    public void ClearBounds()
    {
        _mapBoundsMin = null;
        _mapBoundsMax = null;
    }

    private void ClampToBounds()
    {
        if (_mapBoundsMin.HasValue && _mapBoundsMax.HasValue)
        {
            _targetPosition.X = Mathf.Clamp(_targetPosition.X, _mapBoundsMin.Value.X, _mapBoundsMax.Value.X);
            _targetPosition.Z = Mathf.Clamp(_targetPosition.Z, _mapBoundsMin.Value.Y, _mapBoundsMax.Value.Y);
        }
    }

    #endregion

    #region Camera Position Update

    private void UpdateCameraPosition()
    {
        if (_camera == null) return;

        // Calculate camera offset from focus point based on zoom and pitch
        float horizontalDistance = _currentZoom * Mathf.Cos(_currentPitch);
        float verticalDistance = _currentZoom * Mathf.Sin(_currentPitch);

        // Calculate position offset based on rotation
        float offsetX = -horizontalDistance * Mathf.Sin(_currentRotation);
        float offsetZ = -horizontalDistance * Mathf.Cos(_currentRotation);

        // Set camera position relative to this node (which is at focus point)
        _camera.Position = new Vector3(offsetX, verticalDistance, offsetZ);

        // Look at focus point
        _camera.LookAt(Vector3.Zero, Vector3.Up);
    }

    #endregion

    #region Input Handling Integration

    /// <summary>
    /// Connects this camera controller to an InputManager.
    /// </summary>
    public void ConnectToInputManager(InputManager inputManager)
    {
        inputManager.CameraPanInput += OnPanInput;
        inputManager.CameraZoomInput += OnZoomInput;
        inputManager.CameraRotateInput += OnRotateInput;
    }

    /// <summary>
    /// Disconnects from an InputManager.
    /// </summary>
    public void DisconnectFromInputManager(InputManager inputManager)
    {
        inputManager.CameraPanInput -= OnPanInput;
        inputManager.CameraZoomInput -= OnZoomInput;
        inputManager.CameraRotateInput -= OnRotateInput;
    }

    private void OnPanInput(Vector2 delta)
    {
        Pan(delta);
    }

    private void OnZoomInput(float delta)
    {
        Zoom(delta);
    }

    private void OnRotateInput(float delta)
    {
        Rotate(delta);
    }

    #endregion
}
