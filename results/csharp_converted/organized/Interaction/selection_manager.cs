using Godot;
using Godot.Collections;


//# Manages unit selection via click, ctrl+click, and box selection.

//# Matches web selection behavior from main.ts
[GlobalClass]
public partial class SelectionManager : Godot.Node
{
	[Signal]
	public delegate void SelectionChangedEventHandler(Array<int> selected_ids);

	public Godot.UnitManager UnitManager;
	public Godot.UnitRenderer UnitRenderer;
	public Godot.HexGrid Grid;
	public Godot.Camera3D Camera;
	public Godot.Pathfinder Pathfinder;
	public Godot.PathRenderer PathRenderer;
	public Godot.TurnManager TurnManager;


	// Selected unit IDs
	public Dictionary SelectedUnitIds = new Dictionary{};

	// Set<int>
	// Box selection state
	public bool IsBoxSelecting = false;
	public Vector2 BoxSelectStart = Vector2.Zero;


	// Selection box visual (CanvasLayer)
	public Godot.ColorRect SelectionBox;


	public void Setup(Godot.UnitManager p_unit_manager, Godot.UnitRenderer p_unit_renderer, Godot.HexGrid p_grid, Godot.Camera3D p_camera, Godot.Pathfinder p_pathfinder = null, Godot.PathRenderer p_path_renderer = null, Godot.TurnManager p_turn_manager = null)
	{
		System.Diagnostics.Debug.Assert(p_unit_manager != null, "SelectionManager requires UnitManager");
		System.Diagnostics.Debug.Assert(p_unit_renderer != null, "SelectionManager requires UnitRenderer");
		System.Diagnostics.Debug.Assert(p_grid != null, "SelectionManager requires HexGrid");
		System.Diagnostics.Debug.Assert(p_camera != null, "SelectionManager requires Camera3D");

		UnitManager = p_unit_manager;
		UnitRenderer = p_unit_renderer;
		Grid = p_grid;
		Camera = p_camera;
		Pathfinder = p_pathfinder;
		PathRenderer = p_path_renderer;
		TurnManager = p_turn_manager;
	}


