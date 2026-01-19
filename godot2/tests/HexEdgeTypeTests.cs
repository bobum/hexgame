using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexEdgeType enum.
/// Verifies Tutorial 3 edge type definitions.
/// </summary>
public class HexEdgeTypeTests
{
    [Fact]
    public void HexEdgeType_HasThreeValues()
    {
        Enum.GetValues<HexEdgeType>().Should().HaveCount(3);
    }

    [Fact]
    public void HexEdgeType_HasFlat()
    {
        Enum.IsDefined(typeof(HexEdgeType), HexEdgeType.Flat).Should().BeTrue();
    }

    [Fact]
    public void HexEdgeType_HasSlope()
    {
        Enum.IsDefined(typeof(HexEdgeType), HexEdgeType.Slope).Should().BeTrue();
    }

    [Fact]
    public void HexEdgeType_HasCliff()
    {
        Enum.IsDefined(typeof(HexEdgeType), HexEdgeType.Cliff).Should().BeTrue();
    }

    [Fact]
    public void HexEdgeType_ValuesAreInOrder()
    {
        ((int)HexEdgeType.Flat).Should().Be(0);
        ((int)HexEdgeType.Slope).Should().Be(1);
        ((int)HexEdgeType.Cliff).Should().Be(2);
    }
}
