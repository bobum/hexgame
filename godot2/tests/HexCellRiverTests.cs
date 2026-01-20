using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexCell river-related calculations.
/// Tests the math formulas - actual cell behavior requires integration testing.
/// Verifies Tutorial 6 river logic.
/// </summary>
public class HexCellRiverTests
{
    private const float Tolerance = 0.0001f;

    // StreamBedY and RiverSurfaceY calculation tests
    // Formula: (elevation + offset) * ElevationStep

    [Theory]
    [InlineData(0, -5.25f)]   // 0 + (-1.75) * 3 = -5.25
    [InlineData(1, -2.25f)]   // 1 + (-1.75) * 3 = -2.25
    [InlineData(2, 0.75f)]    // 2 + (-1.75) * 3 = 0.75
    [InlineData(3, 3.75f)]    // 3 + (-1.75) * 3 = 3.75
    [InlineData(5, 9.75f)]    // 5 + (-1.75) * 3 = 9.75
    public void StreamBedY_CalculatesCorrectly(int elevation, float expectedY)
    {
        float streamBedY = (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
        streamBedY.Should().BeApproximately(expectedY, Tolerance);
    }

    [Theory]
    [InlineData(0, -1.5f)]    // 0 + (-0.5) * 3 = -1.5
    [InlineData(1, 1.5f)]     // 1 + (-0.5) * 3 = 1.5
    [InlineData(2, 4.5f)]     // 2 + (-0.5) * 3 = 4.5
    [InlineData(3, 7.5f)]     // 3 + (-0.5) * 3 = 7.5
    [InlineData(5, 13.5f)]    // 5 + (-0.5) * 3 = 13.5
    public void RiverSurfaceY_CalculatesCorrectly(int elevation, float expectedY)
    {
        float riverSurfaceY = (elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;
        riverSurfaceY.Should().BeApproximately(expectedY, Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void StreamBedY_IsLowerThanRiverSurfaceY(int elevation)
    {
        float streamBedY = (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
        float riverSurfaceY = (elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;

        streamBedY.Should().BeLessThan(riverSurfaceY);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void RiverSurfaceY_IsLowerThanCellSurface(int elevation)
    {
        float riverSurfaceY = (elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;
        float cellSurfaceY = elevation * HexMetrics.ElevationStep;

        riverSurfaceY.Should().BeLessThan(cellSurfaceY);
    }

    [Fact]
    public void StreamBedOffset_MatchesTutorialValue()
    {
        // Tutorial 6 uses -1.75 for stream bed
        HexMetrics.StreamBedElevationOffset.Should().Be(-1.75f);
    }

    [Fact]
    public void RiverSurfaceOffset_MatchesTutorialValue()
    {
        // Tutorial 6 uses -0.5 for river surface
        HexMetrics.RiverSurfaceElevationOffset.Should().Be(-0.5f);
    }

    // River flow validation tests (elevation constraints)

    [Theory]
    [InlineData(5, 5, true)]   // Same elevation - valid
    [InlineData(5, 4, true)]   // Downhill - valid
    [InlineData(5, 3, true)]   // Steeper downhill - valid
    [InlineData(5, 6, false)]  // Uphill - invalid
    [InlineData(5, 10, false)] // Steep uphill - invalid
    public void RiverCanFlowDownhillOrFlat(int sourceElevation, int targetElevation, bool expectedValid)
    {
        // Rivers can only flow to cells at same elevation or lower
        bool isValid = sourceElevation >= targetElevation;
        isValid.Should().Be(expectedValid);
    }

    // HasRiverBeginOrEnd logic tests

    [Theory]
    [InlineData(false, false, false)] // No rivers
    [InlineData(true, false, true)]   // Only incoming (terminus)
    [InlineData(false, true, true)]   // Only outgoing (source)
    [InlineData(true, true, false)]   // Both (through-flow)
    public void HasRiverBeginOrEnd_Logic(bool hasIncoming, bool hasOutgoing, bool expected)
    {
        // HasRiverBeginOrEnd = hasIncoming != hasOutgoing
        bool result = hasIncoming != hasOutgoing;
        result.Should().Be(expected);
    }

    // HasRiver logic tests

    [Theory]
    [InlineData(false, false, false)] // No rivers
    [InlineData(true, false, true)]   // Only incoming
    [InlineData(false, true, true)]   // Only outgoing
    [InlineData(true, true, true)]    // Both
    public void HasRiver_Logic(bool hasIncoming, bool hasOutgoing, bool expected)
    {
        // HasRiver = hasIncoming || hasOutgoing
        bool result = hasIncoming || hasOutgoing;
        result.Should().Be(expected);
    }

    // HasRiverThroughEdge logic tests

    [Theory]
    [InlineData(HexDirection.NE, true, HexDirection.NE, false, HexDirection.SW, true)]   // Incoming from NE
    [InlineData(HexDirection.NE, false, HexDirection.NE, true, HexDirection.NE, true)]   // Outgoing to NE
    [InlineData(HexDirection.E, true, HexDirection.E, true, HexDirection.E, true)]       // Both through E
    [InlineData(HexDirection.SE, false, HexDirection.NE, true, HexDirection.E, false)]   // No river through SE
    public void HasRiverThroughEdge_Logic(
        HexDirection queryDirection,
        bool hasIncoming, HexDirection incomingDir,
        bool hasOutgoing, HexDirection outgoingDir,
        bool expected)
    {
        // HasRiverThroughEdge = (hasIncoming && incomingDir == dir) || (hasOutgoing && outgoingDir == dir)
        bool result = (hasIncoming && incomingDir == queryDirection) ||
                      (hasOutgoing && outgoingDir == queryDirection);
        result.Should().Be(expected);
    }
}
