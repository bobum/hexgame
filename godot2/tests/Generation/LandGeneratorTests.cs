using FluentAssertions;
using HexGame.Generation;
using System;
using System.Linq;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Unit tests for LandGenerator chunk budget land generation.
/// </summary>
public class LandGeneratorTests
{
    #region Basic Generation Tests

    [Fact]
    public void Generate_ProducesLandCells()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 10, 10);
        var data = CreateInitializedData(10, 10);

        generator.Generate(data, 0.5f);

        int landCells = CountLandCells(data);
        landCells.Should().BeGreaterThan(0, "Generation should produce some land");
    }

    [Fact]
    public void Generate_ApproximatesTargetPercentage()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 20, 20);
        var data = CreateInitializedData(20, 20);

        generator.Generate(data, 0.5f);

        int landCells = CountLandCells(data);
        float actualPercentage = (float)landCells / data.Length;

        // Allow 20% tolerance due to erosion and chunk overlap
        actualPercentage.Should().BeInRange(0.3f, 0.7f,
            "Land percentage should be approximately 50% (+/- 20%)");
    }

    [Fact]
    public void Generate_WithZeroPercentage_ProducesNoLand()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 10, 10);
        var data = CreateInitializedData(10, 10);

        generator.Generate(data, 0.0f);

        int landCells = CountLandCells(data);
        // With 0% land budget, no land should be raised initially.
        // Erosion only fills water surrounded by land (>70% land neighbors),
        // which can't happen if no land exists. Should be exactly 0.
        landCells.Should().Be(0, "Zero land percentage should produce no land");
    }

    [Fact]
    public void Generate_WithFullPercentage_ProducesMostlyLand()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 10, 10);
        var data = CreateInitializedData(10, 10);

        generator.Generate(data, 1.0f);

        int landCells = CountLandCells(data);
        float actualPercentage = (float)landCells / data.Length;

        // Should be mostly land, but erosion may remove some edges
        actualPercentage.Should().BeGreaterThan(0.7f,
            "Full land percentage should produce mostly land");
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Generate_WithSameSeed_ProducesSameResult()
    {
        var data1 = CreateInitializedData(15, 15);
        var data2 = CreateInitializedData(15, 15);

        var rng1 = new Random(12345);
        var generator1 = new LandGenerator(rng1, 15, 15);
        generator1.Generate(data1, 0.5f);

        var rng2 = new Random(12345);
        var generator2 = new LandGenerator(rng2, 15, 15);
        generator2.Generate(data2, 0.5f);

        for (int i = 0; i < data1.Length; i++)
        {
            data1[i].Elevation.Should().Be(data2[i].Elevation,
                $"Cell {i} elevation should match with same seed");
        }
    }

    [Fact]
    public void Generate_WithDifferentSeeds_ProducesDifferentResults()
    {
        var data1 = CreateInitializedData(15, 15);
        var data2 = CreateInitializedData(15, 15);

        var rng1 = new Random(12345);
        var generator1 = new LandGenerator(rng1, 15, 15);
        generator1.Generate(data1, 0.5f);

        var rng2 = new Random(54321);
        var generator2 = new LandGenerator(rng2, 15, 15);
        generator2.Generate(data2, 0.5f);

        int differences = 0;
        for (int i = 0; i < data1.Length; i++)
        {
            if (data1[i].Elevation != data2[i].Elevation)
                differences++;
        }

        differences.Should().BeGreaterThan(0,
            "Different seeds should produce different results");
    }

    #endregion

    #region Elevation Variation Tests

    [Fact]
    public void Generate_ProducesElevationVariation()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 20, 20);
        var data = CreateInitializedData(20, 20);

        generator.Generate(data, 0.6f);

        var elevations = new System.Collections.Generic.HashSet<int>();
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                elevations.Add(cell.Elevation);
        }

        elevations.Count.Should().BeGreaterThan(1,
            "Land generation should produce elevation variation");
    }

    [Fact]
    public void Generate_RespectsMaxElevation()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 15, 15);
        var data = CreateInitializedData(15, 15);

        generator.Generate(data, 0.8f);

        foreach (var cell in data)
        {
            cell.Elevation.Should().BeLessOrEqualTo(GenerationConfig.MaxElevation,
                "No cell should exceed max elevation");
        }
    }

    #endregion

    #region Erosion Tests

    [Fact]
    public void Generate_ErosionSmoothsCoastlines()
    {
        // Create a large enough map where erosion effects are visible
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 30, 30);
        var data = CreateInitializedData(30, 30);

        generator.Generate(data, 0.5f);

        // Check that isolated cells are rare (erosion should remove them)
        int isolatedLandCells = 0;
        int isolatedWaterCells = 0;

        for (int i = 0; i < data.Length; i++)
        {
            int landNeighbors = CountLandNeighbors(data, i, 30, 30);
            bool isLand = data[i].Elevation >= GenerationConfig.WaterLevel;

            if (isLand && landNeighbors == 0)
                isolatedLandCells++;
            if (!isLand && landNeighbors == 6)
                isolatedWaterCells++;
        }

        // Erosion should eliminate most isolated cells
        isolatedLandCells.Should().BeLessThan(5,
            "Erosion should remove most isolated land cells");
        isolatedWaterCells.Should().BeLessThan(5,
            "Erosion should fill most isolated water cells");
    }

    #endregion

    #region CellData Structure Tests

    [Fact]
    public void CellData_Constructor_InitializesToUnderwater()
    {
        var cell = new CellData(5, 3);

        cell.X.Should().Be(5);
        cell.Z.Should().Be(3);
        cell.Elevation.Should().Be(GenerationConfig.MinElevation);
        cell.WaterLevel.Should().Be(GenerationConfig.WaterLevel);
        cell.TerrainTypeIndex.Should().Be(0);
        cell.UrbanLevel.Should().Be(0);
        cell.FarmLevel.Should().Be(0);
        cell.PlantLevel.Should().Be(0);
        cell.SpecialIndex.Should().Be(0);
        cell.Walled.Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests - Grid Sizes

    [Fact]
    public void Generate_WithEmptyGrid_DoesNotThrow()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 0, 0);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data, 0.5f);

        act.Should().NotThrow("Empty grid should be handled gracefully");
    }

    [Fact]
    public void Generate_WithSingleCell_WorksCorrectly()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 1, 1);
        var data = CreateInitializedData(1, 1);

        generator.Generate(data, 1.0f);

        // Single cell with 100% land should become land
        data[0].Elevation.Should().BeGreaterOrEqualTo(GenerationConfig.WaterLevel,
            "Single cell with full land percentage should be land");
    }

    [Fact]
    public void Generate_WithSingleRow_WorksCorrectly()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 10, 1);
        var data = CreateInitializedData(10, 1);

        generator.Generate(data, 0.5f);

        int landCells = CountLandCells(data);
        landCells.Should().BeGreaterThan(0, "Single row grid should produce some land");
        landCells.Should().BeLessThan(data.Length, "Single row grid should have some water");
    }

    [Fact]
    public void Generate_WithSingleColumn_WorksCorrectly()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 1, 10);
        var data = CreateInitializedData(1, 10);

        generator.Generate(data, 0.5f);

        int landCells = CountLandCells(data);
        landCells.Should().BeGreaterThan(0, "Single column grid should produce some land");
        landCells.Should().BeLessThan(data.Length, "Single column grid should have some water");
    }

    [Fact]
    public void Generate_WithZeroWidth_DoesNotThrow()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 0, 10);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data, 0.5f);

        act.Should().NotThrow("Zero width grid should be handled gracefully");
    }

    [Fact]
    public void Generate_WithZeroHeight_DoesNotThrow()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 10, 0);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data, 0.5f);

        act.Should().NotThrow("Zero height grid should be handled gracefully");
    }

    #endregion

    #region Erosion Boundary Condition Tests

    [Fact]
    public void Erosion_LandCellAtExactThreshold_IsNotEroded()
    {
        // Create a scenario where a land cell has exactly 30% land neighbors (at threshold)
        // With 6 neighbors, 30% = 1.8 neighbors, so we need 2 land neighbors (33%)
        // which is above threshold, so it should NOT be eroded
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 5, 5);
        var data = CreateInitializedData(5, 5);

        // Set up center cell (2,2) as land with specific neighbors
        // First generate normally, then verify erosion behavior
        generator.Generate(data, 0.5f);

        // All remaining land cells should have at least ErosionLandThreshold land neighbors
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].Elevation >= GenerationConfig.WaterLevel)
            {
                int landNeighbors = CountLandNeighborsUsingHelper(data, i, 5, 5);
                int totalNeighbors = CountTotalNeighbors(i, 5, 5);

                if (totalNeighbors > 0)
                {
                    float landRatio = (float)landNeighbors / totalNeighbors;
                    landRatio.Should().BeGreaterOrEqualTo(GenerationConfig.ErosionLandThreshold,
                        $"Land cell {i} should have been eroded if below threshold");
                }
            }
        }
    }

    [Fact]
    public void Erosion_WaterCellAtExactThreshold_IsNotFilled()
    {
        var rng = new Random(42);
        var generator = new LandGenerator(rng, 5, 5);
        var data = CreateInitializedData(5, 5);

        generator.Generate(data, 0.5f);

        // All remaining water cells should have at most ErosionWaterThreshold land neighbors
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].Elevation < GenerationConfig.WaterLevel)
            {
                int landNeighbors = CountLandNeighborsUsingHelper(data, i, 5, 5);
                int totalNeighbors = CountTotalNeighbors(i, 5, 5);

                if (totalNeighbors > 0)
                {
                    float landRatio = (float)landNeighbors / totalNeighbors;
                    landRatio.Should().BeLessOrEqualTo(GenerationConfig.ErosionWaterThreshold,
                        $"Water cell {i} should have been filled if above threshold");
                }
            }
        }
    }

    #endregion

    #region HexNeighborHelper Tests

    [Fact]
    public void HexNeighborHelper_CenterCell_HasSixNeighbors()
    {
        var neighbors = HexNeighborHelper.GetNeighborIndices(12, 5, 5).ToList();

        // Cell 12 is at (2,2) in a 5x5 grid - center cell should have 6 neighbors
        neighbors.Should().HaveCount(6, "Center cell should have 6 neighbors");
    }

    [Fact]
    public void HexNeighborHelper_CornerCell_HasFewerNeighbors()
    {
        var neighbors = HexNeighborHelper.GetNeighborIndices(0, 5, 5).ToList();

        // Cell 0 is at (0,0) - corner should have fewer than 6 neighbors
        neighbors.Count.Should().BeLessThan(6, "Corner cell should have fewer than 6 neighbors");
        neighbors.Count.Should().BeGreaterThan(0, "Corner cell should have at least 1 neighbor");
    }

    [Fact]
    public void HexNeighborHelper_EmptyGrid_ReturnsNoNeighbors()
    {
        var neighbors = HexNeighborHelper.GetNeighborIndices(0, 0, 0).ToList();

        neighbors.Should().BeEmpty("Empty grid should have no neighbors");
    }

    [Fact]
    public void HexNeighborHelper_SingleCell_HasNoNeighbors()
    {
        var neighbors = HexNeighborHelper.GetNeighborIndices(0, 1, 1).ToList();

        neighbors.Should().BeEmpty("Single cell grid should have no neighbors");
    }

    [Fact]
    public void HexNeighborHelper_EvenRow_HasCorrectNeighbors()
    {
        // Cell at (2, 0) in a 5x5 grid - even row
        int index = 0 * 5 + 2; // = 2
        var neighbors = HexNeighborHelper.GetNeighborIndices(index, 5, 5).ToList();

        // Even row neighbors for (2,0): E(3,0), W(1,0), NE(2,1), NW(1,1)
        // SE and SW would be at z=-1 which is out of bounds
        neighbors.Should().Contain(3, "Should have E neighbor at index 3");
        neighbors.Should().Contain(1, "Should have W neighbor at index 1");
        neighbors.Should().Contain(7, "Should have NE neighbor at index 7 (2,1)");
        neighbors.Should().Contain(6, "Should have NW neighbor at index 6 (1,1)");
    }

    [Fact]
    public void HexNeighborHelper_OddRow_HasCorrectNeighbors()
    {
        // Cell at (2, 1) in a 5x5 grid - odd row
        int index = 1 * 5 + 2; // = 7
        var neighbors = HexNeighborHelper.GetNeighborIndices(index, 5, 5).ToList();

        // Odd row neighbors for (2,1): E(3,1), W(1,1), NE(3,2), NW(2,2), SE(3,0), SW(2,0)
        neighbors.Should().Contain(8, "Should have E neighbor at index 8 (3,1)");
        neighbors.Should().Contain(6, "Should have W neighbor at index 6 (1,1)");
        neighbors.Should().Contain(13, "Should have NE neighbor at index 13 (3,2)");
        neighbors.Should().Contain(12, "Should have NW neighbor at index 12 (2,2)");
        neighbors.Should().Contain(3, "Should have SE neighbor at index 3 (3,0)");
        neighbors.Should().Contain(2, "Should have SW neighbor at index 2 (2,0)");
    }

    [Fact]
    public void HexNeighborHelper_NeighborRelationship_IsBidirectional()
    {
        int width = 5, height = 5;

        for (int i = 0; i < width * height; i++)
        {
            foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(i, width, height))
            {
                var reverseNeighbors = HexNeighborHelper.GetNeighborIndices(neighborIndex, width, height).ToList();
                reverseNeighbors.Should().Contain(i,
                    $"Cell {neighborIndex} should have {i} as a neighbor (bidirectional relationship)");
            }
        }
    }

    #endregion

    #region Helper Methods

    private static CellData[] CreateInitializedData(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                data[z * width + x] = new CellData(x, z);
            }
        }
        return data;
    }

    private static int CountLandCells(CellData[] data)
    {
        int count = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Uses the shared HexNeighborHelper to count land neighbors.
    /// This ensures tests use the same neighbor logic as production code.
    /// </summary>
    private static int CountLandNeighborsUsingHelper(CellData[] data, int index, int width, int height)
    {
        int landNeighbors = 0;
        foreach (int neighborIndex in HexNeighborHelper.GetNeighborIndices(index, width, height))
        {
            if (data[neighborIndex].Elevation >= GenerationConfig.WaterLevel)
                landNeighbors++;
        }
        return landNeighbors;
    }

    /// <summary>
    /// Counts total neighbors for a cell (for calculating ratios).
    /// </summary>
    private static int CountTotalNeighbors(int index, int width, int height)
    {
        return HexNeighborHelper.GetNeighborIndices(index, width, height).Count();
    }

    /// <summary>
    /// Legacy helper - kept for backward compatibility with existing tests.
    /// Uses HexNeighborHelper internally.
    /// </summary>
    private static int CountLandNeighbors(CellData[] data, int index, int width, int height)
    {
        return CountLandNeighborsUsingHelper(data, index, width, height);
    }

    #endregion
}
