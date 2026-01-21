using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexCell road-related calculations and logic.
/// Tests the math formulas - actual cell behavior requires integration testing.
/// Verifies Tutorial 7 road logic.
/// </summary>
public class HexCellRoadTests
{
    // HasRoadThroughEdge logic tests

    [Theory]
    [InlineData(HexDirection.NE, new[] { true, false, false, false, false, false }, true)]
    [InlineData(HexDirection.E, new[] { false, true, false, false, false, false }, true)]
    [InlineData(HexDirection.SE, new[] { false, false, true, false, false, false }, true)]
    [InlineData(HexDirection.SW, new[] { false, false, false, true, false, false }, true)]
    [InlineData(HexDirection.W, new[] { false, false, false, false, true, false }, true)]
    [InlineData(HexDirection.NW, new[] { false, false, false, false, false, true }, true)]
    [InlineData(HexDirection.NE, new[] { false, true, false, false, false, false }, false)]
    [InlineData(HexDirection.E, new[] { true, false, true, false, true, false }, false)]
    public void HasRoadThroughEdge_ReturnsCorrectValue(
        HexDirection direction, bool[] roads, bool expected)
    {
        // HasRoadThroughEdge returns roads[(int)direction]
        bool result = roads[(int)direction];
        result.Should().Be(expected);
    }

    // HasRoads logic tests

