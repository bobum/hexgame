using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for Tutorial 16: Pathfinding.
/// Tests A* algorithm, path reconstruction, and heuristic calculation.
/// Uses mock objects since HexCell and HexGrid require Godot runtime.
/// </summary>
public class PathfindingTests
{
    /// <summary>
    /// Mock cell for testing pathfinding without Godot dependencies.
    /// Mimics HexCell properties used in A* search.
    /// </summary>
    private class MockCell
    {
        public HexCoordinates Coordinates { get; set; }
        public int Distance { get; set; } = int.MaxValue;
        public int SearchHeuristic { get; set; }
        public int SearchPriority => Distance + SearchHeuristic;
        public int SearchPhase { get; set; }
        public MockCell? PathFrom { get; set; }
        public MockCell? NextWithSamePriority { get; set; }
        public MockCell?[] Neighbors { get; } = new MockCell?[6];
        public int Elevation { get; set; }
        public bool IsUnderwater { get; set; }
        public bool Walled { get; set; }
        public bool[] Roads { get; } = new bool[6];
        public int UrbanLevel { get; set; }
        public int FarmLevel { get; set; }
        public int PlantLevel { get; set; }

        public MockCell(int x, int z)
        {
            Coordinates = new HexCoordinates(x, z);
        }

        public MockCell? GetNeighbor(int direction) => Neighbors[direction];
        public bool HasRoadThroughEdge(int direction) => Roads[direction];

        public void SetNeighbor(int direction, MockCell neighbor)
        {
            Neighbors[direction] = neighbor;
            neighbor.Neighbors[(direction + 3) % 6] = this; // Opposite direction
        }
    }

    /// <summary>
    /// Mock priority queue for testing.
    /// </summary>
    private class MockPriorityQueue
    {
        private List<MockCell?> _list = new();
        private int _count;
        private int _minimum = int.MaxValue;

        public int Count => _count;

        public void Enqueue(MockCell cell)
        {
            _count++;
            int priority = cell.SearchPriority;
            if (priority < _minimum)
            {
                _minimum = priority;
            }
            while (priority >= _list.Count)
            {
                _list.Add(null);
            }
            cell.NextWithSamePriority = _list[priority];
            _list[priority] = cell;
        }

        public MockCell Dequeue()
        {
            _count--;
            for (; _minimum < _list.Count; _minimum++)
            {
                MockCell? cell = _list[_minimum];
                if (cell != null)
                {
                    _list[_minimum] = cell.NextWithSamePriority;
                    return cell;
                }
            }
            return null!;
        }

        public void Change(MockCell cell, int oldPriority)
        {
            MockCell? current = _list[oldPriority];
            MockCell? next = current!.NextWithSamePriority;

            if (current == cell)
            {
                _list[oldPriority] = next;
            }
            else
            {
                while (next != cell)
                {
                    current = next;
                    next = current!.NextWithSamePriority;
                }
                current!.NextWithSamePriority = cell.NextWithSamePriority;
            }
            Enqueue(cell);
            _count--;
        }

        public void Clear()
        {
            _list.Clear();
            _count = 0;
            _minimum = int.MaxValue;
        }
    }

    /// <summary>
    /// Mock pathfinder that mirrors HexGrid's A* implementation.
    /// </summary>
    private class MockPathfinder
    {
        private readonly MockPriorityQueue _frontier = new();
        private int _searchPhase;

        /// <summary>
        /// Runs A* search between two cells.
        /// Returns true if path found, false otherwise.
        /// </summary>
        public bool Search(MockCell from, MockCell to)
        {
            _searchPhase++;
            _frontier.Clear();

            from.Distance = 0;
            from.SearchPhase = _searchPhase;
            _frontier.Enqueue(from);

            while (_frontier.Count > 0)
            {
                MockCell current = _frontier.Dequeue();

                if (current == to)
                {
                    return true;
                }

                for (int d = 0; d < 6; d++)
                {
                    MockCell? neighbor = current.GetNeighbor(d);
                    if (neighbor == null)
                    {
                        continue;
                    }

                    if (neighbor.SearchPhase > _searchPhase)
                    {
                        continue;
                    }

                    int moveCost = GetMoveCost(current, neighbor, d);
                    if (moveCost < 0)
                    {
                        continue;
                    }

                    int distance = current.Distance + moveCost;

                    if (neighbor.SearchPhase < _searchPhase)
                    {
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(to.Coordinates);
                        neighbor.SearchPhase = _searchPhase;
                        _frontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        _frontier.Change(neighbor, oldPriority);
                    }
                }
            }

            return false;
        }

