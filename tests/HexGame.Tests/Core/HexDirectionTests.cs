using FluentAssertions;
using HexGame.Core;
using Xunit;

namespace HexGame.Tests.Core;

/// <summary>
/// Tests for HexDirection enum and extensions.
/// </summary>
public class HexDirectionTests
{
    [Fact]
    public void AllDirections_HasSixDirections()
    {
        var directions = Enum.GetValues<HexDirection>();

        directions.Should().HaveCount(6);
    }

    [Theory]
    [InlineData(HexDirection.E, HexDirection.W)]
    [InlineData(HexDirection.W, HexDirection.E)]
    [InlineData(HexDirection.NE, HexDirection.SW)]
    [InlineData(HexDirection.SW, HexDirection.NE)]
    [InlineData(HexDirection.SE, HexDirection.NW)]
    [InlineData(HexDirection.NW, HexDirection.SE)]
    public void Opposite_ReturnsCorrectDirection(HexDirection direction, HexDirection expected)
    {
        direction.Opposite().Should().Be(expected);
    }

    [Fact]
    public void Opposite_TwiceReturnsOriginal()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            dir.Opposite().Opposite().Should().Be(dir);
        }
    }

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.E)]
    [InlineData(HexDirection.E, HexDirection.SE)]
    [InlineData(HexDirection.SE, HexDirection.SW)]
    [InlineData(HexDirection.SW, HexDirection.W)]
    [InlineData(HexDirection.W, HexDirection.NW)]
    [InlineData(HexDirection.NW, HexDirection.NE)]
    public void Next_ReturnsNextClockwiseDirection(HexDirection direction, HexDirection expected)
    {
        direction.Next().Should().Be(expected);
    }

    [Fact]
    public void Next_SixTimes_ReturnsOriginal()
    {
        var dir = HexDirection.E;

        for (int i = 0; i < 6; i++)
        {
            dir = dir.Next();
        }

        dir.Should().Be(HexDirection.E);
    }

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.NW)]
    [InlineData(HexDirection.E, HexDirection.NE)]
    [InlineData(HexDirection.SE, HexDirection.E)]
    [InlineData(HexDirection.SW, HexDirection.SE)]
    [InlineData(HexDirection.W, HexDirection.SW)]
    [InlineData(HexDirection.NW, HexDirection.W)]
    public void Previous_ReturnsPreviousDirection(HexDirection direction, HexDirection expected)
    {
        direction.Previous().Should().Be(expected);
    }

    [Fact]
    public void NextAndPrevious_AreInverse()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            dir.Next().Previous().Should().Be(dir);
            dir.Previous().Next().Should().Be(dir);
        }
    }

    [Fact]
    public void GetOffset_ReturnsNonZeroOffset()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var offset = dir.GetOffset();

            // Each direction should have a non-zero offset
            (Math.Abs(offset.X) + Math.Abs(offset.Y)).Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void GetOffset_OppositeDirections_HaveNegatedOffsets()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var offset1 = dir.GetOffset();
            var offset2 = dir.Opposite().GetOffset();

            offset1.X.Should().Be(-offset2.X);
            offset1.Y.Should().Be(-offset2.Y);
        }
    }

    [Theory]
    [InlineData(HexDirection.NE, 1, 0)]
    [InlineData(HexDirection.E, 1, -1)]
    [InlineData(HexDirection.SE, 0, -1)]
    [InlineData(HexDirection.SW, -1, 0)]
    [InlineData(HexDirection.W, -1, 1)]
    [InlineData(HexDirection.NW, 0, 1)]
    public void GetOffset_ReturnsCorrectOffset(HexDirection direction, int expectedQ, int expectedR)
    {
        var offset = direction.GetOffset();

        offset.X.Should().Be(expectedQ);
        offset.Y.Should().Be(expectedR);
    }
}
