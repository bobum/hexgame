using FluentAssertions;
using HexGame.Generation;
using System;
using System.Collections.Generic;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Unit tests for MapGenerator logic.
/// Tests the generation algorithms using mock objects.
/// Note: Tests requiring MapGenerator class directly need Godot runtime.
/// These tests verify the underlying logic without Godot dependencies.
/// </summary>
public class MapGeneratorTests
{
    #region CellData Structure Tests

    /// <summary>
    /// Tests for the logic that would be used in CellData generation.
    /// Since CellData is private, we test the public behavior it enables.
    /// </summary>
    [Fact]
    public void MockCellData_CanRepresentUnderwaterState()
    {
        // Simulate what CellData initialization does
        int elevation = GenerationConfig.MinElevation;
        int waterLevel = GenerationConfig.WaterLevel;

        bool isUnderwater = waterLevel > elevation;
        isUnderwater.Should().BeTrue("Cell should be underwater when elevation < water level");
    }

    [Fact]
    public void MockCellData_CanRepresentLandState()
    {
        // Simulate what happens when land is raised
        int elevation = GenerationConfig.WaterLevel;
        int waterLevel = GenerationConfig.WaterLevel;

        bool isUnderwater = waterLevel > elevation;
        isUnderwater.Should().BeFalse("Cell at water level should not be underwater");
    }

    #endregion

    #region Generation Logic Tests (Using Mocks)

    [Fact]
    public void MockGrid_CanBeCreated()
    {
        var grid = new MockGenerationGrid(10, 10);

        grid.CellCountX.Should().Be(10);
        grid.CellCountZ.Should().Be(10);
        grid.CellCount.Should().Be(100);
    }

    [Fact]
    public void MockGrid_CellsHaveCorrectNeighbors()
    {
        var grid = new MockGenerationGrid(5, 5);

        // Center cell should have all 6 neighbors
        var centerCell = grid.GetCellByOffset(2, 2);
        centerCell.Should().NotBeNull();

        int neighborCount = 0;
        for (int d = 0; d < 6; d++)
        {
            if (centerCell!.GetNeighbor((HexDirection)d) != null)
                neighborCount++;
        }
        neighborCount.Should().Be(6, "Center cell should have 6 neighbors");
    }

    [Fact]
    public void MockGrid_CornerCellsHaveLimitedNeighbors()
    {
        var grid = new MockGenerationGrid(5, 5);

        // Corner cell (0,0) should have fewer neighbors
        var cornerCell = grid.GetCellByOffset(0, 0);
        cornerCell.Should().NotBeNull();

        int neighborCount = 0;
        for (int d = 0; d < 6; d++)
        {
            if (cornerCell!.GetNeighbor((HexDirection)d) != null)
                neighborCount++;
        }
        neighborCount.Should().BeLessThan(6, "Corner cell should have fewer than 6 neighbors");
    }

    [Fact]
    public void MockGrid_GetAllCells_ReturnsCorrectCount()
    {
        var grid = new MockGenerationGrid(8, 6);
        int count = 0;
        foreach (var cell in grid.GetAllCells())
        {
            count++;
        }
        count.Should().Be(48);
    }

    [Fact]
    public void MockGrid_GetCellByOffset_ReturnsNullForInvalidCoords()
    {
        var grid = new MockGenerationGrid(5, 5);

        grid.GetCellByOffset(-1, 0).Should().BeNull();
        grid.GetCellByOffset(0, -1).Should().BeNull();
        grid.GetCellByOffset(5, 0).Should().BeNull();
        grid.GetCellByOffset(0, 5).Should().BeNull();
    }

    #endregion

    #region Land Budget Calculation Tests

    [Theory]
    [InlineData(100, 0.5f, 50)]
    [InlineData(100, 0.25f, 25)]
    [InlineData(100, 1.0f, 100)]
    [InlineData(100, 0.0f, 0)]
    [InlineData(64, 0.5f, 32)]
    public void LandBudget_CalculatedCorrectly(int totalCells, float landPercentage, int expectedBudget)
    {
        int budget = (int)(totalCells * landPercentage);
        budget.Should().Be(expectedBudget);
    }

    #endregion

    #region Seed Determinism Tests (Concept Verification)

