using FluentAssertions;
using Godot;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexCoordinates cube coordinate system.
/// Verifies Tutorial 1 coordinate math.
/// </summary>
public class HexCoordinatesTests
{
    [Fact]
    public void Y_IsDerivedFromXAndZ()
    {
        var coords = new HexCoordinates(2, 3);
        coords.Y.Should().Be(-2 - 3); // Y = -X - Z = -5
    }

    [Fact]
    public void CubeCoordinateConstraint_XPlusYPlusZEqualsZero()
    {
        var coords = new HexCoordinates(2, 3);
        (coords.X + coords.Y + coords.Z).Should().Be(0);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]   // Origin
    [InlineData(1, 0, 1, 0)]   // x=1, z=0 -> X=1
    [InlineData(0, 1, 0, 1)]   // x=0, z=1 -> X=0 (z/2=0 for odd row)
    [InlineData(2, 2, 1, 2)]   // x=2, z=2 -> X=2-1=1
    [InlineData(3, 4, 1, 4)]   // x=3, z=4 -> X=3-2=1
    public void FromOffsetCoordinates_ConvertsCorrectly(int offsetX, int offsetZ, int expectedX, int expectedZ)
    {
        var coords = HexCoordinates.FromOffsetCoordinates(offsetX, offsetZ);
        coords.X.Should().Be(expectedX);
        coords.Z.Should().Be(expectedZ);
    }

    [Fact]
    public void FromOffsetCoordinates_OddRowOffset()
    {
        // Row 1 (odd): cells are offset by 0.5 in display
        // Offset (2, 1) -> cube X = 2 - 1/2 = 2 - 0 = 2
        var coords = HexCoordinates.FromOffsetCoordinates(2, 1);
        coords.X.Should().Be(2);
        coords.Z.Should().Be(1);
    }

    [Fact]
    public void FromOffsetCoordinates_EvenRowOffset()
    {
        // Row 2 (even):
        // Offset (2, 2) -> cube X = 2 - 2/2 = 2 - 1 = 1
        var coords = HexCoordinates.FromOffsetCoordinates(2, 2);
        coords.X.Should().Be(1);
        coords.Z.Should().Be(2);
    }

    [Fact]
    public void FromPosition_OriginReturnsZeroCoordinates()
    {
        var coords = HexCoordinates.FromPosition(new Vector3(0, 0, 0));
        coords.X.Should().Be(0);
        coords.Z.Should().Be(0);
    }

    [Fact]
    public void FromPosition_CellCenterReturnsCorrectCoordinates()
    {
        // Cell at offset (1, 0) has world position:
        // X = (1 + 0 * 0.5 - 0) * (innerRadius * 2) = innerRadius * 2
        // Z = 0 * (outerRadius * 1.5) = 0
        float worldX = HexMetrics.InnerRadius * 2f;
        float worldZ = 0f;

        var coords = HexCoordinates.FromPosition(new Vector3(worldX, 0, worldZ));
        coords.X.Should().Be(1);
        coords.Z.Should().Be(0);
    }

    [Fact]
    public void FromPosition_SecondRowCellCenter()
    {
        // Cell at offset (0, 1) has world position:
        // X = (0 + 1 * 0.5 - 0) * (innerRadius * 2) = innerRadius
        // Z = 1 * (outerRadius * 1.5) = outerRadius * 1.5
        float worldX = HexMetrics.InnerRadius;
        float worldZ = HexMetrics.OuterRadius * 1.5f;

        var coords = HexCoordinates.FromPosition(new Vector3(worldX, 0, worldZ));
        coords.X.Should().Be(0);
        coords.Z.Should().Be(1);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var coords = new HexCoordinates(1, 2);
        coords.ToString().Should().Be("(1, -3, 2)");
    }

    [Fact]
    public void ToStringOnSeparateLines_FormatsCorrectly()
    {
        var coords = new HexCoordinates(1, 2);
        coords.ToStringOnSeparateLines().Should().Be("1\n-3\n2");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    [InlineData(-1, 2)]
    public void CubeCoordinateConstraint_AlwaysHolds(int x, int z)
    {
        var coords = new HexCoordinates(x, z);
        (coords.X + coords.Y + coords.Z).Should().Be(0,
            $"Cube constraint violated for ({x}, {z}): X={coords.X}, Y={coords.Y}, Z={coords.Z}");
    }
}
