using Godot;
using System;

/// <summary>
/// Generates test wall placements to verify wall rendering.
/// Creates distinct test scenarios for walls with various terrain features.
/// Ported from Catlike Coding Hex Map Tutorial 10.
/// </summary>
public static class TestWallGenerator
{
    /// <summary>
    /// Generates test wall placements on the grid.
    /// Creates distinct regions to test wall behavior with roads, rivers, cliffs, and water.
    /// Tutorial 11 adds tower placement tests.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test wall patterns...");

        // Scenario 1: Simple walled area (flat terrain)
        GenerateSimpleWalledArea(getCell);

        // Scenario 2: Walled area with elevation changes (slopes)
        GenerateWalledAreaWithElevation(getCell);

        // Scenario 3: Walled area with river passing through (gap test)
        GenerateWalledAreaWithRiver(getCell);

        // Scenario 4: Walled area with road passing through (gap test)
        GenerateWalledAreaWithRoad(getCell);

        // Scenario 5: Walled area adjacent to water
        GenerateWalledAreaNearWater(getCell);

        // Scenario 6: Walled area with cliffs (wedge test)
        GenerateWalledAreaWithCliffs(getCell);

        // Tutorial 11: Scenario 7: Tower placement test (same elevation)
        GenerateWalledAreaForTowers(getCell);

        // Tutorial 11: Scenario 8: No towers on elevation changes
        GenerateWalledAreaNoTowers(getCell);

