using HexGame.Core;

namespace HexGame.Input;

/// <summary>
/// Centralized input handling for the game.
/// Processes input events and dispatches them to appropriate handlers.
/// </summary>
public partial class InputManager : Node, IService
{
    private Camera3D? _camera;
    private HexGrid? _grid;
    private bool _enabled = true;

    #region Input Actions

    /// <summary>
    /// Action name for primary selection (left click).
    /// </summary>
    public const string ActionSelect = "select";

    /// <summary>
    /// Action name for secondary action (right click).
    /// </summary>
    public const string ActionAction = "action";

    /// <summary>
    /// Action name for camera pan modifier.
    /// </summary>
    public const string ActionPan = "camera_pan";

    /// <summary>
    /// Action name for camera zoom in.
    /// </summary>
    public const string ActionZoomIn = "camera_zoom_in";

    /// <summary>
    /// Action name for camera zoom out.
    /// </summary>
    public const string ActionZoomOut = "camera_zoom_out";

    /// <summary>
    /// Action name for camera rotate left.
    /// </summary>
    public const string ActionRotateLeft = "camera_rotate_left";

    /// <summary>
    /// Action name for camera rotate right.
    /// </summary>
    public const string ActionRotateRight = "camera_rotate_right";

    /// <summary>
    /// Action name for cancel/deselect.
    /// </summary>
    public const string ActionCancel = "cancel";

    /// <summary>
    /// Action name for end turn.
    /// </summary>
    public const string ActionEndTurn = "end_turn";

    #endregion

    #region Events

    /// <summary>
    /// Fired when a hex cell is clicked.
    /// </summary>
    public event Action<HexCell?>? CellClicked;

    /// <summary>
    /// Fired when a hex cell is right-clicked (action).
    /// </summary>
    public event Action<HexCell?>? CellActionClicked;

    /// <summary>
    /// Fired when the mouse hovers over a different cell.
    /// </summary>
    public event Action<HexCell?>? CellHovered;

    /// <summary>
    /// Fired when cancel/escape is pressed.
    /// </summary>
    public event Action? CancelPressed;

    /// <summary>
    /// Fired when end turn is pressed.
    /// </summary>
    public event Action? EndTurnPressed;

    /// <summary>
    /// Fired when camera movement input is received.
    /// </summary>
    public event Action<Vector2>? CameraPanInput;

    /// <summary>
    /// Fired when camera zoom input is received.
    /// </summary>
    public event Action<float>? CameraZoomInput;

    /// <summary>
    /// Fired when camera rotation input is received.
    /// </summary>
    public event Action<float>? CameraRotateInput;

    #endregion

    #region State

    private HexCell? _hoveredCell;
    private Vector2 _lastMousePosition;
    private bool _isPanning;

    /// <summary>
    /// Gets the currently hovered cell.
    /// </summary>
    public HexCell? HoveredCell => _hoveredCell;

    /// <summary>
    /// Gets or sets whether input is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    #endregion

    #region IService Implementation

    public void Initialize()
    {
        EnsureInputActionsExist();
    }

    public void Shutdown()
    {
        CellClicked = null;
        CellActionClicked = null;
        CellHovered = null;
        CancelPressed = null;
        EndTurnPressed = null;
        CameraPanInput = null;
        CameraZoomInput = null;
        CameraRotateInput = null;
    }

    #endregion

    #region Setup

    /// <summary>
    /// Sets the camera used for raycasting.
    /// </summary>
    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Sets the hex grid for cell lookups.
    /// </summary>
    public void SetGrid(HexGrid grid)
    {
        _grid = grid;
    }

    private void EnsureInputActionsExist()
    {
        // Add default input actions if they don't exist
        AddInputActionIfMissing(ActionSelect, MouseButton.Left);
        AddInputActionIfMissing(ActionAction, MouseButton.Right);
        AddInputActionIfMissing(ActionPan, MouseButton.Middle);
        AddInputActionIfMissing(ActionCancel, Key.Escape);
        AddInputActionIfMissing(ActionEndTurn, Key.Space);
        AddInputActionIfMissing(ActionZoomIn, MouseButton.WheelUp);
        AddInputActionIfMissing(ActionZoomOut, MouseButton.WheelDown);
        AddInputActionIfMissing(ActionRotateLeft, Key.Q);
        AddInputActionIfMissing(ActionRotateRight, Key.E);
    }

