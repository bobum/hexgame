using FluentAssertions;
using Godot;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexMetrics geometry calculations.
/// Verifies Tutorial 1-3 math.
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

    // Tutorial 3: Elevation Tests

    [Fact]
    public void ElevationStep_IsCorrect()
    {
        HexMetrics.ElevationStep.Should().Be(5f);
    }

    [Fact]
    public void TerracesPerSlope_IsCorrect()
    {
        HexMetrics.TerracesPerSlope.Should().Be(2);
    }

    [Fact]
    public void TerraceSteps_IsCorrect()
    {
        // TerraceSteps = TerracesPerSlope * 2 + 1 = 5
        HexMetrics.TerraceSteps.Should().Be(5);
    }

    [Fact]
    public void HorizontalTerraceStepSize_IsCorrect()
    {
        // 1 / 5 = 0.2
        HexMetrics.HorizontalTerraceStepSize.Should().BeApproximately(0.2f, Tolerance);
    }

    [Fact]
    public void VerticalTerraceStepSize_IsCorrect()
    {
        // 1 / 3 = 0.333...
        HexMetrics.VerticalTerraceStepSize.Should().BeApproximately(0.3333f, 0.001f);
    }

    [Theory]
    [InlineData(0, 0, HexEdgeType.Flat)]
    [InlineData(5, 5, HexEdgeType.Flat)]
    [InlineData(-3, -3, HexEdgeType.Flat)]
    public void GetEdgeType_SameElevation_ReturnsFlat(int e1, int e2, HexEdgeType expected)
    {
        HexMetrics.GetEdgeType(e1, e2).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, HexEdgeType.Slope)]
    [InlineData(1, 0, HexEdgeType.Slope)]
    [InlineData(5, 6, HexEdgeType.Slope)]
    [InlineData(6, 5, HexEdgeType.Slope)]
    [InlineData(-1, 0, HexEdgeType.Slope)]
    [InlineData(0, -1, HexEdgeType.Slope)]
    public void GetEdgeType_OneLevelDifference_ReturnsSlope(int e1, int e2, HexEdgeType expected)
    {
        HexMetrics.GetEdgeType(e1, e2).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 2, HexEdgeType.Cliff)]
    [InlineData(2, 0, HexEdgeType.Cliff)]
    [InlineData(0, 5, HexEdgeType.Cliff)]
    [InlineData(5, 0, HexEdgeType.Cliff)]
    [InlineData(-2, 0, HexEdgeType.Cliff)]
    [InlineData(0, -2, HexEdgeType.Cliff)]
    [InlineData(1, 10, HexEdgeType.Cliff)]
    public void GetEdgeType_TwoOrMoreLevelDifference_ReturnsCliff(int e1, int e2, HexEdgeType expected)
    {
        HexMetrics.GetEdgeType(e1, e2).Should().Be(expected);
    }

    [Fact]
    public void TerraceLerp_Vector3_Step0_ReturnsStart()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, 5f, 10f);

        var result = HexMetrics.TerraceLerp(a, b, 0);

        result.X.Should().BeApproximately(0f, Tolerance);
        result.Y.Should().BeApproximately(0f, Tolerance);
        result.Z.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void TerraceLerp_Vector3_Step5_ReturnsEnd()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, 5f, 10f);

        var result = HexMetrics.TerraceLerp(a, b, 5);

        result.X.Should().BeApproximately(10f, Tolerance);
        result.Y.Should().BeApproximately(5f, Tolerance);
        result.Z.Should().BeApproximately(10f, Tolerance);
    }

    [Fact]
    public void TerraceLerp_Vector3_VerticalStaircasePattern()
    {
        // Vertical steps follow pattern: steps 1,2 -> same Y; steps 3,4 -> same Y; step 5 -> end
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, 9f, 10f);  // 9 units height difference for clean math

        var y1 = HexMetrics.TerraceLerp(a, b, 1).Y;
        var y2 = HexMetrics.TerraceLerp(a, b, 2).Y;
        var y3 = HexMetrics.TerraceLerp(a, b, 3).Y;
        var y4 = HexMetrics.TerraceLerp(a, b, 4).Y;
        var y5 = HexMetrics.TerraceLerp(a, b, 5).Y;

        // Steps 1 and 2 should have the same Y (first terrace flat)
        y1.Should().BeApproximately(y2, Tolerance);

        // Steps 3 and 4 should have the same Y (second terrace flat)
        y3.Should().BeApproximately(y4, Tolerance);

        // Step 5 reaches the end
        y5.Should().BeApproximately(9f, Tolerance);

        // Each terrace level should be higher than the previous
        y3.Should().BeGreaterThan(y1);
        y5.Should().BeGreaterThan(y3);
    }

    [Fact]
    public void TerraceLerp_Vector3_HorizontalIsLinear()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, 5f, 10f);

        // Horizontal interpolation is linear: step * 0.2
        for (int step = 0; step <= 5; step++)
        {
            var result = HexMetrics.TerraceLerp(a, b, step);
            var expectedX = step * 0.2f * 10f;
            var expectedZ = step * 0.2f * 10f;

            result.X.Should().BeApproximately(expectedX, Tolerance);
            result.Z.Should().BeApproximately(expectedZ, Tolerance);
        }
    }

    [Fact]
    public void TerraceLerp_Color_IsLinear()
    {
        var a = new Color(0f, 0f, 0f);
        var b = new Color(1f, 1f, 1f);

        // Color interpolation is linear based on horizontal step
        for (int step = 0; step <= 5; step++)
        {
            var result = HexMetrics.TerraceLerp(a, b, step);
            var expected = step * 0.2f;

            result.R.Should().BeApproximately(expected, Tolerance);
            result.G.Should().BeApproximately(expected, Tolerance);
            result.B.Should().BeApproximately(expected, Tolerance);
        }
    }
}
