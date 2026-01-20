using Godot;
using System;

/// <summary>
/// Generates test water bodies (lakes) to verify water rendering.
/// Creates underwater cells by setting WaterLevel > Elevation.
///
/// IMPORTANT: Adjacent underwater cells MUST have the same water level.
/// The Catlike Coding tutorial explicitly states this constraint.
/// Different altitude lakes are fine, but they must be separated by land.
/// </summary>
public static class TestWaterGenerator
{
    /// <summary>
    /// Generates test water bodies on the grid.
    /// Creates lakes at different altitudes, separated by land.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test water patterns...");

        // Create a large lowland lake (water level 1, elevation 0)
        // Located in the lower portion of the map
        GenerateLowlandLake(getCell);

        // Create a small highland lake (water level 4, elevation 3)
        // Located away from the lowland lake with land buffer
        GenerateHighlandLake(getCell);

        // Create a river flowing from highland lake to lowland lake
        GenerateConnectingRiver(getCell);

        GD.Print("Test water patterns generated.");
    }

    /// <summary>
    /// Creates a large lowland lake at water level 1.
    /// All cells in this lake share the same water level.
    /// </summary>
    private static void GenerateLowlandLake(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating lowland lake (water level 1)...");
        int waterLevel = 1;
        int count = 0;

        // Define lake region (center-left area of map)
        // Cells at elevation 0 with water level 1 = underwater
        var lakeCells = new (int x, int z)[]
        {
            // Core lake cells
            (8, 8), (9, 8), (10, 8), (11, 8),
            (8, 9), (9, 9), (10, 9), (11, 9),
            (8, 10), (9, 10), (10, 10), (11, 10),
            (9, 11), (10, 11),
        };

        foreach (var (x, z) in lakeCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;

            cell.Elevation = 0;
            cell.WaterLevel = waterLevel;
            cell.Color = new Color(0.2f, 0.4f, 0.7f);  // Deep blue for lake bed
            count++;
        }

        GD.Print($"  Lowland lake created with {count} cells at water level {waterLevel}");
    }

    /// <summary>
    /// Creates a small highland lake at water level 4 (elevation 3).
    /// Separated from lowland lake by elevated terrain.
    /// </summary>
    private static void GenerateHighlandLake(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating highland lake (water level 4)...");
        int waterLevel = 4;
        int lakeElevation = 3;
        int count = 0;

        // Define highland lake region (upper-right, away from lowland lake)
        var lakeCells = new (int x, int z)[]
        {
            (15, 4), (16, 4),
            (15, 5), (16, 5), (17, 5),
            (16, 6),
        };

        // First, create elevated terrain around the highland lake to ensure separation
        var bufferCells = new (int x, int z)[]
        {
            (14, 3), (15, 3), (16, 3), (17, 3),
            (14, 4), (17, 4), (18, 4),
            (14, 5), (18, 5),
            (14, 6), (15, 6), (17, 6), (18, 6),
            (15, 7), (16, 7), (17, 7),
        };

        // Set buffer terrain to elevation 4 (above water level, so not underwater)
        foreach (var (x, z) in bufferCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = waterLevel;  // At water level = shore (not underwater)
            cell.Color = new Color(0.5f, 0.7f, 0.5f);  // Light green for highland shore
        }

        // Set lake cells
        foreach (var (x, z) in lakeCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;

            cell.Elevation = lakeElevation;
            cell.WaterLevel = waterLevel;
            cell.Color = new Color(0.3f, 0.5f, 0.8f);  // Mountain lake blue
            count++;
        }

        GD.Print($"  Highland lake created with {count} cells at water level {waterLevel}");
    }

    /// <summary>
    /// Creates a river flowing from the highland lake down to the lowland lake.
    /// User-specified path (hex coords): 13,-17,4 -> 12,-16,4 -> 11,-16,5 -> 10,-16,6 ->
    ///                                   9,-16,7 -> 9,-17,8 -> 8,-17,9 -> 7,-16,9
    /// Converted to offset coords: (15,4) -> (14,4) -> (13,5) -> (13,6) ->
    ///                             (12,7) -> (13,8) -> (12,9) -> (11,9)
    /// </summary>
    private static void GenerateConnectingRiver(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating connecting river from highland to lowland lake...");

        // River path with elevations (decreasing from highland to lowland)
        // Start: highland lake at elevation 3, End: lowland lake at elevation 0
        var riverPath = new (int x, int z, int elevation)[]
        {
            (15, 4, 3),   // Start - part of highland lake (underwater, will be estuary)
            (14, 4, 3),   // Exit highland lake shore
            (13, 5, 2),   // Descending
            (13, 6, 2),   //
            (12, 7, 1),   // Waterfall here (2->1)
            (13, 8, 1),   //
            (12, 9, 0),   // Approaching lowland lake
            (11, 9, 0),   // End - part of lowland lake (underwater, will be estuary)
        };

        // First pass: set elevations
        GD.Print("    Setting river path elevations...");
        foreach (var (x, z, elevation) in riverPath)
        {
            var cell = getCell(x, z);
            if (cell == null)
            {
                GD.PrintErr($"    River cell ({x},{z}) is NULL!");
                continue;
            }
            cell.Elevation = elevation;
            cell.Color = new Color(0.4f, 0.6f, 0.4f);  // Greenish for river banks
            GD.Print($"    Cell ({x},{z}) elevation set to {elevation}");
        }

        // Second pass: create river connections
        GD.Print("    Creating river connections...");
        int successCount = 0;
        for (int i = 0; i < riverPath.Length - 1; i++)
        {
            var (x1, z1, _) = riverPath[i];
            var (x2, z2, _) = riverPath[i + 1];

            var cell = getCell(x1, z1);
            var nextCell = getCell(x2, z2);

            if (cell == null || nextCell == null)
            {
                GD.PrintErr($"    Cannot connect ({x1},{z1}) -> ({x2},{z2}): null cell");
                continue;
            }

            // Find direction to next cell
            HexDirection? dir = null;
            for (int d = 0; d < 6; d++)
            {
                var neighbor = cell.GetNeighbor((HexDirection)d);
                if (neighbor == nextCell)
                {
                    dir = (HexDirection)d;
                    break;
                }
            }

            if (dir.HasValue)
            {
                cell.SetOutgoingRiver(dir.Value);
                if (cell.HasOutgoingRiver)
                {
                    GD.Print($"    River: ({x1},{z1}) -> ({x2},{z2}) dir={dir.Value} SUCCESS");
                    successCount++;
                }
                else
                {
                    GD.PrintErr($"    River: ({x1},{z1}) -> ({x2},{z2}) FAILED to set");
                }
            }
            else
            {
                GD.PrintErr($"    No direction found from ({x1},{z1}) to ({x2},{z2})");
            }
        }

        GD.Print($"  Connecting river created: {successCount}/{riverPath.Length - 1} segments");
    }
}
