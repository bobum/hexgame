namespace HexGame.Interaction;

using HexGame.Core;
using HexGame.GameState;
using HexGame.Pathfinding;
using HexGame.Rendering;
using HexGame.Units;

/// <summary>
/// Manages unit selection via click, ctrl+click, and box selection.
/// Direct port of selection_manager.gd
/// </summary>
public partial class SelectionManager : Node
{
    [Signal]
    public delegate void SelectionChangedEventHandler(int[] selectedIds);

    private IUnitManager? _unitManager;
    private UnitRenderer? _unitRenderer;
    private HexGrid? _grid;
    private Camera3D? _camera;
    private IPathfinder? _pathfinder;
    private PathRenderer? _pathRenderer;
    private TurnManager? _turnManager;

    // Selected unit IDs (using dictionary as set)
    private readonly Dictionary<int, bool> _selectedUnitIds = new();

    // Box selection state
    private bool _isBoxSelecting;
    private Vector2 _boxSelectStart = Vector2.Zero;

    // Selection box visual
    private ColorRect? _selectionBox;
    private CanvasLayer? _canvasLayer;

    public void Setup(
        IUnitManager unitManager,
        UnitRenderer unitRenderer,
        HexGrid grid,
        Camera3D camera,
        IPathfinder? pathfinder = null,
        PathRenderer? pathRenderer = null,
        TurnManager? turnManager = null)
    {
        _unitManager = unitManager;
        _unitRenderer = unitRenderer;
        _grid = grid;
        _camera = camera;
        _pathfinder = pathfinder;
        _pathRenderer = pathRenderer;
        _turnManager = turnManager;
    }

    public override void _Input(InputEvent @event)
    {
        if (_unitManager == null || _camera == null)
            return;

        // Escape key clears selection
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            ClearSelection();
            return;
        }

