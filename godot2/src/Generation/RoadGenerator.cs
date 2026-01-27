using System;
using System.Collections.Generic;
using System.Threading;

namespace HexGame.Generation;

/// <summary>
/// Generates roads connecting settlements (urban areas and special features).
/// Roads follow terrain constraints and bridges appear automatically where
/// roads cross straight rivers.
///
/// Algorithm:
/// 1. Find settlements (urban cells, castles, ziggurats)
/// 2. Sort by importance (urban level + special feature bonus)
/// 3. Connect each settlement to nearest unconnected settlement
/// 4. Use A* pathfinding respecting:
///    - Elevation difference ≤ 1
///    - No rivers through edge (except straight rivers for bridges)
///    - No special features (megaflora)
/// </summary>
public class RoadGenerator
{
    private readonly int _gridWidth;
    private readonly int _gridHeight;

    /// <summary>
    /// Creates a new RoadGenerator.
    /// </summary>
    /// <param name="rng">Random number generator (unused, kept for API consistency)</param>
    /// <param name="gridWidth">Width of the hex grid</param>
    /// <param name="gridHeight">Height of the hex grid</param>
    public RoadGenerator(Random rng, int gridWidth, int gridHeight)
    {
        // rng parameter kept for API consistency with other generators
        _ = rng; // Suppress unused parameter warning
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
    }

    /// <summary>
    /// Generates roads on the cell data array.
    /// </summary>
    /// <param name="data">Cell data array (modified in place)</param>
    /// <param name="ct">Optional cancellation token for async generation</param>
    public void Generate(CellData[] data, CancellationToken ct = default)
    {
        if (data.Length == 0 || _gridWidth == 0 || _gridHeight == 0)
            return;

        // Find all settlements worth connecting
        var settlements = FindSettlements(data);
        if (settlements.Count < 2)
            return;

        ct.ThrowIfCancellationRequested();

        // Sort settlements by importance (descending)
        settlements.Sort((a, b) => GetSettlementImportance(data[b]).CompareTo(GetSettlementImportance(data[a])));

        // Track which settlements are connected
        var connected = new HashSet<int> { settlements[0] };
        var unconnected = new HashSet<int>(settlements);
        unconnected.Remove(settlements[0]);

        // Connect settlements using a modified Prim's algorithm
        while (unconnected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            int bestFrom = -1;
            int bestTo = -1;
            List<int>? bestPath = null;
            int bestDistance = int.MaxValue;

            // Find the nearest unconnected settlement to any connected settlement
            foreach (int from in connected)
            {
                foreach (int to in unconnected)
                {
                    // Quick distance check to avoid expensive pathfinding
                    int manhattanDist = GetHexDistance(data[from], data[to]);
                    if (manhattanDist > GenerationConfig.MaxSettlementConnectionDistance)
                        continue;

                    var path = FindPath(data, from, to, ct);
                    if (path != null && path.Count < bestDistance)
                    {
                        bestFrom = from;
                        bestTo = to;
                        bestPath = path;
                        bestDistance = path.Count;
                    }
                }
            }

            // If we found a valid path, apply it
            if (bestPath != null)
            {
                ApplyRoad(data, bestPath);
                connected.Add(bestTo);
                unconnected.Remove(bestTo);
            }
            else
            {
                // No path found from any connected settlement to any unconnected one.
                // Move one unconnected settlement to connected so it can potentially
                // serve as a connection point for other settlements (forms islands).
                int toMove = -1;
                foreach (int idx in unconnected)
                {
                    toMove = idx;
                    break;
                }
                if (toMove >= 0)
                {
                    unconnected.Remove(toMove);
                    connected.Add(toMove);
                }
            }
        }
    }

