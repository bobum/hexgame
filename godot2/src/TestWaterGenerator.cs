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

        // Create a small side river flowing into the lowland lake
        GenerateSideRiver(getCell);

        // Create river from highland lake (15,5) -> (15,6) -> (14,7)
        GenerateHighlandOutflow(getCell);

        // Create terraced coastline demonstration (gradual land-to-water transition)
        GenerateTerracedCoastline(getCell);

        GD.Print("Test water patterns generated.");
    }

    /// <summary>
    /// Creates a river flowing from highland area down into a water hex.
    /// Tests waterfall flowing INTO water from above.
    /// Path: 12,-18,6 (15,6) -> 11,-18,7 (14,7) -> 10,-17,7 (13,7 water hex)
    /// </summary>
    private static void GenerateOutflowRiver(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating outflow river into water hex...");

        // First, make (13, 7) a water hex
        var waterCell = getCell(13, 7);
        if (waterCell != null)
        {
            waterCell.Elevation = 0;
            waterCell.WaterLevel = 1;  // Same as lowland lake
            waterCell.TerrainTypeIndex = 2; // Mud for water areas
            GD.Print($"    Made (13,7) a water hex: elevation=0, waterLevel=1");
        }

        // Modify (16, 6) - raise elevation and remove from water
        var cell16_6 = getCell(16, 6);
        if (cell16_6 != null)
        {
            cell16_6.Elevation = 4;   // Raised from 3 to 4
            cell16_6.WaterLevel = 0;  // No longer a water hex
            GD.Print($"    Modified (16,6): elevation=4, waterLevel=0 (not underwater)");
        }

        // Make (15, 5) flow into (15, 6) - river exiting highland lake
        var lakeExitCell = getCell(15, 5);
        var firstRiverCell = getCell(15, 6);
        if (lakeExitCell != null && firstRiverCell != null)
        {
            // (15, 5) stays as water hex (highland lake) but adds outgoing river
            for (int d = 0; d < 6; d++)
            {
                if (lakeExitCell.GetNeighbor((HexDirection)d) == firstRiverCell)
                {
                    lakeExitCell.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River from lake: (15,5) -> (15,6) dir={(HexDirection)d}");
                    break;
                }
            }
        }

        var riverPath = new (int x, int z, int elevation)[]
        {
            (15, 6, 3),   // Start: 12,-18,6 lowered from 4 to 3
            (14, 7, 2),   // 11,-18,7 at elevation 2
            (13, 7, 0),   // End: 10,-17,7 water hex (elevation 0, water level 1)
        };

        // Set elevations
        foreach (var (x, z, elevation) in riverPath)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = elevation;
        }

        // Create river connections
        for (int i = 0; i < riverPath.Length - 1; i++)
        {
            var (x1, z1, _) = riverPath[i];
            var (x2, z2, _) = riverPath[i + 1];

            var cell = getCell(x1, z1);
            var nextCell = getCell(x2, z2);
            if (cell == null || nextCell == null) continue;

            // Find direction
            for (int d = 0; d < 6; d++)
            {
                if (cell.GetNeighbor((HexDirection)d) == nextCell)
                {
                    cell.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River: ({x1},{z1}) -> ({x2},{z2}) dir={(HexDirection)d}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Creates a small river flowing into the lowland lake from the side.
    /// Path: 6,-12,6 (9,6) -> 5,-12,7 (8,7) -> 5,-13,8 (9,8 in lake)
    /// </summary>
    private static void GenerateSideRiver(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating side river into lowland lake...");

        var riverPath = new (int x, int z, int elevation)[]
        {
            (9, 6, 3),   // Start: 6,-12,6 at elevation 3
            (8, 7, 3),   // 5,-12,7 at elevation 3
            (9, 8, 0),   // End: 5,-13,8 in lowland lake (elevation 0, water level 1)
        };

        // Set elevations
        foreach (var (x, z, elevation) in riverPath)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = elevation;
        }

        // Create river connections
        for (int i = 0; i < riverPath.Length - 1; i++)
        {
            var (x1, z1, _) = riverPath[i];
            var (x2, z2, _) = riverPath[i + 1];

            var cell = getCell(x1, z1);
            var nextCell = getCell(x2, z2);
            if (cell == null || nextCell == null) continue;

            // Find direction
            for (int d = 0; d < 6; d++)
            {
                if (cell.GetNeighbor((HexDirection)d) == nextCell)
                {
                    cell.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River: ({x1},{z1}) -> ({x2},{z2}) dir={(HexDirection)d}");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Creates a river from highland lake through (15,6) to (14,7).
    /// Path: 13,-18,5 (15,5) -> 12,-18,6 (15,6) -> 11,-18,7 (14,7)
    /// </summary>
    private static void GenerateHighlandOutflow(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating highland outflow river...");

        // Set (15,6) to elevation 1
        var midCell = getCell(15, 6);
        if (midCell != null)
        {
            midCell.Elevation = 1;
            midCell.TerrainTypeIndex = 1; // Grass for mid areas
            GD.Print($"    Set (15,6) / hex 12,-18,6 to elevation 1");
        }

        // River from (15,5) highland lake -> (15,6)
        var sourceCell = getCell(15, 5);
        if (sourceCell != null && midCell != null)
        {
            for (int d = 0; d < 6; d++)
            {
                if (sourceCell.GetNeighbor((HexDirection)d) == midCell)
                {
                    sourceCell.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River: (15,5) -> (15,6) dir={(HexDirection)d}");
                    break;
                }
            }
        }

        // River from (15,6) -> (14,7)
        var cell14_7 = getCell(14, 7);
        if (midCell != null && cell14_7 != null)
        {
            for (int d = 0; d < 6; d++)
            {
                if (midCell.GetNeighbor((HexDirection)d) == cell14_7)
                {
                    midCell.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River: (15,6) -> (14,7) dir={(HexDirection)d}");
                    break;
                }
            }
        }

        // Set (13,7) / hex 10,-17,7 as water hex
        var waterCell = getCell(13, 7);
        if (waterCell != null)
        {
            waterCell.Elevation = 0;
            waterCell.WaterLevel = 1;  // Same as lowland lake
            waterCell.TerrainTypeIndex = 2; // Mud for water areas
            GD.Print($"    Set (13,7) / hex 10,-17,7 to water hex (elev=0, waterLevel=1)");
        }

        // River from (14,7) -> (13,7) water hex
        if (cell14_7 != null && waterCell != null)
        {
            for (int d = 0; d < 6; d++)
            {
                if (cell14_7.GetNeighbor((HexDirection)d) == waterCell)
                {
                    cell14_7.SetOutgoingRiver((HexDirection)d);
                    GD.Print($"    River: (14,7) -> (13,7) dir={(HexDirection)d}");
                    break;
                }
            }
        }
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
            (8, 8), (9, 8), (10, 8), (11, 8), (12, 8),  // Added (12, 8)
            (8, 9), (9, 9), (10, 9), (11, 9), (12, 9),  // Added (12, 9)
            (8, 10), (9, 10), (10, 10), (11, 10),
            (9, 11), (10, 11),
        };

        foreach (var (x, z) in lakeCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;

            cell.Elevation = 0;
            cell.WaterLevel = waterLevel;
            cell.TerrainTypeIndex = 2; // Mud for water areas
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
        // Note: (16,6) removed - now raised land
        var lakeCells = new (int x, int z)[]
        {
            (15, 4), (16, 4),
            (15, 5), (16, 5), (17, 5),
        };

        // Set (16,6) / hex 13,-19,6 to raised land with beige color
        var raisedCell = getCell(16, 6);
        if (raisedCell != null)
        {
            raisedCell.Elevation = 4;
            raisedCell.WaterLevel = 0;
            raisedCell.TerrainTypeIndex = 0; // Sand for raised areas
            GD.Print("    Set (16,6) / hex 13,-19,6 to elevation 4, beige");
        }

        // First, create elevated terrain around the highland lake to ensure separation
        // Note: (15,6) removed - now part of outflow river
        var bufferCells = new (int x, int z)[]
        {
            (14, 3), (15, 3), (16, 3), (17, 3),
            (14, 4), (17, 4), (18, 4),
            (14, 5), (18, 5),
            (14, 6), (17, 6), (18, 6),
            (15, 7), (16, 7), (17, 7),
        };

        // Set buffer terrain to elevation 4 (above water level, so not underwater)
        foreach (var (x, z) in bufferCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = waterLevel;  // At water level = shore (not underwater)
            cell.TerrainTypeIndex = 1; // Grass for highland shore
        }

        // Set lake cells
        foreach (var (x, z) in lakeCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;

            cell.Elevation = lakeElevation;
            cell.WaterLevel = waterLevel;
            cell.TerrainTypeIndex = 2; // Mud for mountain lake
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
            (14, 5, 4),   // Exit highland lake shore (raised from 3 to 4)
            (13, 5, 2),   // Descending
            (13, 6, 2),   //
            (12, 7, 1),   // Waterfall here (2->1)
            (13, 8, 1),   //
            (12, 9, 0),   // End - flows into lowland lake (removed 11,9)
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
            cell.TerrainTypeIndex = 1; // Grass for mid areas
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

    /// <summary>
    /// Creates a large lake with terraced shoreline demonstrating gradual elevation transitions.
    /// Land elevations: 1 (just above water), 2, and 3 surrounding the water.
    /// This shows the smooth terrain-to-water blending without harsh cliffs.
    /// Located at x=25-35, z=3-12 (unused area on right side of map).
    /// </summary>
    private static void GenerateTerracedCoastline(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating terraced coastline demonstration...");
        int waterLevel = 1;
        int waterCount = 0;
        int terraceCount = 0;

        // Large central lake - water cells (elevation 0, water level 1)
        var waterCells = new (int x, int z)[]
        {
            // Core water body
            (28, 6), (29, 6), (30, 6), (31, 6),
            (28, 7), (29, 7), (30, 7), (31, 7), (32, 7),
            (28, 8), (29, 8), (30, 8), (31, 8), (32, 8),
            (29, 9), (30, 9), (31, 9),
        };

        // Elevation 1 terrace (just above water - this is what user wants to see)
        var terrace1Cells = new (int x, int z)[]
        {
            // Northern shore (elevation 1)
            (27, 5), (28, 5), (29, 5), (30, 5), (31, 5), (32, 5),
            // Western shore
            (26, 6), (27, 6), (27, 7), (26, 8), (27, 8),
            // Eastern shore
            (32, 6), (33, 6), (33, 7), (33, 8), (33, 9),
            // Southern shore
            (28, 9), (32, 9), (28, 10), (29, 10), (30, 10), (31, 10), (32, 10),
        };

        // Elevation 2 terrace (middle terrace)
        var terrace2Cells = new (int x, int z)[]
        {
            // Northern area
            (26, 4), (27, 4), (28, 4), (29, 4), (30, 4), (31, 4), (32, 4), (33, 4),
            // Western area
            (25, 5), (26, 5), (25, 6), (25, 7), (26, 7), (25, 8), (25, 9),
            // Eastern area
            (34, 5), (34, 6), (34, 7), (34, 8), (34, 9),
            // Southern area
            (26, 9), (27, 9), (26, 10), (27, 10), (33, 10), (34, 10),
            (27, 11), (28, 11), (29, 11), (30, 11), (31, 11), (32, 11), (33, 11),
        };

        // Elevation 3 terrace (outer ring)
        var terrace3Cells = new (int x, int z)[]
        {
            // Northern outer ring
            (25, 3), (26, 3), (27, 3), (28, 3), (29, 3), (30, 3), (31, 3), (32, 3), (33, 3), (34, 3),
            // Western outer
            (24, 4), (25, 4), (24, 5), (24, 6), (24, 7), (24, 8), (24, 9), (24, 10),
            // Eastern outer
            (35, 4), (35, 5), (35, 6), (35, 7), (35, 8), (35, 9), (35, 10),
            // Southern outer
            (25, 10), (25, 11), (26, 11), (34, 11), (35, 11),
            (25, 12), (26, 12), (27, 12), (28, 12), (29, 12), (30, 12), (31, 12), (32, 12), (33, 12), (34, 12),
        };

        // Set water cells
        foreach (var (x, z) in waterCells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = 0;
            cell.WaterLevel = waterLevel;
            cell.TerrainTypeIndex = 2; // Mud for water areas
            waterCount++;
        }

        // Set elevation 1 terrace (grass - this creates the smooth shore transition)
        foreach (var (x, z) in terrace1Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = 1;
            cell.WaterLevel = 0;
            cell.TerrainTypeIndex = 1; // Grass
            terraceCount++;
        }

        // Set elevation 2 terrace (grass)
        foreach (var (x, z) in terrace2Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = 2;
            cell.WaterLevel = 0;
            cell.TerrainTypeIndex = 1; // Grass
            terraceCount++;
        }

        // Set elevation 3 terrace (stone for variety)
        foreach (var (x, z) in terrace3Cells)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;
            cell.Elevation = 3;
            cell.WaterLevel = 0;
            cell.TerrainTypeIndex = 3; // Stone
            terraceCount++;
        }

        GD.Print($"  Terraced coastline created: {waterCount} water cells, {terraceCount} terrace cells");
        GD.Print($"    Location: x=24-35, z=3-12 (pan camera right to see it)");
    }
}