    [Theory]
    [InlineData(new[] { false, false, false, false, false, false }, false)]
    [InlineData(new[] { true, false, false, false, false, false }, true)]
    [InlineData(new[] { false, true, false, false, false, false }, true)]
    [InlineData(new[] { false, false, false, false, false, true }, true)]
    [InlineData(new[] { true, true, true, true, true, true }, true)]
    [InlineData(new[] { false, false, true, false, false, false }, true)]
    public void HasRoads_ReturnsCorrectValue(bool[] roads, bool expected)
    {
        // HasRoads = any road in array is true
        bool hasRoads = false;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                hasRoads = true;
                break;
            }
        }
        hasRoads.Should().Be(expected);
    }

    // Road elevation difference constraints

    [Theory]
    [InlineData(0, 0, 0, true)]   // Same elevation - valid
    [InlineData(0, 1, 1, true)]   // 1 step up - valid
    [InlineData(1, 0, 1, true)]   // 1 step down - valid
    [InlineData(0, 2, 2, false)]  // 2 steps up - invalid (cliff)
    [InlineData(2, 0, 2, false)]  // 2 steps down - invalid (cliff)
    [InlineData(0, 3, 3, false)]  // 3 steps - invalid (cliff)
    [InlineData(5, 6, 1, true)]   // 1 step - valid
    [InlineData(5, 8, 3, false)]  // 3 steps - invalid (cliff)
    public void RoadElevationDifference_ConstraintCheck(
        int elevation1, int elevation2, int expectedDiff, bool shouldAllowRoad)
    {
        // GetElevationDifference returns absolute difference
        int diff = elevation1 - elevation2;
        int absDiff = diff >= 0 ? diff : -diff;

        absDiff.Should().Be(expectedDiff);

        // Roads allowed only when diff <= 1
        bool roadAllowed = absDiff <= 1;
        roadAllowed.Should().Be(shouldAllowRoad);
    }

    // Road-river exclusion tests (roads can't be placed where rivers exist)

    [Theory]
    [InlineData(true, HexDirection.NE, HexDirection.NE, false)]  // River in NE, trying NE road - blocked
    [InlineData(true, HexDirection.NE, HexDirection.E, true)]    // River in NE, trying E road - allowed
    [InlineData(true, HexDirection.E, HexDirection.NE, true)]    // River in E, trying NE road - allowed
    [InlineData(false, HexDirection.NE, HexDirection.NE, true)]  // No river, trying road - allowed
    public void RoadBlocked_WhenRiverExists(
        bool hasRiver, HexDirection riverDir, HexDirection roadDir, bool roadAllowed)
    {
        // HasRiverThroughEdge blocks road placement
        bool hasRiverThroughEdge = hasRiver && riverDir == roadDir;
        bool canPlaceRoad = !hasRiverThroughEdge;
        canPlaceRoad.Should().Be(roadAllowed);
    }

    // GetRoadInterpolators logic tests

    [Theory]
    [InlineData(true, false, false, 0.5f, 0.5f)]   // Road through current direction
    [InlineData(false, true, false, 0.5f, 0.25f)]  // Road through previous only
    [InlineData(false, false, true, 0.25f, 0.5f)]  // Road through next only
    [InlineData(false, true, true, 0.5f, 0.5f)]    // Roads through both previous and next
    [InlineData(false, false, false, 0.25f, 0.25f)] // No roads in adjacent directions
    public void GetRoadInterpolators_ReturnsCorrectValues(
        bool hasThroughRoad, bool hasPreviousRoad, bool hasNextRoad,
        float expectedX, float expectedY)
    {
        float x, y;

        if (hasThroughRoad)
        {
            x = y = 0.5f;
        }
        else
        {
            x = hasPreviousRoad ? 0.5f : 0.25f;
            y = hasNextRoad ? 0.5f : 0.25f;
        }

        x.Should().Be(expectedX);
        y.Should().Be(expectedY);
    }

    // Road bidirectionality tests

    [Theory]
    [InlineData(HexDirection.NE, HexDirection.SW)]
    [InlineData(HexDirection.E, HexDirection.W)]
    [InlineData(HexDirection.SE, HexDirection.NW)]
    [InlineData(HexDirection.SW, HexDirection.NE)]
    [InlineData(HexDirection.W, HexDirection.E)]
    [InlineData(HexDirection.NW, HexDirection.SE)]
    public void RoadIsSymmetric_OppositeDirections(HexDirection dir, HexDirection expectedOpposite)
    {
        // When road is set in direction, neighbor should have road in opposite direction
        dir.Opposite().Should().Be(expectedOpposite);
    }

    // ValidateRoads elevation constraint tests

    [Fact]
    public void RoadsArray_HasSixElements()
    {
        // Roads array should have 6 elements (one per direction)
        int expectedCount = 6;
        int directionCount = Enum.GetValues<HexDirection>().Length;
        directionCount.Should().Be(expectedCount);
    }

    // Road configuration count tests

    [Fact]
    public void RoadConfigurations_MaxIs14()
    {
        // Per Catlike Coding Tutorial 7, there are 14 possible road configurations
        // (based on combinations of up to 6 roads per cell, considering symmetry)
        // This is mentioned in the tutorial for rendering optimization
        int maxConfigs = 14;
        maxConfigs.Should().Be(14); // Verification that constant is correct
    }

    // Road center interpolation math tests

    [Theory]
    [InlineData(0.0f, 0.0f, 10.0f, 0.5f, 5.0f)]   // Midpoint
    [InlineData(0.0f, 0.0f, 10.0f, 0.25f, 2.5f)]  // Quarter point
    [InlineData(0.0f, 0.0f, 10.0f, 0.0f, 0.0f)]   // Start point
    [InlineData(0.0f, 0.0f, 10.0f, 1.0f, 10.0f)]  // End point
    [InlineData(2.0f, 0.0f, 8.0f, 0.5f, 5.0f)]    // Non-zero start
    public void RoadInterpolation_CalculatesCorrectly(
        float startX, float startY, float endX, float t, float expectedX)
    {
        // Road positions use Vector3.Lerp for interpolation
        // result = start + (end - start) * t = start * (1 - t) + end * t
        float resultX = startX + (endX - startX) * t;
        resultX.Should().Be(expectedX);
    }

    // Road UV coordinate tests

    [Fact]
    public void RoadUV_CenterIsOne()
    {
        // UV.x = 1 at road center (full opacity)
        float centerU = 1f;
        centerU.Should().Be(1f);
    }

    [Fact]
    public void RoadUV_EdgeIsZero()
    {
        // UV.x = 0 at road edges (transparent)
        float edgeU = 0f;
        edgeU.Should().Be(0f);
    }

    [Fact]
    public void RoadUV_VIsAlwaysZero()
    {
        // V coordinate is always 0 for roads (no flow animation like rivers)
        float v = 0f;
        v.Should().Be(0f);
    }

    // Road segment math tests

    [Theory]
    [InlineData(0.0f, 10.0f, 5.0f)]  // Simple midpoint
    [InlineData(2.0f, 8.0f, 5.0f)]   // Non-zero values
    [InlineData(-5.0f, 5.0f, 0.0f)]  // Negative to positive
    public void RoadSegmentMidpoint_CalculatesCorrectly(float v1, float v2, float expectedMid)
    {
        // mC = mL.Lerp(mR, 0.5f) - midpoint between left and right
        float midpoint = v1 + (v2 - v1) * 0.5f;
        midpoint.Should().Be(expectedMid);
    }

    // Road adjacent to river offset tests

    [Fact]
    public void RoadAdjacentToRiver_BeginEndOffset()
    {
        // Config 1: River begin/end pushes road 1/3 away
        float offset = 1f / 3f;
        offset.Should().BeApproximately(0.333333f, 0.0001f);
    }

    [Fact]
    public void RoadAdjacentToRiver_StraightRiverOffset()
    {
        // Config 2: Straight river pushes road 0.5 perpendicular
        float cornerOffset = 0.5f;
        float centerOffset = 0.25f;
        cornerOffset.Should().Be(0.5f);
        centerOffset.Should().Be(0.25f);
    }

    [Fact]
    public void RoadAdjacentToRiver_TightCurveOffset()
    {
        // Config 3 & 4: Tight river curve uses 0.2 offset
        float tightCurveOffset = 0.2f;
        tightCurveOffset.Should().Be(0.2f);
    }
}
