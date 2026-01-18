using Godot;
using Godot.Collections;


//# A* pathfinder for hex grids.
//# Finds optimal paths considering terrain costs, elevation, and unit obstacles.

//# Matches web/src/pathfinding/Pathfinder.ts
[GlobalClass]
public partial class Pathfinder : Godot.RefCounted
{
	public Godot.HexGrid Grid;
	public Godot.UnitManager UnitManager;


	public override void _Init(Godot.HexGrid p_grid, Godot.UnitManager p_unit_manager = null)
	{
		Grid = p_grid;
		UnitManager = p_unit_manager;


		//# Find the optimal path between two cells using A* algorithm.

	}//# Returns dictionary with path, cost, and reachable status.
	public Dictionary FindPath(Godot.HexCell start, Godot.HexCell end, Dictionary options = new Dictionary{})
	{
		var ignore_units = options.Get("ignore_units", false);
		var max_cost = options.Get("max_cost", Mathf.Inf);
		var unit_type = options.Get("unit_type", null);


		// Quick check: destination must be passable for this unit type
		var dest_passable;
		if(unit_type != null)
		{
			dest_passable = MovementCosts.IsPassableForUnit(end, unit_type);
		}
		else
		{
			dest_passable = MovementCosts.IsPassable(end);
		}

		if(!dest_passable)
		{
			return new Dictionary{{"path", new Array{}},{"cost", Mathf.Inf},{"reachable", false},};
		}


		// Quick check: destination can't have a unit (unless ignoring units)
		if(!ignore_units && UnitManager != null)
		{
			var unit_at_end = UnitManager.GetUnitAt(end.Q, end.R);
			if(unit_at_end != null)
			{
				return new Dictionary{{"path", new Array{}},{"cost", Mathf.Inf},{"reachable", false},};
			}
		}


		// Same cell - trivial path
		if(start.Q == end.Q && start.R == end.R)
		{
			return new Dictionary{{"path", new Array{start, }},{"cost", 0},{"reachable", true},};
		}

		var frontier = PriorityQueue.New();
		var came_from = new Dictionary{};
		// cell_key -> HexCell
		var cost_so_far = new Dictionary{};

		// cell_key -> float
		var start_key = _CellKey(start);
		var end_key = _CellKey(end);

		frontier.Enqueue(start, 0);
		came_from[start_key] = null;
		cost_so_far[start_key] = 0;

		while(!frontier.IsEmpty())
		{
			var current = frontier.Dequeue();
			var current_key = _CellKey(current);


			// Found destination
			if(current_key == end_key)
			{
				return new Dictionary{
									{"path", _ReconstructPath(came_from, start, end)},
									{"cost", cost_so_far[end_key]},
									{"reachable", true},
									};
			}


			// Explore neighbors
			foreach(HexCell neighbor in Grid.GetNeighbors(current))
			{

				// Skip if there's a unit (unless ignoring units)
				if(!ignore_units && UnitManager != null)
				{
					var unit_at_neighbor = UnitManager.GetUnitAt(neighbor.Q, neighbor.R);

					// Allow destination even if pathfinding toward a unit (for attack targeting)
					if(unit_at_neighbor != null && _CellKey(neighbor) != end_key)
					{
						continue;
					}
				}


				// Calculate movement cost (domain-aware if unit type provided)
				var move_cost;
				if(unit_type != null)
				{
					move_cost = MovementCosts.GetMovementCostForUnit(current, neighbor, unit_type);
				}
				else
				{
					move_cost = MovementCosts.GetMovementCost(current, neighbor);
				}


				// Skip impassable terrain
				if(!Mathf.IsFinite(move_cost))
				{
					continue;
				}

				var new_cost = cost_so_far[current_key] + move_cost;


				// Skip if exceeds max cost
				if(new_cost > max_cost)
				{
					continue;
				}

				var neighbor_key = _CellKey(neighbor);

				if(!cost_so_far.ContainsKey(neighbor_key) || new_cost < cost_so_far[neighbor_key])
				{
					cost_so_far[neighbor_key] = new_cost;


					// A* priority = cost so far + heuristic estimate to goal
					var priority = new_cost + _Heuristic(neighbor, end);
					frontier.Enqueue(neighbor, priority);
					came_from[neighbor_key] = current;
				}
			}
		}


		// No path found
		return new Dictionary{{"path", new Array{}},{"cost", Mathf.Inf},{"reachable", false},};


		//# Get all cells reachable from a starting cell within a movement budget.

	}//# Returns dictionary of cell -> movement cost.
	public Dictionary GetReachableCells(Godot.HexCell start, double movement_points, Dictionary options = new Dictionary{})
	{
		var ignore_units = options.Get("ignore_units", false);
		var unit_type = options.Get("unit_type", null);

		var reachable = new Dictionary{};
		// HexCell -> float
		var frontier = PriorityQueue.New();
		var cost_so_far = new Dictionary{};

		// cell_key -> float
		var start_key = _CellKey(start);
		frontier.Enqueue(start, 0);
		cost_so_far[start_key] = 0;
		reachable[start] = 0;

		while(!frontier.IsEmpty())
		{
			var current = frontier.Dequeue();
			if(current == null)
			{
				break;
			}
			var current_key = _CellKey(current);
			var current_cost = cost_so_far.Get(current_key, 0.0);

			foreach(HexCell neighbor in Grid.GetNeighbors(current))
			{

				// Skip if there's a unit (unless ignoring)
				if(!ignore_units && UnitManager != null)
				{
					var unit_at_neighbor = UnitManager.GetUnitAt(neighbor.Q, neighbor.R);
					if(unit_at_neighbor != null)
					{
						continue;
					}
				}


				// Calculate movement cost (domain-aware if unit type provided)
				var move_cost;
				if(unit_type != null)
				{
					move_cost = MovementCosts.GetMovementCostForUnit(current, neighbor, unit_type);
				}
				else
				{
					move_cost = MovementCosts.GetMovementCost(current, neighbor);
				}


				// Skip impassable
				if(!Mathf.IsFinite(move_cost))
				{
					continue;
				}

				var new_cost = current_cost + move_cost;


				// Skip if exceeds movement budget
				if(new_cost > movement_points)
				{
					continue;
				}

				var neighbor_key = _CellKey(neighbor);

				if(!cost_so_far.ContainsKey(neighbor_key) || new_cost < cost_so_far[neighbor_key])
				{
					cost_so_far[neighbor_key] = new_cost;
					frontier.Enqueue(neighbor, new_cost);
					reachable[neighbor] = new_cost;
				}
			}
		}

		return reachable;
	}