    /// <summary>
    /// Finds cells that qualify as settlements worth connecting.
    /// Settlements include: urban areas, castles (special index 1), ziggurats (special index 2).
    /// Excludes underwater cells and megaflora (special index 3).
    /// </summary>
    internal List<int> FindSettlements(CellData[] data)
    {
        var settlements = new List<int>();

        for (int i = 0; i < data.Length; i++)
        {
            ref readonly CellData cell = ref data[i];

            // Skip underwater cells
            if (cell.Elevation < GenerationConfig.WaterLevel)
                continue;

            // Include urban areas
            if (cell.UrbanLevel >= GenerationConfig.MinUrbanLevelForSettlement)
            {
                settlements.Add(i);
                continue;
            }

            // Include castles (1) and ziggurats (2), but not megaflora (3)
            if (cell.SpecialIndex == 1 || cell.SpecialIndex == 2)
            {
                settlements.Add(i);
            }
        }

        return settlements;
    }

    /// <summary>
    /// Gets the importance score for a settlement cell.
    /// Higher values indicate more important settlements to connect first.
    /// </summary>
    private static int GetSettlementImportance(in CellData cell)
    {
        int importance = cell.UrbanLevel;

        // Castles and ziggurats are high priority
        if (cell.SpecialIndex == 1 || cell.SpecialIndex == 2)
            importance += 5;

        return importance;
    }

