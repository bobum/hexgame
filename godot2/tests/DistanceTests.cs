using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for Tutorial 15: Distances and Pathfinding.
/// Tests HexCoordinates.DistanceTo method.
/// </summary>
public class DistanceTests
{
    [Fact]
    public void DistanceTo_SameCell_ReturnsZero()
    {
        var coords = new HexCoordinates(0, 0);
        coords.DistanceTo(coords).Should().Be(0);
    }

    [Fact]
    public void DistanceTo_AdjacentCellEast_ReturnsOne()
    {
        var origin = new HexCoordinates(0, 0);
        var neighbor = new HexCoordinates(1, 0);
        origin.DistanceTo(neighbor).Should().Be(1);
    }

    [Fact]
    public void DistanceTo_AdjacentCellWest_ReturnsOne()
    {
        var origin = new HexCoordinates(0, 0);
        var neighbor = new HexCoordinates(-1, 0);
        origin.DistanceTo(neighbor).Should().Be(1);
    }

    [Fact]
    public void DistanceTo_AdjacentCellNE_ReturnsOne()
    {
        var origin = new HexCoordinates(0, 0);
        var neighbor = new HexCoordinates(0, 1);
        origin.DistanceTo(neighbor).Should().Be(1);
    }

    [Fact]
    public void DistanceTo_AdjacentCellNW_ReturnsOne()
    {
        var origin = new HexCoordinates(0, 0);
        var neighbor = new HexCoordinates(-1, 1);
        origin.DistanceTo(neighbor).Should().Be(1);
    }

    [Fact]
    public void DistanceTo_TwoCellsAwayEast_ReturnsTwo()
    {
        var origin = new HexCoordinates(0, 0);
        var twoAway = new HexCoordinates(2, 0);
        origin.DistanceTo(twoAway).Should().Be(2);
    }

    [Theory]
    [InlineData(0, 0, 1, 1, 2)]   // Diagonal NE then E
    [InlineData(0, 0, 2, 2, 4)]   // Further diagonal
    [InlineData(0, 0, -2, -1, 3)] // Negative direction
    [InlineData(0, 0, 3, -1, 3)]  // Mixed: |3-0| + |-2-0| + |-1-0| = 6 / 2 = 3
    public void DistanceTo_VariousDistances_ReturnsCorrectValue(
        int x1, int z1, int x2, int z2, int expected)
    {
        var from = new HexCoordinates(x1, z1);
        var to = new HexCoordinates(x2, z2);
        from.DistanceTo(to).Should().Be(expected);
    }

    [Fact]
    public void DistanceTo_IsSymmetric()
    {
        var a = new HexCoordinates(3, 2);
        var b = new HexCoordinates(-1, 4);
        a.DistanceTo(b).Should().Be(b.DistanceTo(a));
    }

    [Fact]
    public void DistanceTo_LargeDistance_WorksCorrectly()
    {
        var origin = new HexCoordinates(0, 0);
        var far = new HexCoordinates(10, 5);
        // Distance = (|10-0| + |Y1-Y2| + |5-0|) / 2
        // Y for (10,5) = -10-5 = -15, Y for origin = 0
        // = (10 + 15 + 5) / 2 = 15
        origin.DistanceTo(far).Should().Be(15);
    }

    [Fact]
    public void DistanceTo_FormulaVerification()
    {
        // Verify the formula: (|x1-x2| + |y1-y2| + |z1-z2|) / 2
        // For (2, 3) and (5, -1):
        // Y1 = -2-3 = -5, Y2 = -5-(-1) = -4
        // Distance = (|2-5| + |-5-(-4)| + |3-(-1)|) / 2 = (3 + 1 + 4) / 2 = 4
        var a = new HexCoordinates(2, 3);
        var b = new HexCoordinates(5, -1);
        a.DistanceTo(b).Should().Be(4);
    }
}

