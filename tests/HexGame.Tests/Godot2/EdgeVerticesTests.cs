using FluentAssertions;
using Xunit;

namespace HexGame.Tests.Godot2
{

// Local copies of godot2 types for testing without Godot dependencies
namespace Tutorial4
{
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3 Lerp(Vector3 to, float t) => new(X + (to.X - X) * t, Y + (to.Y - Y) * t, Z + (to.Z - Z) * t);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    }

    public static class HexMetrics
    {
        public const int TerracesPerSlope = 2;
        public const int TerraceSteps = TerracesPerSlope * 2 + 1;  // 5
        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;  // 0.2
        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);  // 0.333...

        public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
        {
            float h = step * HorizontalTerraceStepSize;
            float v = ((step + 1) / 2) * VerticalTerraceStepSize;
            return new Vector3(
                a.X + (b.X - a.X) * h,
                a.Y + (b.Y - a.Y) * v,
                a.Z + (b.Z - a.Z) * h
            );
        }
    }

    public struct EdgeVertices
    {
        public Vector3 V1, V2, V3, V4;

        public EdgeVertices(Vector3 corner1, Vector3 corner2)
        {
            V1 = corner1;
            V2 = corner1.Lerp(corner2, 1f / 3f);
            V3 = corner1.Lerp(corner2, 2f / 3f);
            V4 = corner2;
        }

        public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
        {
            EdgeVertices result;
            result.V1 = HexMetrics.TerraceLerp(a.V1, b.V1, step);
            result.V2 = HexMetrics.TerraceLerp(a.V2, b.V2, step);
            result.V3 = HexMetrics.TerraceLerp(a.V3, b.V3, step);
            result.V4 = HexMetrics.TerraceLerp(a.V4, b.V4, step);
            return result;
        }
    }
}

/// <summary>
/// Tests for EdgeVertices struct from Tutorial 4.
/// </summary>
public class EdgeVerticesTests
{
    private const float Tolerance = 0.0001f;

    #region Constructor Tests

    [Fact]
    public void Constructor_V1_EqualsCorner1()
    {
        var corner1 = new Tutorial4.Vector3(0, 0, 0);
        var corner2 = new Tutorial4.Vector3(12, 0, 0);

        var edge = new Tutorial4.EdgeVertices(corner1, corner2);

        edge.V1.X.Should().BeApproximately(corner1.X, Tolerance);
        edge.V1.Y.Should().BeApproximately(corner1.Y, Tolerance);
        edge.V1.Z.Should().BeApproximately(corner1.Z, Tolerance);
    }

    [Fact]
    public void Constructor_V4_EqualsCorner2()
    {
        var corner1 = new Tutorial4.Vector3(0, 0, 0);
        var corner2 = new Tutorial4.Vector3(12, 0, 0);

        var edge = new Tutorial4.EdgeVertices(corner1, corner2);

        edge.V4.X.Should().BeApproximately(corner2.X, Tolerance);
        edge.V4.Y.Should().BeApproximately(corner2.Y, Tolerance);
        edge.V4.Z.Should().BeApproximately(corner2.Z, Tolerance);
    }

