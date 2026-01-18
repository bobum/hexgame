namespace HexGame.Tests;

using HexGame.Pathfinding;
using HexGame.Units;

/// <summary>
/// Unit tests for Pathfinder.
/// </summary>
public partial class PathfinderTests : Node
{
    private HexGrid _grid = null!;
    private Pathfinder _pathfinder = null!;

    public override void _Ready()
    {
        GD.Print("=== Pathfinder Tests ===");

        SetupTestGrid();

        TestFindPathSameCell();
        TestFindPathAdjacent();
        TestFindPathLonger();
        TestFindPathBlocked();
        TestGetReachableCells();
        TestMovementStrategies();

        GD.Print("=== All Pathfinder Tests Passed ===");
    }

    private void SetupTestGrid()
    {
        _grid = new HexGrid(10, 10);
        _grid.Initialize();

        // Set up all land cells
        foreach (var cell in _grid.GetAllCells())
        {
            cell.Elevation = 6; // Land
            cell.TerrainType = TerrainType.Plains;
        }

        _pathfinder = new Pathfinder(_grid);
        _pathfinder.Initialize();
    }

    private void TestFindPathSameCell()
    {
        var cell = _grid.GetCell(5, 5)!;
        var result = _pathfinder.FindPath(cell, cell);

        Assert(result.Reachable, "Same cell should be reachable");
        Assert(result.Path.Count == 1, "Path to same cell should have 1 element");
        Assert(result.Cost == 0, "Cost to same cell should be 0");

        GD.Print("  [PASS] Path to same cell");
    }

    private void TestFindPathAdjacent()
    {
        var start = _grid.GetCell(5, 5)!;
        var end = _grid.GetCell(6, 5)!; // NE neighbor

        var result = _pathfinder.FindPath(start, end);

        Assert(result.Reachable, "Adjacent cell should be reachable");
        Assert(result.Path.Count == 2, "Path to adjacent cell should have 2 elements");
        Assert(result.Cost > 0, "Cost should be positive");

        GD.Print("  [PASS] Path to adjacent cell");
    }

    private void TestFindPathLonger()
    {
        var start = _grid.GetCell(2, 2)!;
        var end = _grid.GetCell(7, 7)!;

        var result = _pathfinder.FindPath(start, end);

        Assert(result.Reachable, "Distant cell should be reachable");
        Assert(result.Path.Count > 2, "Path should have multiple steps");
        Assert(result.Path[0] == start, "Path should start at start cell");
        Assert(result.Path[^1] == end, "Path should end at end cell");

        GD.Print("  [PASS] Longer path");
    }

    private void TestFindPathBlocked()
    {
        // Create an impassable cell (mountain)
        var blockCell = _grid.GetCell(5, 5)!;
        blockCell.TerrainType = TerrainType.Mountains;

        var start = _grid.GetCell(4, 5)!;
        var end = _grid.GetCell(6, 5)!;

        // Path should still be found by going around
        var result = _pathfinder.FindPath(start, end);
        Assert(result.Reachable, "Should find path around obstacle");
        Assert(!result.Path.Contains(blockCell), "Path should not include blocked cell");

        // Reset
        blockCell.TerrainType = TerrainType.Plains;

        GD.Print("  [PASS] Path around obstacle");
    }

    private void TestGetReachableCells()
    {
        var start = _grid.GetCell(5, 5)!;

        // With 1 movement point, should reach adjacent cells
        var reachable1 = _pathfinder.GetReachableCells(start, 1);
        Assert(reachable1.Count >= 1, "Should reach at least starting cell with 1 MP");
        Assert(reachable1.ContainsKey(start), "Starting cell should be reachable");

        // With more movement, should reach more cells
        var reachable3 = _pathfinder.GetReachableCells(start, 3);
        Assert(reachable3.Count > reachable1.Count, "More movement should reach more cells");

        GD.Print("  [PASS] Reachable cells");
    }

    private void TestMovementStrategies()
    {
        // Test land movement cost
        var from = _grid.GetCell(5, 5)!;
        var to = _grid.GetCell(6, 5)!;

        float landCost = LandMovementStrategy.Instance.GetMovementCost(from, to);
        Assert(landCost > 0 && !float.IsPositiveInfinity(landCost), "Land movement should have finite cost");

        // Test that land strategy blocks water
        to.Elevation = 2; // Water
        float landOnWaterCost = LandMovementStrategy.Instance.GetMovementCost(from, to);
        Assert(float.IsPositiveInfinity(landOnWaterCost), "Land movement to water should be blocked");

        // Test naval strategy allows water
        float navalOnWaterCost = NavalMovementStrategy.Instance.GetMovementCost(from, to);
        Assert(!float.IsPositiveInfinity(navalOnWaterCost), "Naval movement to water should be allowed");

        // Reset
        to.Elevation = 6;

        GD.Print("  [PASS] Movement strategies");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
