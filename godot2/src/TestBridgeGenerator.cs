using Godot;
using System;

/// <summary>
/// Generates test bridge placements to verify bridge rendering across rivers.
/// Creates distinct test scenarios for bridges with various river configurations.
/// Ported from Catlike Coding Hex Map Tutorial 11.
/// </summary>
public static class TestBridgeGenerator
{
    /// <summary>
    /// Generates test bridge placements on the grid.
    /// Creates scenarios to test bridges across straight and curved rivers.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test bridge patterns...");

        // Scenario 1: Bridge across straight river
        GenerateStraightRiverWithBridge(getCell);

        // Scenario 2: Bridge across curved river (gentle curve)
        GenerateCurvedRiverWithBridge(getCell);

        // Scenario 3: Multiple bridges on same river
        GenerateMultipleBridges(getCell);

        GD.Print("Test bridge patterns generated.");
    }

    /// <summary>
    /// Scenario 1: Straight river with road crossing perpendicular.
    /// River flows straight through cell, road crosses from one side to opposite.
    /// Tests bridge placement at straight river crossing.
    /// </summary>
    private static void GenerateStraightRiverWithBridge(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating straight river with bridge...");

        // Set up a straight river segment at (15, 5) through (15, 7)
        // River flows from north to south (straight through)
        int x = 15;
        int baseZ = 5;

        // Set elevations for the river path (flat terrain)
        for (int z = baseZ; z <= baseZ + 2; z++)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.Elevation = 1;
                cell.Color = new Color(0.3f, 0.5f, 0.7f); // Blue-ish for bridge test area
            }
        }

        // Create straight river (incoming from NE, outgoing to SW)
        // Middle cell (15, 6) will have river straight through
        var topCell = getCell(x, baseZ);
        var middleCell = getCell(x, baseZ + 1);
        var bottomCell = getCell(x, baseZ + 2);

        if (topCell != null && middleCell != null)
        {
            // River flows from top to middle
            topCell.SetOutgoingRiver(HexDirection.SW);
        }

        if (middleCell != null && bottomCell != null)
        {
            // River continues from middle to bottom (straight through)
            middleCell.SetOutgoingRiver(HexDirection.SW);
        }

        // Add road crossing the river in the middle cell
        // Road goes from E to W (perpendicular to river)
        if (middleCell != null)
        {
            // Set up neighbor cells for road
            var eastNeighbor = middleCell.GetNeighbor(HexDirection.E);
            var westNeighbor = middleCell.GetNeighbor(HexDirection.W);

            if (eastNeighbor != null)
            {
                eastNeighbor.Elevation = 1;
            }
            if (westNeighbor != null)
            {
                westNeighbor.Elevation = 1;
            }

            // Add roads from neighbors towards the river cell
            // Bridge should appear where road meets river
            if (eastNeighbor != null)
            {
                eastNeighbor.AddRoad(HexDirection.W);
            }
            if (westNeighbor != null)
            {
                westNeighbor.AddRoad(HexDirection.E);
            }
        }

        GD.Print("  Straight river with bridge created at (15, 5-7)");
    }

    /// <summary>
    /// Scenario 2: Curved river with road crossing at the curve.
    /// River curves through cell, road goes from curve apex to opposite side.
    /// Tests bridge placement at curved river crossing.
    /// </summary>
    private static void GenerateCurvedRiverWithBridge(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating curved river with bridge...");

        // Set up a curved river at (18, 5) - river enters from E, exits to SW
        // This creates a gentle curve pattern
        int curveX = 18;
        int curveZ = 6;

        // Set up the curve cell and neighbors
        var curveCell = getCell(curveX, curveZ);
        if (curveCell == null) return;

        curveCell.Elevation = 1;
        curveCell.Color = new Color(0.5f, 0.3f, 0.7f); // Purple-ish for curve test

        // Set up incoming river from E
        var eastCell = curveCell.GetNeighbor(HexDirection.E);
        if (eastCell != null)
        {
            eastCell.Elevation = 1;
            eastCell.SetOutgoingRiver(HexDirection.W);
        }

        // River exits to SW (creating a curve)
        var swCell = curveCell.GetNeighbor(HexDirection.SW);
        if (swCell != null)
        {
            swCell.Elevation = 1;
        }
        curveCell.SetOutgoingRiver(HexDirection.SW);

        // Add road crossing the curve
        // Road goes from NE (opposite of SW exit) through the cell
        var neCell = curveCell.GetNeighbor(HexDirection.NE);
        if (neCell != null)
        {
            neCell.Elevation = 1;
            neCell.AddRoad(HexDirection.SW);
        }

        GD.Print($"  Curved river with bridge created at ({curveX}, {curveZ})");
    }

    /// <summary>
    /// Scenario 3: River with multiple bridge crossings.
    /// Tests multiple bridges on the same river.
    /// </summary>
    private static void GenerateMultipleBridges(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating river with multiple bridges...");

        // Create a longer river with multiple road crossings
        int startX = 15;
        int startZ = 10;

        // Set up a 5-cell river path going diagonally
        var riverPath = new (int x, int z)[]
        {
            (startX, startZ),
            (startX, startZ + 1),
            (startX, startZ + 2),
            (startX, startZ + 3),
            (startX, startZ + 4),
        };

        // Set elevations
        foreach (var (x, z) in riverPath)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.Elevation = 1;
                cell.Color = new Color(0.4f, 0.6f, 0.5f); // Teal for multi-bridge area
            }
        }

        // Create river connections
        for (int i = 0; i < riverPath.Length - 1; i++)
        {
            var cell = getCell(riverPath[i].x, riverPath[i].z);
            if (cell != null)
            {
                cell.SetOutgoingRiver(HexDirection.SW);
            }
        }

        // Add road crossings at cells 1 and 3 (0-indexed)
        for (int i = 1; i < riverPath.Length - 1; i += 2)
        {
            var (x, z) = riverPath[i];
            var cell = getCell(x, z);
            if (cell == null) continue;

            var eastNeighbor = cell.GetNeighbor(HexDirection.E);
            var westNeighbor = cell.GetNeighbor(HexDirection.W);

            if (eastNeighbor != null)
            {
                eastNeighbor.Elevation = 1;
                eastNeighbor.AddRoad(HexDirection.W);
            }
            if (westNeighbor != null)
            {
                westNeighbor.Elevation = 1;
                westNeighbor.AddRoad(HexDirection.E);
            }
        }

        GD.Print($"  River with multiple bridges created at ({startX}, {startZ}-{startZ + 4})");
    }
}
