using Godot;
using System;

/// <summary>
/// Generates test roads on EXISTING terrain without modifying colors or elevations.
/// Roads render on top of whatever terrain exists.
/// </summary>
public static class TestRoadGenerator
{
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("=== Generating test roads ===");

        // Add roads to existing cells WITHOUT changing their colors or elevations
        // Just add road connections where elevation allows (diff <= 1)

        int totalRoads = 0;

        // Create all 14 configurations on existing terrain
        // The cells already have elevations from the initial grid setup

        // Config 1: Single road at (10, 10)
        totalRoads += AddRoadsToCell(getCell, 10, 10, new[] { HexDirection.E });

        // Config 2-5: 2-road configs
        totalRoads += AddRoadsToCell(getCell, 12, 10, new[] { HexDirection.NE, HexDirection.E });
        totalRoads += AddRoadsToCell(getCell, 14, 10, new[] { HexDirection.E, HexDirection.W });
        totalRoads += AddRoadsToCell(getCell, 16, 10, new[] { HexDirection.NE, HexDirection.SW });

        // Config 6-8: 3-road configs
        totalRoads += AddRoadsToCell(getCell, 10, 12, new[] { HexDirection.NE, HexDirection.E, HexDirection.SW });
        totalRoads += AddRoadsToCell(getCell, 12, 12, new[] { HexDirection.NE, HexDirection.SE, HexDirection.SW });
        totalRoads += AddRoadsToCell(getCell, 14, 12, new[] { HexDirection.E, HexDirection.SW, HexDirection.W });

        // Config 9-11: 4-road configs
        totalRoads += AddRoadsToCell(getCell, 10, 14, new[] { HexDirection.NE, HexDirection.E, HexDirection.SW, HexDirection.W });
        totalRoads += AddRoadsToCell(getCell, 12, 14, new[] { HexDirection.NE, HexDirection.SE, HexDirection.SW, HexDirection.NW });

        // Config 12-13: 5 and 6-road configs
        totalRoads += AddRoadsToCell(getCell, 10, 16, new[] { HexDirection.NE, HexDirection.E, HexDirection.SE, HexDirection.SW, HexDirection.W });
        totalRoads += AddRoadsToCell(getCell, 12, 16, new[] { HexDirection.NE, HexDirection.E, HexDirection.SE, HexDirection.SW, HexDirection.W, HexDirection.NW });

        GD.Print($"=== Total roads added: {totalRoads} ===");
    }

    private static int AddRoadsToCell(Func<int, int, HexCell?> getCell, int x, int z, HexDirection[] directions)
    {
        var cell = getCell(x, z);
        if (cell == null)
        {
            GD.Print($"  Cell ({x},{z}) is NULL");
            return 0;
        }

        int added = 0;
        foreach (var dir in directions)
        {
            // Check if road can be added (elevation diff <= 1, no river)
            var neighbor = cell.GetNeighbor(dir);
            if (neighbor == null)
            {
                GD.Print($"  Cell ({x},{z}) -> {dir}: no neighbor");
                continue;
            }

            int elevDiff = cell.GetElevationDifference(dir);
            bool hasRiver = cell.HasRiverThroughEdge(dir);

            if (elevDiff > 1)
            {
                GD.Print($"  Cell ({x},{z}) -> {dir}: elev diff {elevDiff} > 1, skipping");
                continue;
            }
            if (hasRiver)
            {
                GD.Print($"  Cell ({x},{z}) -> {dir}: has river, skipping");
                continue;
            }

            cell.AddRoad(dir);

            if (cell.HasRoadThroughEdge(dir))
            {
                GD.Print($"  Cell ({x},{z}) -> {dir}: ROAD ADDED (elev={cell.Elevation}, neighbor elev={neighbor.Elevation})");
                added++;
            }
            else
            {
                GD.Print($"  Cell ({x},{z}) -> {dir}: AddRoad failed");
            }
        }

        GD.Print($"  Cell ({x},{z}) HasRoads={cell.HasRoads}");
        return added;
    }
}