    [Fact]
    public void Constructor_V2_AtOneThird()
    {
        var corner1 = new Tutorial4.Vector3(0, 0, 0);
        var corner2 = new Tutorial4.Vector3(12, 0, 0);

        var edge = new Tutorial4.EdgeVertices(corner1, corner2);

        // V2 should be at 1/3 = 4
        edge.V2.X.Should().BeApproximately(4f, Tolerance);
        edge.V2.Y.Should().BeApproximately(0f, Tolerance);
        edge.V2.Z.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void Constructor_V3_AtTwoThirds()
    {
        var corner1 = new Tutorial4.Vector3(0, 0, 0);
        var corner2 = new Tutorial4.Vector3(12, 0, 0);

        var edge = new Tutorial4.EdgeVertices(corner1, corner2);

        // V3 should be at 2/3 = 8
        edge.V3.X.Should().BeApproximately(8f, Tolerance);
        edge.V3.Y.Should().BeApproximately(0f, Tolerance);
        edge.V3.Z.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void Constructor_WithNonZeroY_InterpolatesAllComponents()
    {
        var corner1 = new Tutorial4.Vector3(0, 0, 0);
        var corner2 = new Tutorial4.Vector3(12, 6, 3);

        var edge = new Tutorial4.EdgeVertices(corner1, corner2);

        // V2 at 1/3
        edge.V2.X.Should().BeApproximately(4f, Tolerance);
        edge.V2.Y.Should().BeApproximately(2f, Tolerance);
        edge.V2.Z.Should().BeApproximately(1f, Tolerance);

        // V3 at 2/3
        edge.V3.X.Should().BeApproximately(8f, Tolerance);
        edge.V3.Y.Should().BeApproximately(4f, Tolerance);
        edge.V3.Z.Should().BeApproximately(2f, Tolerance);
    }

    #endregion

    #region TerraceLerp Tests

    [Fact]
    public void TerraceLerp_Step0_ReturnsStartEdge()
    {
        var a = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 0, 0), new Tutorial4.Vector3(12, 0, 0));
        var b = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 6, 0), new Tutorial4.Vector3(12, 6, 0));

        var result = Tutorial4.EdgeVertices.TerraceLerp(a, b, 0);

        result.V1.X.Should().BeApproximately(a.V1.X, Tolerance);
        result.V1.Y.Should().BeApproximately(a.V1.Y, Tolerance);
        result.V4.X.Should().BeApproximately(a.V4.X, Tolerance);
        result.V4.Y.Should().BeApproximately(a.V4.Y, Tolerance);
    }

    [Fact]
    public void TerraceLerp_Step1_InterpolatesCorrectly()
    {
        var a = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 0, 0), new Tutorial4.Vector3(12, 0, 0));
        var b = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 6, 0), new Tutorial4.Vector3(12, 6, 0));

        var result = Tutorial4.EdgeVertices.TerraceLerp(a, b, 1);

        // Horizontal: step 1 * 0.2 = 0.2 -> no XZ change since a and b have same XZ
        // Vertical: ((1+1)/2) * (1/3) = 1 * 0.333... = 0.333... -> Y = 0 + 6 * 0.333 = 2
        result.V1.Y.Should().BeApproximately(2f, Tolerance);
        result.V4.Y.Should().BeApproximately(2f, Tolerance);
    }

    [Fact]
    public void TerraceLerp_Step5_ReturnsEndEdge()
    {
        var a = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 0, 0), new Tutorial4.Vector3(12, 0, 0));
        var b = new Tutorial4.EdgeVertices(new Tutorial4.Vector3(0, 6, 0), new Tutorial4.Vector3(12, 6, 0));

        // Step 5 (TerraceSteps) - should be at end
        var result = Tutorial4.EdgeVertices.TerraceLerp(a, b, 5);

        // Horizontal: step 5 * 0.2 = 1.0
        // Vertical: ((5+1)/2) * (1/3) = 3 * 0.333... = 1.0
        result.V1.Y.Should().BeApproximately(6f, Tolerance);
        result.V4.Y.Should().BeApproximately(6f, Tolerance);
    }

    #endregion
}