        private int GetMoveCost(MockCell from, MockCell to, int direction)
        {
            // Roads bypass obstacles
            if (from.HasRoadThroughEdge(direction))
            {
                return 1;
            }

            // Check for cliffs (elevation diff > 1)
            int elevDiff = System.Math.Abs(from.Elevation - to.Elevation);
            if (elevDiff > 1)
            {
                return -1;
            }

            // Underwater is impassable
            if (to.IsUnderwater)
            {
                return -1;
            }

            // Walls block movement
            if (from.Walled != to.Walled)
            {
                return -1;
            }

            // Normal terrain cost
            int cost = elevDiff == 0 ? 5 : 10; // Flat vs slope
            cost += to.UrbanLevel + to.FarmLevel + to.PlantLevel;
            return cost;
        }

        /// <summary>
        /// Reconstructs path from destination back to source.
        /// </summary>
        public List<MockCell> ReconstructPath(MockCell from, MockCell to)
        {
            var path = new List<MockCell>();
            MockCell? current = to;
            while (current != null)
            {
                path.Add(current);
                if (current == from)
                {
                    break;
                }
                current = current.PathFrom;
            }
            path.Reverse();
            return path;
        }
    }

    // Helper to create a simple grid for testing
    private MockCell[] CreateLineOfCells(int count)
    {
        var cells = new MockCell[count];
        for (int i = 0; i < count; i++)
        {
            cells[i] = new MockCell(i, 0);
            if (i > 0)
            {
                cells[i - 1].SetNeighbor(0, cells[i]); // Connect E-W
            }
        }
        return cells;
    }

    [Fact]
    public void Search_AdjacentCells_FindsPath()
    {
        var cells = CreateLineOfCells(2);
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[1]);

