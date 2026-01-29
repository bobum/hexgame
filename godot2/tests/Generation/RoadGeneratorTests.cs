using System;
using System.Threading;
using FluentAssertions;
using HexGame.Generation;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Tests for RoadGenerator - road generation connecting settlements.
/// </summary>
public class RoadGeneratorTests
{
    #region Basic Functionality

    [Fact]
    public void Generate_WithUrbanCells_CreatesRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLandWithUrbanCells(10, 10);

        generator.Generate(data);

        // At least some cells should have roads
        int cellsWithRoads = 0;
        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                if (cell.HasRoadInDirection(dir))
                {
                    cellsWithRoads++;
                    break;
                }
            }
        }
        cellsWithRoads.Should().BeGreaterThan(0, "roads should connect urban cells");
    }

    [Fact]
    public void Generate_NoUrbanCells_NoRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLandNoUrban(10, 10);

        generator.Generate(data);

        // No roads should be created without settlements
        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                cell.HasRoadInDirection(dir).Should().BeFalse("no roads without settlements");
            }
        }
    }

    [Fact]
    public void Generate_DeterministicWithSameSeed()
    {
        var data1 = CreateFlatLandWithUrbanCells(10, 10);
        var data2 = CreateFlatLandWithUrbanCells(10, 10);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var generator1 = new RoadGenerator(rng1, 10, 10);
        var generator2 = new RoadGenerator(rng2, 10, 10);

        generator1.Generate(data1);
        generator2.Generate(data2);

        // Results should be identical
        for (int i = 0; i < data1.Length; i++)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                data1[i].HasRoadInDirection(dir).Should().Be(data2[i].HasRoadInDirection(dir),
                    $"cell {i} direction {dir} should match");
            }
        }
    }

    [Fact]
    public void Generate_CancellationToken_Throws()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 20, 20);
        var data = CreateFlatLandWithUrbanCells(20, 20);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => generator.Generate(data, cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region Pathfinding

    [Fact]
    public void FindPath_FlatTerrain_FindsPath()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        // Mark start and end as urban
        data[11].UrbanLevel = 1; // Cell at (1,1)
        data[55].UrbanLevel = 1; // Cell at (5,5)

        var path = generator.FindPath(data, 11, 55);

        path.Should().NotBeNull("path should exist on flat terrain");
        path!.Count.Should().BeGreaterThan(0, "path should have cells");
        path[0].Should().Be(11, "path should start at start cell");
        path[^1].Should().Be(55, "path should end at end cell");
    }

    [Fact]
    public void FindPath_ElevationCliff_RoutesAround()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        // Create a cliff in the middle (elevation jump of 3)
        for (int z = 0; z < 10; z++)
        {
            int cliffIndex = z * 10 + 5;
            data[cliffIndex].Elevation = GenerationConfig.WaterLevel + 3;
        }

        // Urban cells on either side of cliff
        data[13].UrbanLevel = 1; // Cell at (3,1)
        data[17].UrbanLevel = 1; // Cell at (7,1)

        var path = generator.FindPath(data, 13, 17);

        // Path may be null if no route around, or should avoid the cliff
        if (path != null)
        {
            foreach (int idx in path)
            {
                int x = idx % 10;
                // Should not go through the cliff at x=5 unless elevation is compatible
                if (x == 5)
                {
                    // Verify elevation constraints are respected
                    int prevIdx = path[System.Array.IndexOf(path.ToArray(), idx) - 1];
                    int elevDiff = Math.Abs(data[idx].Elevation - data[prevIdx].Elevation);
                    elevDiff.Should().BeLessOrEqualTo(1, "path should respect elevation constraints");
                }
            }
        }
    }

    [Fact]
    public void FindPath_SameStartAndEnd_ReturnsSingleCell()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        var path = generator.FindPath(data, 55, 55);

        path.Should().NotBeNull();
        path!.Count.Should().Be(1);
        path[0].Should().Be(55);
    }

    #endregion

    #region Constraints

    [Fact]
    public void CanPlaceRoad_ElevationDifferenceOne_ReturnsTrue()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Cell 0 at elevation 1, cell 1 at elevation 2
        data[0].Elevation = GenerationConfig.WaterLevel;
        data[1].Elevation = GenerationConfig.WaterLevel + 1;

        bool canPlace = generator.CanPlaceRoad(data, 0, 1, 1); // Direction E

        canPlace.Should().BeTrue("elevation difference of 1 should allow road");
    }

    [Fact]
    public void CanPlaceRoad_ElevationDifferenceTwo_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Cell 0 at elevation 1, cell 1 at elevation 3
        data[0].Elevation = GenerationConfig.WaterLevel;
        data[1].Elevation = GenerationConfig.WaterLevel + 2;

        bool canPlace = generator.CanPlaceRoad(data, 0, 1, 1); // Direction E

        canPlace.Should().BeFalse("elevation difference > 1 should block road");
    }

    [Fact]
    public void CanPlaceRoad_RiverThroughEdge_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Add outgoing river from cell 0 in direction E (1)
        data[0].HasOutgoingRiver = true;
        data[0].OutgoingRiverDirection = 1;
        data[1].HasIncomingRiver = true;
        data[1].IncomingRiverDirection = 4; // Opposite of E is W

        bool canPlace = generator.CanPlaceRoad(data, 0, 1, 1);

        canPlace.Should().BeFalse("river through edge should block road (not a straight river)");
    }

    [Fact]
    public void CanPlaceRoad_StraightRiver_BridgeAllowed()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Create a straight river through cell 6 (1,1) flowing N to S
        // Direction: 0=NE, 1=E, 2=SE, 3=SW, 4=W, 5=NW
        data[6].HasIncomingRiver = true;
        data[6].IncomingRiverDirection = 0; // Incoming from NE
        data[6].HasOutgoingRiver = true;
        data[6].OutgoingRiverDirection = 3; // Outgoing to SW

        // Try to place road E-W (perpendicular to NE-SW river)
        // Neighbor to the west: cell 5 (0,1)
        // Neighbor to the east: cell 7 (2,1)

        bool canPlaceEast = generator.CanPlaceRoad(data, 6, 7, 1); // Direction E

        // The road in E direction should be allowed if it's perpendicular to river
        // Since river is NE(0) to SW(3), direction E(1) is perpendicular
        canPlaceEast.Should().BeTrue("perpendicular road to straight river should create bridge");
    }

    [Fact]
    public void CanPlaceRoad_SpecialFeatureMegaflora_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Cell with megaflora (special index 3)
        data[0].SpecialIndex = 3;

        bool canPlace = generator.CanPlaceRoad(data, 0, 1, 1);

        canPlace.Should().BeFalse("megaflora cells should not allow roads");
    }

    [Fact]
    public void CanPlaceRoad_UnderwaterCell_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Cell 0 underwater
        data[0].Elevation = GenerationConfig.MinElevation;

        bool canPlace = generator.CanPlaceRoad(data, 0, 1, 1);

        canPlace.Should().BeFalse("underwater cells should not allow roads");
    }

    #endregion

    #region Settlement Detection

    [Fact]
    public void FindSettlements_UrbanCells_Included()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        // MinUrbanLevelForSettlement = 2, so only level 2+ counts
        data[15].UrbanLevel = 2;
        data[45].UrbanLevel = 3;
        data[75].UrbanLevel = 2;

        var settlements = generator.FindSettlements(data);

        settlements.Should().Contain(15, "urban level 2 should be settlement");
        settlements.Should().Contain(45, "urban level 3 should be settlement");
        settlements.Should().Contain(75, "urban level 2 should be settlement");
    }

    [Fact]
    public void FindSettlements_CastlesAndZiggurats_Included()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        data[20].SpecialIndex = 1; // Castle
        data[50].SpecialIndex = 2; // Ziggurat

        var settlements = generator.FindSettlements(data);

        settlements.Should().Contain(20, "castles should be settlements");
        settlements.Should().Contain(50, "ziggurats should be settlements");
    }

    [Fact]
    public void FindSettlements_Megaflora_Excluded()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        data[30].SpecialIndex = 3; // Megaflora

        var settlements = generator.FindSettlements(data);

        settlements.Should().NotContain(30, "megaflora should not be settlements");
    }

    [Fact]
    public void FindSettlements_UnderwaterUrban_Excluded()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        data[40].UrbanLevel = 2;
        data[40].Elevation = GenerationConfig.MinElevation; // Underwater

        var settlements = generator.FindSettlements(data);

        settlements.Should().NotContain(40, "underwater urban should not be settlement");
    }

    #endregion

    #region Road Application

    [Fact]
    public void ApplyRoad_BidirectionalRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        // Create a simple path: 0 -> 1 -> 2 (going east)
        var path = new System.Collections.Generic.List<int> { 0, 1, 2 };

        generator.ApplyRoad(data, path);

        // Cell 0 should have road E, cell 1 should have roads W and E, cell 2 should have road W
        data[0].HasRoadInDirection(1).Should().BeTrue("cell 0 should have road E");
        data[1].HasRoadInDirection(4).Should().BeTrue("cell 1 should have road W");
        data[1].HasRoadInDirection(1).Should().BeTrue("cell 1 should have road E");
        data[2].HasRoadInDirection(4).Should().BeTrue("cell 2 should have road W");
    }

    [Fact]
    public void ApplyRoad_EmptyPath_NoChange()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        var path = new System.Collections.Generic.List<int>();

        generator.ApplyRoad(data, path);

        // No roads should be added
        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                cell.HasRoadInDirection(dir).Should().BeFalse();
            }
        }
    }

    [Fact]
    public void ApplyRoad_SingleCellPath_NoRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        var path = new System.Collections.Generic.List<int> { 12 };

        generator.ApplyRoad(data, path);

        // No roads should be added for single cell
        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                cell.HasRoadInDirection(dir).Should().BeFalse();
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generate_EmptyData_NoException()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 0, 0);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_SingleUrbanCell_NoRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 5, 5);
        var data = CreateFlatLand(5, 5);

        data[12].UrbanLevel = 2; // Single urban cell

        generator.Generate(data);

        // No roads should be created with only one settlement
        int roadCount = 0;
        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                if (cell.HasRoadInDirection(dir)) roadCount++;
            }
        }
        roadCount.Should().Be(0, "single settlement should have no roads");
    }

    [Fact]
    public void Generate_AllWater_NoRoads()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateUnderwaterGrid(10, 10);

        // Even with urban cells, underwater means no roads
        data[25].UrbanLevel = 2;
        data[75].UrbanLevel = 2;

        generator.Generate(data);

        foreach (var cell in data)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                cell.HasRoadInDirection(dir).Should().BeFalse();
            }
        }
    }

    [Fact]
    public void Generate_SettlementsUnreachable_NoException()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 10, 10);
        var data = CreateFlatLand(10, 10);

        // Create two urban cells separated by water
        data[5].UrbanLevel = 2;
        data[95].UrbanLevel = 2;

        // Create a water barrier
        for (int z = 4; z < 6; z++)
        {
            for (int x = 0; x < 10; x++)
            {
                int idx = z * 10 + x;
                data[idx].Elevation = GenerationConfig.MinElevation;
            }
        }

        var act = () => generator.Generate(data);

        act.Should().NotThrow("unreachable settlements should not cause exception");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_FullPipeline_RoadsConnectSettlements()
    {
        const int seed = 54321;
        const int width = 20;
        const int height = 20;

        // Initialize cell data
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z);
            }
        }

        // Step 1: Land generation
        var landRng = new Random(seed);
        var landGenerator = new LandGenerator(landRng, width, height);
        landGenerator.Generate(data, 0.7f);

        // Step 2: Climate generation
        var climateGenerator = new ClimateGenerator(width, height, seed);
        climateGenerator.Generate(data);

        // Step 3: River generation
        var riverRng = new Random(seed + GenerationConfig.RiverSeedOffset);
        var riverGenerator = new RiverGenerator(riverRng, width, height);
        riverGenerator.Generate(data);

        // Step 4: Feature generation
        var featureRng = new Random(seed + GenerationConfig.FeatureSeedOffset);
        var featureGenerator = new FeatureGenerator(featureRng, width, height);
        featureGenerator.Generate(data);

        // Step 5: Road generation
        var roadRng = new Random(seed + GenerationConfig.RoadSeedOffset);
        var roadGenerator = new RoadGenerator(roadRng, width, height);
        roadGenerator.Generate(data);

        // Count settlements and roads
        int settlements = 0;
        int cellsWithRoads = 0;

        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
            {
                if (cell.UrbanLevel >= GenerationConfig.MinUrbanLevelForSettlement ||
                    cell.SpecialIndex == 1 || cell.SpecialIndex == 2)
                    settlements++;
            }

            bool hasRoad = false;
            for (int dir = 0; dir < 6; dir++)
            {
                if (cell.HasRoadInDirection(dir))
                {
                    hasRoad = true;
                    break;
                }
            }
            if (hasRoad) cellsWithRoads++;
        }

        // If we have multiple settlements, we should have roads
        if (settlements >= 2)
        {
            cellsWithRoads.Should().BeGreaterThan(0,
                $"with {settlements} settlements, roads should be created");
        }

        // Verify road constraints
        foreach (var cell in data)
        {
            // No roads on underwater cells
            if (cell.Elevation < GenerationConfig.WaterLevel)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    cell.HasRoadInDirection(dir).Should().BeFalse(
                        "underwater cells should have no roads");
                }
            }

            // No roads on megaflora
            if (cell.SpecialIndex == 3)
            {
                for (int dir = 0; dir < 6; dir++)
                {
                    cell.HasRoadInDirection(dir).Should().BeFalse(
                        "megaflora cells should have no roads");
                }
            }
        }
    }

    [Fact]
    public void Integration_RoadsBidirectional()
    {
        var rng = new Random(12345);
        var generator = new RoadGenerator(rng, 15, 15);
        var data = CreateFlatLandWithUrbanCells(15, 15);

        generator.Generate(data);

        // Verify roads are bidirectional
        for (int i = 0; i < data.Length; i++)
        {
            for (int dir = 0; dir < 6; dir++)
            {
                if (data[i].HasRoadInDirection(dir))
                {
                    int neighborIdx = HexNeighborHelper.GetNeighborByDirection(i, dir, 15, 15);
                    if (neighborIdx >= 0)
                    {
                        int oppositeDir = HexNeighborHelper.GetOppositeDirection(dir);
                        data[neighborIdx].HasRoadInDirection(oppositeDir).Should().BeTrue(
                            $"road from {i} dir {dir} should have matching road from {neighborIdx} dir {oppositeDir}");
                    }
                }
            }
        }
    }

    #endregion

    #region Helper Methods

    private static CellData[] CreateFlatLand(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z)
                {
                    Elevation = GenerationConfig.WaterLevel,
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    private static CellData[] CreateFlatLandWithUrbanCells(int width, int height)
    {
        var data = CreateFlatLand(width, height);

        // Place several urban cells spread across the map
        int cellCount = width * height;
        data[cellCount / 5].UrbanLevel = 1;
        data[cellCount / 3].UrbanLevel = 2;
        data[cellCount / 2].UrbanLevel = 1;
        data[2 * cellCount / 3].UrbanLevel = 1;
        data[4 * cellCount / 5].UrbanLevel = 2;

        return data;
    }

    private static CellData[] CreateFlatLandNoUrban(int width, int height)
    {
        var data = CreateFlatLand(width, height);
        // No urban cells
        return data;
    }

    private static CellData[] CreateUnderwaterGrid(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z)
                {
                    Elevation = GenerationConfig.MinElevation,
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    #endregion
}
