using FluentAssertions;
using HexGame.Rendering;
using Xunit;

namespace HexMapTutorial.Tests.Rendering;

/// <summary>
/// Tests for RenderingConfig constants to ensure valid configuration values.
/// </summary>
public class RenderingConfigTests
{
    #region Chunking Tests

    [Fact]
    public void ChunkSize_ShouldBePositive()
    {
        RenderingConfig.ChunkSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChunkSize_ShouldBeReasonableSize()
    {
        // Chunks should be at least a few cells wide
        RenderingConfig.ChunkSize.Should().BeGreaterOrEqualTo(8.0f);
        RenderingConfig.ChunkSize.Should().BeLessOrEqualTo(64.0f);
    }

    #endregion

    #region LOD Tests

    [Fact]
    public void LodThresholds_ShouldBeInAscendingOrder()
    {
        RenderingConfig.LodHighToMedium.Should().BeLessThan(RenderingConfig.LodMediumToLow);
        RenderingConfig.LodMediumToLow.Should().BeLessThan(RenderingConfig.MaxRenderDistance);
    }

    [Fact]
    public void LodHighToMedium_ShouldBePositive()
    {
        RenderingConfig.LodHighToMedium.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LodMediumToLow_ShouldBePositive()
    {
        RenderingConfig.LodMediumToLow.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MaxRenderDistance_ShouldBePositive()
    {
        RenderingConfig.MaxRenderDistance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LodThresholds_ShouldHaveReasonableGaps()
    {
        // There should be meaningful distance between thresholds
        float gap1 = RenderingConfig.LodMediumToLow - RenderingConfig.LodHighToMedium;
        float gap2 = RenderingConfig.MaxRenderDistance - RenderingConfig.LodMediumToLow;

        gap1.Should().BeGreaterOrEqualTo(10.0f, "LOD transitions need sufficient distance to prevent flickering");
        gap2.Should().BeGreaterOrEqualTo(10.0f, "Culling transition needs sufficient distance");
    }

    #endregion

    #region Fog Tests

    [Fact]
    public void DefaultFogNear_ShouldBePositive()
    {
        RenderingConfig.DefaultFogNear.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DefaultFogFar_ShouldBeGreaterThanFogNear()
    {
        RenderingConfig.DefaultFogFar.Should().BeGreaterThan(RenderingConfig.DefaultFogNear);
    }

    [Fact]
    public void DefaultFogDensity_ShouldBeInValidRange()
    {
        RenderingConfig.DefaultFogDensity.Should().BeGreaterOrEqualTo(0);
        RenderingConfig.DefaultFogDensity.Should().BeLessOrEqualTo(1.0f);
    }

    [Fact]
    public void FogNearRange_ShouldBeValid()
    {
        RenderingConfig.FogNearMin.Should().BeLessThan(RenderingConfig.FogNearMax);
        RenderingConfig.FogNearMin.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FogFarRange_ShouldBeValid()
    {
        RenderingConfig.FogFarMin.Should().BeLessThan(RenderingConfig.FogFarMax);
        RenderingConfig.FogFarMin.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DefaultFogNear_ShouldBeWithinRange()
    {
        RenderingConfig.DefaultFogNear.Should().BeGreaterOrEqualTo(RenderingConfig.FogNearMin);
        RenderingConfig.DefaultFogNear.Should().BeLessOrEqualTo(RenderingConfig.FogNearMax);
    }

    [Fact]
    public void DefaultFogFar_ShouldBeWithinRange()
    {
        RenderingConfig.DefaultFogFar.Should().BeGreaterOrEqualTo(RenderingConfig.FogFarMin);
        RenderingConfig.DefaultFogFar.Should().BeLessOrEqualTo(RenderingConfig.FogFarMax);
    }

    [Fact]
    public void FogRanges_ShouldNotOverlap()
    {
        // Fog near max should be less than fog far min to avoid invalid configurations
        RenderingConfig.FogNearMax.Should().BeLessOrEqualTo(RenderingConfig.FogFarMax);
    }

    #endregion

    #region Performance Monitoring Tests

    [Fact]
    public void FrameHistorySize_ShouldBePositive()
    {
        RenderingConfig.FrameHistorySize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FrameHistorySize_ShouldBeReasonable()
    {
        // Too small loses accuracy, too large uses memory
        RenderingConfig.FrameHistorySize.Should().BeGreaterOrEqualTo(30);
        RenderingConfig.FrameHistorySize.Should().BeLessOrEqualTo(1000);
    }

    [Fact]
    public void TargetFrameTimeMs_ShouldCorrespondTo60Fps()
    {
        // 60 FPS = 16.67ms per frame
        RenderingConfig.TargetFrameTimeMs.Should().BeApproximately(16.67f, 0.1f);
    }

    [Fact]
    public void WarningFrameTimeMs_ShouldCorrespondTo30Fps()
    {
        // 30 FPS = 33.33ms per frame
        RenderingConfig.WarningFrameTimeMs.Should().BeApproximately(33.33f, 0.1f);
    }

    [Fact]
    public void WarningFrameTime_ShouldBeGreaterThanTarget()
    {
        RenderingConfig.WarningFrameTimeMs.Should().BeGreaterThan(RenderingConfig.TargetFrameTimeMs);
    }

    [Fact]
    public void PerformanceGraphWidth_ShouldBePositive()
    {
        RenderingConfig.PerformanceGraphWidth.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceGraphHeight_ShouldBePositive()
    {
        RenderingConfig.PerformanceGraphHeight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceGraphDimensions_ShouldBeReasonable()
    {
        // Graph should fit in a corner of the screen
        RenderingConfig.PerformanceGraphWidth.Should().BeLessOrEqualTo(400);
        RenderingConfig.PerformanceGraphHeight.Should().BeLessOrEqualTo(200);
    }

    #endregion

    #region Control Panel Tests

    [Fact]
    public void ControlPanelWidth_ShouldBePositive()
    {
        RenderingConfig.ControlPanelWidth.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PanelMargin_ShouldBeNonNegative()
    {
        RenderingConfig.PanelMargin.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ControlPanelWidth_ShouldBeReasonable()
    {
        // Panel should fit on screen but be wide enough for controls
        RenderingConfig.ControlPanelWidth.Should().BeGreaterOrEqualTo(150);
        RenderingConfig.ControlPanelWidth.Should().BeLessOrEqualTo(500);
    }

    #endregion

    #region Map Generation Defaults Tests

    [Fact]
    public void DefaultMapWidth_ShouldBeWithinRange()
    {
        RenderingConfig.DefaultMapWidth.Should().BeGreaterOrEqualTo(RenderingConfig.MinMapWidth);
        RenderingConfig.DefaultMapWidth.Should().BeLessOrEqualTo(RenderingConfig.MaxMapWidth);
    }

    [Fact]
    public void DefaultMapHeight_ShouldBeWithinRange()
    {
        RenderingConfig.DefaultMapHeight.Should().BeGreaterOrEqualTo(RenderingConfig.MinMapHeight);
        RenderingConfig.DefaultMapHeight.Should().BeLessOrEqualTo(RenderingConfig.MaxMapHeight);
    }

    [Fact]
    public void MapWidthRange_ShouldBeValid()
    {
        RenderingConfig.MinMapWidth.Should().BeLessThan(RenderingConfig.MaxMapWidth);
        RenderingConfig.MinMapWidth.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MapHeightRange_ShouldBeValid()
    {
        RenderingConfig.MinMapHeight.Should().BeLessThan(RenderingConfig.MaxMapHeight);
        RenderingConfig.MinMapHeight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void MinMapDimensions_ShouldBePlayable()
    {
        // Map should be at least 10x10 for a meaningful game
        RenderingConfig.MinMapWidth.Should().BeGreaterOrEqualTo(10);
        RenderingConfig.MinMapHeight.Should().BeGreaterOrEqualTo(10);
    }

    #endregion
}
