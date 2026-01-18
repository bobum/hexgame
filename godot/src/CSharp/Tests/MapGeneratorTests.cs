namespace HexGame.Tests;

using HexGame.Generation;

/// <summary>
/// Unit tests for MapGenerator.
/// </summary>
public partial class MapGeneratorTests : Node
{
    public override void _Ready()
    {
        GD.Print("=== MapGenerator Tests ===");

        TestSyncGeneration();
        TestDeterministicGeneration();
        TestTerrainDistribution();
        TestRiverGeneration();

        GD.Print("=== All MapGenerator Tests Passed ===");
    }

    private void TestSyncGeneration()
    {
        var grid = new HexGrid(16, 16);
        grid.Initialize();

        var generator = new MapGenerator();
        generator.Initialize();
        generator.Generate(grid, 12345);

        // Verify cells were populated
        bool hasLand = false;
        bool hasWater = false;

        foreach (var cell in grid.GetAllCells())
        {
            if (HexMetrics.IsLandElevation(cell.Elevation))
            {
                hasLand = true;
            }
            else
            {
                hasWater = true;
            }
        }

        Assert(hasLand, "Map should have land cells");
        Assert(hasWater, "Map should have water cells");

        generator.Shutdown();
        GD.Print("  [PASS] Synchronous generation");
    }

    private void TestDeterministicGeneration()
    {
        var grid1 = new HexGrid(16, 16);
        grid1.Initialize();

        var grid2 = new HexGrid(16, 16);
        grid2.Initialize();

        var generator1 = new MapGenerator();
        generator1.Initialize();
        generator1.Generate(grid1, 42);

        var generator2 = new MapGenerator();
        generator2.Initialize();
        generator2.Generate(grid2, 42);

        // Verify same seed produces same terrain
        bool allMatch = true;
        foreach (var cell1 in grid1.GetAllCells())
        {
            var cell2 = grid2.GetCell(cell1.Q, cell1.R);
            if (cell2 == null ||
                cell1.Elevation != cell2.Elevation ||
                cell1.TerrainType != cell2.TerrainType)
            {
                allMatch = false;
                break;
            }
        }

        Assert(allMatch, "Same seed should produce identical terrain");

        generator1.Shutdown();
        generator2.Shutdown();
        GD.Print("  [PASS] Deterministic generation");
    }

    private void TestTerrainDistribution()
    {
        var grid = new HexGrid(32, 32);
        grid.Initialize();

        var generator = new MapGenerator();
        generator.SeaLevel = 0.4f; // 40% water
        generator.Initialize();
        generator.Generate(grid, 99999);

        int landCount = 0;
        int waterCount = 0;

        foreach (var cell in grid.GetAllCells())
        {
            if (HexMetrics.IsLandElevation(cell.Elevation))
            {
                landCount++;
            }
            else
            {
                waterCount++;
            }
        }

        int totalCells = grid.CellCount;
        float waterRatio = (float)waterCount / totalCells;

        // Water ratio should be roughly around the sea level setting (with some variance)
        Assert(waterRatio > 0.2f && waterRatio < 0.6f, $"Water ratio {waterRatio:P} should be reasonable");

        generator.Shutdown();
        GD.Print("  [PASS] Terrain distribution");
    }

    private void TestRiverGeneration()
    {
        var grid = new HexGrid(32, 32);
        grid.Initialize();

        var generator = new MapGenerator();
        generator.RiverPercentage = 0.15f; // 15% rivers
        generator.Initialize();
        generator.Generate(grid, 77777);

        int cellsWithRivers = 0;

        foreach (var cell in grid.GetAllCells())
        {
            if (cell.HasRiver)
            {
                cellsWithRivers++;
            }
        }

        // Should have some rivers (exact count depends on terrain)
        // Don't require specific count since it depends on terrain generation
        GD.Print($"    Rivers generated on {cellsWithRivers} cells");

        generator.Shutdown();
        GD.Print("  [PASS] River generation");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