	public override void _Input(Godot.InputEvent event)
	{
		if(UnitManager == null || Camera == null)
		{
			return ;
		}


		// Mouse button events
		if(event is Godot.InputEventMouseButton)
		{
			var mb = event;


			// Left click
			if(mb.ButtonIndex == MOUSE_BUTTON_LEFT)
			{
				if(mb.Pressed)
				{

					// Shift+click starts box selection
					if(mb.ShiftPressed)
					{
						_StartBoxSelect(mb.Position);
					}
				}

				// Regular click or ctrl+click handled on release
				else
				{

					// Release
					if(IsBoxSelecting)
					{
						_FinishBoxSelect(mb.Position, mb.CtrlPressed);
					}
					else
					{
						_HandleClick(mb.Position, mb.CtrlPressed);
					}
				}
			}


			// Right click - move selected unit
			else if(mb.ButtonIndex == MOUSE_BUTTON_RIGHT && mb.Pressed)
			{
				_HandleRightClick(mb.Position);
			}
		}


		// Mouse motion for box selection
		else if(event is Godot.InputEventMouseMotion)
		{ && IsBoxSelecting;;;;//PANIC! <:> unexpected at Token(type=':', value=':', lineno=69, index=2145, end=2146)

			{_UpdateSelectionBox(event.Position);
			}
		}


		protected void _HandleClick(Vector2 screen_pos, bool ctrl_pressed)
		{

			// Raycast to find clicked unit
			var unit = _GetUnitAtScreenPos(screen_pos);

			if(unit == null)
			{
				if(!ctrl_pressed)
				{
					ClearSelection();
				}
				return ;
			}


			// Only select player 1's units for now
			if(unit.PlayerId != 1)
			{
				if(!ctrl_pressed)
				{
					ClearSelection();
				}
				return ;
			}

			if(ctrl_pressed)
			{

				// Toggle selection
				if(SelectedUnitIds.ContainsKey(unit.Id))
				{
					SelectedUnitIds.Erase(unit.Id);
				}
				else
				{
					SelectedUnitIds[unit.Id] = true;
				}
			}
			else
			{

				// Replace selection
				SelectedUnitIds.Clear();
				SelectedUnitIds[unit.Id] = true;
			}

			_UpdateSelectionVisuals();
		}


		protected void _HandleRightClick(Vector2 screen_pos)
		{

			// Need exactly one unit selected
			if(SelectedUnitIds.Size() != 1)
			{
				return ;
			}

			var unit_id = SelectedUnitIds.Keys()[0];
			var unit = UnitManager.GetUnit(unit_id);
			if(unit == null)
			{
				return ;
			}


			// Check turn system - must be movement phase for current player's unit
			if(TurnManager)
			{
				if(!TurnManager.CanMove())
				{
					GD.Print("Not in movement phase");
					return ;
				}
				if(!TurnManager.IsCurrentPlayerUnit(unit.PlayerId))
				{
					GD.Print("Not your turn");
					return ;
				}
			}
			else
			{

				// Fallback: only move player 1's units
				if(unit.PlayerId != 1)
				{
					return ;
				}
			}


			// Check if unit can move
			if(unit.Movement <= 0)
			{
				GD.Print("Unit has no movement left");
				return ;
			}


			// Raycast to find target hex
			var target_cell = _GetCellAtScreenPos(screen_pos);
			if(target_cell == null)
			{
				return ;
			}


			// Get current cell
			var start_cell = Grid.GetCell(unit.Q, unit.R);
			if(start_cell == null)
			{
				return ;
			}


			// Use pathfinding if available
			if(Pathfinder != null)
			{
				var result = Pathfinder.FindPath(start_cell, target_cell, new Dictionary{
									{"unit_type", unit.Type},
									{"max_cost", Float(unit.Movement)},
									});

				if(result["reachable"])
				{
					var path = result["path"];
					var cost = result["cost"];


					// Move along the path to the destination
					if(path.Size() >= 2)
					{
						var end_cell = path[path.Size() - 1];
						if(UnitManager.MoveUnit(unit_id, end_cell.Q, end_cell.R, Int(Mathf.Ceil(cost))))
						{
							GD.Print("Moved unit %d to (%d, %d) via %d cells, cost: %.1f" % new Array{unit_id, end_cell.Q, end_cell.R, path.Size(), cost, });
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

				// Fallback: Simple direct move (for testing)
				var is_water = target_cell.Elevation < HexMetrics.SEA_LEVEL;
				if(is_water && !unit.CanTraverseWater())
				{
					return ;
				}
				if(!is_water && !unit.CanTraverseLand())
				{
					return ;
				}

				if(UnitManager.MoveUnit(unit_id, target_cell.Q, target_cell.R, 1))
				{
					GD.Print("Moved unit %d to (%d, %d)" % new Array{unit_id, target_cell.Q, target_cell.R, });
				}
			}
		}


		protected void _StartBoxSelect(Vector2 screen_pos)
		{
			IsBoxSelecting = true;
			BoxSelectStart = screen_pos;
			_ShowSelectionBox();
		}


		protected void _FinishBoxSelect(Vector2 end_pos, bool ctrl_pressed)
		{
			IsBoxSelecting = false;
			_HideSelectionBox();

			var min_x = Mathf.Min(BoxSelectStart.X, end_pos.X);
			var max_x = Mathf.Max(BoxSelectStart.X, end_pos.X);
			var min_y = Mathf.Min(BoxSelectStart.Y, end_pos.Y);
			var max_y = Mathf.Max(BoxSelectStart.Y, end_pos.Y);


			// Minimum drag distance to count as box select
			if(max_x - min_x < 5 && max_y - min_y < 5)
			{
				return ;
			}

			if(!ctrl_pressed)
			{
				SelectedUnitIds.Clear();
			}


			// Check each unit's screen position
			foreach(Unit unit in UnitManager.GetAllUnits())
			{

				// Only select player 1's units
				if(unit.PlayerId != 1)
				{
					continue;
				}

				var world_pos = unit.GetWorldPosition();
				var cell = Grid.GetCell(unit.Q, unit.R);
				if(cell)
				{
					world_pos.Y = cell.Elevation * HexMetrics.ELEVATION_STEP + 0.25;
				}


				// Project to screen
				if(!Camera.IsPositionBehind(world_pos))
				{
					var screen_pos = Camera.UnprojectPosition(world_pos);
					if(screen_pos.X >= min_x && screen_pos.X <= max_x && screen_pos.Y >= min_y && screen_pos.Y <= max_y)
					{
						SelectedUnitIds[unit.Id] = true;
					}
				}
			}

			_UpdateSelectionVisuals();
		}


		protected void _UpdateSelectionBox(Vector2 current_pos)
		{
			if(SelectionBox == null)
			{
				return ;
			}

			var left = Mathf.Min(BoxSelectStart.X, current_pos.X);
			var top = Mathf.Min(BoxSelectStart.Y, current_pos.Y);
			var width = Mathf.Abs(current_pos.X - BoxSelectStart.X);
			var height = Mathf.Abs(current_pos.Y - BoxSelectStart.Y);

			SelectionBox.Position = new Vector2(left, top);
			SelectionBox.Size = new Vector2(width, height);
		}


		protected void _ShowSelectionBox()
		{
			if(SelectionBox == null)
			{

				// Create selection box
				var canvas_layer = CanvasLayer.New();
				canvas_layer.Layer = 10;
				AddChild(canvas_layer);

				SelectionBox = ColorRect.New();
				SelectionBox.Color = new Color(0.3, 0.5, 0.9, 0.3);
				canvas_layer.AddChild(SelectionBox);
			}

			SelectionBox.Visible = true;
			SelectionBox.Position = BoxSelectStart;
			SelectionBox.Size = Vector2.Zero;
		}


		protected void _HideSelectionBox()
		{
			if(SelectionBox)
			{
				SelectionBox.Visible = false;
			}
		}


		//# Update path preview when hovering over a cell
		public void UpdatePathPreview(Godot.HexCell target_cell)
		{
			if(PathRenderer == null || Pathfinder == null)
			{
				return ;
			}


			// Only show path if exactly one unit selected
			if(SelectedUnitIds.Size() != 1)
			{
				PathRenderer.HidePath();
				return ;
			}

			var unit_id = SelectedUnitIds.Keys()[0];
			var unit = UnitManager.GetUnit(unit_id);
			if(unit == null)
			{
				PathRenderer.HidePath();
				return ;
			}

			var start_cell = Grid.GetCell(unit.Q, unit.R);
			if(start_cell == null)
			{
				PathRenderer.HidePath();
				return ;
			}


			// Don't show path to current position
			if(start_cell.Q == target_cell.Q && start_cell.R == target_cell.R)
			{
				PathRenderer.HidePath();
				return ;
			}


			// Find path
			var result = Pathfinder.FindPath(start_cell, target_cell, new Dictionary{
							{"unit_type", unit.Type},
							});

			if(result["reachable"] && result["path"].Size() > 0)
			{
				PathRenderer.ShowPath(result["path"]);

				// Color indicates if within movement range
				PathRenderer.SetPathValid(result["cost"] <= unit.Movement);
			}
			else
			{
				PathRenderer.HidePath();
			}
		}


		//# Clear path preview when not hovering
		public void ClearPathPreview()
		{
			if(PathRenderer)
			{
				PathRenderer.HidePath();
			}
		}


		protected void _UpdateSelectionVisuals()
		{
			if(UnitRenderer)
			{
				var ids = new Array{};
				foreach(Variant id in SelectedUnitIds.Keys())
				{
					ids.Append(id);
				}
				UnitRenderer.SetSelectedUnits(ids);
			}


			// Show reachable cells for single selected unit
			if(PathRenderer && Pathfinder)
			{
				if(SelectedUnitIds.Size() == 1)
				{
					var unit_id = SelectedUnitIds.Keys()[0];
					var unit = UnitManager.GetUnit(unit_id);
					if(unit && unit.Movement > 0)
					{
						var start_cell = Grid.GetCell(unit.Q, unit.R);
						if(start_cell)
						{
							var reachable = Pathfinder.GetReachableCells(start_cell, Float(unit.Movement), new Dictionary{
															{"unit_type", unit.Type},
															});
							PathRenderer.ShowReachableCells(reachable);
						}
					}
					else
					{
						PathRenderer.HideReachableCells();
					}
				}
				else
				{
					PathRenderer.HideReachableCells();
				}
			}

			EmitSignal("SelectionChanged", GetSelectedIds());
		}


		protected Godot.Unit _GetUnitAtScreenPos(Vector2 screen_pos)
		{
			var cell = _GetCellAtScreenPos(screen_pos);
			if(cell == null)
			{
				return null;
			}
			return UnitManager.GetUnitAt(cell.Q, cell.R);
		}


		protected Godot.HexCell _GetCellAtScreenPos(Vector2 screen_pos)
		{
			var ray_origin = Camera.ProjectRayOrigin(screen_pos);
			var ray_dir = Camera.ProjectRayNormal(screen_pos);

			if(Mathf.Abs(ray_dir.Y) < 0.001)
			{
				return null;


				// Raycast against multiple elevation levels to find the actual terrain surface

			}// Check from highest to lowest elevation to find the first valid intersection
			var best_cell = null;
			var best_distance = Mathf.Inf;

			foreach(int elev in GD.Range(HexMetrics.MAX_ELEVATION, HexMetrics.MIN_ELEVATION - 1,  - 1))
			{
				var plane_y = elev * HexMetrics.ELEVATION_STEP;
				var t = (plane_y - ray_origin.Y) / ray_dir.Y;

				if(t <= 0)
				{
					continue;
				}

				// Behind camera
				var hit_point = ray_origin + ray_dir * t;
				var coords = HexCoordinates.FromWorldPosition(hit_point);
				var cell = Grid.GetCell(coords.Q, coords.R);

				if(cell && cell.Elevation == elev)
				{

					// Found a cell at this elevation
					if(t < best_distance)
					{
						best_distance = t;
						best_cell = cell;
						break;

						// First valid hit from high to low is closest

					}
				}
			}// Fallback to water level if no elevated cell found
			if(best_cell == null)
			{
				var t =  - ray_origin.Y / ray_dir.Y;
				if(t > 0)
				{
					var hit_point = ray_origin + ray_dir * t;
					var coords = HexCoordinates.FromWorldPosition(hit_point);
					best_cell = Grid.GetCell(coords.Q, coords.R);
				}
			}

			return best_cell;
		}


		public void ClearSelection()
		{
			SelectedUnitIds.Clear();
			_UpdateSelectionVisuals();
		}


		public Array<int> GetSelectedIds()
		{
			var ids = new Array{};
			foreach(Variant id in SelectedUnitIds.Keys())
			{
				ids.Append(id);
			}
			return ids;
		}


		public Array<Unit> GetSelectedUnits()
		{
			var units = new Array{};
			foreach(Variant id in SelectedUnitIds.Keys())
			{
				var unit = UnitManager.GetUnit(id);
				if(unit)
				{
					units.Append(unit);
				}
			}
			return units;
		}


		public bool HasSelection()
		{
			return SelectedUnitIds.Size() > 0;
		}


		public Godot.Unit GetSingleSelectedUnit()
		{
			if(SelectedUnitIds.Size() != 1)
			{
				return null;
			}
			return UnitManager.GetUnit(SelectedUnitIds.Keys()[0]);
		}


	}
}