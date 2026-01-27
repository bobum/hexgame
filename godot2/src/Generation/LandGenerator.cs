using System;
using System.Collections.Generic;
using System.Threading;

namespace HexGame.Generation;

/// <summary>
/// Generates land masses using Catlike Coding's chunk budget system.
/// Produces natural-looking coastlines by growing land in chunks rather than using pure noise.
///
/// Algorithm:
/// 1. Start with all cells underwater
/// 2. While land budget remains:
///    a. Pick random starting cell
///    b. Grow a chunk of connected cells using BFS
///    c. Raise chunk cells above water level
/// 3. Apply erosion to smooth coastlines
/// </summary>
public class LandGenerator
{
    private readonly Random _rng;
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    public LandGenerator(Random rng, int gridWidth, int gridHeight)
    {
        _rng = rng;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
    }

    /// <summary>
    /// Generates land on the cell data array.
    /// </summary>
    /// <param name="data">Cell data array (modified in place)</param>
    /// <param name="landPercentage">Target percentage of cells that should be land (0-1)</param>
    /// <param name="ct">Optional cancellation token for async generation</param>
    public void Generate(CellData[] data, float landPercentage, CancellationToken ct = default)
    {
        // Handle empty grids gracefully
        if (data.Length == 0 || _gridWidth == 0 || _gridHeight == 0)
            return;

        int totalCells = data.Length;
        int landBudget = (int)(totalCells * landPercentage);

        // Phase 1: Raise land chunks
        int iterations = 0;
        int cellsRaised = 0;

        while (landBudget > 0 && iterations < GenerationConfig.MaxChunkIterations)
        {
            // Check cancellation every 100 iterations to avoid overhead
            if ((iterations & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            iterations++;

            // Pick random starting cell
            int startIndex = _rng.Next(totalCells);

            // Determine chunk size
            int chunkSize = _rng.Next(GenerationConfig.MinChunkSize, GenerationConfig.MaxChunkSize + 1);

            // Grow and raise the chunk
            int raised = RaiseChunk(data, startIndex, chunkSize);
            landBudget -= raised;
            cellsRaised += raised;
        }

        ct.ThrowIfCancellationRequested();

        // Phase 2: Apply erosion to smooth coastlines
        ApplyErosion(data);
    }

    /// <summary>
    /// Raises a chunk of cells starting from the given index.
    /// Uses BFS to expand outward, probabilistically including neighbors.
    /// </summary>
    private int RaiseChunk(CellData[] data, int startIndex, int budget)
    {
        int raised = 0;
        var searchFrontier = new Queue<int>();
        var visited = new HashSet<int>();

        searchFrontier.Enqueue(startIndex);
        visited.Add(startIndex);

        while (budget > 0 && searchFrontier.Count > 0)
        {
            int currentIndex = searchFrontier.Dequeue();
            ref CellData cell = ref data[currentIndex];

            // Raise this cell
            if (RaiseCell(ref cell))
            {
                raised++;
                budget--;
            }

            // Expand to neighbors
            if (budget > 0)
            {
                foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(currentIndex, _gridWidth, _gridHeight))
                {
                    if (!visited.Contains(neighborIndex))
                    {
                        visited.Add(neighborIndex);

                        // Probabilistically add to frontier
                        if (_rng.NextDouble() < GenerationConfig.ChunkExpansionChance)
                        {
                            searchFrontier.Enqueue(neighborIndex);
                        }
                    }
                }
            }
        }

        return raised;
    }

    /// <summary>
    /// Raises a single cell's elevation.
    /// Returns true if the cell was actually raised.
    /// </summary>
    private bool RaiseCell(ref CellData cell)
    {
        if (cell.Elevation < GenerationConfig.WaterLevel)
        {
            // Underwater cell - raise to water level (makes it land)
            cell.Elevation = GenerationConfig.WaterLevel;
            return true;
        }
        else if (_rng.NextDouble() < GenerationConfig.ElevationRaiseChance)
        {
            // Already land - sometimes raise higher for hills/mountains
            if (cell.Elevation < GenerationConfig.MaxElevation)
            {
                cell.Elevation++;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Applies erosion to smooth coastlines.
    /// Lowers land cells that are mostly surrounded by water,
    /// raises water cells that are mostly surrounded by land.
    /// </summary>
    private void ApplyErosion(CellData[] data)
    {
        // Track cells to modify (can't modify during iteration)
        var toRaise = new List<int>();
        var toLower = new List<int>();

        for (int i = 0; i < data.Length; i++)
        {
            int landNeighbors = 0;
            int totalNeighbors = 0;

            foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(i, _gridWidth, _gridHeight))
            {
                totalNeighbors++;
                if (data[neighborIndex].Elevation >= GenerationConfig.WaterLevel)
                    landNeighbors++;
            }

            if (totalNeighbors == 0) continue;

            bool isLand = data[i].Elevation >= GenerationConfig.WaterLevel;
            float landRatio = (float)landNeighbors / totalNeighbors;

            if (isLand && landRatio < GenerationConfig.ErosionLandThreshold)
            {
                // Land cell mostly surrounded by water - erode it
                toLower.Add(i);
            }
            else if (!isLand && landRatio > GenerationConfig.ErosionWaterThreshold)
            {
                // Water cell mostly surrounded by land - fill it in
                toRaise.Add(i);
            }
        }

        // Apply changes
        foreach (int i in toLower)
        {
            data[i].Elevation = GenerationConfig.MinElevation;
        }

        foreach (int i in toRaise)
        {
            data[i].Elevation = GenerationConfig.WaterLevel;
        }
    }
}

/// <summary>
/// Static helper for hex grid neighbor calculations.
/// Used by LandGenerator and available for testing.
/// </summary>
public static class HexNeighborHelper
{
    /// <summary>
    /// Gets the indices of all valid neighbors for a cell in a hex grid.
    /// Uses offset coordinates where odd rows are shifted right.
    /// </summary>
    /// <param name="index">Linear index of the cell</param>
    /// <param name="gridWidth">Width of the grid</param>
    /// <param name="gridHeight">Height of the grid</param>
    /// <returns>Enumerable of valid neighbor indices</returns>
    public static IEnumerable<int> GetNeighborIndices(int index, int gridWidth, int gridHeight)
    {
        if (gridWidth <= 0 || gridHeight <= 0)
            yield break;

        int x = index % gridWidth;
        int z = index / gridWidth;

        // Hex grid neighbor offsets depend on whether we're in an even or odd row
        bool evenRow = (z & 1) == 0;

        // NE neighbor
        if (evenRow)
        {
            if (z + 1 < gridHeight)
                yield return (z + 1) * gridWidth + x;
        }
        else
        {
            if (x + 1 < gridWidth && z + 1 < gridHeight)
                yield return (z + 1) * gridWidth + (x + 1);
        }

        // E neighbor
        if (x + 1 < gridWidth)
            yield return z * gridWidth + (x + 1);

        // SE neighbor
        if (evenRow)
        {
            if (z > 0)
                yield return (z - 1) * gridWidth + x;
        }
        else
        {
            if (x + 1 < gridWidth && z > 0)
                yield return (z - 1) * gridWidth + (x + 1);
        }

        // SW neighbor
        if (evenRow)
        {
            if (x > 0 && z > 0)
                yield return (z - 1) * gridWidth + (x - 1);
        }
        else
        {
            if (z > 0)
                yield return (z - 1) * gridWidth + x;
        }

        // W neighbor
        if (x > 0)
            yield return z * gridWidth + (x - 1);

        // NW neighbor
        if (evenRow)
        {
            if (x > 0 && z + 1 < gridHeight)
                yield return (z + 1) * gridWidth + (x - 1);
        }
        else
        {
            if (z + 1 < gridHeight)
                yield return (z + 1) * gridWidth + x;
        }
    }
}

/// <summary>
/// Cell data structure for generation (shared with MapGenerator).
/// Public struct to allow LandGenerator to work with it.
/// </summary>
public struct CellData
{
    public int X;
    public int Z;
    public int Elevation;
    public int WaterLevel;
    public int TerrainTypeIndex;
    public int UrbanLevel;
    public int FarmLevel;
    public int PlantLevel;
    public int SpecialIndex;
    public bool Walled;

    public CellData(int x, int z)
    {
        X = x;
        Z = z;
        Elevation = GenerationConfig.MinElevation;
        WaterLevel = GenerationConfig.WaterLevel;
        TerrainTypeIndex = 0;
        UrbanLevel = 0;
        FarmLevel = 0;
        PlantLevel = 0;
        SpecialIndex = 0;
        Walled = false;
    }
}