        if (@event is InputEventMouseButton mb)
        {
            // Left click
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    // Shift+click starts box selection
                    if (mb.ShiftPressed)
                    {
                        StartBoxSelect(mb.Position);
                    }
                }
                else
                {
                    // Release
                    if (_isBoxSelecting)
                    {
                        FinishBoxSelect(mb.Position, mb.CtrlPressed);
                    }
                    else
                    {
                        HandleClick(mb.Position, mb.CtrlPressed);
                    }
                }
            }
            // Right click - move selected unit
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
            {
                HandleRightClick(mb.Position);
            }
        }
        // Mouse motion for box selection
        else if (@event is InputEventMouseMotion mm && _isBoxSelecting)
        {
            UpdateSelectionBox(mm.Position);
        }
    }

    private void HandleClick(Vector2 screenPos, bool ctrlPressed)
    {
        var unit = GetUnitAtScreenPos(screenPos);

        if (unit == null)
        {
            if (!ctrlPressed)
            {
                ClearSelection();
            }
            return;
        }

        // Only select player 1's units
        if (unit.PlayerId != 1)
        {
            if (!ctrlPressed)
            {
                ClearSelection();
            }
            return;
        }

        if (ctrlPressed)
        {
            // Toggle selection
            if (_selectedUnitIds.ContainsKey(unit.Id))
            {
                _selectedUnitIds.Remove(unit.Id);
            }
            else
            {
                _selectedUnitIds[unit.Id] = true;
            }
        }
        else
        {
            // Replace selection
            _selectedUnitIds.Clear();
            _selectedUnitIds[unit.Id] = true;
        }

        UpdateSelectionVisuals();
    }

    private void HandleRightClick(Vector2 screenPos)
    {
        // Need exactly one unit selected
        if (_selectedUnitIds.Count != 1)
            return;

        int unitId = _selectedUnitIds.Keys.First();
        var unit = _unitManager!.GetUnit(unitId);
        if (unit == null)
            return;

        // Check turn system
        if (_turnManager != null)
        {
            if (!_turnManager.CanMove)
            {
                GD.Print("Not in movement phase");
                return;
            }
            if (!_turnManager.IsCurrentPlayerUnit(unit.PlayerId))
            {
                GD.Print("Not your turn");
                return;
            }
        }
        else
        {
            // Fallback: only move player 1's units
            if (unit.PlayerId != 1)
                return;
        }

        // Check if unit can move
        if (unit.Movement <= 0)
        {
            GD.Print("Unit has no movement left");
            return;
        }

        // Raycast to find target hex
        var targetCell = GetCellAtScreenPos(screenPos);
        if (targetCell == null)
            return;

        // Get current cell
        var startCell = _grid!.GetCell(unit.Q, unit.R);
        if (startCell == null)
            return;

        // Use pathfinding if available
        if (_pathfinder != null)
        {
            var result = _pathfinder.FindPath(startCell, targetCell, new PathOptions
            {
                UnitType = unit.UnitType,
                MaxCost = unit.Movement
            });

            if (result.Reachable)
            {
                var path = result.Path.ToList();
                float cost = result.Cost;

                if (path.Count >= 2)
                {
                    var endCell = path[path.Count - 1];
                    if (_unitManager.MoveUnit(unitId, endCell.Q, endCell.R, (int)Math.Ceiling(cost)))
                    {
                        GD.Print($"Moved unit {unitId} to ({endCell.Q}, {endCell.R}) via {path.Count} cells, cost: {cost:F1}");
                    }
                }
            }
            else
            {
                GD.Print("No valid path to destination");
            }
        }
        else
        {
            // Fallback: Simple direct move
            bool isWater = targetCell.Elevation < HexMetrics.SeaLevel;
            if (isWater && !unit.CanTraverseWater)
                return;
            if (!isWater && !unit.CanTraverseLand)
                return;

            if (_unitManager.MoveUnit(unitId, targetCell.Q, targetCell.R, 1))
            {
                GD.Print($"Moved unit {unitId} to ({targetCell.Q}, {targetCell.R})");
            }
        }
    }

    private void StartBoxSelect(Vector2 screenPos)
    {
        _isBoxSelecting = true;
        _boxSelectStart = screenPos;
        ShowSelectionBox();
    }

    private void FinishBoxSelect(Vector2 endPos, bool ctrlPressed)
    {
        _isBoxSelecting = false;
        HideSelectionBox();

        float minX = Mathf.Min(_boxSelectStart.X, endPos.X);
        float maxX = Mathf.Max(_boxSelectStart.X, endPos.X);
        float minY = Mathf.Min(_boxSelectStart.Y, endPos.Y);
        float maxY = Mathf.Max(_boxSelectStart.Y, endPos.Y);

        // Minimum drag distance
        if (maxX - minX < 5 && maxY - minY < 5)
            return;

        if (!ctrlPressed)
        {
            _selectedUnitIds.Clear();
        }

        // Check each unit's screen position
        foreach (var unit in _unitManager!.GetAllUnits())
        {
            // Only select player 1's units
            if (unit.PlayerId != 1)
                continue;

            var worldPos = unit.GetWorldPosition();
            var cell = _grid!.GetCell(unit.Q, unit.R);
            if (cell != null)
            {
                worldPos.Y = cell.Elevation * HexMetrics.ElevationStep + 0.25f;
            }

            // Project to screen
            if (!_camera!.IsPositionBehind(worldPos))
            {
                var screenP = _camera.UnprojectPosition(worldPos);
                if (screenP.X >= minX && screenP.X <= maxX && screenP.Y >= minY && screenP.Y <= maxY)
                {
                    _selectedUnitIds[unit.Id] = true;
                }
            }
        }

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionBox(Vector2 currentPos)
    {
        if (_selectionBox == null)
            return;

        float left = Mathf.Min(_boxSelectStart.X, currentPos.X);
        float top = Mathf.Min(_boxSelectStart.Y, currentPos.Y);
        float width = Mathf.Abs(currentPos.X - _boxSelectStart.X);
        float height = Mathf.Abs(currentPos.Y - _boxSelectStart.Y);

        _selectionBox.Position = new Vector2(left, top);
        _selectionBox.Size = new Vector2(width, height);
    }

    private void ShowSelectionBox()
    {
        if (_selectionBox == null)
        {
            _canvasLayer = new CanvasLayer { Layer = 10 };
            AddChild(_canvasLayer);

            _selectionBox = new ColorRect
            {
                Color = new Color(0.3f, 0.5f, 0.9f, 0.3f)
            };
            _canvasLayer.AddChild(_selectionBox);
        }

        _selectionBox.Visible = true;
        _selectionBox.Position = _boxSelectStart;
        _selectionBox.Size = Vector2.Zero;
    }

    private void HideSelectionBox()
    {
        if (_selectionBox != null)
        {
            _selectionBox.Visible = false;
        }
    }

    /// <summary>
    /// Update path preview when hovering over a cell.
    /// </summary>
    public void UpdatePathPreview(HexCell targetCell)
    {
        if (_pathRenderer == null || _pathfinder == null)
            return;

        // Only show path if exactly one unit selected
        if (_selectedUnitIds.Count != 1)
        {
            _pathRenderer.HidePath();
            return;
        }

        int unitId = _selectedUnitIds.Keys.First();
        var unit = _unitManager!.GetUnit(unitId);
        if (unit == null)
        {
            _pathRenderer.HidePath();
            return;
        }

        var startCell = _grid!.GetCell(unit.Q, unit.R);
        if (startCell == null)
        {
            _pathRenderer.HidePath();
            return;
        }

        // Don't show path to current position
        if (startCell.Q == targetCell.Q && startCell.R == targetCell.R)
        {
            _pathRenderer.HidePath();
            return;
        }

        // Find path
        var result = _pathfinder.FindPath(startCell, targetCell, new PathOptions
        {
            UnitType = unit.UnitType
        });

        if (result.Reachable && result.Path.Count > 0)
        {
            _pathRenderer.ShowPath(result.Path.ToList());
            _pathRenderer.SetPathValid(result.Cost <= unit.Movement);
        }
        else
        {
            _pathRenderer.HidePath();
        }
    }

    /// <summary>
    /// Clear path preview.
    /// </summary>
    public void ClearPathPreview()
    {
        _pathRenderer?.HidePath();
    }

    private void UpdateSelectionVisuals()
    {
        if (_unitRenderer != null)
        {
            var ids = _selectedUnitIds.Keys.ToArray();
            _unitRenderer.SetSelectedUnits(ids);
        }

        // Show reachable cells for single selected unit
        if (_pathRenderer != null && _pathfinder != null)
        {
            if (_selectedUnitIds.Count == 1)
            {
                int unitId = _selectedUnitIds.Keys.First();
                var unit = _unitManager!.GetUnit(unitId);
                if (unit != null && unit.Movement > 0)
                {
                    var startCell = _grid!.GetCell(unit.Q, unit.R);
                    if (startCell != null)
                    {
                        var reachable = _pathfinder.GetReachableCells(startCell, unit.Movement, new PathOptions
                        {
                            UnitType = unit.UnitType
                        });
                        _pathRenderer.ShowReachableCells(reachable);
                    }
                }
                else
                {
                    _pathRenderer.HideReachableCells();
                }
            }
            else
            {
                _pathRenderer.HideReachableCells();
            }
        }

        EmitSignal(SignalName.SelectionChanged, GetSelectedIds());
    }

    private Unit? GetUnitAtScreenPos(Vector2 screenPos)
    {
        var cell = GetCellAtScreenPos(screenPos);
        if (cell == null)
            return null;
        return _unitManager!.GetUnitAt(cell.Q, cell.R);
    }

    private HexCell? GetCellAtScreenPos(Vector2 screenPos)
    {
        var rayOrigin = _camera!.ProjectRayOrigin(screenPos);
        var rayDir = _camera.ProjectRayNormal(screenPos);

        // Use physics raycasting against terrain mesh (like Three.js intersectObjects)
        var spaceState = _camera.GetWorld3D().DirectSpaceState;
        var rayEnd = rayOrigin + rayDir * 1000.0f;

        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            var hitPoint = (Vector3)result["position"];
            var coords = HexCoordinates.FromWorldPosition(hitPoint);
            return _grid!.GetCell(coords.Q, coords.R);
        }

        // Fallback to sea level plane if no physics hit (for water areas)
        if (Mathf.Abs(rayDir.Y) < 0.001f)
            return null;

        float seaLevelY = HexMetrics.SeaLevel * HexMetrics.ElevationStep;
        float t = (seaLevelY - rayOrigin.Y) / rayDir.Y;

        if (t <= 0)
            return null;

        var fallbackHit = rayOrigin + rayDir * t;
        var fallbackCoords = HexCoordinates.FromWorldPosition(fallbackHit);
        return _grid!.GetCell(fallbackCoords.Q, fallbackCoords.R);
    }

    public void ClearSelection()
    {
        _selectedUnitIds.Clear();
        UpdateSelectionVisuals();
    }

    public int[] GetSelectedIds()
    {
        return _selectedUnitIds.Keys.ToArray();
    }

    public List<Unit> GetSelectedUnits()
    {
        var units = new List<Unit>();
        foreach (var id in _selectedUnitIds.Keys)
        {
            var unit = _unitManager!.GetUnit(id);
            if (unit != null)
            {
                units.Add(unit);
            }
        }
        return units;
    }

    public bool HasSelection() => _selectedUnitIds.Count > 0;

    public Unit? GetSingleSelectedUnit()
    {
        if (_selectedUnitIds.Count != 1)
            return null;
        return _unitManager!.GetUnit(_selectedUnitIds.Keys.First());
    }
}
