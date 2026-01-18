using Godot;
using Godot.Collections;


//# Manages all units in the game.

//# Matches web/src/units/UnitManager.ts
[GlobalClass]
public partial class UnitManager : Godot.RefCounted
{
	public Dictionary Units = new Dictionary{};
	// id -> Unit
	public int NextId = 1;
	public Godot.HexGrid Grid;


	// Spatial indexing for O(1) lookups
	protected Dictionary _HexPositions = new Dictionary{};
	// "q,r" -> Unit (for hex coordinate lookups)
	protected Godot.SpatialHash _SpatialHash;

	// For world coordinate queries (radius, rect)
	// Object pooling for units (reduces GC pressure)
	protected Godot.ObjectPool _UnitPool;

	[Signal]
	public delegate void UnitCreatedEventHandler(Godot.Unit unit);
	[Signal]
	public delegate void UnitRemovedEventHandler(int unit_id);
	[Signal]
	public delegate void UnitMovedEventHandler(Godot.Unit unit, int from_q, int from_r);


	public override void _Init(Godot.HexGrid p_grid)
	{
		Grid = p_grid;

		// Cell size of 2.0 works well for hex grids (slightly larger than hex radius)
		_SpatialHash = SpatialHash.New(2.0);

		// Initialize unit pool (will be set up after _init completes)
		_UnitPool = null;
	}


	//# Set up object pooling (call after construction)
	public void SetupPool()
	{


		// Max pool size
		_UnitPool = ObjectPool.New(, new Callable(this, "_create_unit_for_pool"), new Callable(this, "_reset_unit_for_pool"), 500);
	}


	//# Factory function for unit pool.
	protected Godot.Unit _CreateUnitForPool()
	{
		return Unit.New();
	}


	//# Reset function for unit pool.
	protected void _ResetUnitForPool(Godot.Unit unit)
	{
		unit.ResetForPool();
	}


	//# Create a new unit at the specified hex.
	public Godot.Unit CreateUnit(UnitTypes.Type type, int q, int r, int player_id)
	{

		// Check if hex is valid
		var cell = Grid.GetCell(q, r);
		if(cell == null)
		{
			return null;
		}


		// Check domain compatibility
		var is_water = cell.Elevation < HexMetrics.SEA_LEVEL;
		if(is_water && !UnitTypes.CanTraverseWater(type))
		{
			return null;
		}
		// Land unit can't be placed on water
		if(!is_water && !UnitTypes.CanTraverseLand(type))
		{
			return null;

			// Naval unit can't be placed on land

		}// Check if hex is already occupied
		if(GetUnitAt(q, r) != null)
		{
			return null;
		}


		// Acquire unit from pool (or create directly if pool not set up)
		var unit;
		if(_UnitPool)
		{
			unit = _UnitPool.Acquire();
		}
		else
		{
			unit = Unit.New();
		}
		unit.Id = NextId;
		NextId += 1;
		unit.InitWith(type, q, r, player_id);


		// Add to tracking
		Units[unit.Id] = unit;


		// Add to spatial indexes
		var hex_key = "%d,%d" % new Array{q, r, };
		_HexPositions[hex_key] = unit;
		var world_pos = HexCoordinates.New(q, r).ToWorldPosition(cell.Elevation);
		_SpatialHash.Insert(unit, world_pos.X, world_pos.Z);

		EmitSignal("UnitCreated", unit);

		return unit;
	}


	//# Remove a unit from the game.
	public bool RemoveUnit(int unit_id)
	{
		if(!Units.ContainsKey(unit_id))
		{
			return false;
		}

		var unit = Units[unit_id];


		// Remove from spatial indexes
		var hex_key = "%d,%d" % new Array{unit.Q, unit.R, };
		_HexPositions.Erase(hex_key);
		_SpatialHash.Remove(unit);

		Units.Erase(unit_id);


		// Release unit back to pool for reuse (if pool is set up)
		if(_UnitPool)
		{
			_UnitPool.Release(unit);
		}

		EmitSignal("UnitRemoved", unit_id);
		return true;
	}


	//# Move a unit to a new hex.
	public bool MoveUnit(int unit_id, int to_q, int to_r, int movement_cost =  - 1)
	{
		var unit = Units.Get(unit_id);
		if(unit == null)
		{
			return false;
		}


		// Check destination is valid
		var cell = Grid.GetCell(to_q, to_r);
		if(cell == null)
		{
			return false;
		}


		// Check domain compatibility
		var is_water = cell.Elevation < HexMetrics.SEA_LEVEL;
		if(is_water && !unit.CanTraverseWater())
		{
			return false;
		}
		if(!is_water && !unit.CanTraverseLand())
		{
			return false;
		}


		// Check not occupied
		if(GetUnitAt(to_q, to_r) != null)
		{
			return false;
		}


		// Check movement cost if provided
		if(movement_cost >= 0)
		{
			if(unit.Movement < movement_cost)
			{
				return false;
			}
			unit.SpendMovement(movement_cost);
		}


		// Store old position
		var from_q = unit.Q;
		var from_r = unit.R;


		// Update spatial indexes (remove old, add new)
		var old_hex_key = "%d,%d" % new Array{from_q, from_r, };
		var new_hex_key = "%d,%d" % new Array{to_q, to_r, };
		_HexPositions.Erase(old_hex_key);
		_HexPositions[new_hex_key] = unit;


		// Update position
		unit.Q = to_q;
		unit.R = to_r;


		// Update spatial hash with new world position
		var world_pos = HexCoordinates.New(to_q, to_r).ToWorldPosition(cell.Elevation);
		_SpatialHash.Update(unit, world_pos.X, world_pos.Z);

		EmitSignal("UnitMoved", unit, from_q, from_r);
		return true;
	}