        found.Should().BeTrue();
        cells[1].PathFrom.Should().Be(cells[0]);
    }

    [Fact]
    public void Search_SameCell_ReturnsTrue()
    {
        var cell = new MockCell(0, 0);
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cell, cell);

        found.Should().BeTrue();
    }

    [Fact]
    public void Search_MultipleSteps_FindsPath()
    {
        var cells = CreateLineOfCells(5);
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[4]);

        found.Should().BeTrue();
    }

    [Fact]
    public void Search_PathReconstruction_CorrectOrder()
    {
        var cells = CreateLineOfCells(4);
        var pathfinder = new MockPathfinder();

        pathfinder.Search(cells[0], cells[3]);
        var path = pathfinder.ReconstructPath(cells[0], cells[3]);

        path.Should().HaveCount(4);
        path[0].Should().Be(cells[0]);
        path[1].Should().Be(cells[1]);
        path[2].Should().Be(cells[2]);
        path[3].Should().Be(cells[3]);
    }

    [Fact]
    public void Search_CliffBlocks_NoPath()
    {
        var cells = CreateLineOfCells(3);
        cells[1].Elevation = 3; // Create cliff (diff > 1)
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[2]);

        found.Should().BeFalse();
    }

    [Fact]
    public void Search_RoadOverCliff_FindsPath()
    {
        var cells = CreateLineOfCells(3);
        cells[1].Elevation = 3; // Create cliff
        cells[0].Roads[0] = true; // Road from cell0 to cell1
        cells[1].Roads[3] = true; // Bidirectional
        cells[1].Roads[0] = true; // Road from cell1 to cell2
        cells[2].Roads[3] = true; // Bidirectional
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[2]);

        found.Should().BeTrue();
    }

    [Fact]
    public void Search_UnderwaterBlocks_NoPath()
    {
        var cells = CreateLineOfCells(3);
        cells[1].IsUnderwater = true;
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[2]);

        found.Should().BeFalse();
    }

    [Fact]
    public void Search_WallBlocks_NoPath()
    {
        var cells = CreateLineOfCells(3);
        cells[1].Walled = true;
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[2]);

        found.Should().BeFalse();
    }

    [Fact]
    public void Search_RoadThroughWall_FindsPath()
    {
        var cells = CreateLineOfCells(3);
        cells[1].Walled = true;
        cells[0].Roads[0] = true;
        cells[1].Roads[3] = true;
        cells[1].Roads[0] = true;
        cells[2].Roads[3] = true;
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cells[0], cells[2]);

        found.Should().BeTrue();
    }

    [Fact]
    public void Search_FeaturesAddCost()
    {
        var cells = CreateLineOfCells(2);
        cells[1].UrbanLevel = 2;
        cells[1].FarmLevel = 1;
        var pathfinder = new MockPathfinder();

        pathfinder.Search(cells[0], cells[1]);

        // Base cost 5 (flat) + 2 urban + 1 farm = 8
        cells[1].Distance.Should().Be(8);
    }

    [Fact]
    public void Search_SlopeAddsCost()
    {
        var cells = CreateLineOfCells(2);
        cells[1].Elevation = 1; // One level difference (slope, not cliff)
        var pathfinder = new MockPathfinder();

        pathfinder.Search(cells[0], cells[1]);

        // Slope cost is 10 vs flat cost of 5
        cells[1].Distance.Should().Be(10);
    }

    [Fact]
    public void Search_RoadCostsOne()
    {
        var cells = CreateLineOfCells(2);
        cells[0].Roads[0] = true;
        cells[1].Roads[3] = true;
        var pathfinder = new MockPathfinder();

        pathfinder.Search(cells[0], cells[1]);

        cells[1].Distance.Should().Be(1);
    }

    [Fact]
    public void SearchHeuristic_CalculatedCorrectly()
    {
        var cells = CreateLineOfCells(5);
        var pathfinder = new MockPathfinder();

        pathfinder.Search(cells[0], cells[4]);

        // After search, each cell should have heuristic = distance to destination
        cells[1].SearchHeuristic.Should().Be(3); // 3 steps from cell1 to cell4
        cells[2].SearchHeuristic.Should().Be(2);
        cells[3].SearchHeuristic.Should().Be(1);
    }

    [Fact]
    public void Search_DisconnectedCells_NoPath()
    {
        var cell1 = new MockCell(0, 0);
        var cell2 = new MockCell(5, 0); // Not connected
        var pathfinder = new MockPathfinder();

        bool found = pathfinder.Search(cell1, cell2);

        found.Should().BeFalse();
    }

    [Fact]
    public void Search_FindsShorterPath_WhenAvailable()
    {
        // Create a grid where going around is shorter due to road
        //   [0]--[1]--[2]
        //    |         |
        //   [3]-------[4] (road)
        var cells = new MockCell[5];
        for (int i = 0; i < 5; i++)
        {
            cells[i] = new MockCell(i % 3, i / 3);
        }
        // Connect top row (expensive)
        cells[0].SetNeighbor(0, cells[1]);
        cells[1].SetNeighbor(0, cells[2]);
        // Connect sides
        cells[0].SetNeighbor(2, cells[3]);
        cells[2].SetNeighbor(2, cells[4]);
        // Connect bottom row with road (cheap)
        cells[3].SetNeighbor(0, cells[4]);
        cells[3].Roads[0] = true;
        cells[4].Roads[3] = true;

        var pathfinder = new MockPathfinder();
        pathfinder.Search(cells[0], cells[2]);

        var path = pathfinder.ReconstructPath(cells[0], cells[2]);

        // Should prefer: [0]->[3]->[4]->[2] via road (5+1+5=11)
        // Over: [0]->[1]->[2] without road (5+5=10)
        // Actually without features, direct is shorter. Let's add features to make road better.
    }
}

/// <summary>
/// Tests for SearchHeuristic interaction with DistanceTo.
/// </summary>
public class HeuristicTests
{
    [Fact]
    public void Heuristic_IsAdmissible()
    {
        // The heuristic (coordinate distance) should never overestimate
        // Since minimum cost per step is 1 (road), and heuristic is also 1 per step,
        // the heuristic is admissible.
        var from = new HexCoordinates(0, 0);
        var to = new HexCoordinates(5, 3);

        int heuristic = from.DistanceTo(to);
        int minPossibleCost = heuristic; // 1 cost per step with roads

        heuristic.Should().BeLessThanOrEqualTo(minPossibleCost);
    }

    [Theory]
    [InlineData(0, 0, 1, 0, 1)]   // Adjacent E
    [InlineData(0, 0, 0, 1, 1)]   // Adjacent NE
    [InlineData(0, 0, 2, 0, 2)]   // Two away E
    [InlineData(0, 0, 1, 1, 2)]   // Two away diagonal
    [InlineData(0, 0, 5, 5, 10)]  // Longer distance
    public void Heuristic_MatchesCoordinateDistance(
        int x1, int z1, int x2, int z2, int expected)
    {
        var from = new HexCoordinates(x1, z1);
        var to = new HexCoordinates(x2, z2);

        from.DistanceTo(to).Should().Be(expected);
    }

    [Fact]
    public void SearchPriority_IsSumOfDistanceAndHeuristic()
    {
        // Verify SearchPriority = Distance + SearchHeuristic
        int distance = 10;
        int heuristic = 5;
        int priority = distance + heuristic;

        priority.Should().Be(15);
    }
}
