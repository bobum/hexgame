using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace HexGame.Generation;

/// <summary>
/// Generates rivers flowing from high-elevation/high-moisture sources downhill to water.
/// Follows Catlike Coding Hex Map Tutorial 23-26 patterns with weighted random walk.
///
/// Algorithm:
/// 1. Calculate target number of river cells based on land cell count
/// 2. Find candidate source cells (high elevation + high moisture)
/// 3. For each river:
///    a. Select weighted source from candidates
///    b. Trace path downhill using steepness-weighted selection
///    c. Discard if too short, otherwise apply river directions
/// </summary>
public class RiverGenerator
{
    /// <summary>
    /// Number of hex directions (NE, E, SE, SW, W, NW).
    /// </summary>
    public const int HexDirectionCount = 6;

    private readonly Random _rng;
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    public RiverGenerator(Random rng, int gridWidth, int gridHeight)
    {
        _rng = rng;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
    }

    /// <summary>
    /// Generates rivers on the cell data array.
    /// </summary>
    /// <param name="data">Cell data array (modified in place)</param>
    /// <param name="ct">Optional cancellation token for async generation</param>
    public void Generate(CellData[] data, CancellationToken ct = default)
    {
        if (data.Length == 0 || _gridWidth == 0 || _gridHeight == 0)
            return;

        // Count land cells to determine target rivers
        int landCells = CountLandCells(data);
        if (landCells == 0)
            return;

        int targetRiverCells = (int)(landCells * GenerationConfig.RiverPercentage);
        int riverCellsCreated = 0;

        // Find all candidate sources - use HashSet for O(1) removal
        var candidates = FindRiverSourcesAsSet(data);
        if (candidates.Count == 0)
            return;

        ct.ThrowIfCancellationRequested();

        // Generate rivers until we reach our target or run out of candidates
        int maxAttempts = candidates.Count * 2; // Prevent infinite loops
        int attempts = 0;
        int riversCreated = 0;
        int riversTooShort = 0;

        GD.Print($"[RiverGen] Target: {targetRiverCells} river cells, {candidates.Count} candidates, maxAttempts: {maxAttempts}");

        while (riverCellsCreated < targetRiverCells && candidates.Count > 0 && attempts < maxAttempts)
        {
            if ((attempts & 0x1F) == 0)
                ct.ThrowIfCancellationRequested();

            attempts++;

            // Select weighted source
            int sourceIndex = SelectWeightedSource(candidates, data);
            if (sourceIndex < 0)
                break;

            // Remove this candidate - O(1) with HashSet
            candidates.Remove(sourceIndex);

            // Trace river path from source (includes direction info)
            var path = TraceRiverWithDirections(data, sourceIndex);

            // Validate and apply if long enough
            if (path.Count >= GenerationConfig.MinRiverLength)
            {
                ApplyRiverFromPath(data, path);
                riverCellsCreated += path.Count;
                riversCreated++;
                if (riversCreated <= 3)
                {
                    GD.Print($"[RiverGen] River {riversCreated}: source={sourceIndex}, length={path.Count}");
                }
            }
            else
            {
                riversTooShort++;
            }
        }

        GD.Print($"[RiverGen] Done: {riversCreated} rivers created, {riversTooShort} too short, {riverCellsCreated} total cells");
    }