/// <summary>
/// Unit tests for HexCellPriorityQueue.
/// These tests use a mock cell class since HexCell requires Godot runtime.
/// </summary>
public class PriorityQueueTests
{
    /// <summary>
    /// Mock cell class for testing priority queue without Godot dependencies.
    /// </summary>
    private class MockCell
    {
        public int Distance { get; set; }
        public int SearchHeuristic { get; set; }
        public int SearchPriority => Distance + SearchHeuristic;
        public MockCell? NextWithSamePriority { get; set; }
    }

    /// <summary>
    /// Priority queue implementation for mock cells.
    /// Mirrors HexCellPriorityQueue logic for testing.
    /// </summary>
    private class MockPriorityQueue
    {
        private System.Collections.Generic.List<MockCell?> _list = new();
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

    private MockCell CreateTestCell(int distance, int heuristic)
    {
        return new MockCell { Distance = distance, SearchHeuristic = heuristic };
    }

    [Fact]
    public void Enqueue_SingleCell_CountIsOne()
    {
        var queue = new MockPriorityQueue();
        var cell = CreateTestCell(5, 0);

        queue.Enqueue(cell);

        queue.Count.Should().Be(1);
    }

    [Fact]
    public void Dequeue_SingleCell_ReturnsCell()
    {
        var queue = new MockPriorityQueue();
        var cell = CreateTestCell(5, 0);
        queue.Enqueue(cell);

        var result = queue.Dequeue();

        result.Should().Be(cell);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Dequeue_MultipleCells_ReturnsLowestPriority()
    {
        var queue = new MockPriorityQueue();
        var highPriority = CreateTestCell(10, 0);  // Priority 10
        var lowPriority = CreateTestCell(3, 0);    // Priority 3
        var medPriority = CreateTestCell(7, 0);    // Priority 7

        queue.Enqueue(highPriority);
        queue.Enqueue(lowPriority);
        queue.Enqueue(medPriority);

        // Should return in priority order (lowest first)
        queue.Dequeue().Should().Be(lowPriority);
        queue.Dequeue().Should().Be(medPriority);
        queue.Dequeue().Should().Be(highPriority);
    }

    [Fact]
    public void Dequeue_SamePriority_ReturnsInLIFOOrder()
    {
        var queue = new MockPriorityQueue();
        var cell1 = CreateTestCell(5, 0);
        var cell2 = CreateTestCell(5, 0);
        var cell3 = CreateTestCell(5, 0);

        queue.Enqueue(cell1);
        queue.Enqueue(cell2);
        queue.Enqueue(cell3);

        // With linked list implementation, last in is first out at same priority
        queue.Dequeue().Should().Be(cell3);
        queue.Dequeue().Should().Be(cell2);
        queue.Dequeue().Should().Be(cell1);
    }

    [Fact]
    public void Clear_EmptiesQueue()
    {
        var queue = new MockPriorityQueue();
        queue.Enqueue(CreateTestCell(5, 0));
        queue.Enqueue(CreateTestCell(10, 0));

        queue.Clear();

        queue.Count.Should().Be(0);
    }

    [Fact]
    public void Change_UpdatesPriority()
    {
        var queue = new MockPriorityQueue();
        var cell1 = CreateTestCell(10, 0);  // Priority 10
        var cell2 = CreateTestCell(5, 0);   // Priority 5

        queue.Enqueue(cell1);
        queue.Enqueue(cell2);

        // Change cell1 to have lower priority
        int oldPriority = cell1.SearchPriority;
        cell1.Distance = 2;  // New priority 2
        queue.Change(cell1, oldPriority);

        // Now cell1 should come out first
        queue.Dequeue().Should().Be(cell1);
        queue.Dequeue().Should().Be(cell2);
    }

    [Fact]
    public void SearchPriority_IncludesHeuristic()
    {
        var cell = CreateTestCell(5, 3);
        cell.SearchPriority.Should().Be(8);  // 5 + 3
    }
}
