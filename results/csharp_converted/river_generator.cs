using Godot;
using Godot.Collections;


//# Generates rivers flowing from high elevation to water.
//# Uses steepest descent algorithm - rivers can ONLY flow downhill.

//# Matches web/src/generation/RiverGenerator.ts
[GlobalClass]
public partial class RiverGenerator : Godot.RefCounted
{
	public Godot.HexGrid Grid;
	public double RiverPercentage = 0.1;
	public int SeedVal = 0;


	// Minimum number of edges for a valid river
	public const int MIN_RIVER_LENGTH = 3;


	// Simple seeded random
	protected Godot.RandomNumberGenerator _Rng;


	public override void _Init(Godot.HexGrid p_grid)
	{
		Grid = p_grid;
		_Rng = RandomNumberGenerator.New();
	}


	//# Generate rivers on the map.
	public void Generate(int p_seed = 0, double p_river_percentage = 0.1)
	{
		SeedVal = ( p_seed != 0 ? p_seed : GD.Randi() );
		RiverPercentage = p_river_percentage;


		// Use different seed offset for rivers to avoid correlation with terrain
		_Rng.Seed = SeedVal + 7777;


		// Clear existing rivers
		foreach(HexCell cell in Grid.GetAllCells())
		{
			cell.RiverDirections.Clear();
			cell.HasRiver = false;
		}


		// Count land cells for budget calculation
		var land_cells = new Array{};
		foreach(HexCell cell in Grid.GetAllCells())
		{
			if(cell.Elevation >= HexMetrics.SEA_LEVEL)
			{
				land_cells.Append(cell);
			}
		}

		if(land_cells.IsEmpty())
		{
			return ;
		}


		// Calculate river budget based on percentage
		var river_budget = Int(land_cells.Size() * RiverPercentage);


		// Find potential river sources (high elevation + moisture)
		var sources = _FindRiverSources(land_cells);


		// Generate rivers until budget exhausted or no more sources
		var attempts = 0;
		var max_attempts = sources.Size() * 2;

		while(river_budget > 0 && !sources.IsEmpty() && attempts < max_attempts)
		{
			attempts += 1;


			// Pick a random source (weighted toward better candidates)
			var source_index = _PickWeightedSource(sources);
			var source = sources[source_index];


			// Try to create a river from this source
			var river_length = _TraceRiver(source);

			if(river_length > 0)
			{
				river_budget -= river_length;

				// Remove used source
				sources.RemoveAt(source_index);
			}
			else
			{

				// Source didn't work, remove it
				sources.RemoveAt(source_index);


				//# Find cells that are good river sources.

			}
		}
	}//# High elevation + high moisture = good source.
	protected Array<HexCell> _FindRiverSources(Array<HexCell> land_cells)
	{
		var sources = new Array{};
		var elevation_range = HexMetrics.MAX_ELEVATION - HexMetrics.SEA_LEVEL;

		foreach(HexCell cell in land_cells)
		{

			// Skip cells already with rivers
			if(!cell.RiverDirections.IsEmpty())
			{
				continue;
			}


			// Skip cells adjacent to water (too close to ocean)
			if(_IsAdjacentToWater(cell))
			{
				continue;
			}


			// Skip cells adjacent to existing rivers
			if(_IsAdjacentToRiver(cell))
			{
				continue;
			}


			// Calculate source fitness score
			var elevation_factor = Float(cell.Elevation - HexMetrics.SEA_LEVEL) / elevation_range;
			var score = cell.Moisture * elevation_factor;


			// Add to sources with weighting
			if(score > 0.25)
			{
				sources.Append(cell);
			}
		}

		return sources;
	}