	//# Check if a path exists between two cells.
	public bool HasPath(Godot.HexCell start, Godot.HexCell end, bool ignore_units = false)
	{
		var result = FindPath(start, end, new Dictionary{{"ignore_units", ignore_units},});
		return result["reachable"];
	}


	//# Get the movement cost between two adjacent cells.
	public double GetStepCost(Godot.HexCell from, Godot.HexCell to)
	{

		// Check if adjacent
		var from_coords = HexCoordinates.New(from.Q, from.R);
		var to_coords = HexCoordinates.New(to.Q, to.R);

		if(from_coords.DistanceTo(to_coords) != 1)
		{
			return Mathf.Inf;
		}

		// Not adjacent
		return MovementCosts.GetMovementCost(from, to);
	}


	//# Heuristic function for A* - hex distance.
	protected double _Heuristic(Godot.HexCell a, Godot.HexCell b)
	{
		var coords_a = HexCoordinates.New(a.Q, a.R);
		var coords_b = HexCoordinates.New(b.Q, b.R);
		return Float(coords_a.DistanceTo(coords_b));
	}


	//# Generate a unique key for a cell.
	protected String _CellKey(Godot.HexCell cell)
	{
		return "%d,%d" % new Array{cell.Q, cell.R, };
	}


	//# Reconstruct the path from the came_from map.
	protected Array<HexCell> _ReconstructPath(Dictionary came_from, Godot.HexCell start, Godot.HexCell end)
	{
		var path = new Array{};
		var current = end;

		while(current != null)
		{
			path.PushFront(current);
			var key = _CellKey(current);
			current = came_from.Get(key);
		}

		return path;
	}


}