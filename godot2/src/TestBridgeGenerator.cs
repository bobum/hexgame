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

        // SECTION 6: Complete river loop with all 8 bridge types (matching reference image)
        GD.Print("\n--- Section 6: River Loop with 8 Bridges ---");
        GenerateRiverLoopWithAllBridges(getCell);             // Row 22+, Column 8+

        // SECTION 7: Waterfall test - rivers with elevation changes
        GD.Print("\n--- Section 7: Waterfall Test ---");
        GenerateWaterfallTest(getCell);                       // Column 20, Row 2-6

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

    // ========================================================================
    // SECTION 6: RIVER LOOP WITH ALL 8 BRIDGE TYPES
    // ========================================================================

    /// <summary>
    /// Creates a closed river loop matching the reference image with 8 bridges.
    /// Uses GetNeighbor to ensure cells are actually adjacent.
    /// Flattens a large area first with consistent elevation and color.
    /// </summary>
    private static void GenerateRiverLoopWithAllBridges(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating river loop with 8 bridges...");

        // Start cell - use offset coords, will report cube coords
        // Using offset (8, 6) as starting point for the loop
        var startCell = getCell(8, 6);
        if (startCell == null)
        {
            GD.PrintErr("    Failed to get start cell!");
            return;
        }

        GD.Print($"    Start cell cube coords: {startCell.Coordinates}");

        // First, flatten a large area around the loop (7x7 cells)
        // Tutorial 14: Use terrain type 0 (sand) instead of color
        // Extended west to include Cell 6's W neighbor at offset (5, 7)
        // Keep original east boundary but restore highland lake cliff terrain after
        FlattenArea(getCell, 4, 4, 14, 12, 0); // Sand terrain type

        // Restore cliff terrain around highland lake to prevent floating water
        // Highland lake is at water level 4, so surrounding terrain needs elevation 4+
        const int cliffTerrainType = 3; // Stone
        var cliffCells = new (int x, int z)[]
        {
            // Row Z=3: cube (13,-16,3) to (16,-19,3)
            (14, 3), (15, 3), (16, 3), (17, 3),
            // Row Z=4: cube (12,-16,4), (15,-19,4), (16,-20,4)
            (14, 4), (17, 4), (18, 4),
            // Row Z=5: cube (12,-17,5), (16,-21,5)
            (14, 5), (18, 5),
            // Row Z=6: cube (13,-19,6) to (15,-21,6)
            (16, 6), (17, 6), (18, 6),
        };
        foreach (var (x, z) in cliffCells)
        {
            var cell = getCell(x, z);
            if (cell != null)
            {
                cell.Elevation = 4;
                cell.TerrainTypeIndex = cliffTerrainType;
                cell.Walled = false; // Remove walls
            }
        }

        // Set up waterfall path: (15,5) -> (15,6) -> (14,7) -> lake
        var cell_15_5 = getCell(15, 5);
        var cell_15_6 = getCell(15, 6);
        var cell_14_7 = getCell(14, 7);

        // (15,5) cube (13,-18,5) - elevation 3, keep river from TestWaterGenerator
        if (cell_15_5 != null)
        {
            cell_15_5.Elevation = 3;
            cell_15_5.TerrainTypeIndex = 1; // Grass
            cell_15_5.Walled = false;
        }

        // Create river outflow from highland lake: (15,4) -> (14,4) -> (13,5)
        var cell_15_4 = getCell(15, 4);
        var cell_14_4 = getCell(14, 4);
        var cell_13_5 = getCell(13, 5);

        // (15,4) cube (13,-17,4) - elevation 3, river to (14,4)
        if (cell_15_4 != null)
        {
            cell_15_4.Elevation = 3; // Lowered
            cell_15_4.Walled = false;

            if (cell_14_4 != null)
            {
                for (int d = 0; d < 6; d++)
                {
                    if (cell_15_4.GetNeighbor((HexDirection)d) == cell_14_4)
                    {
                        cell_15_4.SetOutgoingRiver((HexDirection)d);
                        GD.Print($"    River: (15,4) -> (14,4) dir={(HexDirection)d}");
                        break;
                    }
                }
            }
        }

        // (14,4) cube (12,-16,4) - river to (13,5)
        if (cell_14_4 != null)
        {
            cell_14_4.Walled = false;

            if (cell_13_5 != null)
            {
                for (int d = 0; d < 6; d++)
                {
                    if (cell_14_4.GetNeighbor((HexDirection)d) == cell_13_5)
                    {
                        cell_14_4.SetOutgoingRiver((HexDirection)d);
                        GD.Print($"    River: (14,4) -> (13,5) dir={(HexDirection)d}");
                        break;
                    }
                }
            }
        }

        // (15,6) cube (12,-18,6) - river continues to (14,7)
        if (cell_15_6 != null)
        {
            cell_15_6.Elevation = 3;
            cell_15_6.TerrainTypeIndex = 1; // Grass
            cell_15_6.Walled = false;

            if (cell_14_7 != null)
            {
                for (int d = 0; d < 6; d++)
                {
                    if (cell_15_6.GetNeighbor((HexDirection)d) == cell_14_7)
                    {
                        cell_15_6.SetOutgoingRiver((HexDirection)d);
                        GD.Print($"    River: (15,6) -> (14,7) dir={(HexDirection)d}");
                        break;
                    }
                }
            }
        }

        if (cell_14_7 != null)
        {
            cell_14_7.Elevation = 2; // Lower - waterfall drop
            cell_14_7.TerrainTypeIndex = 1; // Grass
            cell_14_7.Walled = false; // Remove wall

            // River continues to (14,8) cube (10,-18,8)
            var cell_14_8 = getCell(14, 8);
            if (cell_14_8 != null)
            {
                for (int d = 0; d < 6; d++)
                {
                    if (cell_14_7.GetNeighbor((HexDirection)d) == cell_14_8)
                    {
                        cell_14_7.SetOutgoingRiver((HexDirection)d);
                        GD.Print($"    River: (14,7) -> (14,8) dir={(HexDirection)d}");
                        break;
                    }
                }
            }
        }

        // Build the 8-cell loop using GetNeighbor to ensure adjacency
        // Loop layout (clockwise):
        //
        //     [0] -E-> [1]
        //      ^        |
        //     NE       SE
        //      |        v
        //     [7]      [2]
        //      ^        |
        //     NW       SW
        //      |        v
        //     [6] <-W- [5] <-W- [4] <-W- [3]
        //
        // Rivers flow: 0->1->2->3->4->5->6->7->0

        var loop = new HexCell?[8];

        // Build loop by following neighbors
        loop[0] = startCell;
        loop[1] = loop[0]?.GetNeighbor(HexDirection.E);
        loop[2] = loop[1]?.GetNeighbor(HexDirection.SE);
        loop[3] = loop[2]?.GetNeighbor(HexDirection.SW);
        loop[4] = loop[3]?.GetNeighbor(HexDirection.W);
        loop[5] = loop[4]?.GetNeighbor(HexDirection.W);
        loop[6] = loop[5]?.GetNeighbor(HexDirection.NW);
        loop[7] = loop[6]?.GetNeighbor(HexDirection.NE);

        // Verify loop closes (cell 7's E neighbor should be cell 0)
        var loopCheck = loop[7]?.GetNeighbor(HexDirection.E);
        if (loopCheck != loop[0])
        {
            GD.PrintErr("    WARNING: Loop doesn't close properly!");
        }

        // Print cube coordinates of all loop cells
        for (int i = 0; i < 8; i++)
        {
            if (loop[i] != null)
            {
                GD.Print($"    Loop[{i}]: {loop[i].Coordinates}");
            }
        }

        // Set river flow around the loop (clockwise)
        // Direction from each cell to the next
        HexDirection[] riverDirs = {
            HexDirection.E,   // 0 -> 1
            HexDirection.SE,  // 1 -> 2
            HexDirection.SW,  // 2 -> 3
            HexDirection.W,   // 3 -> 4
            HexDirection.W,   // 4 -> 5
            HexDirection.NW,  // 5 -> 6
            HexDirection.NE,  // 6 -> 7
            HexDirection.E    // 7 -> 0 (closes loop)
        };

        for (int i = 0; i < 8; i++)
        {
            loop[i]?.SetOutgoingRiver(riverDirs[i]);
        }

        GD.Print("    River loop created");

        // Add roads crossing the river at each cell
        // For each cell, determine perpendicular road directions based on river flow
        // River in->out determines which directions are "across" the river

        // Cell 0: River W->E (straight), Config 2 at dir SE needs road through NW
        AddRoadCrossing(loop[0], HexDirection.NW, HexDirection.SE);

        // Cell 1: River W->SE, free edges: NE,E,SW,NW, crossing: NE-SW
        AddRoadCrossing(loop[1], HexDirection.NE, HexDirection.SW);

        // Cell 2: River NW->SW, free edges: NE,E,SE,W, crossing: E-W
        AddRoadCrossing(loop[2], HexDirection.E, HexDirection.W);

        // Cell 3: River NE->W, free edges: E,SE,SW,NW, crossing: SE-NW
        AddRoadCrossing(loop[3], HexDirection.SE, HexDirection.NW);

        // Cell 4: River E->W (straight), free edges: NE,SE,SW,NW, crossing: NW-SE
        AddRoadCrossing(loop[4], HexDirection.NW, HexDirection.SE);

        // Cell 5: River E->NW, free edges: NE,SE,SW,W, crossing: NE-SW
        AddRoadCrossing(loop[5], HexDirection.NE, HexDirection.SW);

        // Cell 6: River SE->NE, free edges: E,SW,W,NW, crossing: E-W
        AddRoadCrossing(loop[6], HexDirection.E, HexDirection.W);

        // Cell 7: River SW->E, free edges: NE,SE,W,NW, crossing: NW-SE
        AddRoadCrossing(loop[7], HexDirection.NW, HexDirection.SE);

        GD.Print("    Roads added - check for 8 bridges");
    }

    /// <summary>
    /// Flattens a rectangular area to consistent elevation and terrain type.
    /// Tutorial 14: Changed from Color to int terrainType.
    /// </summary>
    private static void FlattenArea(Func<int, int, HexCell?> getCell,
        int minX, int minZ, int maxX, int maxZ, int terrainType)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                var cell = getCell(x, z);
                if (cell != null)
                {
                    cell.Elevation = 1;
                    cell.WaterLevel = 0;
                    cell.TerrainTypeIndex = terrainType;
                    cell.PlantLevel = 0;
                    cell.FarmLevel = 0;
                    cell.UrbanLevel = 0;
                    cell.SpecialIndex = 0;
                    // Clear any existing rivers
                    cell.RemoveRiver();
                }
            }
        }
    }

    /// <summary>
    /// Helper to add a road crossing through a cell in two directions.
    /// </summary>
    private static void AddRoadCrossing(HexCell? cell, HexDirection dir1, HexDirection dir2)
    {
        if (cell == null) return;

        var neighbor1 = cell.GetNeighbor(dir1);
        var neighbor2 = cell.GetNeighbor(dir2);

        if (neighbor1 != null)
        {
            neighbor1.AddRoad(dir1.Opposite());
        }

        if (neighbor2 != null)
        {
            neighbor2.AddRoad(dir2.Opposite());
        }
    }

    // ========================================================================
    // SECTION 7: WATERFALL TEST
    // ========================================================================

    /// <summary>
    /// Creates a river with elevation changes to demonstrate waterfall rendering.
    /// Located at offset (20, 2-6) to avoid overlap with other tests.
    /// Rivers flowing between cells of different elevations create waterfalls.
    /// </summary>
    private static void GenerateWaterfallTest(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating waterfall demonstration...");

        // Waterfall path: descending from mountain (elevation 5) to lowland (elevation 1)
        // Each step creates a visible waterfall
        var waterfallPath = new (int x, int z, int elevation)[]
        {
            (20, 2, 5),   // Mountain peak - SOURCE
            (20, 3, 4),   // Waterfall 1: drop 5->4
            (20, 4, 3),   // Waterfall 2: drop 4->3
            (20, 5, 2),   // Waterfall 3: drop 3->2
            (20, 6, 1),   // Waterfall 4: drop 2->1 - TERMINUS
        };

        // Set up the waterfall path cells with elevations
        foreach (var (x, z, elevation) in waterfallPath)
        {
            var cell = getCell(x, z);
            if (cell == null)
            {
                GD.PrintErr($"    Failed to get cell at ({x}, {z})");
                continue;
            }

            cell.Elevation = elevation;
            cell.PlantLevel = 0;
            cell.FarmLevel = 0;
            cell.UrbanLevel = 0;
            cell.SpecialIndex = 0;
            cell.WaterLevel = 0;

            // Tutorial 14: Terrain type based on elevation
            // Higher elevation = stone (3), lower = grass (1)
            cell.TerrainTypeIndex = elevation >= 4 ? 3 : 1;
        }

        // Set up riverbank neighbors at matching elevations
        foreach (var (x, z, elevation) in waterfallPath)
        {
            var cell = getCell(x, z);
            if (cell == null) continue;

            // E and W neighbors form the riverbanks
            var eastNeighbor = cell.GetNeighbor(HexDirection.E);
            var westNeighbor = cell.GetNeighbor(HexDirection.W);

            if (eastNeighbor != null)
            {
                eastNeighbor.Elevation = elevation;
                eastNeighbor.PlantLevel = 0;
                eastNeighbor.FarmLevel = 0;
                eastNeighbor.UrbanLevel = 0;
                eastNeighbor.TerrainTypeIndex = 1; // Grass for riverbank
            }

            if (westNeighbor != null)
            {
                westNeighbor.Elevation = elevation;
                westNeighbor.PlantLevel = 0;
                westNeighbor.FarmLevel = 0;
                westNeighbor.UrbanLevel = 0;
                westNeighbor.TerrainTypeIndex = 1; // Grass for riverbank
            }
        }

        // Create river flowing downhill (SW direction based on hex geometry)
        // Rivers automatically create waterfalls when flowing between different elevations
        for (int i = 0; i < waterfallPath.Length - 1; i++)
        {
            var (x, z, elev) = waterfallPath[i];
            var cell = getCell(x, z);
            if (cell == null) continue;

            // Find direction to next cell in path
            var (nextX, nextZ, nextElev) = waterfallPath[i + 1];
            var nextCell = getCell(nextX, nextZ);
            if (nextCell == null) continue;

            // Find which direction leads to next cell
            for (int d = 0; d < 6; d++)
            {
                var dir = (HexDirection)d;
                if (cell.GetNeighbor(dir) == nextCell)
                {
                    cell.SetOutgoingRiver(dir);
                    GD.Print($"    Waterfall: elevation {elev} -> {nextElev} (drop of {elev - nextElev})");
                    break;
                }
            }
        }

        GD.Print($"    Waterfall test complete at offset (20, 2-6)");
        GD.Print($"    Navigate to this area to see 4 sequential waterfalls");
    }
}
