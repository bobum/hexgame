using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexDirection enum and extensions.
/// Verifies Tutorial 2 direction logic.
/// </summary>
public class HexDirectionTests
{
    [Theory]
    [InlineData(HexDirection.NE, HexDirection.SW)]
    [InlineData(HexDirection.E, HexDirection.W)]
    [InlineData(HexDirection.SE, HexDirection.NW)]
    [InlineData(HexDirection.SW, HexDirection.NE)]
    [InlineData(HexDirection.W, HexDirection.E)]
    [InlineData(HexDirection.NW, HexDirection.SE)]
    public void Opposite_ReturnsCorrectDirection(HexDirection input, HexDirection expected)
    {
        input.Opposite().Should().Be(expected);
    }

    [Fact]
    public void Opposite_IsSymmetric()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            dir.Opposite().Opposite().Should().Be(dir);
        }
    }

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.NW)] // Wrap case
    [InlineData(HexDirection.E, HexDirection.NE)]
    [InlineData(HexDirection.SE, HexDirection.E)]
    [InlineData(HexDirection.SW, HexDirection.SE)]
    [InlineData(HexDirection.W, HexDirection.SW)]
    [InlineData(HexDirection.NW, HexDirection.W)]
    public void Previous_ReturnsCorrectDirection(HexDirection input, HexDirection expected)
    {
        input.Previous().Should().Be(expected);
    }

    [Theory]
    [InlineData(HexDirection.NW, HexDirection.NE)] // Wrap case
    [InlineData(HexDirection.NE, HexDirection.E)]
    [InlineData(HexDirection.E, HexDirection.SE)]
    [InlineData(HexDirection.SE, HexDirection.SW)]
    [InlineData(HexDirection.SW, HexDirection.W)]
    [InlineData(HexDirection.W, HexDirection.NW)]
    public void Next_ReturnsCorrectDirection(HexDirection input, HexDirection expected)
    {
        input.Next().Should().Be(expected);
    }

    [Fact]
    public void Previous_And_Next_AreInverses()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            dir.Previous().Next().Should().Be(dir);
            dir.Next().Previous().Should().Be(dir);
        }
    }

    [Fact]
    public void SixNexts_ReturnToOriginal()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var current = dir;
            for (int i = 0; i < 6; i++)
            {
                current = current.Next();
            }
            current.Should().Be(dir);
        }
    }

    // Tutorial 6: Previous2 and Next2 tests

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.W)]   // NE - 2 = W (wraps)
    [InlineData(HexDirection.E, HexDirection.NW)]   // E - 2 = NW (wraps)
    [InlineData(HexDirection.SE, HexDirection.NE)]  // SE - 2 = NE
    [InlineData(HexDirection.SW, HexDirection.E)]   // SW - 2 = E
    [InlineData(HexDirection.W, HexDirection.SE)]   // W - 2 = SE
    [InlineData(HexDirection.NW, HexDirection.SW)]  // NW - 2 = SW
    public void Previous2_ReturnsCorrectDirection(HexDirection input, HexDirection expected)
    {
        input.Previous2().Should().Be(expected);
    }

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.SE)]  // NE + 2 = SE
    [InlineData(HexDirection.E, HexDirection.SW)]   // E + 2 = SW
    [InlineData(HexDirection.SE, HexDirection.W)]   // SE + 2 = W
    [InlineData(HexDirection.SW, HexDirection.NW)]  // SW + 2 = NW
    [InlineData(HexDirection.W, HexDirection.NE)]   // W + 2 = NE (wraps)
    [InlineData(HexDirection.NW, HexDirection.E)]   // NW + 2 = E (wraps)
    public void Next2_ReturnsCorrectDirection(HexDirection input, HexDirection expected)
    {
        input.Next2().Should().Be(expected);
    }

    [Fact]
    public void Previous2_And_Next2_AreInverses()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            dir.Previous2().Next2().Should().Be(dir);
            dir.Next2().Previous2().Should().Be(dir);
        }
    }

    [Fact]
    public void ThreeNext2s_ReturnToOriginal()
    {
        foreach (HexDirection dir in Enum.GetValues<HexDirection>())
        {
            var current = dir;
            for (int i = 0; i < 3; i++)
            {
                current = current.Next2();
            }
            current.Should().Be(dir);
        }
    }
}