	//# Pick a source using weighted random selection.
	protected int _PickWeightedSource(Array<HexCell> sources)
	{
		var elevation_range = HexMetrics.MAX_ELEVATION - HexMetrics.SEA_LEVEL;


		// Build weighted selection list
		var weights = new Array{};
		var total_weight = 0.0;

		foreach(HexCell cell in sources)
		{
			var elevation_factor = Float(cell.Elevation - HexMetrics.SEA_LEVEL) / elevation_range;
			var score = cell.Moisture * elevation_factor;


			// Higher score = more weight
			var weight = 1.0;
			if(score > 0.75)
			{
				weight = 4.0;
			}
			else if(score > 0.5)
			{
				weight = 2.0;
			}

			weights.Append(weight);
			total_weight += weight;
		}


		// Random selection
		var pick = _Rng.Randf() * total_weight;
		foreach(int i in GD.Range(weights.Size()))
		{
			pick -= weights[i];
			if(pick <= 0)
			{
				return i;
			}
		}

		return sources.Size() - 1;


		//# Trace a river from source to water using steepest descent.
		//# Rivers can ONLY flow downhill. If stuck or too short, discard entirely.

	}//# Returns the length of the river created (0 if discarded).
	protected int _TraceRiver(Godot.HexCell source)
	{
		var current = source;
		var visited = new Dictionary{};


		// Track cells we add river segments to, so we can remove them if river is too short
		var river_cells = new Array{};

		while(current.Elevation >= HexMetrics.SEA_LEVEL)
		{
			var key = "%d,%d" % new Array{current.Q, current.R, };
			if(visited.ContainsKey(key))
			{
				break;
			}
			// Avoid loops
			visited[key] = true;


			// Find best direction to flow (strictly downhill only)
			var flow_dir = _FindFlowDirection(current);

			if(flow_dir < 0)
			{

				// Can't flow anywhere - dead end, stop here
				break;
			}


			// Get the neighbor in that direction
			var neighbor = Grid.GetNeighbor(current, flow_dir);
			if(!neighbor)
			{
				break;
			}


			// Record this segment (don't add to cell yet)
			river_cells.Append(new Dictionary{{"cell", current},{"direction", flow_dir},});


			// Check if neighbor already has a river (merge point)
			if(!neighbor.RiverDirections.IsEmpty())
			{
				break;
			}


			// Check if we reached water
			if(neighbor.Elevation < HexMetrics.SEA_LEVEL)
			{
				break;
			}


			// Move to next cell
			current = neighbor;


			// Safety limit
			if(river_cells.Size() > 100)
			{
				break;
			}
		}


		// Check minimum length - discard if too short
		if(river_cells.Size() < MIN_RIVER_LENGTH)
		{
			return 0;

			// River too short, don't create it

		}// River is long enough - actually add the segments to cells
		foreach(Dictionary entry in river_cells)
		{
			var cell = entry["cell"];
			var direction = entry["direction"];
			cell.RiverDirections.Append(direction);
			cell.HasRiver = true;
		}

		return river_cells.Size();


		//# Find the best direction for water to flow from a cell.
		//# ONLY allows strictly downhill flow (no flat terrain).

	}//# Prefers steepest descent, with randomness for variety.
	protected int _FindFlowDirection(Godot.HexCell cell)
	{
		var candidates = new Array{};

		foreach(int dir in GD.Range(6))
		{
			var neighbor = Grid.GetNeighbor(cell, dir);
			if(!neighbor)
			{
				continue;
			}


			// Calculate elevation difference (positive = downhill)
			var elevation_diff = cell.Elevation - neighbor.Elevation;


			// ONLY allow strictly downhill (elevation must decrease)
			if(elevation_diff <= 0)
			{
				continue;
			}


			// Weight based on steepness - steeper = more likely
			var weight = 1.0 + elevation_diff * 3.0;

			candidates.Append(new Dictionary{{"dir", dir},{"weight", weight},});
		}

		if(candidates.IsEmpty())
		{
			return  - 1;
		}


		// Weighted random selection
		var total_weight = 0.0;
		foreach(Dictionary c in candidates)
		{
			total_weight += c["weight"];
		}

		var pick = _Rng.Randf() * total_weight;

		foreach(Dictionary candidate in candidates)
		{
			pick -= candidate["weight"];
			if(pick <= 0)
			{
				return candidate["dir"];
			}
		}

		return candidates[candidates.Size() - 1]["dir"];
	}


	//# Check if cell is adjacent to water.
	protected bool _IsAdjacentToWater(Godot.HexCell cell)
	{
		foreach(int dir in GD.Range(6))
		{
			var neighbor = Grid.GetNeighbor(cell, dir);
			if(neighbor && neighbor.Elevation < HexMetrics.SEA_LEVEL)
			{
				return true;
			}
		}
		return false;
	}


	//# Check if cell is adjacent to an existing river.
	protected bool _IsAdjacentToRiver(Godot.HexCell cell)
	{
		foreach(int dir in GD.Range(6))
		{
			var neighbor = Grid.GetNeighbor(cell, dir);
			if(neighbor && !neighbor.RiverDirections.IsEmpty())
			{
				return true;
			}
		}
		return false;
	}


}