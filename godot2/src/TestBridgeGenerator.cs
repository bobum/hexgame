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

        // Use cell (10, 3) area which is clear from other tests
        int riverX = 10;
        int riverZ = 3;

        // Get the main river cell and set it up
        var riverCell = getCell(riverX, riverZ);
        if (riverCell == null)
        {
            GD.PrintErr($"  River cell ({riverX}, {riverZ}) is NULL!");
            return;
        }

        // Set up all cells at same elevation first
        riverCell.Elevation = 1;
        riverCell.Color = new Color(0.3f, 0.5f, 0.7f);
        // Clear all features from bridge cell to avoid visual clutter
        riverCell.PlantLevel = 0;
        riverCell.FarmLevel = 0;
        riverCell.UrbanLevel = 0;

        // Set up the upstream cell (river source)
        var upstreamCell = riverCell.GetNeighbor(HexDirection.NE);
        if (upstreamCell != null)
        {
            upstreamCell.Elevation = 1;
            upstreamCell.Color = new Color(0.3f, 0.5f, 0.7f);
        }

        // Set up downstream cell
        var downstreamCell = riverCell.GetNeighbor(HexDirection.SW);
        if (downstreamCell != null)
        {
            downstreamCell.Elevation = 1;
            downstreamCell.Color = new Color(0.3f, 0.5f, 0.7f);
        }

        // Create the river: NE -> riverCell -> SW (straight through)
        if (upstreamCell != null)
        {
            upstreamCell.SetOutgoingRiver(HexDirection.SW);
            GD.Print($"    River: upstream ({upstreamCell.Coordinates}) -> SW");
        }
        riverCell.SetOutgoingRiver(HexDirection.SW);
        GD.Print($"    River: riverCell ({riverCell.Coordinates}) -> SW");
        GD.Print($"    RiverCell HasIncoming={riverCell.HasIncomingRiver} from {riverCell.IncomingRiver}, HasOutgoing={riverCell.HasOutgoingRiver} to {riverCell.OutgoingRiver}");

        // Now add roads perpendicular to the river (E-W direction)
        var eastCell = riverCell.GetNeighbor(HexDirection.E);
        var westCell = riverCell.GetNeighbor(HexDirection.W);

        if (eastCell != null)
        {
            eastCell.Elevation = 1;
            eastCell.Color = new Color(0.5f, 0.4f, 0.3f); // Brown for road
            // Clear features
            eastCell.PlantLevel = 0;
            eastCell.FarmLevel = 0;
            eastCell.UrbanLevel = 0;
            // Add road from east cell into the river cell
            eastCell.AddRoad(HexDirection.W);
            GD.Print($"    Road: eastCell ({eastCell.Coordinates}) -> W, HasRoad={eastCell.HasRoadThroughEdge(HexDirection.W)}");
        }

        if (westCell != null)
        {
            westCell.Elevation = 1;
            westCell.Color = new Color(0.5f, 0.4f, 0.3f);
            // Clear features
            westCell.PlantLevel = 0;
            westCell.FarmLevel = 0;
            westCell.UrbanLevel = 0;
            // Add road from west cell into the river cell
            westCell.AddRoad(HexDirection.E);
            GD.Print($"    Road: westCell ({westCell.Coordinates}) -> E, HasRoad={westCell.HasRoadThroughEdge(HexDirection.E)}");
        }

        // Check if riverCell has roads on E and W edges
        GD.Print($"    RiverCell roads: E={riverCell.HasRoadThroughEdge(HexDirection.E)}, W={riverCell.HasRoadThroughEdge(HexDirection.W)}");
        GD.Print($"    RiverCell HasRoads={riverCell.HasRoads}");

        GD.Print($"  Straight river with bridge created at ({riverX}, {riverZ})");
    }

    /// <summary>
    /// Scenario 2: Curved river with road crossing at the curve.
    /// Uses cell (7,7) on showcase river which has a TIGHT curve (Config 3).
    /// At (7,7): IncomingRiver=W, OutgoingRiver=NW (W == NW.Previous(), so Config 3)
    /// </summary>
    private static void GenerateCurvedRiverWithBridge(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating curved river bridge on showcase river (tight curve)...");

        // Use existing showcase river cell at offset (7,7)
        // This cell has river: incoming W, outgoing NW - a TIGHT left curve (Config 3)
        int bridgeX = 7;
        int bridgeZ = 7;

        var bridgeCell = getCell(bridgeX, bridgeZ);
        if (bridgeCell == null)
        {
            GD.PrintErr($"  Bridge cell ({bridgeX}, {bridgeZ}) is NULL!");
            return;
        }

        GD.Print($"    Bridge cell ({bridgeX}, {bridgeZ}) = {bridgeCell.Coordinates}");
        GD.Print($"    HasIncoming={bridgeCell.HasIncomingRiver} from {bridgeCell.IncomingRiver}");
        GD.Print($"    HasOutgoing={bridgeCell.HasOutgoingRiver} to {bridgeCell.OutgoingRiver}");

        // Check if this is Config 3: IncomingRiver == OutgoingRiver.Previous()
        bool isConfig3 = bridgeCell.IncomingRiver == bridgeCell.OutgoingRiver.Previous();
        GD.Print($"    IsConfig3 (tight left)? {bridgeCell.IncomingRiver}=={bridgeCell.OutgoingRiver}.Previous()={bridgeCell.OutgoingRiver.Previous()} -> {isConfig3}");

        // For Config 3, bridge is placed when direction == IncomingRiver.Next()
        // IncomingRiver = W, so bridge direction = W.Next() = NW
        // Road must be at direction.Opposite() = NW.Opposite() = SE
        HexDirection bridgeDir = bridgeCell.IncomingRiver.Next();
        HexDirection roadDir = bridgeDir.Opposite();
        GD.Print($"    Bridge needs: direction={bridgeDir}, road at {roadDir}");

        // Add road through SE edge (from SE neighbor into bridge cell)
        var seNeighbor = bridgeCell.GetNeighbor(HexDirection.SE);
        if (seNeighbor != null)
        {
            seNeighbor.Elevation = bridgeCell.Elevation;
            seNeighbor.AddRoad(HexDirection.NW);
            GD.Print($"    Road: SE neighbor -> NW into bridge cell");
        }

        // Also add road on opposite side to complete the crossing
        var nwNeighbor = bridgeCell.GetNeighbor(HexDirection.NW);
        if (nwNeighbor != null)
        {
            nwNeighbor.Elevation = bridgeCell.Elevation;
            nwNeighbor.AddRoad(HexDirection.SE);
            GD.Print($"    Road: NW neighbor -> SE into bridge cell");
        }

        GD.Print($"    BridgeCell HasRoads={bridgeCell.HasRoads}");
        for (int d = 0; d < 6; d++)
        {
            if (bridgeCell.HasRoadThroughEdge((HexDirection)d))
            {
                GD.Print($"      Road through {(HexDirection)d}");
            }
        }
        GD.Print($"  Curved river bridge test created at ({bridgeX}, {bridgeZ})");
    }

    /// <summary>
    /// Scenario 3: River with multiple bridge crossings.
    /// Tests multiple bridges on the same river.
    /// </summary>
    private static void GenerateMultipleBridges(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating river with multiple bridges...");

        // Create a river at (16, 3) going down with road crossings
        int startX = 16;
        int startZ = 2;

        // Set up cells along river path - use valid coordinates
        var riverCells = new HexCell?[4];
        for (int i = 0; i < 4; i++)
        {
            riverCells[i] = getCell(startX, startZ + i);
            if (riverCells[i] != null)
            {
                riverCells[i].Elevation = 1;
                riverCells[i].Color = new Color(0.4f, 0.6f, 0.5f);
            }
        }

        // Create river flowing NE->SW through each cell
        for (int i = 0; i < riverCells.Length - 1; i++)
        {
            if (riverCells[i] != null)
            {
                riverCells[i].SetOutgoingRiver(HexDirection.SW);
            }
        }

        GD.Print($"    River created from ({startX}, {startZ}) to ({startX}, {startZ + 3})");

        // Add road crossings at cells 1 and 2 (middle cells)
        for (int i = 1; i <= 2; i++)
        {
            var cell = riverCells[i];
            if (cell == null) continue;

            var eastNeighbor = cell.GetNeighbor(HexDirection.E);
            var westNeighbor = cell.GetNeighbor(HexDirection.W);

            if (eastNeighbor != null)
            {
                eastNeighbor.Elevation = 1;
                eastNeighbor.Color = new Color(0.5f, 0.4f, 0.3f);
                eastNeighbor.AddRoad(HexDirection.W);
                GD.Print($"    Road crossing {i}: eastNeighbor -> W");
            }
            if (westNeighbor != null)
            {
                westNeighbor.Elevation = 1;
                westNeighbor.Color = new Color(0.5f, 0.4f, 0.3f);
                westNeighbor.AddRoad(HexDirection.E);
            }

            GD.Print($"    Cell {i} roads: E={cell.HasRoadThroughEdge(HexDirection.E)}, W={cell.HasRoadThroughEdge(HexDirection.W)}");
        }

        GD.Print($"  Multiple bridges scenario created");
    }
}