	//# Reset movement for all units (called at start of turn).
	public void ResetAllMovement()
	{
		foreach(Variant unit in Units.Values())
		{
			unit.ResetMovement();
		}
	}


	//# Reset movement for units of a specific player.
	public void ResetPlayerMovement(int player_id)
	{
		foreach(Variant unit in Units.Values())
		{
			if(unit.PlayerId == player_id)
			{
				unit.ResetMovement();
			}
		}
	}


	//# Get unit at a specific hex (O(1) lookup via spatial index).
	public Godot.Unit GetUnitAt(int q, int r)
	{
		var hex_key = "%d,%d" % new Array{q, r, };
		return _HexPositions.Get(hex_key);
	}


	//# Get unit by ID.
	public Godot.Unit GetUnit(int id)
	{
		return Units.Get(id);
	}


	//# Get all units.
	public Array<Unit> GetAllUnits()
	{
		var result = new Array{};
		foreach(Variant unit in Units.Values())
		{
			result.Append(unit);
		}
		return result;
	}


	//# Get units for a specific player.
	public Array<Unit> GetPlayerUnits(int player_id)
	{
		var result = new Array{};
		foreach(Variant unit in Units.Values())
		{
			if(unit.PlayerId == player_id)
			{
				result.Append(unit);
			}
		}
		return result;
	}


	//# Get unit count.
	public int GetUnitCount()
	{
		return Units.Size();
	}


	//# Get counts by domain (land vs naval).
	public Dictionary GetUnitCounts()
	{
		var land = 0;
		var naval = 0;
		foreach(Variant unit in Units.Values())
		{
			if(UnitTypes.GetDomain(unit.Type) == UnitTypes.Domain.NAVAL)
			{
				naval += 1;
			}
			else
			{
				land += 1;
			}
		}
		return new Dictionary{{"land", land},{"naval", naval},};
	}


	//# Clear all units.
	public void Clear()
	{

		// Release all units back to pool (if pool is set up)
		if(_UnitPool)
		{
			foreach(Variant unit in Units.Values())
			{
				_UnitPool.Release(unit);
			}
		}
		Units.Clear();
		_HexPositions.Clear();
		_SpatialHash.Clear();
		NextId = 1;
	}


	//# Get units within a world-coordinate radius (for range attacks, area effects).
	public Array<Unit> GetUnitsInRadius(double world_x, double world_z, double radius)
	{
		var results = new Array{};
		foreach(Variant item in _SpatialHash.QueryRadius(world_x, world_z, radius))
		{
			if(item is Godot.Unit)
			{
				results.Append(item);
			}
		}
		return results;
	}


	//# Get units within a world-coordinate rectangle (for selection box).
	public Array<Unit> GetUnitsInRect(double min_x, double min_z, double max_x, double max_z)
	{
		var results = new Array{};
		foreach(Variant item in _SpatialHash.QueryRect(min_x, min_z, max_x, max_z))
		{
			if(item is Godot.Unit)
			{
				results.Append(item);
			}
		}
		return results;
	}


	//# Get spatial hash statistics for debugging.
	public Dictionary GetSpatialStats()
	{
		return _SpatialHash.GetStats();
	}


	//# Get object pool statistics for debugging.
	public Dictionary GetPoolStats()
	{
		if(_UnitPool)
		{
			return _UnitPool.GetStats();
		}
		return new Dictionary{{"available", 0},{"active", 0},{"created", 0},{"reused", 0},{"peak", 0},{"reuse_rate", 0.0},};
	}


	//# Prewarm unit pool with objects.
	public void PrewarmPool(int count)
	{
		if(_UnitPool)
		{
			_UnitPool.Prewarm(count);
		}
	}


	//# Spawn random land units for testing.
	public int SpawnRandomUnits(int count, int player_id = 1)
	{
		var land_cells = new Array{};
		foreach(HexCell cell in Grid.GetAllCells())
		{
			if(cell.Elevation >= HexMetrics.SEA_LEVEL)
			{
				land_cells.Append(cell);
			}
		}

		var spawned = 0;
		var land_types = UnitTypes.GetLandTypes();

		foreach(int i in GD.Range(count))
		{
			if(land_cells.IsEmpty())
			{
				break;
			}
			var idx = GD.Randi() % land_cells.Size();
			var cell = land_cells[idx];
			var type = land_types[GD.Randi() % land_types.Size()];

			if(CreateUnit(type, cell.Q, cell.R, player_id))
			{
				spawned += 1;
				land_cells.RemoveAt(idx);
			}
		}

		return spawned;
	}


	//# Spawn random naval units for testing.
	public int SpawnRandomNavalUnits(int count, int player_id = 1)
	{
		var water_cells = new Array{};
		foreach(HexCell cell in Grid.GetAllCells())
		{
			if(cell.Elevation < HexMetrics.SEA_LEVEL)
			{
				water_cells.Append(cell);
			}
		}

		var spawned = 0;
		var naval_types = UnitTypes.GetNavalTypes();

		foreach(int i in GD.Range(count))
		{
			if(water_cells.IsEmpty())
			{
				break;
			}
			var idx = GD.Randi() % water_cells.Size();
			var cell = water_cells[idx];
			var type = naval_types[GD.Randi() % naval_types.Size()];

			if(CreateUnit(type, cell.Q, cell.R, player_id))
			{
				spawned += 1;
				water_cells.RemoveAt(idx);
			}
		}

		return spawned;
	}


	//# Spawn a mix of land and naval units for testing.
	public Dictionary SpawnMixedUnits(int land_count, int naval_count, int player_id = 1)
	{
		return new Dictionary{
					{"land", SpawnRandomUnits(land_count, player_id)},
					{"naval", SpawnRandomNavalUnits(naval_count, player_id)},
					};
	}


}