    /// <summary>
    /// Counts the number of land cells (elevation >= water level).
    /// </summary>
    private int CountLandCells(CellData[] data)
    {
        int count = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Finds all candidate river sources based on fitness threshold.
    /// Returns a List for backwards compatibility with tests.
    /// </summary>
    internal List<int> FindRiverSources(CellData[] data)
    {
        return new List<int>(FindRiverSourcesAsSet(data));
    }

    /// <summary>
    /// Finds all candidate river sources based on fitness threshold.
    /// Returns a HashSet for O(1) removal during generation.
    /// </summary>
    private HashSet<int> FindRiverSourcesAsSet(CellData[] data)
    {
        var candidates = new HashSet<int>();

        for (int i = 0; i < data.Length; i++)
        {
            float fitness = CalculateSourceFitness(ref data[i]);
            if (fitness >= GenerationConfig.RiverSourceMinFitness)
            {
                candidates.Add(i);
            }
        }

        return candidates;
    }

    /// <summary>
    /// Calculates fitness score for a cell to be a river source.
    /// Higher elevation and higher moisture = higher fitness.
    /// Cells at water level can still be sources if they have high moisture.
    /// </summary>
    internal float CalculateSourceFitness(ref CellData cell)
    {
        // Only land cells can be sources
        if (cell.Elevation < GenerationConfig.WaterLevel)
            return 0f;

        // Calculate elevation factor (0-1 based on elevation above water)
        // Add 1 to numerator so cells at water level get a base factor of 1/(range+1)
        float elevationRange = GenerationConfig.MaxElevation - GenerationConfig.WaterLevel;
        if (elevationRange <= 0)
            return 0f;

        // elevationBonus: 0 at water level, up to 1 at max elevation
        float elevationBonus = (float)(cell.Elevation - GenerationConfig.WaterLevel) / elevationRange;

        // Base fitness from moisture (0.5 weight) + elevation bonus (0.5 weight)
        // This allows high-moisture cells at water level to still be candidates
        float fitness = (0.5f * cell.Moisture) + (0.5f * elevationBonus * cell.Moisture);

        return fitness;
    }

    /// <summary>
    /// Selects a source from candidates using weighted random selection.
    /// Higher fitness cells are more likely to be selected.
    /// </summary>
    internal int SelectWeightedSource(ICollection<int> candidates, CellData[] data)
    {
        if (candidates.Count == 0)
            return -1;

        // Build weights list
        var items = new List<(int index, float weight)>();

        foreach (int cellIndex in candidates)
        {
            float fitness = CalculateSourceFitness(ref data[cellIndex]);

            // Apply weight multipliers based on fitness thresholds
            float weight = GetWeightForFitness(fitness);
            items.Add((cellIndex, weight));
        }

        // Use shared weighted selection
        int selectedIdx = WeightedRandomSelect(items, item => item.weight);
        return selectedIdx >= 0 ? items[selectedIdx].index : -1;
    }

    /// <summary>
    /// Gets the weight multiplier for a given fitness value.
    /// </summary>
    private static float GetWeightForFitness(float fitness)
    {
        if (fitness >= GenerationConfig.WeightedSelectionHighThreshold)
            return GenerationConfig.WeightHighPriority;
        if (fitness >= GenerationConfig.WeightedSelectionMediumThreshold)
            return GenerationConfig.WeightMediumPriority;
        return GenerationConfig.WeightLowPriority;
    }

    /// <summary>
    /// Performs weighted random selection on a list of items.
    /// Returns the index of the selected item, or -1 if empty.
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="items">List of items to select from</param>
    /// <param name="weightSelector">Function to extract weight from an item</param>
    /// <returns>Index of selected item, or -1 if list is empty</returns>
    private int WeightedRandomSelect<T>(IReadOnlyList<T> items, Func<T, float> weightSelector)
    {
        if (items.Count == 0)
            return -1;

        float totalWeight = 0f;
        foreach (var item in items)
            totalWeight += weightSelector(item);

        if (totalWeight <= 0)
            return _rng.Next(items.Count);

        float random = (float)_rng.NextDouble() * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weightSelector(items[i]);
            if (random <= cumulative)
                return i;
        }

        // Fallback to last item
        return items.Count - 1;
    }

    /// <summary>
    /// Traces a river path from source, walking downhill toward water.
    /// Returns list of indices only (for backwards compatibility).
    /// </summary>
    internal List<int> TraceRiver(CellData[] data, int sourceIndex)
    {
        var pathWithDirs = TraceRiverWithDirections(data, sourceIndex);
        var path = new List<int>(pathWithDirs.Count);
        foreach (var (index, _) in pathWithDirs)
            path.Add(index);
        return path;
    }

    /// <summary>
    /// Traces a river path from source, walking downhill toward water.
    /// Returns list of (cellIndex, outgoingDirection) tuples.
    /// The last cell has direction = -1 (no outgoing).
    /// </summary>
    private List<(int index, int outgoingDirection)> TraceRiverWithDirections(CellData[] data, int sourceIndex)
    {
        var path = new List<(int index, int outgoingDirection)>();
        var visited = new HashSet<int> { sourceIndex };

        int currentIndex = sourceIndex;
        int steps = 0;

        while (steps < GenerationConfig.MaxRiverTraceSteps)
        {
            steps++;

            // Check if we've reached water (underwater cell)
            if (data[currentIndex].Elevation < GenerationConfig.WaterLevel)
            {
                path.Add((currentIndex, -1)); // Terminus, no outgoing
                break;
            }

            // Find next cell and direction
            var (nextIndex, direction) = SelectNextCellWithDirection(data, currentIndex, visited);

            if (nextIndex < 0)
            {
                path.Add((currentIndex, -1)); // Dead end, no outgoing
                break;
            }

            path.Add((currentIndex, direction));
            visited.Add(nextIndex);
            currentIndex = nextIndex;
        }

        // If we hit max steps, add final cell
        if (path.Count > 0 && path[path.Count - 1].index != currentIndex)
        {
            path.Add((currentIndex, -1));
        }
        else if (path.Count == 0)
        {
            path.Add((sourceIndex, -1));
        }

        return path;
    }

