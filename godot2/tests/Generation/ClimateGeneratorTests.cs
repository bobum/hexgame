using FluentAssertions;
using HexGame.Generation;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Tests for ClimateGenerator - moisture generation and biome assignment.
/// </summary>
public class ClimateGeneratorTests
{
    #region Empty/Edge Cases

    [Fact]
    public void Generate_EmptyArray_DoesNotThrow()
    {
        var generator = new ClimateGenerator(0, 0, 12345);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_ZeroWidth_DoesNotThrow()
    {
        var generator = new ClimateGenerator(0, 10, 12345);
        var data = new CellData[10];

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_ZeroHeight_DoesNotThrow()
    {
        var generator = new ClimateGenerator(10, 0, 12345);
        var data = new CellData[10];

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_SingleCell_AssignsBiome()
    {
        var generator = new ClimateGenerator(1, 1, 12345);
        var data = new CellData[] { new CellData(0, 0) { Elevation = GenerationConfig.WaterLevel } };

        generator.Generate(data);

        // Single cell should have a valid terrain type assigned
        data[0].TerrainTypeIndex.Should().BeInRange(0, 4);
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void Generate_WidthHeightMismatch_ThrowsOnNeighborAccess()
    {
        // Grid says 5x5 but array has different size.
        // When neighbor calculations try to access indices based on declared dimensions,
        // this results in IndexOutOfRangeException. This is expected behavior -
        // callers must ensure array size matches declared dimensions.
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = new CellData[10]; // Not 25 - dimensions don't match
        for (int i = 0; i < 10; i++)
        {
            data[i] = new CellData(i % 5, i / 5) { Elevation = GenerationConfig.WaterLevel };
        }

        // Should throw because neighbor calculations access out-of-bounds indices
        var act = () => generator.Generate(data);
        act.Should().Throw<IndexOutOfRangeException>(
            "mismatched dimensions cause neighbor calculations to access invalid indices");
    }

    [Fact]
    public void Generate_CorrectDimensions_Succeeds()
    {
        // Verify that correctly sized arrays work as expected
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = new CellData[25]; // Correct: 5*5 = 25
        for (int i = 0; i < 25; i++)
        {
            data[i] = new CellData(i % 5, i / 5) { Elevation = GenerationConfig.WaterLevel };
        }

        var act = () => generator.Generate(data);
        act.Should().NotThrow();

        // All cells should have valid biome assignments
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 4);
        }
    }

    [Fact]
    public void Generate_NegativeSeed_WorksCorrectly()
    {
        var generator = new ClimateGenerator(5, 5, -12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.WaterLevel);

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
        // All cells should have valid biomes
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 4);
        }
    }

    #endregion

    #region Biome Assignment Based on Elevation