        GD.Print("Test wall patterns generated.");
    }

    /// <summary>
    /// Scenario 1: Simple 3x3 walled area on flat terrain.
    /// Tests basic wall rendering around a walled region.
    /// </summary>
    private static void GenerateSimpleWalledArea(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating simple walled area (flat terrain)...");
        int count = 0;

        // 3x3 walled area at coordinates (2,2) to (4,4)
        for (int z = 2; z <= 4; z++)
        {
            for (int x = 2; x <= 4; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 1; // Flat elevation
                count++;
            }
        }

        GD.Print($"  Simple walled area created with {count} cells");
    }

    /// <summary>
    /// Scenario 2: Walled area with varying elevations.
    /// Tests walls on slopes and terraced terrain.
    /// </summary>
    private static void GenerateWalledAreaWithElevation(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area with elevation changes...");
        int count = 0;

        // 3x3 walled area with increasing elevation
        for (int z = 2; z <= 4; z++)
        {
            for (int x = 8; x <= 10; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 1 + (z - 2); // Elevation 1, 2, 3
                count++;
            }
        }

        GD.Print($"  Walled area with elevation created with {count} cells");
    }

    /// <summary>
    /// Scenario 3: Walled area with a river passing through.
    /// Tests wall gaps where rivers cross the boundary.
    /// </summary>
    private static void GenerateWalledAreaWithRiver(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area with river...");
        int count = 0;

        // 3x3 walled area with river through center column
        for (int z = 6; z <= 8; z++)
        {
            for (int x = 2; x <= 4; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 1;

                // Add river through center column (x=3)
                if (x == 3)
                {
                    // River flowing from north to south
                    if (z == 6)
                    {
                        // Top cell - river flows in from NE or E
                        cell.SetOutgoingRiver(HexDirection.SW);
                    }
                    else if (z == 7)
                    {
                        // Middle cell - river passes through
                        cell.SetOutgoingRiver(HexDirection.SW);
                    }
                    else if (z == 8)
                    {
                        // Bottom cell - river flows out
                        var southNeighbor = getCell(x, z + 1);
                        if (southNeighbor != null)
                        {
                            southNeighbor.Elevation = 0;
                        }
                        cell.SetOutgoingRiver(HexDirection.SW);
                    }
                }
                count++;
            }
        }

        GD.Print($"  Walled area with river created with {count} cells");
    }

    /// <summary>
    /// Scenario 4: Walled area with a road passing through.
    /// Tests wall gaps where roads cross the boundary.
    /// </summary>
    private static void GenerateWalledAreaWithRoad(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area with road...");
        int count = 0;

        // 3x3 walled area with road through center
        for (int z = 6; z <= 8; z++)
        {
            for (int x = 8; x <= 10; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 1;

                // Add road through center column (x=9)
                if (x == 9)
                {
                    // Road connects to neighbors outside wall
                    if (z == 6)
                    {
                        var northNeighbor = getCell(x, z - 1);
                        if (northNeighbor != null)
                        {
                            northNeighbor.Elevation = 1;
                            cell.AddRoad(HexDirection.NE);
                        }
                    }
                    if (z == 8)
                    {
                        var southNeighbor = getCell(x, z + 1);
                        if (southNeighbor != null)
                        {
                            southNeighbor.Elevation = 1;
                            cell.AddRoad(HexDirection.SW);
                        }
                    }
                    // Connect within walled area
                    if (z < 8)
                    {
                        cell.AddRoad(HexDirection.SW);
                    }
                }
                count++;
            }
        }

        GD.Print($"  Walled area with road created with {count} cells");
    }

    /// <summary>
    /// Scenario 5: Walled area adjacent to water.
    /// Tests wall avoidance of underwater cells.
    /// </summary>
    private static void GenerateWalledAreaNearWater(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area near water...");
        int count = 0;

        // Create water area first
        for (int z = 10; z <= 12; z++)
        {
            var waterCell = getCell(1, z);
            if (waterCell != null)
            {
                waterCell.Elevation = 0;
                waterCell.WaterLevel = 1;
            }
        }

        // 3x3 walled area adjacent to water
        for (int z = 10; z <= 12; z++)
        {
            for (int x = 2; x <= 4; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 1;
                count++;
            }
        }

        GD.Print($"  Walled area near water created with {count} cells");
    }

    /// <summary>
    /// Scenario 6: Walled area with cliffs.
    /// Tests wall wedges connecting to cliff faces.
    /// </summary>
    private static void GenerateWalledAreaWithCliffs(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area with cliffs (wedge test)...");
        int count = 0;

        // Create high cliff area
        for (int z = 10; z <= 12; z++)
        {
            for (int x = 8; x <= 10; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                // Create cliff with 2+ elevation difference
                cell.Elevation = x == 8 ? 1 : 3; // Creates cliff between x=8 and x=9
                count++;
            }
        }

        GD.Print($"  Walled area with cliffs created with {count} cells");
    }

    /// <summary>
    /// Tutorial 11: Scenario 7: Large flat walled area for tower placement.
    /// Towers appear when hash.e < 0.5f and cells have same elevation.
    /// With a larger area, statistically some wall corners will get towers.
    /// </summary>
    private static void GenerateWalledAreaForTowers(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area for tower placement test...");
        int count = 0;

        // 5x5 walled area at flat elevation to maximize tower placement chance
        // Located at (14, 2) to (18, 6)
        for (int z = 2; z <= 6; z++)
        {
            for (int x = 14; x <= 18; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                cell.Elevation = 2; // Flat elevation
                cell.Color = new Color(0.7f, 0.5f, 0.3f); // Brown for tower test area
                count++;
            }
        }

        GD.Print($"  Walled area for towers created with {count} cells at elevation 2");
    }

    /// <summary>
    /// Tutorial 11: Scenario 8: Walled area with varying elevations where towers should NOT appear.
    /// Towers only placed when leftCell.Elevation == rightCell.Elevation.
    /// </summary>
    private static void GenerateWalledAreaNoTowers(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating walled area with varied elevation (no towers expected)...");
        int count = 0;

        // 3x3 walled area with alternating elevations
        // Located at (14, 10) to (16, 12)
        for (int z = 10; z <= 12; z++)
        {
            for (int x = 14; x <= 16; x++)
            {
                var cell = getCell(x, z);
                if (cell == null) continue;

                cell.Walled = true;
                // Alternating pattern ensures adjacent cells have different elevations
                cell.Elevation = ((x + z) % 2 == 0) ? 1 : 3;
                cell.Color = new Color(0.5f, 0.5f, 0.7f); // Blue-gray for no-tower test
                count++;
            }
        }

        GD.Print($"  Walled area with varied elevation created with {count} cells (towers should NOT appear)");
    }
}