    /// <summary>
    /// Selects the next cell for river flow based on steepness-weighted selection.
    /// Returns (neighborIndex, direction) or (-1, -1) if no valid neighbor.
    /// </summary>
    internal (int index, int direction) SelectNextCellWithDirection(CellData[] data, int currentIndex, HashSet<int> visited)
    {
        int currentElevation = data[currentIndex].Elevation;
        var validNeighbors = new List<(int index, int direction, float weight)>();
        bool hasDownhill = false;

        // Check all 6 directions
        for (int dir = 0; dir < HexDirectionCount; dir++)
        {
            int neighborIndex = HexNeighborHelper.GetNeighborByDirection(currentIndex, dir, _gridWidth, _gridHeight);
            if (neighborIndex < 0)
                continue;

            // Skip visited cells
            if (visited.Contains(neighborIndex))
                continue;

            // Skip cells that already have rivers (incoming or outgoing)
            if (data[neighborIndex].HasIncomingRiver || data[neighborIndex].HasOutgoingRiver)
                continue;

            int neighborElevation = data[neighborIndex].Elevation;
            int elevationDrop = currentElevation - neighborElevation;

            if (elevationDrop > 0)
            {
                // Downhill - prefer steeper drops
                hasDownhill = true;
                float weight = GenerationConfig.RiverSteepnessWeight * elevationDrop;
                validNeighbors.Add((neighborIndex, dir, weight));
            }
            else if (elevationDrop == 0)
            {
                // Flat - may be allowed with flat flow chance
                validNeighbors.Add((neighborIndex, dir, 1f));
            }
            // Uphill neighbors are never valid
        }

        if (validNeighbors.Count == 0)
            return (-1, -1);

        // If we have downhill options, filter out flat options
        // If no downhill, use flat flow chance to decide if we continue
        if (!hasDownhill)
        {
            if (_rng.NextDouble() >= GenerationConfig.RiverFlatFlowChance)
                return (-1, -1); // Stop here - no downhill and flat flow chance failed
        }
        else
        {
            // Remove flat neighbors when downhill is available
            validNeighbors.RemoveAll(n => data[n.index].Elevation == currentElevation);
            if (validNeighbors.Count == 0)
                return (-1, -1);
        }

        // Use shared weighted selection
        int selectedIdx = WeightedRandomSelect(validNeighbors, n => n.weight);
        if (selectedIdx < 0)
            return (-1, -1);

        var selected = validNeighbors[selectedIdx];
        return (selected.index, selected.direction);
    }

    /// <summary>
    /// Selects the next cell for river flow (backwards compatible version).
    /// </summary>
    internal int SelectNextCell(CellData[] data, int currentIndex, HashSet<int> visited)
    {
        var (index, _) = SelectNextCellWithDirection(data, currentIndex, visited);
        return index;
    }

    /// <summary>
    /// Applies river directions along the path using pre-computed directions.
    /// Sets outgoing direction on each cell and incoming on the next.
    /// </summary>
    private void ApplyRiverFromPath(CellData[] data, List<(int index, int outgoingDirection)> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            var (currentIndex, outgoingDirection) = path[i];

            if (outgoingDirection < 0)
                continue;

            int nextIndex = path[i + 1].index;

            // Set outgoing river on current cell
            data[currentIndex].HasOutgoingRiver = true;
            data[currentIndex].OutgoingRiverDirection = outgoingDirection;

            // Set incoming river on next cell
            int incomingDirection = HexNeighborHelper.GetOppositeDirection(outgoingDirection);
            data[nextIndex].HasIncomingRiver = true;
            data[nextIndex].IncomingRiverDirection = incomingDirection;
        }
    }

    /// <summary>
    /// Applies river directions along the path (backwards compatible version).
    /// </summary>
    internal void ApplyRiver(CellData[] data, List<int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            int currentIndex = path[i];
            int nextIndex = path[i + 1];

            // Find the direction from current to next
            int outgoingDirection = FindDirection(currentIndex, nextIndex);
            if (outgoingDirection < 0)
                continue;

            // Set outgoing river on current cell
            data[currentIndex].HasOutgoingRiver = true;
            data[currentIndex].OutgoingRiverDirection = outgoingDirection;

            // Set incoming river on next cell
            int incomingDirection = HexNeighborHelper.GetOppositeDirection(outgoingDirection);
            data[nextIndex].HasIncomingRiver = true;
            data[nextIndex].IncomingRiverDirection = incomingDirection;
        }
    }

    /// <summary>
    /// Finds the direction from one cell to an adjacent cell.
    /// </summary>
    private int FindDirection(int fromIndex, int toIndex)
    {
        for (int dir = 0; dir < HexDirectionCount; dir++)
        {
            int neighborIndex = HexNeighborHelper.GetNeighborByDirection(fromIndex, dir, _gridWidth, _gridHeight);
            if (neighborIndex == toIndex)
                return dir;
        }
        return -1;
    }
}
