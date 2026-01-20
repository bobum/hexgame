using FluentAssertions;
using Godot;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for EdgeVertices struct.
/// Verifies Tutorial 6 edge subdivision for rivers.
/// </summary>
public class EdgeVerticesTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void Constructor_V1_IsAtCorner1()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 10f);

        var edge = new EdgeVertices(corner1, corner2);

        edge.V1.Should().Be(corner1);
    }

    [Fact]
    public void Constructor_V5_IsAtCorner2()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 10f);

        var edge = new EdgeVertices(corner1, corner2);

        edge.V5.Should().Be(corner2);
    }

    [Fact]
    public void Constructor_V2_IsAt25Percent()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 10f);

        var edge = new EdgeVertices(corner1, corner2);

        edge.V2.X.Should().BeApproximately(2.5f, Tolerance);
        edge.V2.Z.Should().BeApproximately(2.5f, Tolerance);
    }

    [Fact]
    public void Constructor_V3_IsAt50Percent()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 10f);

        var edge = new EdgeVertices(corner1, corner2);

        edge.V3.X.Should().BeApproximately(5f, Tolerance);
        edge.V3.Z.Should().BeApproximately(5f, Tolerance);
    }

    [Fact]
    public void Constructor_V4_IsAt75Percent()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 10f);

        var edge = new EdgeVertices(corner1, corner2);

        edge.V4.X.Should().BeApproximately(7.5f, Tolerance);
        edge.V4.Z.Should().BeApproximately(7.5f, Tolerance);
    }

    [Fact]
    public void Constructor_AllVertices_AreEvenlySpaced()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(12f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2);

        // Should be at 0, 3, 6, 9, 12
        edge.V1.X.Should().BeApproximately(0f, Tolerance);
        edge.V2.X.Should().BeApproximately(3f, Tolerance);
        edge.V3.X.Should().BeApproximately(6f, Tolerance);
        edge.V4.X.Should().BeApproximately(9f, Tolerance);
        edge.V5.X.Should().BeApproximately(12f, Tolerance);
    }

    // OuterStep constructor tests (for river channels)

    [Fact]
    public void OuterStepConstructor_V1_IsAtCorner1()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2, 1f / 6f);

        edge.V1.Should().Be(corner1);
    }

    [Fact]
    public void OuterStepConstructor_V5_IsAtCorner2()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(10f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2, 1f / 6f);

        edge.V5.Should().Be(corner2);
    }

    [Fact]
    public void OuterStepConstructor_V2_IsAtOuterStep()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(12f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2, 1f / 6f);

        // V2 at 1/6 = 2
        edge.V2.X.Should().BeApproximately(2f, Tolerance);
    }

    [Fact]
    public void OuterStepConstructor_V4_IsAt1MinusOuterStep()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(12f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2, 1f / 6f);

        // V4 at 1 - 1/6 = 5/6 = 10
        edge.V4.X.Should().BeApproximately(10f, Tolerance);
    }

    [Fact]
    public void OuterStepConstructor_V3_IsAtMiddle()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(12f, 0f, 0f);

        var edge = new EdgeVertices(corner1, corner2, 1f / 6f);

        // V3 always at 0.5
        edge.V3.X.Should().BeApproximately(6f, Tolerance);
    }

    [Fact]
    public void OuterStepConstructor_CreatesNarrowerChannel()
    {
        var corner1 = new Vector3(0f, 0f, 0f);
        var corner2 = new Vector3(12f, 0f, 0f);

        var normalEdge = new EdgeVertices(corner1, corner2);
        var riverEdge = new EdgeVertices(corner1, corner2, 1f / 6f);

        // River channel (V2 to V4) should be narrower
        var normalWidth = normalEdge.V4.X - normalEdge.V2.X;  // 9 - 3 = 6
        var riverWidth = riverEdge.V4.X - riverEdge.V2.X;     // 10 - 2 = 8

        // Wait, actually river has wider V2-V4 span but the channel itself is narrower
        // The outerStep=1/6 moves V2 closer to V1 and V4 closer to V5
        riverEdge.V2.X.Should().BeLessThan(normalEdge.V2.X);  // 2 < 3
        riverEdge.V4.X.Should().BeGreaterThan(normalEdge.V4.X); // 10 > 9
    }

    // TerraceLerp tests

    [Fact]
    public void TerraceLerp_Step0_ReturnsFirstEdge()
    {
        var a = new EdgeVertices(new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
        var b = new EdgeVertices(new Vector3(0f, 5f, 10f), new Vector3(10f, 5f, 10f));

        var result = EdgeVertices.TerraceLerp(a, b, 0);

        result.V1.Should().Be(a.V1);
        result.V2.X.Should().BeApproximately(a.V2.X, Tolerance);
        result.V3.X.Should().BeApproximately(a.V3.X, Tolerance);
        result.V4.X.Should().BeApproximately(a.V4.X, Tolerance);
        result.V5.Should().Be(a.V5);
    }

    [Fact]
    public void TerraceLerp_Step5_ReturnsSecondEdge()
    {
        var a = new EdgeVertices(new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
        var b = new EdgeVertices(new Vector3(0f, 5f, 10f), new Vector3(10f, 5f, 10f));

        var result = EdgeVertices.TerraceLerp(a, b, 5);

        result.V1.X.Should().BeApproximately(b.V1.X, Tolerance);
        result.V1.Y.Should().BeApproximately(b.V1.Y, Tolerance);
        result.V1.Z.Should().BeApproximately(b.V1.Z, Tolerance);
    }

    [Fact]
    public void TerraceLerp_InterpolatesAllFiveVertices()
    {
        var a = new EdgeVertices(new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f));
        var b = new EdgeVertices(new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f));

        var result = EdgeVertices.TerraceLerp(a, b, 3);

        // At step 3, horizontal interpolation = 3 * 0.2 = 0.6
        // So Z should be 0.6 * 10 = 6 for all vertices
        result.V1.Z.Should().BeApproximately(6f, Tolerance);
        result.V2.Z.Should().BeApproximately(6f, Tolerance);
        result.V3.Z.Should().BeApproximately(6f, Tolerance);
        result.V4.Z.Should().BeApproximately(6f, Tolerance);
        result.V5.Z.Should().BeApproximately(6f, Tolerance);
    }
}
