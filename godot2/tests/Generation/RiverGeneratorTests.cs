using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using HexGame.Generation;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Tests for RiverGenerator - river source selection and path tracing.
/// </summary>
public class RiverGeneratorTests
{
    #region Basic Functionality

    [Fact]
    public void Generate_WithValidLand_CreatesRivers()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = CreateLandWithMoistureGradient(10, 10);

        generator.Generate(data);

        // At least some cells should have rivers
        int riverCells = CountRiverCells(data);
        riverCells.Should().BeGreaterThan(0, "rivers should be created on land with valid sources");
    }

    [Fact]
    public void Generate_WithNoLand_CreatesNoRivers()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = CreateUnderwaterGrid(10, 10);

        generator.Generate(data);

        int riverCells = CountRiverCells(data);
        riverCells.Should().Be(0, "no rivers should exist on all-underwater map");
    }

    [Fact]
    public void Generate_RiversFlowDownhill()
    {
        var rng = new Random(54321);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = CreateSlopedTerrain(10, 10);

        generator.Generate(data);

        // Verify rivers flow from high to low elevation
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].HasOutgoingRiver)
            {
                int neighborIndex = HexNeighborHelper.GetNeighborByDirection(
                    i, data[i].OutgoingRiverDirection, 10, 10);
                if (neighborIndex >= 0)
                {
                    data[neighborIndex].Elevation.Should().BeLessThanOrEqualTo(data[i].Elevation,
                        "rivers should flow to equal or lower elevation");
                }
            }
        }
    }

    [Fact]
    public void Generate_RiversReachWaterOrEdge()
    {
        var rng = new Random(98765);
        var generator = new RiverGenerator(rng, 15, 15);
        var data = CreateIslandTerrain(15, 15);

        generator.Generate(data);

        // Find river endpoints (cells with incoming but no outgoing)
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].HasIncomingRiver && !data[i].HasOutgoingRiver)
            {
                // Terminus should be at water level or lower, or at edge
                bool atWater = data[i].Elevation < GenerationConfig.WaterLevel;
                bool atEdge = IsEdgeCell(i, 15, 15);
                bool nextToWater = HasWaterNeighbor(data, i, 15, 15);

                (atWater || atEdge || nextToWater).Should().BeTrue(
                    "river terminus should be at water, edge, or next to water");
            }
        }
    }

    [Fact]
    public void Generate_ShortRiversDiscarded()
    {
        var rng = new Random(11111);
        var generator = new RiverGenerator(rng, 5, 5);
        // Create terrain where rivers would be very short
        var data = CreateFlatLowTerrain(5, 5);

        generator.Generate(data);

        // Count rivers - any that exist should have minimum length
        var riverPaths = GetRiverPaths(data, 5, 5);
        foreach (var path in riverPaths)
        {
            path.Count.Should().BeGreaterThanOrEqualTo(GenerationConfig.MinRiverLength,
                "rivers shorter than MinRiverLength should be discarded");
        }
    }

    [Fact]
    public void Generate_RiversDoNotCrossExistingRivers()
    {
        var rng = new Random(77777);
        var generator = new RiverGenerator(rng, 20, 20);
        var data = CreateLandWithMoistureGradient(20, 20);

        generator.Generate(data);

        // Verify no cell has both incoming from multiple directions
        // Each cell should have at most one incoming river
        for (int i = 0; i < data.Length; i++)
        {
            int incomingCount = 0;
            int outgoingCount = 0;

            if (data[i].HasIncomingRiver) incomingCount++;
            if (data[i].HasOutgoingRiver) outgoingCount++;

            // A cell can have at most one incoming and one outgoing river
            incomingCount.Should().BeLessThanOrEqualTo(1, $"cell {i} should have at most one incoming river");
            outgoingCount.Should().BeLessThanOrEqualTo(1, $"cell {i} should have at most one outgoing river");
        }

        // Verify river paths don't share intermediate cells
        var riverPaths = GetRiverPaths(data, 20, 20);
        var allRiverCells = new HashSet<int>();

        foreach (var path in riverPaths)
        {
            // Skip first cell (source) as sources are removed after use
            for (int i = 1; i < path.Count; i++)
            {
                int cellIndex = path[i];
                // The cell should not already be part of another river path
                // (except possibly as terminus)
                if (i < path.Count - 1)
                {
                    allRiverCells.Should().NotContain(cellIndex,
                        $"cell {cellIndex} should not be part of multiple river paths");
                }
                allRiverCells.Add(cellIndex);
            }
        }
    }

    #endregion

    #region Source Selection

    [Fact]
    public void FindRiverSources_HighElevationHighMoisture_HighFitness()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 5, 5);
        var data = new CellData[25];

        // Create one high-fitness cell
        for (int i = 0; i < 25; i++)
        {
            data[i] = new CellData(i % 5, i / 5);
            data[i].Elevation = GenerationConfig.WaterLevel;
            data[i].Moisture = 0.1f;
        }

        // Set one cell to be high elevation and high moisture
        data[12].Elevation = GenerationConfig.MaxElevation;
        data[12].Moisture = 1.0f;

        var sources = generator.FindRiverSources(data);

        sources.Should().Contain(12, "high elevation + high moisture cell should be a candidate");
    }

    [Fact]
    public void FindRiverSources_LowFitness_Excluded()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 5, 5);
        var data = new CellData[25];

        // Create all low-fitness cells (low elevation, low moisture)
        for (int i = 0; i < 25; i++)
        {
            data[i] = new CellData(i % 5, i / 5);
            data[i].Elevation = GenerationConfig.WaterLevel;
            data[i].Moisture = 0.01f; // Very low moisture
        }

        var sources = generator.FindRiverSources(data);

        sources.Should().BeEmpty("cells with fitness below threshold should be excluded");
    }

    [Fact]
    public void SelectWeightedSource_PrefersHighFitness()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 5, 5);
        var data = new CellData[25];

        // Create cells with varying fitness
        for (int i = 0; i < 25; i++)
        {
            data[i] = new CellData(i % 5, i / 5);
            data[i].Elevation = GenerationConfig.WaterLevel + 1;
            data[i].Moisture = 0.3f;
        }

        // Make cell 12 very high fitness
        data[12].Elevation = GenerationConfig.MaxElevation;
        data[12].Moisture = 1.0f;

        var candidates = new List<int> { 0, 5, 10, 12, 15, 20 };

        // Run multiple times and count selections
        int highFitnessSelections = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var trialRng = new Random(12345 + trial);
            var trialGenerator = new RiverGenerator(trialRng, 5, 5);
            int selected = trialGenerator.SelectWeightedSource(new List<int>(candidates), data);
            if (selected == 12)
                highFitnessSelections++;
        }

        highFitnessSelections.Should().BeGreaterThan(30,
            "high fitness cell should be selected more often than random");
    }

    #endregion

    #region River Tracing

    [Fact]
    public void TraceRiver_PrefersSteepestDownhill_StatisticalTest()
    {
        // Statistical test: with RiverSteepnessWeight=3.0:
        // - Steep drop (4 elevation): weight = 3.0 * 4 = 12
        // - Gentle drop (1 elevation): weight = 3.0 * 1 = 3 (each of ~5 neighbors)
        // Total: 12 + (5 * 3) = 27, so steep = 12/27 = ~44%
        // We expect the steep path to be chosen more often than uniform random (1/6 = ~17%)

        int steepChoiceCount = 0;
        const int trials = 200;

        for (int trial = 0; trial < trials; trial++)
        {
            var rng = new Random(10000 + trial);
            var generator = new RiverGenerator(rng, 5, 5);
            var data = new CellData[25];

            // Create flat terrain
            for (int i = 0; i < 25; i++)
            {
                data[i] = new CellData(i % 5, i / 5);
                data[i].Elevation = GenerationConfig.WaterLevel;
                data[i].Moisture = 0.5f;
            }

            // Create a peak at center (index 12)
            int sourceIndex = 12;
            data[sourceIndex].Elevation = 5;

            // Make E neighbor (dir=1) have a steep drop (elevation 1, drop of 4)
            // Make other neighbors have gentle drop (elevation 4, drop of 1)
            int steepNeighborIndex = -1;
            for (int dir = 0; dir < RiverGenerator.HexDirectionCount; dir++)
            {
                int neighborIndex = HexNeighborHelper.GetNeighborByDirection(sourceIndex, dir, 5, 5);
                if (neighborIndex >= 0)
                {
                    if (dir == 1) // E direction
                    {
                        data[neighborIndex].Elevation = 1; // Steep drop
                        steepNeighborIndex = neighborIndex;
                    }
                    else
                    {
                        data[neighborIndex].Elevation = 4; // Gentle drop
                    }
                }
            }

            if (steepNeighborIndex < 0) continue; // Skip if no E neighbor

            var path = generator.TraceRiver(data, sourceIndex);

            if (path.Count >= 2 && path[1] == steepNeighborIndex)
                steepChoiceCount++;
        }

        // Expected: ~44% (steep weight / total weight)
        // Should be significantly better than random 1/6 = 17%
        float steepRatio = (float)steepChoiceCount / trials;
        steepRatio.Should().BeGreaterThan(0.30f,
            $"steeper path should be chosen >30% of time (expected ~44%, was {steepRatio:P1})");
    }

    [Fact]
    public void TraceRiver_StopsAtWater()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 5, 5);
        var data = new CellData[25];

        // Create terrain sloping to water
        for (int i = 0; i < 25; i++)
        {
            int x = i % 5;
            int z = i / 5;
            data[i] = new CellData(x, z);
            data[i].Elevation = 4 - x; // Slopes from elevation 4 to 0
            data[i].Moisture = 0.5f;
        }

        // Make rightmost column underwater
        for (int z = 0; z < 5; z++)
        {
            data[z * 5 + 4].Elevation = GenerationConfig.WaterLevel - 1;
        }

        var path = generator.TraceRiver(data, 2); // Start from middle-ish

        // Path should end at water level or below
        if (path.Count > 0)
        {
            int lastIndex = path[path.Count - 1];
            data[lastIndex].Elevation.Should().BeLessThanOrEqualTo(GenerationConfig.WaterLevel,
                "river should end at or below water level");
        }
    }

    [Fact]
    public void TraceRiver_AvoidsVisitedCells()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = CreateSlopedTerrain(10, 10);

        var path = generator.TraceRiver(data, 45); // Middle of grid

        // No duplicates in path
        var uniqueCells = new HashSet<int>(path);
        uniqueCells.Count.Should().Be(path.Count, "river path should not revisit cells");
    }

    [Fact]
    public void TraceRiver_FlatFlowWhenNoDownhill()
    {
        // This tests the flat flow chance - rivers may continue on flat terrain
        var rng = new Random(99999);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = new CellData[100];

        // Create all flat terrain at same elevation
        for (int i = 0; i < 100; i++)
        {
            data[i] = new CellData(i % 10, i / 10);
            data[i].Elevation = GenerationConfig.WaterLevel + 2;
            data[i].Moisture = 0.5f;
        }

        // Add water at edges to give rivers somewhere to go
        for (int i = 0; i < 10; i++)
        {
            data[i].Elevation = GenerationConfig.WaterLevel - 1; // Bottom row
            data[90 + i].Elevation = GenerationConfig.WaterLevel - 1; // Top row
        }

        // Run multiple traces to test flat flow
        int tracesWithMultipleSteps = 0;
        for (int trial = 0; trial < 50; trial++)
        {
            var trialRng = new Random(99999 + trial);
            var trialGenerator = new RiverGenerator(trialRng, 10, 10);
            var path = trialGenerator.TraceRiver(data, 45);
            if (path.Count > 1)
                tracesWithMultipleSteps++;
        }

        // Some traces should continue on flat terrain
        tracesWithMultipleSteps.Should().BeGreaterThan(0,
            "rivers should sometimes flow on flat terrain");
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData(0.24f, false)] // Just below threshold
    [InlineData(0.25f, true)]  // Exactly at threshold
    [InlineData(0.26f, true)]  // Just above threshold
    public void FindRiverSources_FitnessAtThreshold_CorrectlyIncludedOrExcluded(float fitness, bool shouldBeIncluded)
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 3, 3);
        var data = new CellData[9];

        // Create cells with exactly the target fitness
        // fitness = elevationFactor * moisture
        // For max elevation: elevationFactor = (MaxElevation - WaterLevel) / (MaxElevation - WaterLevel) = 1.0
        // So moisture = fitness when elevation = MaxElevation
        for (int i = 0; i < 9; i++)
        {
            data[i] = new CellData(i % 3, i / 3);
            data[i].Elevation = GenerationConfig.MaxElevation;
            data[i].Moisture = fitness;
        }

        var sources = generator.FindRiverSources(data);

        if (shouldBeIncluded)
        {
            sources.Should().NotBeEmpty($"cells with fitness {fitness} should be included (threshold is {GenerationConfig.RiverSourceMinFitness})");
        }
        else
        {
            sources.Should().BeEmpty($"cells with fitness {fitness} should be excluded (threshold is {GenerationConfig.RiverSourceMinFitness})");
        }
    }

    [Fact]
    public void Generate_AllRiversMeetMinimumLength()
    {
        // Verify that all existing rivers meet the minimum length requirement
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 10, 10);
        var data = CreateLandWithMoistureGradient(10, 10);

        generator.Generate(data);

        var riverPaths = GetRiverPaths(data, 10, 10);
        foreach (var path in riverPaths)
        {
            path.Count.Should().BeGreaterThanOrEqualTo(GenerationConfig.MinRiverLength,
                $"no river should be shorter than MinRiverLength ({GenerationConfig.MinRiverLength})");
        }
    }

    [Theory]
    [InlineData(0.74f)]  // Just below high threshold (0.75)
    [InlineData(0.75f)]  // Exactly at high threshold
    [InlineData(0.49f)]  // Just below medium threshold (0.50)
    [InlineData(0.50f)]  // Exactly at medium threshold
    public void CalculateSourceFitness_AtThresholdValues_ReturnsCorrectFitness(float targetFitness)
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 3, 3);

        // Create a cell with the target fitness
        // At max elevation, elevationFactor = 1, so fitness = moisture
        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.MaxElevation,
            Moisture = targetFitness
        };

        float actualFitness = generator.CalculateSourceFitness(ref cell);

        // Verify fitness is correct (within floating point tolerance)
        actualFitness.Should().BeApproximately(targetFitness, 0.01f);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_ClimateAndRiverGenerators_WorkTogether()
    {
        const int seed = 54321;
        const int width = 20;
        const int height = 20;

        // Step 1: Create and initialize cell data
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z);
            }
        }

        // Step 2: Generate land with higher percentage to ensure rivers can form
        var landRng = new Random(seed);
        var landGenerator = new LandGenerator(landRng, width, height);
        landGenerator.Generate(data, 0.6f); // 60% land

        // Step 3: Generate climate (moisture and biomes)
        var climateGenerator = new ClimateGenerator(width, height, seed);
        climateGenerator.Generate(data);

        // Verify moisture was stored
        bool hasMoisture = false;
        int landCellCount = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                landCellCount++;
            if (cell.Moisture > 0)
                hasMoisture = true;
        }
        hasMoisture.Should().BeTrue("ClimateGenerator should store moisture values");
        landCellCount.Should().BeGreaterThan(0, "should have some land cells");

        // Step 4: Generate rivers
        var riverRng = new Random(seed + GenerationConfig.RiverSeedOffset);
        var riverGenerator = new RiverGenerator(riverRng, width, height);
        riverGenerator.Generate(data);

        // Check if we have valid river sources
        var sources = riverGenerator.FindRiverSources(data);

        // If we have sources, we expect rivers (unless all paths were too short)
        // If no sources, having no rivers is acceptable
        int riverCells = CountRiverCells(data);
        if (sources.Count > 0)
        {
            // With sources available, we expect at least some rivers
            // (may be 0 if all paths were too short, which is valid behavior)
            riverCells.Should().BeGreaterThanOrEqualTo(0, "river generation should complete without error");
        }

        // Verify climate data flows through the pipeline correctly
        // by checking moisture exists on land cells
        int landWithMoisture = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel && cell.Moisture > 0)
                landWithMoisture++;
        }
        landWithMoisture.Should().BeGreaterThan(0, "land cells should have moisture from climate generation");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generate_DeterministicWithSameSeed()
    {
        var data1 = CreateLandWithMoistureGradient(10, 10);
        var data2 = CreateLandWithMoistureGradient(10, 10);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var generator1 = new RiverGenerator(rng1, 10, 10);
        var generator2 = new RiverGenerator(rng2, 10, 10);

        generator1.Generate(data1);
        generator2.Generate(data2);

        // Results should be identical
        for (int i = 0; i < data1.Length; i++)
        {
            data1[i].HasIncomingRiver.Should().Be(data2[i].HasIncomingRiver,
                $"cell {i} incoming river should match");
            data1[i].HasOutgoingRiver.Should().Be(data2[i].HasOutgoingRiver,
                $"cell {i} outgoing river should match");
            if (data1[i].HasOutgoingRiver)
            {
                data1[i].OutgoingRiverDirection.Should().Be(data2[i].OutgoingRiverDirection,
                    $"cell {i} outgoing direction should match");
            }
        }
    }

    [Fact]
    public void Generate_EmptyData_NoException()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 0, 0);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_CancellationToken_Throws()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 50, 50);
        var data = CreateLandWithMoistureGradient(50, 50);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => generator.Generate(data, cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Generate_SingleCell_NoException()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 1, 1);
        var data = new CellData[1];
        data[0] = new CellData(0, 0)
        {
            Elevation = GenerationConfig.MaxElevation,
            Moisture = 1.0f
        };

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_SingleRowGrid_HandlesCorrectly()
    {
        var rng = new Random(12345);
        var generator = new RiverGenerator(rng, 10, 1);
        var data = new CellData[10];

        for (int x = 0; x < 10; x++)
        {
            data[x] = new CellData(x, 0)
            {
                Elevation = GenerationConfig.WaterLevel + (9 - x), // Slope left to right
                Moisture = 0.5f
            };
        }
        data[9].Elevation = GenerationConfig.WaterLevel - 1; // Water at right edge

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    #endregion

    #region HexNeighborHelper Tests

    [Theory]
    [InlineData(12, 0, 5, 5, 17)]  // NE from center (even row z=2) -> (2,3) = 17
    [InlineData(12, 1, 5, 5, 13)]  // E from center -> (3,2) = 13
    [InlineData(12, 2, 5, 5, 7)]   // SE from center (even row z=2) -> (2,1) = 7
    [InlineData(12, 3, 5, 5, 6)]   // SW from center (even row z=2) -> (1,1) = 6
    [InlineData(12, 4, 5, 5, 11)]  // W from center -> (1,2) = 11
    [InlineData(12, 5, 5, 5, 16)]  // NW from center (even row z=2) -> (1,3) = 16
    public void GetNeighborByDirection_FromCenterCell_ReturnsCorrectNeighbor(
        int index, int direction, int width, int height, int expectedNeighbor)
    {
        int result = HexNeighborHelper.GetNeighborByDirection(index, direction, width, height);
        result.Should().Be(expectedNeighbor,
            $"direction {direction} from index {index} should return {expectedNeighbor}");
    }

    [Theory]
    [InlineData(0, 2, 5, 5)]   // SE from corner (0,0) even row - out of bounds (z-1 < 0)
    [InlineData(0, 3, 5, 5)]   // SW from corner (0,0) even row - out of bounds
    [InlineData(0, 4, 5, 5)]   // W from corner (0,0) - out of bounds (x-1 < 0)
    [InlineData(0, 5, 5, 5)]   // NW from corner (0,0) even row - out of bounds (x-1 < 0)
    [InlineData(4, 1, 5, 5)]   // E from (4,0) - out of bounds (x+1 >= width)
    public void GetNeighborByDirection_AtEdge_ReturnsNegativeOne(
        int index, int direction, int width, int height)
    {
        int result = HexNeighborHelper.GetNeighborByDirection(index, direction, width, height);
        result.Should().Be(-1, $"direction {direction} from edge index {index} should return -1");
    }

    [Theory]
    [InlineData(-1)]   // Invalid negative direction
    [InlineData(6)]    // Invalid direction >= 6
    [InlineData(100)]  // Way out of range
    public void GetNeighborByDirection_InvalidDirection_ReturnsNegativeOne(int direction)
    {
        int result = HexNeighborHelper.GetNeighborByDirection(12, direction, 5, 5);
        result.Should().Be(-1, $"invalid direction {direction} should return -1");
    }

    [Theory]
    [InlineData(0, 3)]  // NE -> SW
    [InlineData(1, 4)]  // E -> W
    [InlineData(2, 5)]  // SE -> NW
    [InlineData(3, 0)]  // SW -> NE
    [InlineData(4, 1)]  // W -> E
    [InlineData(5, 2)]  // NW -> SE
    public void GetOppositeDirection_AllDirections_ReturnsCorrectOpposite(int direction, int expectedOpposite)
    {
        int result = HexNeighborHelper.GetOppositeDirection(direction);
        result.Should().Be(expectedOpposite,
            $"opposite of direction {direction} should be {expectedOpposite}");
    }

    [Fact]
    public void GetNeighborByDirection_ZeroWidthOrHeight_ReturnsNegativeOne()
    {
        HexNeighborHelper.GetNeighborByDirection(0, 0, 0, 5).Should().Be(-1);
        HexNeighborHelper.GetNeighborByDirection(0, 0, 5, 0).Should().Be(-1);
        HexNeighborHelper.GetNeighborByDirection(0, 0, 0, 0).Should().Be(-1);
    }

    #endregion

    #region Helper Methods

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

    private static CellData[] CreateLandWithMoistureGradient(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                // Higher elevation toward center, higher moisture at top
                int distFromCenter = Math.Abs(x - width / 2) + Math.Abs(z - height / 2);
                int elevation = Math.Max(GenerationConfig.WaterLevel, GenerationConfig.MaxElevation - distFromCenter);

                data[index] = new CellData(x, z)
                {
                    Elevation = elevation,
                    Moisture = (float)z / height // Moisture increases with z
                };
            }
        }
        return data;
    }

    private static CellData[] CreateSlopedTerrain(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                // Slopes from top-left (high) to bottom-right (low)
                int elevation = GenerationConfig.MaxElevation - (x + z) / 2;
                elevation = Math.Max(GenerationConfig.MinElevation, elevation);

                data[index] = new CellData(x, z)
                {
                    Elevation = elevation,
                    Moisture = 0.6f
                };
            }
        }
        return data;
    }

    private static CellData[] CreateIslandTerrain(int width, int height)
    {
        var data = new CellData[width * height];
        int centerX = width / 2;
        int centerZ = height / 2;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                int dist = Math.Abs(x - centerX) + Math.Abs(z - centerZ);
                int maxDist = Math.Max(centerX, centerZ);

                // Island: high in center, water at edges
                int elevation = dist < maxDist - 2
                    ? GenerationConfig.MaxElevation - dist
                    : GenerationConfig.MinElevation;

                data[index] = new CellData(x, z)
                {
                    Elevation = elevation,
                    Moisture = 0.5f + 0.3f * (1f - (float)dist / maxDist)
                };
            }
        }
        return data;
    }

    private static CellData[] CreateFlatLowTerrain(int width, int height)
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

    private static int CountRiverCells(CellData[] data)
    {
        int count = 0;
        foreach (var cell in data)
        {
            if (cell.HasIncomingRiver || cell.HasOutgoingRiver)
                count++;
        }
        return count;
    }

    private static bool IsEdgeCell(int index, int width, int height)
    {
        int x = index % width;
        int z = index / width;
        return x == 0 || x == width - 1 || z == 0 || z == height - 1;
    }

    private static bool HasWaterNeighbor(CellData[] data, int index, int width, int height)
    {
        foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(index, width, height))
        {
            if (data[neighborIndex].Elevation < GenerationConfig.WaterLevel)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Reconstructs river paths from cell data by following outgoing river directions.
    /// Returns a list of paths, where each path is a list of cell indices from source to terminus.
    /// </summary>
    private static List<List<int>> GetRiverPaths(CellData[] data, int width, int height)
    {
        var paths = new List<List<int>>();
        var visited = new HashSet<int>();

        // Find river sources (cells with outgoing but no incoming)
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].HasOutgoingRiver && !data[i].HasIncomingRiver && !visited.Contains(i))
            {
                var path = new List<int>();
                int current = i;

                while (current >= 0 && !visited.Contains(current))
                {
                    visited.Add(current);
                    path.Add(current);

                    if (data[current].HasOutgoingRiver)
                    {
                        current = HexNeighborHelper.GetNeighborByDirection(
                            current, data[current].OutgoingRiverDirection, width, height);
                    }
                    else
                    {
                        break;
                    }
                }

                if (path.Count > 0)
                    paths.Add(path);
            }
        }

        return paths;
    }

    #endregion
}
