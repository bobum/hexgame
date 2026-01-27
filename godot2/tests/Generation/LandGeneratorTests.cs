using FluentAssertions;
using HexGame.Generation;
using System;
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
        // Erosion might fill in some isolated water, so allow small tolerance
        landCells.Should().BeLessThan(10, "Zero land percentage should produce minimal land");
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

    private static int CountLandNeighbors(CellData[] data, int index, int width, int height)
    {
        int x = index % width;
        int z = index / width;
        bool evenRow = (z & 1) == 0;

        int landNeighbors = 0;

        // Check all 6 hex neighbors
        int[][] offsets = evenRow
            ? new[] {
                new[] { 0, 1 },   // NE
                new[] { 1, 0 },   // E
                new[] { 0, -1 },  // SE
                new[] { -1, -1 }, // SW
                new[] { -1, 0 },  // W
                new[] { -1, 1 }   // NW
            }
            : new[] {
                new[] { 1, 1 },   // NE
                new[] { 1, 0 },   // E
                new[] { 1, -1 },  // SE
                new[] { 0, -1 }, // SW
                new[] { -1, 0 },  // W
                new[] { 0, 1 }   // NW
            };

        foreach (var offset in offsets)
        {
            int nx = x + offset[0];
            int nz = z + offset[1];

            if (nx >= 0 && nx < width && nz >= 0 && nz < height)
            {
                int neighborIndex = nz * width + nx;
                if (data[neighborIndex].Elevation >= GenerationConfig.WaterLevel)
                    landNeighbors++;
            }
        }

        return landNeighbors;
    }

    #endregion
}