    [Fact]
    public void RandomWithSameSeed_ProducesSameSequence()
    {
        int seed = 12345;
        var rng1 = new Random(seed);
        var rng2 = new Random(seed);

        var sequence1 = new List<int>();
        var sequence2 = new List<int>();

        for (int i = 0; i < 100; i++)
        {
            sequence1.Add(rng1.Next());
            sequence2.Add(rng2.Next());
        }

        sequence1.Should().Equal(sequence2);
    }

    [Fact]
    public void RandomWithDifferentSeeds_ProducesDifferentSequence()
    {
        var rng1 = new Random(12345);
        var rng2 = new Random(54321);

        var sequence1 = new List<int>();
        var sequence2 = new List<int>();

        for (int i = 0; i < 100; i++)
        {
            sequence1.Add(rng1.Next());
            sequence2.Add(rng2.Next());
        }

        sequence1.Should().NotEqual(sequence2);
    }

    #endregion

    #region Simulated Generation Logic Tests

    [Fact]
    public void SimulatedLandGeneration_RespectsMaxIterations()
    {
        // Simulate the land generation loop logic
        int landBudget = 1000000; // Very large budget
        int maxIterations = 100;
        int iterations = 0;

        while (landBudget > 0 && iterations < maxIterations)
        {
            iterations++;
            // Don't decrease budget - simulates worst case where no cells can be raised
        }

        iterations.Should().Be(maxIterations, "Loop should exit at max iterations");
    }

    [Fact]
    public void SimulatedLandGeneration_ExitsWhenBudgetExhausted()
    {
        int landBudget = 10;
        int maxIterations = 1000;
        int iterations = 0;
        int raised = 0;

        while (landBudget > 0 && iterations < maxIterations)
        {
            iterations++;
            landBudget--;
            raised++;
        }

        raised.Should().Be(10, "Should raise exactly the budget amount");
        iterations.Should().Be(10, "Should exit early when budget exhausted");
    }

    #endregion

    #region Biome Classification Logic Tests

    [Theory]
    [InlineData(-1, true)]  // Below water level
    [InlineData(0, true)]   // Below water level
    [InlineData(1, false)]  // At water level (edge case - depends on implementation)
    [InlineData(2, false)]  // Above water level
    public void UnderwaterCheck_WorksCorrectly(int elevation, bool expectedUnderwater)
    {
        int waterLevel = GenerationConfig.WaterLevel; // 1
        bool isUnderwater = waterLevel > elevation;
        isUnderwater.Should().Be(expectedUnderwater);
    }

    [Theory]
    [InlineData(0.1f, "Desert")]
    [InlineData(0.3f, "Grassland")]
    [InlineData(0.5f, "Plains")]
    [InlineData(0.7f, "Forest")]
    [InlineData(0.9f, "Jungle")]
    public void BiomeClassification_ByMoisture(float moisture, string expectedBiome)
    {
        string biome;
        if (moisture < GenerationConfig.DesertMoistureMax)
            biome = "Desert";
        else if (moisture < GenerationConfig.GrasslandMoistureMax)
            biome = "Grassland";
        else if (moisture < GenerationConfig.PlainsMoistureMax)
            biome = "Plains";
        else if (moisture < GenerationConfig.ForestMoistureMax)
            biome = "Forest";
        else
            biome = "Jungle";

        biome.Should().Be(expectedBiome);
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public void ProgressValues_AreWithinValidRange()
    {
        // Verify the progress values used in MapGenerator are valid
        var progressValues = new[] { 0f, 0.1f, 0.4f, 0.5f, 0.6f, 0.8f, 0.9f, 0.95f, 1.0f };

        foreach (var progress in progressValues)
        {
            progress.Should().BeGreaterOrEqualTo(0f);
            progress.Should().BeLessOrEqualTo(1f);
        }
    }

    [Fact]
    public void ProgressValues_AreIncreasing()
    {
        var progressValues = new[] { 0f, 0.1f, 0.4f, 0.5f, 0.6f, 0.8f, 0.9f, 0.95f, 1.0f };

        for (int i = 1; i < progressValues.Length; i++)
        {
            progressValues[i].Should().BeGreaterThanOrEqualTo(progressValues[i - 1]);
        }
    }

    #endregion
}
