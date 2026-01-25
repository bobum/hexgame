using Godot;
using System;

/// <summary>
/// Generates comprehensive test bridge placements to verify bridge rendering.
/// Creates scenarios for all bridge/road/river permutations from Catlike Coding Tutorial 11.
///
/// Key Rule: Bridges ONLY appear where roads exist on BOTH sides of a STRAIGHT river.
/// Curved rivers and river begin/end cells do NOT support bridges.
/// </summary>
public static class TestBridgeGenerator
{
    // Test area starts at row 10 to avoid conflicts with other generators
    private const int TestAreaStartZ = 10;
    private const int TestAreaStartX = 2;

    /// <summary>
    /// Generates all test bridge patterns on the grid.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("=== Generating Bridge Test Patterns ===");

        // SECTION 1: Straight rivers WITH bridges (positive tests)
        GD.Print("\n--- Section 1: Straight Rivers with Bridges ---");
        GenerateStraightRiver_NE_SW_WithBridge(getCell);      // Column 2-4
        GenerateStraightRiver_E_W_WithBridge(getCell);        // Column 6-8
        GenerateStraightRiver_NW_SE_WithBridge(getCell);      // Column 10-12

        // SECTION 2: Straight rivers WITHOUT bridges (negative - missing road)
        GD.Print("\n--- Section 2: Straight Rivers - No Bridge (One Road Only) ---");
        GenerateStraightRiver_OneRoadOnly(getCell);           // Column 14-16

        // SECTION 3: Curved rivers
        GD.Print("\n--- Section 3: Curved Rivers ---");
        GenerateSharpLeftCurve_NoBridge(getCell);             // Row 14, Column 2-4 - NO bridge
        GenerateSharpRightCurve_NoBridge(getCell);            // Row 14, Column 6-8 - NO bridge
        GenerateGentleCurve_WithBridge(getCell);              // Row 14, Column 10-12 - BRIDGE expected

        // SECTION 4: River begin/end - NO bridges
        GD.Print("\n--- Section 4: River Begin/End - No Bridges ---");
        GenerateRiverSource_NoBridge(getCell);                // Row 14, Column 14-16
        GenerateRiverMouth_NoBridge(getCell);                 // Row 14, Column 18-20

        // SECTION 5: Multiple bridges on same river
        GD.Print("\n--- Section 5: Multiple Bridges ---");
        GenerateMultipleBridgesOnRiver(getCell);              // Row 18, Column 2-8

