using FluentAssertions;
using HexGame.Generation;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Unit tests for GenerationConfig constants.
/// Validates configuration values are within valid ranges and properly ordered.
/// </summary>
public class GenerationConfigTests
{
    #region Land Generation Tests

    [Fact]
    public void LandPercentage_IsWithinValidRange()
    {
        GenerationConfig.LandPercentage.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.LandPercentage.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void ChunkSizeRange_MinIsLessThanOrEqualToMax()
    {
        GenerationConfig.MinChunkSize.Should().BeLessOrEqualTo(GenerationConfig.MaxChunkSize);
    }

    [Fact]
    public void MinChunkSize_IsPositive()
    {
        GenerationConfig.MinChunkSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChunkExpansionChance_IsWithinValidRange()
    {
        GenerationConfig.ChunkExpansionChance.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.ChunkExpansionChance.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void ElevationRaiseChance_IsWithinValidRange()
    {
        GenerationConfig.ElevationRaiseChance.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.ElevationRaiseChance.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void MaxChunkIterations_IsPositive()
    {
        GenerationConfig.MaxChunkIterations.Should().BeGreaterThan(0);
    }

    #endregion

    #region Erosion Tests

    [Fact]
    public void ErosionLandThreshold_IsWithinValidRange()
    {
        GenerationConfig.ErosionLandThreshold.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.ErosionLandThreshold.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void ErosionWaterThreshold_IsWithinValidRange()
    {
        GenerationConfig.ErosionWaterThreshold.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.ErosionWaterThreshold.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void ErosionThresholds_LandIsLessThanWater()
    {
        // Land threshold should be lower than water threshold
        // This ensures there's a buffer zone where neither erosion nor filling occurs
        GenerationConfig.ErosionLandThreshold.Should()
            .BeLessThan(GenerationConfig.ErosionWaterThreshold);
    }

    #endregion

    #region Elevation Tests

    [Fact]
    public void ElevationRange_MinIsLessThanMax()
    {
        GenerationConfig.MinElevation.Should().BeLessThan(GenerationConfig.MaxElevation);
    }

    [Fact]
    public void WaterLevel_IsBetweenMinAndMaxElevation()
    {
        GenerationConfig.WaterLevel.Should().BeGreaterOrEqualTo(GenerationConfig.MinElevation);
        GenerationConfig.WaterLevel.Should().BeLessOrEqualTo(GenerationConfig.MaxElevation);
    }

    [Fact]
    public void ElevationThresholds_AreOrdered_Hill_LessThan_Mountain()
    {
        GenerationConfig.HillElevation.Should().BeLessThan(GenerationConfig.MountainElevation);
    }

    [Fact]
    public void HillElevation_IsAboveWaterLevel()
    {
        GenerationConfig.HillElevation.Should().BeGreaterThan(GenerationConfig.WaterLevel);
    }

    [Fact]
    public void MountainElevation_IsWithinMaxElevation()
    {
        GenerationConfig.MountainElevation.Should().BeLessOrEqualTo(GenerationConfig.MaxElevation);
    }

    #endregion

    #region Moisture/Climate Tests

    [Fact]
    public void MoistureNoiseScale_IsPositive()
    {
        GenerationConfig.MoistureNoiseScale.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void CoastalMoistureBoost_IsWithinValidRange()
    {
        GenerationConfig.CoastalMoistureBoost.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.CoastalMoistureBoost.Should().BeLessOrEqualTo(1f);
    }

    #endregion

    #region Biome Threshold Tests

    [Fact]
    public void BiomeMoistureThresholds_AreIncreasing()
    {
        GenerationConfig.DesertMoistureMax.Should().BeLessThan(GenerationConfig.GrasslandMoistureMax);
        GenerationConfig.GrasslandMoistureMax.Should().BeLessThan(GenerationConfig.PlainsMoistureMax);
        GenerationConfig.PlainsMoistureMax.Should().BeLessThan(GenerationConfig.ForestMoistureMax);
    }

    [Fact]
    public void BiomeMoistureThresholds_AllWithinZeroToOne()
    {
        GenerationConfig.DesertMoistureMax.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
        GenerationConfig.GrasslandMoistureMax.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
        GenerationConfig.PlainsMoistureMax.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
        GenerationConfig.ForestMoistureMax.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
    }

    [Fact]
    public void DesertMoistureMax_IsGreaterThanZero()
    {
        // Desert should have some moisture threshold, not be zero
        GenerationConfig.DesertMoistureMax.Should().BeGreaterThan(0f);
    }

    #endregion

    #region River Generation Tests

    [Fact]
    public void RiverPercentage_IsWithinValidRange()
    {
        GenerationConfig.RiverPercentage.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.RiverPercentage.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void MinRiverLength_IsPositive()
    {
        GenerationConfig.MinRiverLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RiverSourceMinFitness_IsWithinValidRange()
    {
        GenerationConfig.RiverSourceMinFitness.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.RiverSourceMinFitness.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void RiverSteepnessWeight_IsPositive()
    {
        GenerationConfig.RiverSteepnessWeight.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void RiverFlatFlowChance_IsWithinValidRange()
    {
        GenerationConfig.RiverFlatFlowChance.Should().BeGreaterOrEqualTo(0f);
        GenerationConfig.RiverFlatFlowChance.Should().BeLessOrEqualTo(1f);
    }

    [Fact]
    public void MaxRiverTraceSteps_IsPositive()
    {
        GenerationConfig.MaxRiverTraceSteps.Should().BeGreaterThan(0);
    }

    #endregion

    #region Weighted Selection Tests

    [Fact]
    public void WeightedSelectionThresholds_HighIsGreaterThanMedium()
    {
        GenerationConfig.WeightedSelectionHighThreshold.Should()
            .BeGreaterThan(GenerationConfig.WeightedSelectionMediumThreshold);
    }

    [Fact]
    public void WeightedSelectionThresholds_AreWithinValidRange()
    {
        GenerationConfig.WeightedSelectionHighThreshold.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
        GenerationConfig.WeightedSelectionMediumThreshold.Should().BeGreaterOrEqualTo(0f).And.BeLessOrEqualTo(1f);
    }

    [Theory]
    [InlineData(nameof(GenerationConfig.WeightHighPriority))]
    [InlineData(nameof(GenerationConfig.WeightMediumPriority))]
    [InlineData(nameof(GenerationConfig.WeightLowPriority))]
    public void Weights_ArePositive(string weightName)
    {
        float weight = weightName switch
        {
            nameof(GenerationConfig.WeightHighPriority) => GenerationConfig.WeightHighPriority,
            nameof(GenerationConfig.WeightMediumPriority) => GenerationConfig.WeightMediumPriority,
            nameof(GenerationConfig.WeightLowPriority) => GenerationConfig.WeightLowPriority,
            _ => 0f
        };
        weight.Should().BeGreaterThan(0f, $"{weightName} should be positive");
    }

    [Fact]
    public void Weights_AreOrderedHighToLow()
    {
        GenerationConfig.WeightHighPriority.Should().BeGreaterThan(GenerationConfig.WeightMediumPriority);
        GenerationConfig.WeightMediumPriority.Should().BeGreaterThan(GenerationConfig.WeightLowPriority);
    }

    #endregion

    #region Seed Offset Tests

    [Fact]
    public void SeedOffsets_AreDistinct()
    {
        // Seed offsets should be different to ensure decorrelation
        var offsets = new[]
        {
            GenerationConfig.MoistureSeedOffset,
            GenerationConfig.RiverSeedOffset,
            GenerationConfig.FeatureSeedOffset
        };

        offsets.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