    [Fact]
    public void Generate_UnderwaterCells_GetSandBiome()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.MinElevation);

        generator.Generate(data);

        // All underwater cells should be sand (terrain type 0)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().Be(0, "underwater cells should be sand");
        }
    }

    [Fact]
    public void Generate_MountainElevation_GetsSnowBiome()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.MountainElevation);

        generator.Generate(data);

        // Mountain elevation cells should always be snow (4)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().Be(4, "mountain elevation cells should be snow");
        }
    }

    [Fact]
    public void Generate_HillElevation_GetsStoneOrSnow()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.HillElevation);

        generator.Generate(data);

        // Hill elevation cells should be stone (3) or snow (4) depending on moisture
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeOneOf(new[] { 3, 4 },
                because: "hill elevation cells should be stone or snow");
        }
    }

    #endregion

    #region Elevation Boundary Tests

    [Fact]
    public void Generate_AtWaterLevel_IsLand()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.WaterLevel);

        generator.Generate(data);

        // Cells at water level are land, should get land biomes (0, 1, or 2)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2,
                "cells at water level should have land biomes");
        }
    }

    [Fact]
    public void Generate_JustBelowWaterLevel_IsUnderwater()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.WaterLevel - 1);

        generator.Generate(data);

        // Cells below water level should be sand (underwater)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().Be(0, "underwater cells should be sand");
        }
    }

    [Fact]
    public void Generate_AtHillElevation_GetsHillBiome()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.HillElevation);

        generator.Generate(data);

        // Cells at hill elevation should be stone (3) or snow (4)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeOneOf(new[] { 3, 4 },
                because: "hill elevation should get stone or snow");
        }
    }

    [Fact]
    public void Generate_JustBelowHillElevation_GetsLandBiome()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.HillElevation - 1);

        generator.Generate(data);

        // Cells below hill elevation should get land biomes (0, 1, or 2)
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2,
                "below hill elevation should have land biomes");
        }
    }

    [Fact]
    public void Generate_AtMountainElevation_GetsSnow()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.MountainElevation);

        generator.Generate(data);

        // Cells at mountain elevation should always be snow
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().Be(4, "mountain elevation should always be snow");
        }
    }

    [Fact]
    public void Generate_JustBelowMountainElevation_GetsHillBiome()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.MountainElevation - 1);

        generator.Generate(data);

        // If still at/above hill elevation, should be stone/snow; otherwise land
        int expectedMin = GenerationConfig.MountainElevation - 1 >= GenerationConfig.HillElevation ? 3 : 0;
        int expectedMax = GenerationConfig.MountainElevation - 1 >= GenerationConfig.HillElevation ? 4 : 2;

        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(expectedMin, expectedMax);
        }
    }

    [Fact]
    public void Generate_AtMaxElevation_GetsSnow()
    {
        var generator = new ClimateGenerator(5, 5, 12345);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.MaxElevation);

        generator.Generate(data);

        // Max elevation should be snow
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().Be(4, "max elevation should be snow");
        }
    }

    #endregion

    #region Coastal Moisture Boost

    [Fact]
    public void Generate_CoastalCells_ReceiveMoistureBoost()
    {
        // Test that coastal cells have equal or higher moisture biomes across multiple seeds.
        // We compare the SUM of biome indices (sand=0, grass=1, mud=2) rather than just counts,
        // since coastal boost increases moisture which pushes biomes toward higher indices.
        int totalCoastalBiomeSum = 0;
        int totalInlandBiomeSum = 0;
        int coastalDesertCount = 0;
        int inlandDesertCount = 0;

        // Run across multiple seeds to get statistically meaningful results
        for (int seed = 1; seed <= 50; seed++)
        {
            var generator = new ClimateGenerator(10, 10, seed);

            // Create grid: left half underwater, right half land
            var data = new CellData[100];
            for (int z = 0; z < 10; z++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int index = z * 10 + x;
                    data[index] = new CellData(x, z);
                    data[index].Elevation = x < 5 ? GenerationConfig.MinElevation : GenerationConfig.WaterLevel;
                }
            }

            generator.Generate(data);

            // Sum biome indices for coastal cells (x=5) vs inland cells (x=9)
            for (int z = 0; z < 10; z++)
            {
                int coastalIndex = z * 10 + 5;
                int inlandIndex = z * 10 + 9;

                totalCoastalBiomeSum += data[coastalIndex].TerrainTypeIndex;
                totalInlandBiomeSum += data[inlandIndex].TerrainTypeIndex;

                if (data[coastalIndex].TerrainTypeIndex == 0) coastalDesertCount++;
                if (data[inlandIndex].TerrainTypeIndex == 0) inlandDesertCount++;
            }
        }

        // Coastal boost of 0.2 should result in:
        // 1. Higher average biome index (more grass/mud vs sand)
        // 2. Fewer desert cells (sand biome)
        // Using >= because the effect should never make coastal worse than inland
        totalCoastalBiomeSum.Should().BeGreaterThanOrEqualTo(totalInlandBiomeSum,
            "coastal cells should have equal or higher average moisture biomes");
        coastalDesertCount.Should().BeLessThanOrEqualTo(inlandDesertCount,
            "coastal cells should have equal or fewer desert biomes due to moisture boost");
    }

    [Fact]
    public void Generate_InlandCells_NoCoastalBoost()
    {
        // Create a grid that's all land - inland cells won't get coastal boost
        var generator = new ClimateGenerator(5, 5, 99999);
        var data = CreateGridWithElevation(5, 5, GenerationConfig.WaterLevel);

        generator.Generate(data);

        // Without coastal boost, biomes depend only on noise
        // Just verify biomes are assigned validly
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2);
        }
    }

    [Fact]
    public void Generate_CoastalBoost_ClampsToMaxMoisture()
    {
        // Test that moisture is properly clamped to 1.0 when coastal boost would exceed it.
        // High base moisture + coastal boost should not produce invalid biome indices.
        // We use multiple seeds to find cases where base moisture is already high.
        for (int seed = 1; seed <= 50; seed++)
        {
            var generator = new ClimateGenerator(5, 5, seed);

            // Create grid with center cell as land surrounded by water (maximum coastal boost)
            var data = new CellData[25];
            for (int z = 0; z < 5; z++)
            {
                for (int x = 0; x < 5; x++)
                {
                    int index = z * 5 + x;
                    data[index] = new CellData(x, z);
                    // Only center cell (2,2) is land, all others are water
                    bool isCenter = x == 2 && z == 2;
                    data[index].Elevation = isCenter ? GenerationConfig.WaterLevel : GenerationConfig.MinElevation;
                }
            }

            generator.Generate(data);

            // Center cell receives maximum coastal boost (surrounded by water on all sides)
            // Even if base moisture was near 1.0, clamping should keep biome valid
            int centerIndex = 2 * 5 + 2;
            data[centerIndex].TerrainTypeIndex.Should().BeInRange(0, 2,
                $"seed {seed}: coastal cell with clamped moisture should have valid land biome");
        }
    }

    #endregion

    #region HexNeighborHelper Edge Cases

    [Fact]
    public void Generate_CornerCells_HandleNeighborsCorrectly()
    {
        // Test that corner cells (fewer neighbors) don't cause issues
        var generator = new ClimateGenerator(3, 3, 12345);

        // Create 3x3 grid with corners as land surrounded by water
        var data = new CellData[9];
        for (int z = 0; z < 3; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                int index = z * 3 + x;
                data[index] = new CellData(x, z);
                // Only corner cells are land
                bool isCorner = (x == 0 || x == 2) && (z == 0 || z == 2);
                data[index].Elevation = isCorner ? GenerationConfig.WaterLevel : GenerationConfig.MinElevation;
            }
        }

        generator.Generate(data);

        // Corner cells should have valid biomes (they are coastal)
        data[0].TerrainTypeIndex.Should().BeInRange(0, 2); // (0,0)
        data[2].TerrainTypeIndex.Should().BeInRange(0, 2); // (2,0)
        data[6].TerrainTypeIndex.Should().BeInRange(0, 2); // (0,2)
        data[8].TerrainTypeIndex.Should().BeInRange(0, 2); // (2,2)
    }

    [Fact]
    public void Generate_EdgeCells_HandleNeighborsCorrectly()
    {
        // Test edge cells that have fewer than 6 neighbors
        var generator = new ClimateGenerator(5, 5, 12345);

        // All land at water level
        var data = CreateGridWithElevation(5, 5, GenerationConfig.WaterLevel);

        generator.Generate(data);

        // Edge cells should have valid biomes
        // Top edge (z=0)
        for (int x = 0; x < 5; x++)
        {
            data[x].TerrainTypeIndex.Should().BeInRange(0, 2);
        }
        // Bottom edge (z=4)
        for (int x = 0; x < 5; x++)
        {
            data[4 * 5 + x].TerrainTypeIndex.Should().BeInRange(0, 2);
        }
    }

    [Fact]
    public void Generate_SingleRowGrid_HandlesCorrectly()
    {
        var generator = new ClimateGenerator(5, 1, 12345);
        var data = new CellData[5];
        for (int x = 0; x < 5; x++)
        {
            data[x] = new CellData(x, 0) { Elevation = GenerationConfig.WaterLevel };
        }

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2);
        }
    }

    [Fact]
    public void Generate_SingleColumnGrid_HandlesCorrectly()
    {
        var generator = new ClimateGenerator(1, 5, 12345);
        var data = new CellData[5];
        for (int z = 0; z < 5; z++)
        {
            data[z] = new CellData(0, z) { Elevation = GenerationConfig.WaterLevel };
        }

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2);
        }
    }

    #endregion

    #region Determinism

    [Fact]
    public void Generate_SameSeed_ProducesSameResults()
    {
        const int seed = 54321;
        const int width = 10;
        const int height = 10;

        // First generation
        var generator1 = new ClimateGenerator(width, height, seed);
        var data1 = CreateGridWithElevation(width, height, GenerationConfig.WaterLevel);
        generator1.Generate(data1);

        // Second generation with same seed
        var generator2 = new ClimateGenerator(width, height, seed);
        var data2 = CreateGridWithElevation(width, height, GenerationConfig.WaterLevel);
        generator2.Generate(data2);

        // Results should be identical
        for (int i = 0; i < data1.Length; i++)
        {
            data1[i].TerrainTypeIndex.Should().Be(data2[i].TerrainTypeIndex,
                $"cell {i} should have same terrain with same seed");
        }
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentResults()
    {
        const int width = 10;
        const int height = 10;

        // First generation
        var generator1 = new ClimateGenerator(width, height, 11111);
        var data1 = CreateGridWithElevation(width, height, GenerationConfig.WaterLevel);
        generator1.Generate(data1);

        // Second generation with different seed
        var generator2 = new ClimateGenerator(width, height, 22222);
        var data2 = CreateGridWithElevation(width, height, GenerationConfig.WaterLevel);
        generator2.Generate(data2);

        // Results should differ significantly (at least 10% of cells)
        int differences = 0;
        for (int i = 0; i < data1.Length; i++)
        {
            if (data1[i].TerrainTypeIndex != data2[i].TerrainTypeIndex)
                differences++;
        }

        differences.Should().BeGreaterThanOrEqualTo(data1.Length / 10,
            "different seeds should produce significantly different terrain");
    }

    #endregion

    #region Biome Distribution

    [Fact]
    public void Generate_LandCells_HaveValidBiomes()
    {
        // Land cells at water level should get land biomes (0-2: sand, grass, mud)
        var generator = new ClimateGenerator(20, 20, 77777);
        var data = CreateGridWithElevation(20, 20, GenerationConfig.WaterLevel);

        generator.Generate(data);

        // All cells should have valid biome indices
        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 2,
                "low elevation land cells should have land biomes (sand/grass/mud)");
        }
    }

    [Fact]
    public void Generate_MixedElevation_HasElevationBasedBiomes()
    {
        var generator = new ClimateGenerator(10, 10, 88888);

        // Create grid with varied elevations
        var data = new CellData[100];
        for (int z = 0; z < 10; z++)
        {
            for (int x = 0; x < 10; x++)
            {
                int index = z * 10 + x;
                data[index] = new CellData(x, z);
                // Elevation increases with x coordinate
                data[index].Elevation = GenerationConfig.MinElevation + x;
            }
        }

        generator.Generate(data);

        // High elevation columns (x >= MountainElevation offset) should have stone/snow
        bool hasHighElevationBiomes = false;
        for (int z = 0; z < 10; z++)
        {
            // x=8 gives elevation = MinElevation + 8 = -2 + 8 = 6 = MountainElevation
            int highElevIndex = z * 10 + 8;
            if (data[highElevIndex].TerrainTypeIndex >= 3) // Stone or Snow
                hasHighElevationBiomes = true;
        }

        hasHighElevationBiomes.Should().BeTrue("high elevation cells should have mountain biomes");
    }

    #endregion

    #region Stone vs Snow Differentiation at Hill Elevation

    [Fact]
    public void Generate_HillElevation_StoneVsSnowBasedOnMoisture()
    {
        // Test with multiple seeds to find cases where both stone and snow appear
        // This is a statistical test since moisture varies by seed
        int stoneCount = 0;
        int snowCount = 0;

        for (int seed = 1; seed <= 100; seed++)
        {
            var generator = new ClimateGenerator(5, 5, seed);
            var data = CreateGridWithElevation(5, 5, GenerationConfig.HillElevation);
            generator.Generate(data);

            foreach (var cell in data)
            {
                if (cell.TerrainTypeIndex == 3) stoneCount++;
                if (cell.TerrainTypeIndex == 4) snowCount++;
            }
        }

        // Both stone and snow should appear across different seeds
        stoneCount.Should().BeGreaterThan(0, "some hill cells should be stone");
        snowCount.Should().BeGreaterThan(0, "some hill cells should be snow (wet hills)");
    }

    #endregion

    #region Valid Biome Values

    [Fact]
    public void Generate_AllCells_HaveValidTerrainTypeIndex()
    {
        var generator = new ClimateGenerator(15, 15, 12345);

        // Mix of underwater and land
        var data = new CellData[225];
        for (int z = 0; z < 15; z++)
        {
            for (int x = 0; x < 15; x++)
            {
                int index = z * 15 + x;
                data[index] = new CellData(x, z);
                // Checkerboard pattern of water and varying elevations
                if ((x + z) % 2 == 0)
                    data[index].Elevation = GenerationConfig.MinElevation;
                else
                    data[index].Elevation = GenerationConfig.WaterLevel + (x % 5);
            }
        }

        generator.Generate(data);

        foreach (var cell in data)
        {
            cell.TerrainTypeIndex.Should().BeInRange(0, 4,
                "terrain type index should be valid (0-4)");
        }
    }

    #endregion

    #region Cancellation Token Support

    [Fact]
    public void Generate_WithCancelledToken_ThrowsOperationCancelledException()
    {
        var generator = new ClimateGenerator(10, 10, 12345);
        var data = CreateGridWithElevation(10, 10, GenerationConfig.WaterLevel);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => generator.Generate(data, cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region Helper Methods

    private static CellData[] CreateGridWithElevation(int width, int height, int elevation)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z) { Elevation = elevation };
            }
        }
        return data;
    }

    #endregion
}
