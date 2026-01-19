using FluentAssertions;
using HexGame.Core;
using Xunit;

namespace HexGame.Tests.Core;

/// <summary>
/// Tests for HexCoordinates struct.
/// </summary>
public class HexCoordinatesTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_WithValidAxialCoordinates_SetsQAndR()
    {
        // Arrange & Act
        var coords = new HexCoordinates(3, -2);

        // Assert
        coords.Q.Should().Be(3);
        coords.R.Should().Be(-2);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-5, 3)]
    [InlineData(10, -10)]
    public void Constructor_VariousCoordinates_StoresCorrectly(int q, int r)
    {
        var coords = new HexCoordinates(q, r);

        coords.Q.Should().Be(q);
        coords.R.Should().Be(r);
    }

    #endregion

    #region Cube Coordinate Tests

    [Fact]
    public void CubeCoordinates_SumToZero()
    {
        // The cube coordinate constraint: x + y + z = 0
        var coords = new HexCoordinates(3, -2);

        var sum = coords.X + coords.Y + coords.Z;

        sum.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, -3)]
    [InlineData(-2, 7)]
    public void CubeCoordinates_AlwaysSumToZero(int q, int r)
    {
        var coords = new HexCoordinates(q, r);

        (coords.X + coords.Y + coords.Z).Should().Be(0);
    }

    [Fact]
    public void X_Equals_Q()
    {
        var coords = new HexCoordinates(5, -3);

        coords.X.Should().Be(coords.Q);
    }

    [Fact]
    public void Z_Equals_R()
    {
        var coords = new HexCoordinates(5, -3);

        coords.Z.Should().Be(coords.R);
    }

    [Fact]
    public void Y_Equals_NegativeXMinusZ()
    {
        var coords = new HexCoordinates(5, -3);

        coords.Y.Should().Be(-coords.X - coords.Z);
    }

    #endregion

    #region Distance Tests

    [Fact]
    public void DistanceTo_SameCoordinate_ReturnsZero()
    {
        var a = new HexCoordinates(3, 4);
        var b = new HexCoordinates(3, 4);

        a.DistanceTo(b).Should().Be(0);
    }

    [Fact]
    public void DistanceTo_AdjacentHex_ReturnsOne()
    {
        var center = new HexCoordinates(0, 0);
        var adjacent = new HexCoordinates(1, 0);

        center.DistanceTo(adjacent).Should().Be(1);
    }

    [Theory]
    [InlineData(0, 0, 3, 0, 3)]   // East
    [InlineData(0, 0, 0, 3, 3)]   // South
    [InlineData(0, 0, -3, 3, 3)]  // Southwest
    [InlineData(0, 0, 2, -2, 2)]  // Northeast
    public void DistanceTo_VariousDirections_ReturnsCorrectDistance(
        int q1, int r1, int q2, int r2, int expectedDistance)
    {
        var a = new HexCoordinates(q1, r1);
        var b = new HexCoordinates(q2, r2);

        a.DistanceTo(b).Should().Be(expectedDistance);
    }

    [Fact]
    public void DistanceTo_IsSymmetric()
    {
        var a = new HexCoordinates(5, -3);
        var b = new HexCoordinates(-2, 7);

        a.DistanceTo(b).Should().Be(b.DistanceTo(a));
    }

    #endregion

    #region Neighbor Tests

    [Fact]
    public void GetNeighbor_East_ReturnsCorrectCoordinate()
    {
        var center = new HexCoordinates(0, 0);

        // E direction has offset (1, -1) in axial coordinates
        var neighbor = center.GetNeighbor(HexDirection.E);

        neighbor.Q.Should().Be(1);
        neighbor.R.Should().Be(-1);
    }

    [Fact]
    public void GetNeighbor_AllDirections_AreDistanceOne()
    {
        var center = new HexCoordinates(5, 3);

        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var neighbor = center.GetNeighbor(dir);
            center.DistanceTo(neighbor).Should().Be(1, $"neighbor in direction {dir} should be distance 1");
        }
    }

    [Fact]
    public void GetNeighbor_OppositeDirections_AreDistanceTwo()
    {
        var center = new HexCoordinates(0, 0);

        var east = center.GetNeighbor(HexDirection.E);
        var west = center.GetNeighbor(HexDirection.W);

        east.DistanceTo(west).Should().Be(2);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameCoordinates_ReturnsTrue()
    {
        var a = new HexCoordinates(3, 4);
        var b = new HexCoordinates(3, 4);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentCoordinates_ReturnsFalse()
    {
        var a = new HexCoordinates(3, 4);
        var b = new HexCoordinates(4, 3);

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameCoordinates_ReturnsSameHash()
    {
        var a = new HexCoordinates(3, 4);
        var b = new HexCoordinates(3, 4);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region World Position Tests

    [Fact]
    public void ToWorldPosition_Origin_ReturnsZeroXZ()
    {
        var coords = new HexCoordinates(0, 0);

        var worldPos = coords.ToWorldPosition(0);

        worldPos.X.Should().BeApproximately(0f, 0.001f);
        worldPos.Z.Should().BeApproximately(0f, 0.001f);
    }

    [Fact]
    public void ToWorldPosition_WithElevation_SetsYCoordinate()
    {
        var coords = new HexCoordinates(0, 0);
        int elevation = 5;

        var worldPos = coords.ToWorldPosition(elevation);

        // Y = elevation * HexMetrics.ElevationStep (0.4f)
        worldPos.Y.Should().BeApproximately(elevation * HexMetrics.ElevationStep, 0.001f);
    }

    [Fact]
    public void FromWorldPosition_RoundTrip_ReturnsOriginalCoordinates()
    {
        var original = new HexCoordinates(5, -3);
        var worldPos = original.ToWorldPosition(0);

        var roundTrip = HexCoordinates.FromWorldPosition(worldPos);

        roundTrip.Should().Be(original);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var coords = new HexCoordinates(3, -2);

        var str = coords.ToString();

        str.Should().Contain("3");
        str.Should().Contain("-2");
    }

    #endregion
}
