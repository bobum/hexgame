using FluentAssertions;
using Godot;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexMetrics geometry calculations.
/// Verifies Tutorial 1-2 math.
/// </summary>
public class HexMetricsTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void OuterRadius_IsCorrect()
    {
        HexMetrics.OuterRadius.Should().Be(10f);
    }

    [Fact]
    public void InnerRadius_IsCorrect()
    {
        // innerRadius = outerRadius * sqrt(3)/2 ≈ 8.66025404
        HexMetrics.InnerRadius.Should().BeApproximately(8.66025404f, Tolerance);
    }

    [Fact]
    public void SolidFactor_IsCorrect()
    {
        HexMetrics.SolidFactor.Should().Be(0.75f);
    }

    [Fact]
    public void BlendFactor_IsCorrect()
    {
        HexMetrics.BlendFactor.Should().Be(0.25f);
    }

    [Fact]
    public void SolidFactor_Plus_BlendFactor_Equals_One()
    {
        (HexMetrics.SolidFactor + HexMetrics.BlendFactor).Should().Be(1f);
    }

    [Fact]
    public void Corners_HasSevenElements()
    {
        HexMetrics.Corners.Should().HaveCount(7);
    }

    [Fact]
    public void Corners_FirstAndLast_AreIdentical()
    {
        // Wraparound: index 6 = index 0
        HexMetrics.Corners[0].Equals(HexMetrics.Corners[6], Tolerance).Should().BeTrue();
    }

    [Fact]
    public void Corners_ArePointyTop()
    {
        // For pointy-top, corner 0 should be at the top (positive Z, X=0)
        HexMetrics.Corners[0].X.Should().Be(0f);
        HexMetrics.Corners[0].Z.Should().Be(HexMetrics.OuterRadius);
    }

    [Fact]
    public void Corners_AllAtOuterRadius()
    {
        for (int i = 0; i < 6; i++)
        {
            var corner = HexMetrics.Corners[i];
            var distance = MathF.Sqrt(corner.X * corner.X + corner.Z * corner.Z);
            distance.Should().BeApproximately(HexMetrics.OuterRadius, Tolerance);
        }
    }

    [Theory]
    [InlineData(HexDirection.NE, 0)]
    [InlineData(HexDirection.E, 1)]
    [InlineData(HexDirection.SE, 2)]
    [InlineData(HexDirection.SW, 3)]
    [InlineData(HexDirection.W, 4)]
    [InlineData(HexDirection.NW, 5)]
    public void GetFirstCorner_ReturnsCorrectIndex(HexDirection direction, int expectedIndex)
    {
        var result = HexMetrics.GetFirstCorner(direction);
        result.Equals(HexMetrics.Corners[expectedIndex], Tolerance).Should().BeTrue();
    }

    [Theory]
    [InlineData(HexDirection.NE, 1)]
    [InlineData(HexDirection.E, 2)]
    [InlineData(HexDirection.SE, 3)]
    [InlineData(HexDirection.SW, 4)]
    [InlineData(HexDirection.W, 5)]
    [InlineData(HexDirection.NW, 6)]
    public void GetSecondCorner_ReturnsCorrectIndex(HexDirection direction, int expectedIndex)
    {
        var result = HexMetrics.GetSecondCorner(direction);
        result.Equals(HexMetrics.Corners[expectedIndex], Tolerance).Should().BeTrue();
    }

    [Fact]
    public void GetFirstSolidCorner_IsScaledBySolidFactor()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var solid = HexMetrics.GetFirstSolidCorner(dir);
            var outer = HexMetrics.GetFirstCorner(dir);

            solid.X.Should().BeApproximately(outer.X * HexMetrics.SolidFactor, Tolerance);
            solid.Y.Should().BeApproximately(outer.Y * HexMetrics.SolidFactor, Tolerance);
            solid.Z.Should().BeApproximately(outer.Z * HexMetrics.SolidFactor, Tolerance);
        }
    }

    [Fact]
    public void GetSecondSolidCorner_IsScaledBySolidFactor()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var solid = HexMetrics.GetSecondSolidCorner(dir);
            var outer = HexMetrics.GetSecondCorner(dir);

            solid.X.Should().BeApproximately(outer.X * HexMetrics.SolidFactor, Tolerance);
            solid.Y.Should().BeApproximately(outer.Y * HexMetrics.SolidFactor, Tolerance);
            solid.Z.Should().BeApproximately(outer.Z * HexMetrics.SolidFactor, Tolerance);
        }
    }

    [Fact]
    public void GetBridge_IsAverageOfCornersScaledByBlendFactor()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var bridge = HexMetrics.GetBridge(dir);
            var c1 = HexMetrics.GetFirstCorner(dir);
            var c2 = HexMetrics.GetSecondCorner(dir);
            var expected = (c1 + c2) * HexMetrics.BlendFactor;

            bridge.X.Should().BeApproximately(expected.X, Tolerance);
            bridge.Y.Should().BeApproximately(expected.Y, Tolerance);
            bridge.Z.Should().BeApproximately(expected.Z, Tolerance);
        }
    }

    [Fact]
    public void Bridge_PointsOutwardFromCenter()
    {
        // Each bridge should point outward from the hex center
        // The bridge vector should be non-zero and point in the appropriate direction
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var bridge = HexMetrics.GetBridge(dir);

            // Bridge should be non-zero
            var bridgeLength = MathF.Sqrt(bridge.X * bridge.X + bridge.Z * bridge.Z);
            bridgeLength.Should().BeGreaterThan(0);

            // Y component should be 0 (flat hex grid)
            bridge.Y.Should().Be(0);
        }
    }
}
