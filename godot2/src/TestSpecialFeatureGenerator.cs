using Godot;
using System;

/// <summary>
/// Generates test special feature placements to verify special feature rendering.
/// Creates distinct test scenarios for castle, ziggurat, and megaflora features.
/// Ported from Catlike Coding Hex Map Tutorial 11.
/// </summary>
public static class TestSpecialFeatureGenerator
{
    /// <summary>
    /// Generates test special feature placements on the grid.
    /// Creates scenarios to test all three special feature types.
    /// </summary>
    public static void GenerateTestPatterns(Func<int, int, HexCell?> getCell)
    {
        GD.Print("Generating test special feature patterns...");

        // Scenario 1: One of each special feature type
        GenerateAllSpecialTypes(getCell);

        // Scenario 2: Special features with roads (roads should be blocked)
        GenerateSpecialWithRoadAttempt(getCell);

        // Scenario 3: Special features with rivers (rivers override special)
        GenerateSpecialWithRiverOverride(getCell);

        // Scenario 4: Special features underwater (should not appear)
        GenerateSpecialUnderwater(getCell);

        GD.Print("Test special feature patterns generated.");
    }

    /// <summary>
    /// Scenario 1: Place one of each special feature type.
    /// SpecialIndex 1 = Castle, 2 = Ziggurat, 3 = Megaflora
    /// </summary>
    private static void GenerateAllSpecialTypes(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating all special feature types...");

        // Castle at (20, 2)
        var castleCell = getCell(20, 2);
        if (castleCell != null)
        {
            castleCell.Elevation = 2;
            castleCell.Color = new Color(0.6f, 0.6f, 0.6f); // Gray for castle
            castleCell.SpecialIndex = 1;
            GD.Print("    Castle placed at (20, 2)");
        }

        // Ziggurat at (22, 2)
        var zigguratCell = getCell(22, 2);
        if (zigguratCell != null)
        {
            zigguratCell.Elevation = 2;
            zigguratCell.Color = new Color(0.8f, 0.7f, 0.5f); // Sandy for ziggurat
            zigguratCell.SpecialIndex = 2;
            GD.Print("    Ziggurat placed at (22, 2)");
        }

        // Megaflora at (24, 2)
        var megafloraCell = getCell(24, 2);
        if (megafloraCell != null)
        {
            megafloraCell.Elevation = 1;
            megafloraCell.Color = new Color(0.2f, 0.6f, 0.2f); // Green for megaflora
            megafloraCell.SpecialIndex = 3;
            GD.Print("    Megaflora placed at (24, 2)");
        }

        GD.Print("  All special feature types created");
    }

    /// <summary>
    /// Scenario 2: Special feature cells should block road placement.
    /// Attempt to add roads to cells with special features - they should fail.
    /// </summary>
    private static void GenerateSpecialWithRoadAttempt(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Testing special feature road blocking...");

        // Place a castle
        var specialCell = getCell(20, 5);
        if (specialCell == null) return;

        specialCell.Elevation = 1;
        specialCell.Color = new Color(0.6f, 0.6f, 0.6f);
        specialCell.SpecialIndex = 1; // Castle

        // Set up neighbors at same elevation
        for (int d = 0; d < 6; d++)
        {
            var neighbor = specialCell.GetNeighbor((HexDirection)d);
            if (neighbor != null)
            {
                neighbor.Elevation = 1;
            }
        }

        // Attempt to add roads (should fail due to IsSpecial check)
        bool roadExistsBefore = specialCell.HasRoads;
        specialCell.AddRoad(HexDirection.E);
        specialCell.AddRoad(HexDirection.W);
        bool roadExistsAfter = specialCell.HasRoads;

        if (roadExistsBefore == roadExistsAfter && !roadExistsAfter)
        {
            GD.Print("    PASS: Roads correctly blocked on special cell (20, 5)");
        }
        else
        {
            GD.PrintErr("    FAIL: Roads were added to special cell!");
        }

        GD.Print("  Special feature road blocking test complete");
    }

    /// <summary>
    /// Scenario 3: Rivers should override special features.
    /// Place a special feature, then add a river - special should be cleared.
    /// </summary>
    private static void GenerateSpecialWithRiverOverride(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Testing river override of special feature...");

        // Place a ziggurat
        var specialCell = getCell(22, 5);
        if (specialCell == null) return;

        specialCell.Elevation = 2;
        specialCell.Color = new Color(0.8f, 0.7f, 0.5f);
        specialCell.SpecialIndex = 2; // Ziggurat

        int specialBefore = specialCell.SpecialIndex;

        // Set up neighbor for river
        var swNeighbor = specialCell.GetNeighbor(HexDirection.SW);
        if (swNeighbor != null)
        {
            swNeighbor.Elevation = 1; // Lower elevation for river flow
        }

        // Add outgoing river (should clear special)
        specialCell.SetOutgoingRiver(HexDirection.SW);

        int specialAfter = specialCell.SpecialIndex;

        if (specialBefore == 2 && specialAfter == 0)
        {
            GD.Print("    PASS: River correctly cleared special feature at (22, 5)");
        }
        else
        {
            GD.PrintErr($"    FAIL: Special was {specialBefore} before, {specialAfter} after river");
        }

        GD.Print("  River override test complete");
    }

    /// <summary>
    /// Scenario 4: Special features should not render underwater.
    /// HexMesh checks IsUnderwater before placing special features.
    /// </summary>
    private static void GenerateSpecialUnderwater(Func<int, int, HexCell?> getCell)
    {
        GD.Print("  Creating underwater special feature test...");

        // Place a megaflora underwater
        var underwaterCell = getCell(24, 5);
        if (underwaterCell != null)
        {
            underwaterCell.Elevation = 0;
            underwaterCell.WaterLevel = 2; // Water above elevation
            underwaterCell.Color = new Color(0.2f, 0.4f, 0.8f); // Water blue
            underwaterCell.SpecialIndex = 3; // Megaflora (won't render due to underwater check)
            GD.Print("    Underwater special at (24, 5) - should NOT render feature");
        }

        // Place a megaflora just above water for comparison
        var aboveWaterCell = getCell(25, 5);
        if (aboveWaterCell != null)
        {
            aboveWaterCell.Elevation = 2;
            aboveWaterCell.WaterLevel = 1; // Water below elevation
            aboveWaterCell.Color = new Color(0.2f, 0.6f, 0.2f);
            aboveWaterCell.SpecialIndex = 3; // Megaflora (will render)
            GD.Print("    Above-water special at (25, 5) - should render feature");
        }

        GD.Print("  Underwater special feature test created");
    }
}