    /// <summary>
    /// Finds a path between two cells using A* algorithm.
    /// Returns null if no valid path exists within the maximum path length.
    /// </summary>
    internal List<int>? FindPath(CellData[] data, int start, int end, CancellationToken ct = default)
    {
        if (start == end)
            return new List<int> { start };

        var openSet = new PriorityQueue<int, int>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, int> { [start] = 0 };

        int startHeuristic = GetHexDistance(data[start], data[end]);
        openSet.Enqueue(start, startHeuristic);

        int iterations = 0;
        while (openSet.Count > 0)
        {
            // Check cancellation periodically to allow responsive cancellation
            if ((++iterations & 0xFF) == 0)
                ct.ThrowIfCancellationRequested();

            int current = openSet.Dequeue();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            // Safety limit to prevent infinite loops
            if (gScore[current] > GenerationConfig.MaxRoadPathLength)
                continue;

            // Explore neighbors
            for (int dir = 0; dir < 6; dir++)
            {
                int neighbor = HexNeighborHelper.GetNeighborByDirection(current, dir, _gridWidth, _gridHeight);
                if (neighbor < 0)
                    continue;

                // Check if road can be placed
                if (!CanPlaceRoad(data, current, neighbor, dir))
                    continue;

                int tentativeG = gScore[current] + GetMovementCost(data, current, neighbor, dir);

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    int fScore = tentativeG + GetHexDistance(data[neighbor], data[end]);
                    openSet.Enqueue(neighbor, fScore);
                }
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Reconstructs the path from the A* search.
    /// Uses Add() + Reverse() for O(n) instead of Insert(0) which is O(n²).
    /// </summary>
    private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var path = new List<int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Gets the movement cost for pathfinding.
    /// Prefers existing roads and lower elevation changes.
    /// </summary>
    private int GetMovementCost(CellData[] data, int fromIndex, int toIndex, int direction)
    {
        ref readonly CellData fromCell = ref data[fromIndex];
        ref readonly CellData toCell = ref data[toIndex];

        int cost = 1;

        // Prefer existing roads (much lower cost)
        if (fromCell.HasRoadInDirection(direction))
            return 1;

        // Elevation difference penalty
        int elevDiff = Math.Abs(fromCell.Elevation - toCell.Elevation);
        cost += elevDiff * 2;

        // River adjacent penalty (routing near rivers is slightly more costly)
        if (HasRiverInCell(in fromCell) || HasRiverInCell(in toCell))
            cost += 1;

        return cost;
    }

    /// <summary>
    /// Applies a road along a path of cell indices.
    /// Roads are bidirectional, so both cells get the road flag.
    /// </summary>
    internal void ApplyRoad(CellData[] data, List<int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            int fromIndex = path[i];
            int toIndex = path[i + 1];

            // Find the direction from -> to
            for (int dir = 0; dir < 6; dir++)
            {
                int neighbor = HexNeighborHelper.GetNeighborByDirection(fromIndex, dir, _gridWidth, _gridHeight);
                if (neighbor == toIndex)
                {
                    // Set road in both directions (bidirectional)
                    data[fromIndex].SetRoad(dir, true);
                    int oppositeDir = HexNeighborHelper.GetOppositeDirection(dir);
                    data[toIndex].SetRoad(oppositeDir, true);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Checks if a road can be placed between two adjacent cells.
    /// Constraints:
    /// - Elevation difference must be ≤ 1
    /// - No river through the edge (unless it's a valid bridge point)
    /// - Neither cell has megaflora (special index 3)
    /// - Both cells must be above water
    /// </summary>
    internal bool CanPlaceRoad(CellData[] data, int fromIndex, int toIndex, int direction)
    {
        ref readonly CellData fromCell = ref data[fromIndex];
        ref readonly CellData toCell = ref data[toIndex];

        // Both cells must be above water
        if (fromCell.Elevation < GenerationConfig.WaterLevel ||
            toCell.Elevation < GenerationConfig.WaterLevel)
            return false;

        // Elevation difference must be ≤ 1
        int elevDiff = Math.Abs(fromCell.Elevation - toCell.Elevation);
        if (elevDiff > 1)
            return false;

        // No roads on megaflora cells
        if (fromCell.SpecialIndex == 3 || toCell.SpecialIndex == 3)
            return false;

        // Roads cannot be placed on edges with rivers flowing through them.
        // Bridges form automatically when roads exist on both non-river edges
        // of a cell with a straight river - the pathfinding will route through
        // those non-river edges naturally.
        if (HasRiverThroughEdge(data, fromIndex, toIndex, direction))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if there's a river flowing through the edge between two cells.
    /// Rivers block road placement on their edges.
    /// </summary>
    private static bool HasRiverThroughEdge(CellData[] data, int fromIndex, int toIndex, int direction)
    {
        ref readonly CellData fromCell = ref data[fromIndex];
        ref readonly CellData toCell = ref data[toIndex];
        int oppositeDir = HexNeighborHelper.GetOppositeDirection(direction);

        // River flowing out from 'from' through this edge
        if (fromCell.HasOutgoingRiver && fromCell.OutgoingRiverDirection == direction)
            return true;

        // River flowing in to 'from' through this edge
        if (fromCell.HasIncomingRiver && fromCell.IncomingRiverDirection == direction)
            return true;

        // River flowing out from 'to' through this edge (toward 'from')
        if (toCell.HasOutgoingRiver && toCell.OutgoingRiverDirection == oppositeDir)
            return true;

        // River flowing in to 'to' through this edge (from 'from')
        if (toCell.HasIncomingRiver && toCell.IncomingRiverDirection == oppositeDir)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a cell has any river (incoming or outgoing).
    /// Used for movement cost calculation - routing near rivers has slight penalty.
    /// </summary>
    private static bool HasRiverInCell(in CellData cell)
    {
        return cell.HasIncomingRiver || cell.HasOutgoingRiver;
    }

    /// <summary>
    /// Gets the hex distance between two cells (admissible heuristic for A*).
    /// Uses cube coordinate conversion for accurate hex grid distance.
    /// </summary>
    private static int GetHexDistance(in CellData a, in CellData b)
    {
        // Convert offset coordinates to cube coordinates
        int ax = a.X - a.Z / 2;
        int az = a.Z;
        int ay = -ax - az;

        int bx = b.X - b.Z / 2;
        int bz = b.Z;
        int by = -bx - bz;

        // Hex distance is half the sum of absolute differences in cube coords
        return (Math.Abs(ax - bx) + Math.Abs(ay - by) + Math.Abs(az - bz)) / 2;
    }
}