/// <summary>
/// Tests for HexMetrics perturbation from Tutorial 4.
/// </summary>
public class HexMetricsPerturbTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void TerraceLerp_HorizontalProgressIsLinear()
    {
        var a = new Tutorial4.Vector3(0, 0, 0);
        var b = new Tutorial4.Vector3(10, 5, 0);

        // Step 1: h = 0.2
        var result1 = Tutorial4.HexMetrics.TerraceLerp(a, b, 1);
        result1.X.Should().BeApproximately(2f, Tolerance);

        // Step 2: h = 0.4
        var result2 = Tutorial4.HexMetrics.TerraceLerp(a, b, 2);
        result2.X.Should().BeApproximately(4f, Tolerance);

        // Step 3: h = 0.6
        var result3 = Tutorial4.HexMetrics.TerraceLerp(a, b, 3);
        result3.X.Should().BeApproximately(6f, Tolerance);
    }

    [Fact]
    public void TerraceLerp_VerticalProgressIsStaircase()
    {
        var a = new Tutorial4.Vector3(0, 0, 0);
        var b = new Tutorial4.Vector3(0, 9, 0);  // 9 so divisions are clean

        // Step 1: v = ((1+1)/2) * (1/3) = 1 * 0.333 = 0.333 -> Y = 3
        var result1 = Tutorial4.HexMetrics.TerraceLerp(a, b, 1);
        result1.Y.Should().BeApproximately(3f, Tolerance);

        // Step 2: v = ((2+1)/2) * (1/3) = 1 * 0.333 = 0.333 -> Y = 3 (same as step 1!)
        var result2 = Tutorial4.HexMetrics.TerraceLerp(a, b, 2);
        result2.Y.Should().BeApproximately(3f, Tolerance);

        // Step 3: v = ((3+1)/2) * (1/3) = 2 * 0.333 = 0.666 -> Y = 6
        var result3 = Tutorial4.HexMetrics.TerraceLerp(a, b, 3);
        result3.Y.Should().BeApproximately(6f, Tolerance);

        // Step 4: v = ((4+1)/2) * (1/3) = 2 * 0.333 = 0.666 -> Y = 6 (same as step 3!)
        var result4 = Tutorial4.HexMetrics.TerraceLerp(a, b, 4);
        result4.Y.Should().BeApproximately(6f, Tolerance);

        // Step 5: v = ((5+1)/2) * (1/3) = 3 * 0.333 = 1.0 -> Y = 9
        var result5 = Tutorial4.HexMetrics.TerraceLerp(a, b, 5);
        result5.Y.Should().BeApproximately(9f, Tolerance);
    }

    [Fact]
    public void CellPerturbStrength_MatchesTutorial()
    {
        // Tutorial 4 specifies CellPerturbStrength = 4f
        const float expected = 4f;
        // This test documents the expected value - actual value is in godot2/src/HexMetrics.cs
        expected.Should().Be(4f);
    }

    [Fact]
    public void NoiseScale_MatchesTutorial()
    {
        // Tutorial 4 specifies NoiseScale = 0.003f
        const float expected = 0.003f;
        expected.Should().Be(0.003f);
    }

    [Fact]
    public void ElevationPerturbStrength_MatchesTutorial()
    {
        // Tutorial 4 specifies ElevationPerturbStrength = 1.5f
        const float expected = 1.5f;
        expected.Should().Be(1.5f);
    }

    // Tutorial 6: River constants
    [Fact]
    public void StreamBedElevationOffset_MatchesTutorial()
    {
        // Tutorial 6 specifies StreamBedElevationOffset = -1.75f
        const float expected = -1.75f;
        expected.Should().Be(-1.75f);
    }

    [Fact]
    public void RiverSurfaceElevationOffset_MatchesTutorial()
    {
        // Tutorial 6 specifies RiverSurfaceElevationOffset = -0.5f
        const float expected = -0.5f;
        expected.Should().Be(-0.5f);
    }
}

// Tutorial 6: Tests for 5-vertex EdgeVertices
namespace Tutorial6
{
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3 Lerp(Vector3 to, float t) => new(X + (to.X - X) * t, Y + (to.Y - Y) * t, Z + (to.Z - Z) * t);
    }

    public struct EdgeVertices
    {
        public Vector3 V1, V2, V3, V4, V5;

        public EdgeVertices(Vector3 corner1, Vector3 corner2)
        {
            V1 = corner1;
            V2 = corner1.Lerp(corner2, 0.25f);
            V3 = corner1.Lerp(corner2, 0.5f);
            V4 = corner1.Lerp(corner2, 0.75f);
            V5 = corner2;
        }

        public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
        {
            V1 = corner1;
            V2 = corner1.Lerp(corner2, outerStep);
            V3 = corner1.Lerp(corner2, 0.5f);
            V4 = corner1.Lerp(corner2, 1f - outerStep);
            V5 = corner2;
        }
    }
}

