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
}
