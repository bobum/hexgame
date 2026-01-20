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

        // Pattern 1: Straight river (NE-SW) at column 2
        GenerateStraightRiver(getCell, 2, 1, 6);

        // Pattern 2: Sharp right turn at (6, 4)
        GenerateSharpRightTurn(getCell, 6, 4);

        // Pattern 3: Sharp left turn at (6, 8)
        GenerateSharpLeftTurn(getCell, 6, 8);

        // Pattern 4: Gentle right curve at (10, 4)
        GenerateGentleRightCurve(getCell, 10, 4);

        // Pattern 5: Gentle left curve at (10, 8)
        GenerateGentleLeftCurve(getCell, 10, 8);

        // Pattern 6: River source (begin) at (14, 2)
        GenerateRiverSource(getCell, 14, 2);

        // Pattern 7: River terminus (end) at (14, 6)
        GenerateRiverTerminus(getCell, 14, 6);

        // Pattern 8: Waterfall (elevation change) at column 17
        GenerateWaterfall(getCell, 17, 3, 7);

        // Pattern 9: River merge at (3, 10)
        GenerateRiverMerge(getCell, 3, 10);

        GD.Print("Test river patterns generated.");
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