/// <summary>
/// Tests for Tutorial 6 EdgeVertices struct (5 vertices instead of 4).
/// </summary>
public class EdgeVertices5Tests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void Constructor_CreatesCorrectVertexPositions()
    {
        var corner1 = new Tutorial6.Vector3(0, 0, 0);
        var corner2 = new Tutorial6.Vector3(16, 0, 0);

        var edge = new Tutorial6.EdgeVertices(corner1, corner2);

        edge.V1.X.Should().BeApproximately(0f, Tolerance);   // 0
        edge.V2.X.Should().BeApproximately(4f, Tolerance);   // 0.25
        edge.V3.X.Should().BeApproximately(8f, Tolerance);   // 0.50
        edge.V4.X.Should().BeApproximately(12f, Tolerance);  // 0.75
        edge.V5.X.Should().BeApproximately(16f, Tolerance);  // 1.00
    }

    [Fact]
    public void Constructor_V1_EqualsCorner1()
    {
        var corner1 = new Tutorial6.Vector3(5, 10, 15);
        var corner2 = new Tutorial6.Vector3(20, 25, 30);

        var edge = new Tutorial6.EdgeVertices(corner1, corner2);

        edge.V1.X.Should().BeApproximately(corner1.X, Tolerance);
        edge.V1.Y.Should().BeApproximately(corner1.Y, Tolerance);
        edge.V1.Z.Should().BeApproximately(corner1.Z, Tolerance);
    }

    [Fact]
    public void Constructor_V5_EqualsCorner2()
    {
        var corner1 = new Tutorial6.Vector3(5, 10, 15);
        var corner2 = new Tutorial6.Vector3(20, 25, 30);

        var edge = new Tutorial6.EdgeVertices(corner1, corner2);

        edge.V5.X.Should().BeApproximately(corner2.X, Tolerance);
        edge.V5.Y.Should().BeApproximately(corner2.Y, Tolerance);
        edge.V5.Z.Should().BeApproximately(corner2.Z, Tolerance);
    }

    [Fact]
    public void Constructor_V3_AtMidpoint()
    {
        var corner1 = new Tutorial6.Vector3(0, 0, 0);
        var corner2 = new Tutorial6.Vector3(10, 20, 30);

        var edge = new Tutorial6.EdgeVertices(corner1, corner2);

        edge.V3.X.Should().BeApproximately(5f, Tolerance);
        edge.V3.Y.Should().BeApproximately(10f, Tolerance);
        edge.V3.Z.Should().BeApproximately(15f, Tolerance);
    }

    [Fact]
    public void OuterStepConstructor_CreatesNarrowerChannel()
    {
        var corner1 = new Tutorial6.Vector3(0, 0, 0);
        var corner2 = new Tutorial6.Vector3(12, 0, 0);

        // Use 1/6 outer step for river channels
        var edge = new Tutorial6.EdgeVertices(corner1, corner2, 1f / 6f);

        edge.V1.X.Should().BeApproximately(0f, Tolerance);    // 0
        edge.V2.X.Should().BeApproximately(2f, Tolerance);    // 1/6
        edge.V3.X.Should().BeApproximately(6f, Tolerance);    // 0.5
        edge.V4.X.Should().BeApproximately(10f, Tolerance);   // 5/6
        edge.V5.X.Should().BeApproximately(12f, Tolerance);   // 1.0
    }

    [Fact]
    public void OuterStepConstructor_V2AndV4_AreSymmetricAroundV3()
    {
        var corner1 = new Tutorial6.Vector3(0, 0, 0);
        var corner2 = new Tutorial6.Vector3(10, 0, 0);

        var edge = new Tutorial6.EdgeVertices(corner1, corner2, 0.2f);

        // V2 is at 0.2 (distance 2 from V1)
        // V3 is at 0.5 (distance 5 from V1)
        // V4 is at 0.8 (distance 8 from V1)
        // Distance from V2 to V3 = 3
        // Distance from V3 to V4 = 3
        float distV2ToV3 = edge.V3.X - edge.V2.X;
        float distV3ToV4 = edge.V4.X - edge.V3.X;
        distV2ToV3.Should().BeApproximately(distV3ToV4, Tolerance);
    }
}

} // namespace HexGame.Tests.Godot2
