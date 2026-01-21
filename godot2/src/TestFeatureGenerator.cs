using Godot;
using System;

/// <summary>
/// Generates test feature placements to verify terrain feature rendering.
/// Sets UrbanLevel, FarmLevel, and PlantLevel on cells in designated test areas.
/// Ported from Catlike Coding Hex Map Tutorial 9.
/// </summary>
public static class TestFeatureGenerator
{
    /// <summary>
    /// Generates test feature placements on the grid.
    /// Creates distinct regions for urban, farm, and plant features at varying densities.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test feature patterns...");

        // Create urban zone (top-left area)
        GenerateUrbanZone(getCell);

        // Create farm zone (top-right area)
        GenerateFarmZone(getCell);

        // Create plant zone (bottom area, avoiding water)
        GeneratePlantZone(getCell);

        // Create mixed zone with multiple feature types
        GenerateMixedZone(getCell);

        // Create road + feature test zone to verify features avoid roads
        GenerateRoadFeatureTestZone(getCell);

        GD.Print("Test feature patterns generated.");
    }

    /// <summary>
    /// Creates an urban zone with varying density levels.
    /// Urban features represent buildings and city structures.
    /// </summary>
    private static void GenerateUrbanZone(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating urban zone (levels 1-3)...");
        int count = 0;

        // Level 1 urban (sparse buildings) - row 0
        var level1Cells = new (int x, int z)[]
        {
            (0, 0), (1, 0), (2, 0), (3, 0),
        };

        foreach (var (x, z) in level1Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.UrbanLevel = 1;
            count++;
        }

        // Level 2 urban (moderate density) - row 1
        var level2Cells = new (int x, int z)[]
        {
            (0, 1), (1, 1), (2, 1), (3, 1),
        };

        foreach (var (x, z) in level2Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.UrbanLevel = 2;
            count++;
        }

        // Level 3 urban (dense city) - row 2
        var level3Cells = new (int x, int z)[]
        {
            (0, 2), (1, 2), (2, 2), (3, 2),
        };

        foreach (var (x, z) in level3Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.UrbanLevel = 3;
            count++;
        }

        GD.Print($"  Urban zone created with {count} cells");
    }

    /// <summary>
    /// Creates a farm zone with varying density levels.
    /// Farm features represent agricultural structures.
    /// </summary>
    private static void GenerateFarmZone(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating farm zone (levels 1-3)...");
        int count = 0;

        // Level 1 farm (small farms) - row 0
        var level1Cells = new (int x, int z)[]
        {
            (15, 0), (16, 0), (17, 0), (18, 0), (19, 0),
        };

        foreach (var (x, z) in level1Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.FarmLevel = 1;
            count++;
        }

        // Level 2 farm (medium farms) - row 1
        var level2Cells = new (int x, int z)[]
        {
            (15, 1), (16, 1), (17, 1), (18, 1), (19, 1),
        };

        foreach (var (x, z) in level2Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.FarmLevel = 2;
            count++;
        }

        // Level 3 farm (large farms) - row 2
        var level3Cells = new (int x, int z)[]
        {
            (15, 2), (16, 2), (17, 2), (18, 2), (19, 2),
        };

        foreach (var (x, z) in level3Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.FarmLevel = 3;
            count++;
        }

        GD.Print($"  Farm zone created with {count} cells");
    }

    /// <summary>
    /// Creates a plant zone with varying density levels.
    /// Plant features represent trees, bushes, and vegetation.
    /// </summary>
    private static void GeneratePlantZone(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating plant zone (levels 1-3)...");
        int count = 0;

        // Level 1 plants (sparse vegetation) - bottom-left
        var level1Cells = new (int x, int z)[]
        {
            (0, 12), (1, 12), (2, 12), (3, 12), (4, 12),
            (0, 13), (1, 13), (2, 13), (3, 13), (4, 13),
        };

        foreach (var (x, z) in level1Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.PlantLevel = 1;
            count++;
        }

        // Level 2 plants (moderate vegetation) - bottom-middle
        var level2Cells = new (int x, int z)[]
        {
            (5, 12), (6, 12), (7, 12),
            (5, 13), (6, 13), (7, 13),
        };

        foreach (var (x, z) in level2Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.PlantLevel = 2;
            count++;
        }

        // Level 3 plants (dense forest) - avoid water area
        var level3Cells = new (int x, int z)[]
        {
            (15, 12), (16, 12), (17, 12), (18, 12), (19, 12),
            (15, 13), (16, 13), (17, 13), (18, 13), (19, 13),
        };

        foreach (var (x, z) in level3Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            cell.PlantLevel = 3;
            count++;
        }

        GD.Print($"  Plant zone created with {count} cells");
    }

    /// <summary>
    /// Creates a mixed zone where multiple feature types can compete.
    /// Tests the hash-based selection where lowest hash wins.
    /// </summary>
    private static void GenerateMixedZone(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating mixed feature zone...");
        int count = 0;

        // Central area with all three feature types set
        // The hash grid will determine which feature actually appears
        var mixedCells = new (int x, int z)[]
        {
            (6, 3), (7, 3), (8, 3), (9, 3), (10, 3),
            (6, 4), (7, 4), (8, 4), (9, 4), (10, 4),
            (6, 5), (7, 5), (8, 5), (9, 5), (10, 5),
        };

        foreach (var (x, z) in mixedCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver || cell.HasRoads) continue;

            // Set all three feature types
            // The lowest hash value will determine which appears
            cell.UrbanLevel = 2;
            cell.FarmLevel = 2;
            cell.PlantLevel = 2;
            count++;
        }

        GD.Print($"  Mixed zone created with {count} cells (urban/farm/plant all level 2)");
    }

    /// <summary>
    /// Creates a test zone on cells that have roads to verify features avoid road directions.
    /// Features should appear in triangles WITHOUT roads, but not in triangles WITH roads.
    /// This tests the HasRoadThroughEdge check in triangle center feature placement.
    /// </summary>
    private static void GenerateRoadFeatureTestZone(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating road + feature test zone...");
        int count = 0;

        // These cells have roads from TestRoadGenerator
        // We set feature levels on them to verify features avoid road directions
        var roadCells = new (int x, int z)[]
        {
            // Row 10: Various road configs
            (10, 10), (12, 10), (14, 10), (16, 10),
            // Row 12: 3-road configs
            (10, 12), (12, 12), (14, 12),
            // Row 14: 4-road configs
            (10, 14), (12, 14),
            // Row 16: 5 and 6-road configs
            (10, 16), (12, 16),
        };

        foreach (var (x, z) in roadCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            if (cell.IsUnderwater || cell.HasRiver) continue;

            // Set high plant level - should see trees in non-road directions
            // but NO trees where roads pass through
            cell.PlantLevel = 3;
            count++;

            GD.Print($"    Cell ({x},{z}) HasRoads={cell.HasRoads}, PlantLevel=3");
        }

        GD.Print($"  Road + feature test zone: {count} cells with PlantLevel=3 on roads");
        GD.Print("    -> Features should appear ONLY in directions WITHOUT roads");
    }
}