    private static void AddInputActionIfMissing(string actionName, MouseButton button)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
            var mouseEvent = new InputEventMouseButton
            {
                ButtonIndex = button
            };
            InputMap.ActionAddEvent(actionName, mouseEvent);
        }
    }

    private static void AddInputActionIfMissing(string actionName, Key key)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
            var keyEvent = new InputEventKey
            {
                Keycode = key
            };
            InputMap.ActionAddEvent(actionName, keyEvent);
        }
    }

    #endregion

    #region Input Processing

    public override void _Input(InputEvent @event)
    {
        if (!_enabled) return;

        // Handle mouse button events
        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
        }
        // Handle mouse motion for hover and panning
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            HandleMouseMotion(mouseMotion);
        }
        // Handle key events
        else if (@event is InputEventKey keyEvent)
        {
            HandleKeyInput(keyEvent);
        }
    }

    public override void _Process(double delta)
    {
        if (!_enabled) return;

        // Handle continuous camera input
        ProcessCameraInput((float)delta);
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.Pressed)
        {
            // Selection (left click)
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                var cell = GetCellUnderMouse(mouseButton.Position);
                CellClicked?.Invoke(cell);
            }
            // Action (right click)
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                var cell = GetCellUnderMouse(mouseButton.Position);
                CellActionClicked?.Invoke(cell);
            }
            // Pan start (middle click)
            else if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                _isPanning = true;
                _lastMousePosition = mouseButton.Position;
            }
            // Zoom
            else if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                CameraZoomInput?.Invoke(-1f);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                CameraZoomInput?.Invoke(1f);
            }
        }
        else
        {
            // Pan end
            if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                _isPanning = false;
            }
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        // Update hovered cell
        var cell = GetCellUnderMouse(mouseMotion.Position);
        if (cell != _hoveredCell)
        {
            _hoveredCell = cell;
            CellHovered?.Invoke(cell);
        }

        // Handle panning
        if (_isPanning)
        {
            var delta = mouseMotion.Position - _lastMousePosition;
            _lastMousePosition = mouseMotion.Position;
            CameraPanInput?.Invoke(delta);
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        if (!keyEvent.Pressed) return;

        if (keyEvent.Keycode == Key.Escape)
        {
            CancelPressed?.Invoke();
        }
        else if (keyEvent.Keycode == Key.Space)
        {
            EndTurnPressed?.Invoke();
        }
    }

    private void ProcessCameraInput(float delta)
    {
        // Keyboard rotation
        if (Godot.Input.IsActionPressed(ActionRotateLeft))
        {
            CameraRotateInput?.Invoke(-1f);
        }
        else if (Godot.Input.IsActionPressed(ActionRotateRight))
        {
            CameraRotateInput?.Invoke(1f);
        }

        // Edge panning (optional - when mouse near screen edge)
        var viewport = GetViewport();
        if (viewport != null)
        {
            var mousePos = viewport.GetMousePosition();
            var viewportSize = viewport.GetVisibleRect().Size;
            var edgePan = Vector2.Zero;

            float edgeThreshold = 20f;

            if (mousePos.X < edgeThreshold)
                edgePan.X = -1f;
            else if (mousePos.X > viewportSize.X - edgeThreshold)
                edgePan.X = 1f;

            if (mousePos.Y < edgeThreshold)
                edgePan.Y = -1f;
            else if (mousePos.Y > viewportSize.Y - edgeThreshold)
                edgePan.Y = 1f;

            if (edgePan != Vector2.Zero)
            {
                CameraPanInput?.Invoke(edgePan * 10f);
            }
        }
    }

    #endregion

    #region Raycasting

    /// <summary>
    /// Gets the hex cell under the mouse cursor.
    /// </summary>
    public HexCell? GetCellUnderMouse(Vector2 screenPosition)
    {
        if (_camera == null || _grid == null) return null;

        var worldPos = GetWorldPositionFromScreen(screenPosition);
        if (worldPos.HasValue)
        {
            var coords = HexCoordinates.FromWorldPosition(worldPos.Value);
            return _grid.GetCell(coords.Q, coords.R);
        }

        return null;
    }

    /// <summary>
    /// Converts screen position to world position via raycast.
    /// </summary>
    public Vector3? GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        if (_camera == null) return null;

        var spaceState = _camera.GetWorld3D()?.DirectSpaceState;
        if (spaceState == null) return null;

        var rayOrigin = _camera.ProjectRayOrigin(screenPosition);
        var rayEnd = rayOrigin + _camera.ProjectRayNormal(screenPosition) * 1000f;

        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0 && result.TryGetValue("position", out var position))
        {
            return (Vector3)position;
        }

        // Fallback: intersect with Y=0 plane
        return IntersectGroundPlane(rayOrigin, rayEnd);
    }

    private static Vector3? IntersectGroundPlane(Vector3 rayOrigin, Vector3 rayEnd)
    {
        var rayDir = (rayEnd - rayOrigin).Normalized();

        // Check if ray is parallel to ground
        if (Mathf.Abs(rayDir.Y) < 0.001f) return null;

        // Calculate intersection with Y=0 plane
        float t = -rayOrigin.Y / rayDir.Y;
        if (t < 0) return null;

        return rayOrigin + rayDir * t;
    }

    #endregion
}
