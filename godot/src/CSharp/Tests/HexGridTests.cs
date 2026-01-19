namespace HexGame.Tests;

/// <summary>
/// Unit tests for HexGrid.
/// </summary>
public partial class HexGridTests : Node
{
    public override void _Ready()
    {
        GD.Print("=== HexGrid Tests ===");

        TestInitialization();
        TestGetSetCell();
        TestBoundsChecking();
        TestNeighbors();
        TestCellsInRadius();

        GD.Print("=== All HexGrid Tests Passed ===");
    }

    private void TestInitialization()
    {
        var grid = new HexGrid(16, 16);
        Assert(grid.Width == 16, "Width should be 16");
        Assert(grid.Height == 16, "Height should be 16");
        Assert(grid.CellCount == 0, "Cell count should be 0 before init");

        grid.Initialize();
        Assert(grid.CellCount == 256, "Cell count should be 256 after init");

        GD.Print("  [PASS] Initialization");
    }

    private void TestGetSetCell()
    {
        var grid = new HexGrid(10, 10);
        grid.Initialize();

        var cell = grid.GetCell(5, 5);
        Assert(cell != null, "Cell at (5,5) should exist");
        Assert(cell!.Q == 5 && cell.R == 5, "Cell coords should be (5,5)");

        // Test setting cell
        cell.Elevation = 10;
        cell.TerrainType = TerrainType.Mountains;

        var retrieved = grid.GetCell(5, 5);
        Assert(retrieved != null, "Retrieved cell should exist");
        Assert(retrieved!.Elevation == 10, "Elevation should be 10");
        Assert(retrieved.TerrainType == TerrainType.Mountains, "Terrain should be Mountains");

        // Test with HexCoordinates
        var coords = new HexCoordinates(3, 7);
        var cellByCoords = grid.GetCell(coords);
        Assert(cellByCoords != null, "Cell by coords should exist");
        Assert(cellByCoords!.Q == 3 && cellByCoords.R == 7, "Coords should match");

        GD.Print("  [PASS] Get/Set cell");
    }

    private void TestBoundsChecking()
    {
        var grid = new HexGrid(10, 10);
        grid.Initialize();

        Assert(grid.IsValid(0, 0), "(0,0) should be valid");
        Assert(grid.IsValid(9, 9), "(9,9) should be valid");
        Assert(!grid.IsValid(-1, 0), "(-1,0) should be invalid");
        Assert(!grid.IsValid(0, -1), "(0,-1) should be invalid");
        Assert(!grid.IsValid(10, 0), "(10,0) should be invalid");
        Assert(!grid.IsValid(0, 10), "(0,10) should be invalid");

        Assert(grid.GetCell(-1, 0) == null, "Invalid cell should return null");
        Assert(grid.GetCell(100, 100) == null, "Out of bounds cell should return null");

        GD.Print("  [PASS] Bounds checking");
    }

    private void TestNeighbors()
    {
        var grid = new HexGrid(10, 10);
        grid.Initialize();

        var center = grid.GetCell(5, 5)!;
        var neighbors = grid.GetNeighbors(center);

        Assert(neighbors.Count == 6, "Center cell should have 6 neighbors");

        // Verify each neighbor is at distance 1
        var centerCoords = center.Coordinates;
        foreach (var neighbor in neighbors)
        {
            int distance = centerCoords.DistanceTo(neighbor.Coordinates);
            Assert(distance == 1, $"Neighbor should be at distance 1, got {distance}");
        }

        // Test corner cell (should have fewer neighbors)
        var corner = grid.GetCell(0, 0)!;
        var cornerNeighbors = grid.GetNeighbors(corner);
        Assert(cornerNeighbors.Count < 6, "Corner cell should have fewer than 6 neighbors");

        // Test specific direction
        var neNeighbor = grid.GetNeighbor(center, HexDirection.NE);
        Assert(neNeighbor != null, "NE neighbor should exist");
        Assert(neNeighbor!.Q == 6 && neNeighbor.R == 5, "NE neighbor should be at (6,5)");

        GD.Print("  [PASS] Neighbors");
    }

    private void TestCellsInRadius()
    {
        var grid = new HexGrid(20, 20);
        grid.Initialize();

        var center = grid.GetCell(10, 10)!;

        // Radius 0 should return just the center
        var radius0 = grid.GetCellsInRadius(center, 0).ToList();
        Assert(radius0.Count == 1, "Radius 0 should return 1 cell");

        // Radius 1 should return center + 6 neighbors = 7
        var radius1 = grid.GetCellsInRadius(center, 1).ToList();
        Assert(radius1.Count == 7, $"Radius 1 should return 7 cells, got {radius1.Count}");

        // Radius 2: 1 + 6 + 12 = 19
        var radius2 = grid.GetCellsInRadius(center, 2).ToList();
        Assert(radius2.Count == 19, $"Radius 2 should return 19 cells, got {radius2.Count}");

        GD.Print("  [PASS] Cells in radius");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
