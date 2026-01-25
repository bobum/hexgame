using Godot;
using System;

/// <summary>
/// Generates deterministic test rivers to verify all river configurations.
/// Creates 9 specific patterns covering:
/// - Straight river
/// - Sharp right turn (zigzag)
/// - Sharp left turn (zigzag)
/// - Gentle right curve (Next2)
/// - Gentle left curve (Previous2)
/// - River source (begin)
/// - River terminus (end)
/// - Waterfall (elevation change)
/// - River connection/merge
/// </summary>
public static class TestRiverGenerator
{
    /// <summary>
    /// Generates all test river patterns on the grid.
    /// Requires grid to be at least 4x3 chunks (20x15 cells).
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test river patterns...");

        // Generate one long showcase river demonstrating all 5 configurations + waterfall
        GenerateShowcaseRiver(getCell);

        GD.Print("Test river patterns generated.");
    }

    /// <summary>
    /// Generates a single long river demonstrating all 5 river configurations:
    /// - Straight sections
    /// - Sharp right turn (zigzag)
    /// - Sharp left turn (zigzag)
    /// - Gentle right curve (Next2)
    /// - Gentle left curve (Previous2)
    /// Plus a waterfall with elevation change.
    /// </summary>
    private static void GenerateShowcaseRiver(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating showcase river: Mountain -> Waterfall -> All turn types");

        // Path design:
        // 1. Mountain SOURCE at high elevation
        // 2. WATERFALL descending to flat area
        // 3. S-curve demonstrating all turn types on flat terrain
        // 4. TERMINUS
        //
        // Hex adjacency (offset coords):
        // Even row z: SE=(x,z+1), SW=(x-1,z+1), E=(x+1,z), W=(x-1,z)
        // Odd row z:  SE=(x+1,z+1), SW=(x,z+1), E=(x+1,z), W=(x-1,z)

        var path = new (int x, int z, int elevation)[]
        {
            // === MOUNTAIN SOURCE ===
            (5, 2, 6),    // Start at elevation 6 (mountain peak)

            // === WATERFALL (elevation 6 -> 1) ===
            (5, 3, 5),    // z=2 even: SE to (5,3)
            (5, 4, 4),    // z=3 odd: SW to (5,4)
            (5, 5, 3),    // z=4 even: SE to (5,5)
            (5, 6, 2),    // z=5 odd: SW to (5,6)
            (5, 7, 1),    // z=6 even: SE to (5,7) - reached flat area

            // === FLAT AREA - TURN EAST (demonstrates turn from SE to E) ===
            (6, 7, 1),    // same row z=7: E to (6,7)
            (7, 7, 1),    // same row z=7: E to (7,7)

            // === TURN SOUTH (demonstrates turn from E to SW) ===
            (7, 8, 1),    // z=7 odd: SW to (7,8)

            // === TURN WEST (demonstrates turn from SW to W) ===
            (6, 8, 1),    // same row z=8: W to (6,8)
            (5, 8, 1),    // same row z=8: W to (5,8)

            // === TURN SOUTH-EAST (demonstrates turn from W to SE) ===
            (5, 9, 1),    // z=8 even: SE to (5,9)

            // === TERMINUS ===
            (5, 10, 0),   // z=9 odd: SW to (5,10) - end at elevation 0
        };

        // First pass: set all elevations
        GD.Print("  Pass 1: Setting elevations...");
        foreach (var (x, z, elevation) in path)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.Elevation = elevation;
                cell.TerrainTypeIndex = 1; // Grass for river path visibility
                GD.Print($"    Cell ({x},{z}) elevation set to {elevation}");
            }
            else
            {
                GD.PrintErr($"    Cell ({x},{z}) is NULL!");
            }
        }

        // Second pass: create river connections
        GD.Print("  Pass 2: Creating river connections...");
        int successCount = 0;
        for (int i = 0; i < path.Length - 1; i++)
        {
            var (x1, z1, e1) = path[i];
            var (x2, z2, e2) = path[i + 1];

            var cell = getCell(x1, z1);
            if (cell == null)
            {
                GD.PrintErr($"    Source cell ({x1},{z1}) is NULL!");
                continue;
            }

            var nextCell = getCell(x2, z2);
            if (nextCell == null)
            {
                GD.PrintErr($"    Target cell ({x2},{z2}) is NULL!");
                continue;
            }

            // Determine direction to next cell by checking actual neighbors
            HexDirection? dir = GetDirectionBetweenCells(getCell, x1, z1, x2, z2);
            if (dir.HasValue)
            {
                GD.Print($"    ({x1},{z1}) e={cell.Elevation} -> ({x2},{z2}) e={nextCell.Elevation} dir={dir.Value}");
                cell.SetOutgoingRiver(dir.Value);

                // Verify the river was set
                if (cell.HasOutgoingRiver && cell.OutgoingRiver == dir.Value)
                {
                    GD.Print($"      SUCCESS: River set!");
                    successCount++;
                }
                else
                {
                    GD.PrintErr($"      FAILED: River NOT set! (maybe elevation issue?)");
                }
            }
            else
            {
                GD.PrintErr($"    No direction found from ({x1},{z1}) to ({x2},{z2})!");
            }
        }

        GD.Print($"  Showcase river: {successCount}/{path.Length - 1} segments created");
    }

    /// <summary>
    /// Determines the hex direction from one cell to an adjacent cell.
    /// Instead of computing from coordinates, we check actual neighbors.
    /// </summary>
    private static HexDirection? GetDirectionBetweenCells(
        Func<int, int, HexCell?> getCell, int x1, int z1, int x2, int z2)
    {
        var sourceCell = getCell(x1, z1);
        var targetCell = getCell(x2, z2);

        if (sourceCell == null || targetCell == null)
        {
            GD.PrintErr($"  Null cell: source=({x1},{z1}) target=({x2},{z2})");
            return null;
        }

        // Check all 6 directions to find which neighbor matches target
        for (int d = 0; d < 6; d++)
        {
            var dir = (HexDirection)d;
            var neighbor = sourceCell.GetNeighbor(dir);
            if (neighbor == targetCell)
            {
                return dir;
            }
        }

        GD.PrintErr($"  Cells not adjacent: ({x1},{z1}) to ({x2},{z2})");
        return null;
    }

    /// <summary>
    /// Generates a straight river flowing from (x, startZ) to (x, endZ).
    /// Sets elevations flat to ensure valid flow.
    /// </summary>
    private static void GenerateStraightRiver(
        Func<int, int, HexCell?> getCell, int x, int startZ, int endZ)
    {
        // Set elevations first (flat terrain)
        for (int z = startZ; z <= endZ; z++)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.Elevation = 1;
            }
        }

        // Create river flowing south (NE to SW direction varies by row)
        for (int z = startZ; z < endZ; z++)
        {
            var cell = getCell(x, z);
            var nextCell = getCell(x, z + 1);
            if (cell != null && nextCell != null)
            {
                // Flow direction depends on row parity
                HexDirection flowDir = (z & 1) == 0 ? HexDirection.SE : HexDirection.SW;
                cell.SetOutgoingRiver(flowDir);
            }
        }
        GD.Print($"  Straight river: ({x}, {startZ}) to ({x}, {endZ})");
    }

    /// <summary>
    /// Generates a sharp right turn (zigzag) pattern.
    /// River enters from one direction, exits at direction.Next().
    /// </summary>
    private static void GenerateSharpRightTurn(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var prevCell = getCell(x - 1, z);
        var nextCell = getCell(x, z + 1);

        if (cell == null) return;

        // Set flat elevation
        cell.Elevation = 1;
        if (prevCell != null) prevCell.Elevation = 1;
        if (nextCell != null) nextCell.Elevation = 1;

        // Incoming from W, outgoing to SE (sharp right)
        if (prevCell != null)
        {
            prevCell.SetOutgoingRiver(HexDirection.E);
        }
        // Flow direction for sharp right: E.Next() = SE
        cell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SE : HexDirection.SW);

        GD.Print($"  Sharp right turn at ({x}, {z})");
    }

    /// <summary>
    /// Generates a sharp left turn (zigzag) pattern.
    /// River enters from one direction, exits at direction.Previous().
    /// </summary>
    private static void GenerateSharpLeftTurn(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var prevCell = getCell(x + 1, z);
        var nextCell = getCell(x, z + 1);

        if (cell == null) return;

        // Set flat elevation
        cell.Elevation = 1;
        if (prevCell != null) prevCell.Elevation = 1;
        if (nextCell != null) nextCell.Elevation = 1;

        // Incoming from E, outgoing to SW (sharp left)
        if (prevCell != null)
        {
            prevCell.SetOutgoingRiver(HexDirection.W);
        }
        cell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SW : HexDirection.SE);

        GD.Print($"  Sharp left turn at ({x}, {z})");
    }

    /// <summary>
    /// Generates a gentle right curve (Next2) pattern.
    /// River enters from one direction, exits at direction.Next2().
    /// </summary>
    private static void GenerateGentleRightCurve(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var prevCell = getCell(x - 1, z);

        if (cell == null) return;

        // Set flat elevation
        cell.Elevation = 1;
        if (prevCell != null) prevCell.Elevation = 1;

        // Incoming from W (E.Opposite), outgoing to S (E.Next2 = SE.Next = S)
        if (prevCell != null)
        {
            prevCell.SetOutgoingRiver(HexDirection.E);
        }
        // Gentle right: E -> E.Next2() = S
        // But we need to check which neighbor direction is valid
        cell.SetOutgoingRiver(HexDirection.SW);

        GD.Print($"  Gentle right curve at ({x}, {z})");
    }

    /// <summary>
    /// Generates a gentle left curve (Previous2) pattern.
    /// River enters from one direction, exits at direction.Previous2().
    /// </summary>
    private static void GenerateGentleLeftCurve(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var prevCell = getCell(x + 1, z);

        if (cell == null) return;

        // Set flat elevation
        cell.Elevation = 1;
        if (prevCell != null) prevCell.Elevation = 1;

        // Incoming from E, outgoing via gentle left
        if (prevCell != null)
        {
            prevCell.SetOutgoingRiver(HexDirection.W);
        }
        cell.SetOutgoingRiver(HexDirection.SE);

        GD.Print($"  Gentle left curve at ({x}, {z})");
    }

    /// <summary>
    /// Generates a river source (beginning) - only outgoing, no incoming.
    /// </summary>
    private static void GenerateRiverSource(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var nextCell = getCell(x, z + 1);

        if (cell == null) return;

        cell.Elevation = 2;
        if (nextCell != null) nextCell.Elevation = 1;

        // River starts here, flows out
        cell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SE : HexDirection.SW);

        GD.Print($"  River source at ({x}, {z})");
    }

    /// <summary>
    /// Generates a river terminus (end) - only incoming, no outgoing.
    /// </summary>
    private static void GenerateRiverTerminus(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        var cell = getCell(x, z);
        var prevCell = getCell(x, z - 1);

        if (cell == null) return;

        cell.Elevation = 1;
        if (prevCell != null)
        {
            prevCell.Elevation = 2;
            // River flows into terminus
            prevCell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SW : HexDirection.SE);
        }

        GD.Print($"  River terminus at ({x}, {z})");
    }

    /// <summary>
    /// Generates a waterfall with elevation change.
    /// </summary>
    private static void GenerateWaterfall(
        Func<int, int, HexCell?> getCell, int x, int startZ, int endZ)
    {
        // High elevation at start, low at end
        for (int z = startZ; z <= endZ; z++)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                // Elevation decreases along the river
                cell.Elevation = Math.Max(0, 4 - (z - startZ));
            }
        }

        // Create river with elevation changes
        for (int z = startZ; z < endZ; z++)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SE : HexDirection.SW);
            }
        }

        GD.Print($"  Waterfall: ({x}, {startZ}) to ({x}, {endZ})");
    }

    /// <summary>
    /// Generates two rivers merging into one.
    /// </summary>
    private static void GenerateRiverMerge(
        Func<int, int, HexCell?> getCell, int x, int z)
    {
        // Set up cells for merge
        var leftCell = getCell(x - 1, z);
        var rightCell = getCell(x + 1, z);
        var mergeCell = getCell(x, z);
        var downCell = getCell(x, z + 1);

        if (mergeCell == null) return;

        // Set elevations (incoming cells higher)
        if (leftCell != null) leftCell.Elevation = 2;
        if (rightCell != null) rightCell.Elevation = 2;
        mergeCell.Elevation = 1;
        if (downCell != null) downCell.Elevation = 0;

        // Two rivers flow into merge cell, one flows out
        if (leftCell != null)
        {
            leftCell.SetOutgoingRiver(HexDirection.E);
        }
        // Only one outgoing river per cell, so merge cell has incoming from left
        // and outgoing downward
        mergeCell.SetOutgoingRiver((z & 1) == 0 ? HexDirection.SE : HexDirection.SW);

        GD.Print($"  River merge at ({x}, {z})");
    }
}
