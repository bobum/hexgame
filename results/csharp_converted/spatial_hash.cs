using Godot;
using Godot.Collections;


//# Generic spatial hash grid for O(1) point lookups and efficient range queries.
//# Used for finding units, features, or any spatially-located objects.

//# Matches web/src/utils/SpatialHash.ts
[GlobalClass]
public partial class SpatialHash : Godot.RefCounted
{
	protected Dictionary _Buckets = new Dictionary{};
	// "bx,bz" -> Array of items
	protected Dictionary _ItemPositions = new Dictionary{};
	// item -> {x, z}
	protected double _CellSize;


	// Statistics
	protected int _InsertCount = 0;
	protected int _QueryCount = 0;
	protected int _PeakItems = 0;


	//# Create a spatial hash with the given cell size.
	//# Smaller cells = more memory but faster range queries.
	//# Recommended: cell size slightly larger than typical query radius.
	public override void _Init(double cell_size = 2.0)
	{
		_CellSize = cell_size;
	}


	//# Get the bucket key for a world position.
	protected String _GetKey(double x, double z)
	{
		var bx = Int(Mathf.Floor(x / _CellSize));
		var bz = Int(Mathf.Floor(z / _CellSize));
		return "%d,%d" % new Array{bx, bz, };
	}


	//# Get bucket coordinates for a world position.
	protected Vector2i _GetBucketCoords(double x, double z)
	{
		return new Vector2i(Int(Mathf.Floor(x / _CellSize)), Int(Mathf.Floor(z / _CellSize)));
	}


	//# Insert an item at the given position.
	public void Insert(Godot.Variant item, double x, double z)
	{

		// Remove from old position if already tracked
		if(_ItemPositions.ContainsKey(item))
		{
			Remove(item);
		}

		var key = _GetKey(x, z);
		if(!_Buckets.ContainsKey(key))
		{
			_Buckets[key] = new Array{};
		}
		_Buckets[key].Append(item);
		_ItemPositions[item] = new Dictionary{{"x", x},{"z", z},};

		_InsertCount += 1;
		_PeakItems = Mathf.Max(_PeakItems, _ItemPositions.Size());
	}


	//# Remove an item from the hash.
	public bool Remove(Godot.Variant item)
	{
		if(!_ItemPositions.ContainsKey(item))
		{
			return false;
		}

		var pos = _ItemPositions[item];
		var key = _GetKey(pos["x"], pos["z"]);

		if(_Buckets.ContainsKey(key))
		{
			var bucket = _Buckets[key];
			bucket.Erase(item);
			if(bucket.IsEmpty())
			{
				_Buckets.Erase(key);
			}
		}

		_ItemPositions.Erase(item);
		return true;
	}


	//# Update an item's position (remove + insert).
	public void Update(Godot.Variant item, double x, double z)
	{
		Insert(item, x, z);


		// insert handles removal automatically

	}//# Get all items at exact bucket position (O(1)).
	public Array GetAt(double x, double z)
	{
		_QueryCount += 1;
		var key = _GetKey(x, z);
		if(_Buckets.ContainsKey(key))
		{
			return _Buckets[key].Duplicate();
		}
		return new Array{};
	}


	//# Get the first item at a position, or null.
	public Godot.Variant GetFirstAt(double x, double z)
	{
		_QueryCount += 1;
		var key = _GetKey(x, z);
		if(_Buckets.ContainsKey(key))
		{
			var bucket = _Buckets[key];
			if(!bucket.IsEmpty())
			{
				return bucket[0];
			}
		}
		return null;
	}


	//# Check if any item exists at position.
	public bool HasAt(double x, double z)
	{
		var key = _GetKey(x, z);
		if(_Buckets.ContainsKey(key))
		{
			var bucket = _Buckets[key];
			return !bucket.IsEmpty();
		}
		return false;


		//# Query all items within a radius of the given point.

	}//# Uses squared distance for efficiency.
	public Array QueryRadius(double x, double z, double radius)
	{
		_QueryCount += 1;
		var results = new Array{};
		var radius_sq = radius * radius;


		// Calculate bucket range to check
		var min_b = _GetBucketCoords(x - radius, z - radius);
		var max_b = _GetBucketCoords(x + radius, z + radius);


		// Check all buckets in range
		foreach(int bx in GD.Range(min_b.X, max_b.X + 1))
		{
			foreach(int bz in GD.Range(min_b.Y, max_b.Y + 1))
			{
				var key = "%d,%d" % new Array{bx, bz, };
				if(!_Buckets.ContainsKey(key))
				{
					continue;
				}

				foreach(Variant item in _Buckets[key])
				{
					var pos = _ItemPositions.Get(item);
					if(pos == null)
					{
						continue;
					}

					var dx = pos["x"] - x;
					var dz = pos["z"] - z;
					if(dx * dx + dz * dz <= radius_sq)
					{
						results.Append(item);
					}
				}
			}
		}

		return results;
	}


	//# Query all items within a rectangular area.
	public Array QueryRect(double min_x, double min_z, double max_x, double max_z)
	{
		_QueryCount += 1;
		var results = new Array{};

		var min_b = _GetBucketCoords(min_x, min_z);
		var max_b = _GetBucketCoords(max_x, max_z);

		foreach(int bx in GD.Range(min_b.X, max_b.X + 1))
		{
			foreach(int bz in GD.Range(min_b.Y, max_b.Y + 1))
			{
				var key = "%d,%d" % new Array{bx, bz, };
				if(!_Buckets.ContainsKey(key))
				{
					continue;
				}

				foreach(Variant item in _Buckets[key])
				{
					var pos = _ItemPositions.Get(item);
					if(pos == null)
					{
						continue;
					}

					if(pos["x"] >= min_x && pos["x"] <= max_x && pos["z"] >= min_z && pos["z"] <= max_z)
					{
						results.Append(item);
					}
				}
			}
		}

		return results;
	}


	//# Get all items in the hash.
	public Array GetAll()
	{
		return _ItemPositions.Keys();
	}


	//# Get the position of an item.
	public Godot.Variant GetPosition(Godot.Variant item)
	{
		return _ItemPositions.Get(item);
	}


	//# Check if an item is in the hash.
	public bool Has(Godot.Variant item)
	{
		return _ItemPositions.ContainsKey(item);
	}


	//# Clear all items.
	public void Clear()
	{
		_Buckets.Clear();
		_ItemPositions.Clear();
	}


	//# Get the number of items in the hash.
	public int GetSize()
	{
		return _ItemPositions.Size();
	}


	//# Get the number of active buckets.
	public int GetBucketCount()
	{
		return _Buckets.Size();
	}


	//# Get statistics for debugging.
	public Dictionary GetStats()
	{
		return new Dictionary{
					{"items", _ItemPositions.Size()},
					{"buckets", _Buckets.Size()},
					{"insert_count", _InsertCount},
					{"query_count", _QueryCount},
					{"peak_items", _PeakItems},
					{"cell_size", _CellSize},
					};
	}


	//# Reset statistics counters.
	public void ResetStats()
	{
		_InsertCount = 0;
		_QueryCount = 0;
	}


}