        GD.Print("\n=== Bridge Test Patterns Complete ===");
    }

    /// <summary>
    /// Helper to set up a cell at consistent elevation with cleared features.
    /// </summary>
    private static void SetupCell(HexCell? cell, int elevation = 1)
    {
        if (cell == null) return;
        cell.Elevation = elevation;
        cell.PlantLevel = 0;
        cell.FarmLevel = 0;
        cell.UrbanLevel = 0;
        cell.SpecialIndex = 0;
    }

    // ========================================================================
    // SECTION 1: STRAIGHT RIVERS WITH BRIDGES
    // ========================================================================

    /// <summary>
    /// Test 1.1: Straight river NE→SW axis with E-W roads.
    /// Expected: Bridge appears spanning E to W.
    /// </summary>
    private static void GenerateStraightRiver_NE_SW_WithBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX;
        int z = TestAreaStartZ;
        GD.Print($"  Test 1.1: NE-SW river with E-W roads at ({x}, {z})");

        // Get cells
        var center = getCell(x, z);
        var upstream = getCell(x, z)?.GetNeighbor(HexDirection.NE);
        var downstream = getCell(x, z)?.GetNeighbor(HexDirection.SW);
        var east = getCell(x, z)?.GetNeighbor(HexDirection.E);
        var west = getCell(x, z)?.GetNeighbor(HexDirection.W);

        // Setup all cells at same elevation
        SetupCell(center);
        SetupCell(upstream);
        SetupCell(downstream);
        SetupCell(east);
        SetupCell(west);

        // Create straight river: NE → center → SW
        upstream?.SetOutgoingRiver(HexDirection.SW);
        center?.SetOutgoingRiver(HexDirection.SW);

        // Add roads on BOTH sides (E and W) - should create bridge
        east?.AddRoad(HexDirection.W);
        west?.AddRoad(HexDirection.E);

        GD.Print($"    River: NE→SW, Roads: E+W → BRIDGE expected");
    }

    /// <summary>
    /// Test 1.2: Straight river E→W axis with NE-SW roads.
    /// Expected: Bridge appears spanning the perpendicular roads.
    /// </summary>
    private static void GenerateStraightRiver_E_W_WithBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 5;
        int z = TestAreaStartZ;
        GD.Print($"  Test 1.2: E-W river with NE-SW roads at ({x}, {z})");

        var center = getCell(x, z);
        var upstream = center?.GetNeighbor(HexDirection.E);
        var downstream = center?.GetNeighbor(HexDirection.W);
        var ne = center?.GetNeighbor(HexDirection.NE);
        var sw = center?.GetNeighbor(HexDirection.SW);

        SetupCell(center);
        SetupCell(upstream);
        SetupCell(downstream);
        SetupCell(ne);
        SetupCell(sw);

        // Create straight river: E → center → W
        upstream?.SetOutgoingRiver(HexDirection.W);
        center?.SetOutgoingRiver(HexDirection.W);

        // Add roads perpendicular to river
        ne?.AddRoad(HexDirection.SW);
        sw?.AddRoad(HexDirection.NE);

        GD.Print($"    River: E→W, Roads: NE+SW → BRIDGE expected");
    }

    /// <summary>
    /// Test 1.3: Straight river NW→SE axis with perpendicular roads.
    /// Expected: Bridge appears.
    /// </summary>
    private static void GenerateStraightRiver_NW_SE_WithBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 10;
        int z = TestAreaStartZ;
        GD.Print($"  Test 1.3: NW-SE river with perpendicular roads at ({x}, {z})");

        var center = getCell(x, z);
        var upstream = center?.GetNeighbor(HexDirection.NW);
        var downstream = center?.GetNeighbor(HexDirection.SE);
        var ne = center?.GetNeighbor(HexDirection.NE);
        var sw = center?.GetNeighbor(HexDirection.SW);

        SetupCell(center);
        SetupCell(upstream);
        SetupCell(downstream);
        SetupCell(ne);
        SetupCell(sw);

        // Create straight river: NW → center → SE
        upstream?.SetOutgoingRiver(HexDirection.SE);
        center?.SetOutgoingRiver(HexDirection.SE);

        // Add roads perpendicular
        ne?.AddRoad(HexDirection.SW);
        sw?.AddRoad(HexDirection.NE);

        GD.Print($"    River: NW→SE, Roads: NE+SW → BRIDGE expected");
    }

    // ========================================================================
    // SECTION 2: STRAIGHT RIVERS - NO BRIDGE (missing road on one side)
    // ========================================================================

    /// <summary>
    /// Test 2.1: Straight river with road on only ONE side.
    /// Expected: NO bridge (need roads on BOTH sides).
    /// </summary>
    private static void GenerateStraightRiver_OneRoadOnly(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 14;
        int z = TestAreaStartZ;
        GD.Print($"  Test 2.1: NE-SW river with E road ONLY at ({x}, {z})");

        var center = getCell(x, z);
        var upstream = center?.GetNeighbor(HexDirection.NE);
        var downstream = center?.GetNeighbor(HexDirection.SW);
        var east = center?.GetNeighbor(HexDirection.E);

        SetupCell(center);
        SetupCell(upstream);
        SetupCell(downstream);
        SetupCell(east);

        // Create straight river
        upstream?.SetOutgoingRiver(HexDirection.SW);
        center?.SetOutgoingRiver(HexDirection.SW);

        // Add road on ONLY the east side - no bridge should appear
        east?.AddRoad(HexDirection.W);

        GD.Print($"    River: NE→SW, Roads: E only → NO bridge expected");
    }

    // ========================================================================
    // SECTION 3: CURVED RIVERS - NO BRIDGES
    // ========================================================================

    /// <summary>
    /// Test 3.1: Sharp left curve - NO bridge.
    /// Config 3: IncomingRiver == OutgoingRiver.Previous()
    /// </summary>
    private static void GenerateSharpLeftCurve_NoBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX;
        int z = TestAreaStartZ + 4;
        GD.Print($"  Test 3.1: Sharp LEFT curve (NE in, E out) at ({x}, {z})");

        var center = getCell(x, z);
        var incoming = center?.GetNeighbor(HexDirection.NE);
        var outgoing = center?.GetNeighbor(HexDirection.E);
        var se = center?.GetNeighbor(HexDirection.SE);
        var w = center?.GetNeighbor(HexDirection.W);

        SetupCell(center);
        SetupCell(incoming);
        SetupCell(outgoing);
        SetupCell(se);
        SetupCell(w);

        // Create sharp left curve: NE → center → E (NE == E.Previous())
        incoming?.SetOutgoingRiver(HexDirection.SW);  // flows into center from NE
        center?.SetOutgoingRiver(HexDirection.E);     // flows out to E

        // Add roads that would cross IF bridges were supported
        se?.AddRoad(HexDirection.NW);
        w?.AddRoad(HexDirection.E);

        GD.Print($"    River: NE→E (sharp left), Roads: SE+W → NO bridge expected");
    }

    /// <summary>
    /// Test 3.2: Sharp right curve - NO bridge.
    /// Config 4: IncomingRiver == OutgoingRiver.Next()
    /// </summary>
    private static void GenerateSharpRightCurve_NoBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 5;
        int z = TestAreaStartZ + 4;
        GD.Print($"  Test 3.2: Sharp RIGHT curve (E in, NE out) at ({x}, {z})");

        var center = getCell(x, z);
        var incoming = center?.GetNeighbor(HexDirection.E);
        var outgoing = center?.GetNeighbor(HexDirection.NE);
        var nw = center?.GetNeighbor(HexDirection.NW);
        var sw = center?.GetNeighbor(HexDirection.SW);

        SetupCell(center);
        SetupCell(incoming);
        SetupCell(outgoing);
        SetupCell(nw);
        SetupCell(sw);

        // Create sharp right curve: E → center → NE (E == NE.Next())
        incoming?.SetOutgoingRiver(HexDirection.W);   // flows into center from E
        center?.SetOutgoingRiver(HexDirection.NE);    // flows out to NE

        // Add roads
        nw?.AddRoad(HexDirection.SE);
        sw?.AddRoad(HexDirection.NE);

        GD.Print($"    River: E→NE (sharp right), Roads: NW+SW → NO bridge expected");
    }

    /// <summary>
    /// Test 3.3: Gentle curve (2 steps apart) - BRIDGE expected!
    /// Gentle curves have 1 empty hex side between river entry and exit.
    /// </summary>
    private static void GenerateGentleCurve_WithBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 10;
        int z = TestAreaStartZ + 4;
        GD.Print($"  Test 3.3: Gentle curve (NE in, SE out) at ({x}, {z})");

        var center = getCell(x, z);
        var incoming = center?.GetNeighbor(HexDirection.NE);
        var outgoing = center?.GetNeighbor(HexDirection.SE);
        var e = center?.GetNeighbor(HexDirection.E);
        var w = center?.GetNeighbor(HexDirection.W);

        SetupCell(center);
        SetupCell(incoming);
        SetupCell(outgoing);
        SetupCell(e);
        SetupCell(w);

        // Create gentle curve: NE → center → SE (2 steps clockwise)
        incoming?.SetOutgoingRiver(HexDirection.SW);
        center?.SetOutgoingRiver(HexDirection.SE);

        // Add roads on both sides - SHOULD create bridge
        e?.AddRoad(HexDirection.W);
        w?.AddRoad(HexDirection.E);

        GD.Print($"    River: NE→SE (gentle curve), Roads: E+W → BRIDGE expected");
    }

    // ========================================================================
    // SECTION 4: RIVER BEGIN/END - NO BRIDGES
    // ========================================================================

    /// <summary>
    /// Test 4.1: River source (outgoing only) - NO bridge.
    /// </summary>
    private static void GenerateRiverSource_NoBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 14;
        int z = TestAreaStartZ + 4;
        GD.Print($"  Test 4.1: River SOURCE (outgoing only) at ({x}, {z})");

        var center = getCell(x, z);
        var downstream = center?.GetNeighbor(HexDirection.SW);
        var e = center?.GetNeighbor(HexDirection.E);
        var w = center?.GetNeighbor(HexDirection.W);

        SetupCell(center);
        SetupCell(downstream);
        SetupCell(e);
        SetupCell(w);

        // River starts here - only outgoing, no incoming
        center?.SetOutgoingRiver(HexDirection.SW);

        // Add roads on both sides
        e?.AddRoad(HexDirection.W);
        w?.AddRoad(HexDirection.E);

        GD.Print($"    River: SOURCE→SW, Roads: E+W → NO bridge expected");
    }

    /// <summary>
    /// Test 4.2: River mouth (incoming only) - NO bridge.
    /// </summary>
    private static void GenerateRiverMouth_NoBridge(Func<int, int, HexCell?> getCell)
    {
        int x = TestAreaStartX + 18;
        int z = TestAreaStartZ + 4;
        GD.Print($"  Test 4.2: River MOUTH (incoming only) at ({x}, {z})");

        var center = getCell(x, z);
        var upstream = center?.GetNeighbor(HexDirection.NE);
        var e = center?.GetNeighbor(HexDirection.E);
        var w = center?.GetNeighbor(HexDirection.W);

        SetupCell(center);
        SetupCell(upstream);
        SetupCell(e);
        SetupCell(w);

        // River ends here - incoming from NE, no outgoing
        upstream?.SetOutgoingRiver(HexDirection.SW);
        // Don't set outgoing on center - it's a river mouth

        // Add roads on both sides
        e?.AddRoad(HexDirection.W);
        w?.AddRoad(HexDirection.E);

        GD.Print($"    River: NE→MOUTH, Roads: E+W → NO bridge expected");
    }

    // ========================================================================
    // SECTION 5: MULTIPLE BRIDGES ON SAME RIVER
    // ========================================================================

    /// <summary>
    /// Test 5.1: Long straight river with multiple road crossings.
    /// Expected: Multiple bridges, one at each crossing.
    /// </summary>
    private static void GenerateMultipleBridgesOnRiver(Func<int, int, HexCell?> getCell)
    {
        int startX = TestAreaStartX;
        int z = TestAreaStartZ + 8;
        GD.Print($"  Test 5.1: Multiple bridges on straight river at row {z}");

        // Create a long straight river flowing SW through multiple cells
        HexCell?[] riverCells = new HexCell?[5];
        for (int i = 0; i < 5; i++)
        {
            riverCells[i] = getCell(startX + i * 2, z);
            SetupCell(riverCells[i]);

            // Also setup upstream/downstream neighbors
            var ne = riverCells[i]?.GetNeighbor(HexDirection.NE);
            var sw = riverCells[i]?.GetNeighbor(HexDirection.SW);
            SetupCell(ne);
            SetupCell(sw);
        }

        // Create continuous straight river through all cells
        for (int i = 0; i < 5; i++)
        {
            var cell = riverCells[i];
            var upstream = cell?.GetNeighbor(HexDirection.NE);

            // Set incoming river from NE
            upstream?.SetOutgoingRiver(HexDirection.SW);
            // Set outgoing river to SW
            cell?.SetOutgoingRiver(HexDirection.SW);
        }

        // Add road crossings at alternating cells (cells 0, 2, 4)
        for (int i = 0; i < 5; i += 2)
        {
            var cell = riverCells[i];
            var east = cell?.GetNeighbor(HexDirection.E);
            var west = cell?.GetNeighbor(HexDirection.W);

            SetupCell(east);
            SetupCell(west);

            east?.AddRoad(HexDirection.W);
            west?.AddRoad(HexDirection.E);

            GD.Print($"    Bridge {i/2 + 1} at cell {i}: E+W roads");
        }

        GD.Print($"    Expected: 3 bridges at cells 0, 2, 4");
    }
}
