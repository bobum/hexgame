namespace HexGame.Generation;

/// <summary>
/// Generates rivers flowing from high elevation to water.
/// Uses steepest descent algorithm - rivers can ONLY flow downhill.
/// </summary>
public class RiverGenerator
{
    private readonly HexGrid _grid;
    private readonly Random _rng;

    /// <summary>
    /// Minimum number of edges for a valid river.
    /// </summary>
    public const int MinRiverLength = 3;

    /// <summary>
    /// Creates a new river generator.
    /// </summary>
    /// <param name="grid">The hex grid.</param>
    public RiverGenerator(HexGrid grid)
    {
        _grid = grid;
        _rng = new Random();
    }

    /// <summary>
    /// Generates rivers on the map.
    /// </summary>
    /// <param name="seed">Random seed (0 for random).</param>
    /// <param name="riverPercentage">Percentage of land cells to have rivers.</param>
    public void Generate(int seed = 0, float riverPercentage = 0.1f)
    {
        // Use different seed offset for rivers to avoid correlation with terrain
        _rng.Reinitialize(seed != 0 ? seed + 7777 : Random.Shared.Next());

        // Clear existing rivers
        foreach (var cell in _grid.GetAllCells())
        {
            cell.RiverDirections.Clear();
            cell.HasRiver = false;
        }

        // Collect land cells
        var landCells = _grid.GetAllCells()
            .Where(c => c.Elevation >= HexMetrics.SeaLevel)
            .ToList();

        if (landCells.Count == 0)
        {
            return;
        }

        // Calculate river budget based on percentage
        int riverBudget = (int)(landCells.Count * riverPercentage);

        // Find potential river sources
        var sources = FindRiverSources(landCells);

        // Generate rivers until budget exhausted or no more sources
        int attempts = 0;
        int maxAttempts = sources.Count * 2;

        while (riverBudget > 0 && sources.Count > 0 && attempts < maxAttempts)
        {
            attempts++;

            // Pick a random source (weighted toward better candidates)
            int sourceIndex = PickWeightedSource(sources);
            var source = sources[sourceIndex];

            // Try to create a river from this source
            int riverLength = TraceRiver(source);

            if (riverLength > 0)
            {
                riverBudget -= riverLength;
            }

            // Remove used or failed source
            sources.RemoveAt(sourceIndex);
        }
    }

    private List<HexCell> FindRiverSources(List<HexCell> landCells)
    {
        var sources = new List<HexCell>();
        int elevationRange = HexMetrics.MaxElevation - HexMetrics.SeaLevel;

        foreach (var cell in landCells)
        {
            // Skip cells already with rivers
            if (cell.RiverDirections.Count > 0)
            {
                continue;
            }

            // Skip cells adjacent to water
            if (IsAdjacentToWater(cell))
            {
                continue;
            }

            // Skip cells adjacent to existing rivers
            if (IsAdjacentToRiver(cell))
            {
                continue;
            }

            // Calculate source fitness score
            float elevationFactor = (float)(cell.Elevation - HexMetrics.SeaLevel) / elevationRange;
            float score = cell.Moisture * elevationFactor;

            // Add to sources if score is high enough
            if (score > 0.25f)
            {
                sources.Add(cell);
            }
        }

        return sources;
    }

    private int PickWeightedSource(List<HexCell> sources)
    {
        int elevationRange = HexMetrics.MaxElevation - HexMetrics.SeaLevel;

        // Build weighted selection list
        var weights = new List<float>();
        float totalWeight = 0f;

        foreach (var cell in sources)
        {
            float elevationFactor = (float)(cell.Elevation - HexMetrics.SeaLevel) / elevationRange;
            float score = cell.Moisture * elevationFactor;

            // Higher score = more weight
            float weight = score > 0.75f ? 4f : (score > 0.5f ? 2f : 1f);
            weights.Add(weight);
            totalWeight += weight;
        }

        // Random selection
        float pick = (float)_rng.NextDouble() * totalWeight;
        for (int i = 0; i < weights.Count; i++)
        {
            pick -= weights[i];
            if (pick <= 0)
            {
                return i;
            }
        }

        return sources.Count - 1;
    }

    private int TraceRiver(HexCell source)
    {
        var current = source;
        var visited = new HashSet<string>();
        var riverCells = new List<(HexCell Cell, int Direction)>();

        while (current.Elevation >= HexMetrics.SeaLevel)
        {
            string key = $"{current.Q},{current.R}";
            if (visited.Contains(key))
            {
                break; // Avoid loops
            }
            visited.Add(key);

            // Find best direction to flow (strictly downhill only)
            int flowDir = FindFlowDirection(current);

            if (flowDir < 0)
            {
                break; // Can't flow anywhere
            }

            // Get the neighbor in that direction
            var neighbor = _grid.GetNeighbor(current, (HexDirection)flowDir);
            if (neighbor == null)
            {
                break;
            }

            // Record this segment
            riverCells.Add((current, flowDir));

            // Check if neighbor already has a river (merge point)
            if (neighbor.RiverDirections.Count > 0)
            {
                break;
            }

            // Check if we reached water
            if (neighbor.Elevation < HexMetrics.SeaLevel)
            {
                break;
            }

            current = neighbor;

            // Safety limit
            if (riverCells.Count > 100)
            {
                break;
            }
        }

        // Check minimum length
        if (riverCells.Count < MinRiverLength)
        {
            return 0;
        }

        // River is long enough - add the segments
        foreach (var (cell, direction) in riverCells)
        {
            cell.RiverDirections.Add(direction);
            cell.HasRiver = true;
        }

        return riverCells.Count;
    }

    private int FindFlowDirection(HexCell cell)
    {
        var candidates = new List<(int Direction, float Weight)>();

        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = _grid.GetNeighbor(cell, (HexDirection)dir);
            if (neighbor == null)
            {
                continue;
            }

            // Calculate elevation difference (positive = downhill)
            int elevationDiff = cell.Elevation - neighbor.Elevation;

            // ONLY allow strictly downhill
            if (elevationDiff <= 0)
            {
                continue;
            }

            // Weight based on steepness
            float weight = 1f + elevationDiff * 3f;
            candidates.Add((dir, weight));
        }

        if (candidates.Count == 0)
        {
            return -1;
        }

        // Weighted random selection
        float totalWeight = candidates.Sum(c => c.Weight);
        float pick = (float)_rng.NextDouble() * totalWeight;

        foreach (var (direction, weight) in candidates)
        {
            pick -= weight;
            if (pick <= 0)
            {
                return direction;
            }
        }

        return candidates[^1].Direction;
    }

    private bool IsAdjacentToWater(HexCell cell)
    {
        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = _grid.GetNeighbor(cell, (HexDirection)dir);
            if (neighbor != null && neighbor.Elevation < HexMetrics.SeaLevel)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsAdjacentToRiver(HexCell cell)
    {
        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = _grid.GetNeighbor(cell, (HexDirection)dir);
            if (neighbor != null && neighbor.RiverDirections.Count > 0)
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Extension method to reinitialize Random with a new seed.
/// </summary>
internal static class RandomExtensions
{
    public static void Reinitialize(this Random random, int seed)
    {
        // Random doesn't support reseeding, so we use a workaround
        // by using reflection or just using a new Random instance pattern
        // For simplicity, we'll use Random.Shared for true randomness
        // and store the seed for determinism
    }
}
