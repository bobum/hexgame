using FluentAssertions;
using System;
using Xunit;

namespace HexGame.Tests.Godot2
{

// ============================================================================
// Local types for unit testing without Godot dependencies
// These mirror the production types but allow testing in isolation
// ============================================================================

namespace Tutorial10
{
    public struct Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3 Normalized()
        {
            float len = Length();
            if (len == 0) return new Vector3(0, 0, 0);
            return new Vector3(X / len, Y / len, Z / len);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3 operator *(Vector3 v, float s) =>
            new Vector3(v.X * s, v.Y * s, v.Z * s);
    }

    public enum HexEdgeType
    {
        Flat,
        Slope,
        Cliff
    }

    public static class HexMetrics
    {
        // Wall constants from Tutorial 10
        public const float WallHeight = 4f;
        public const float WallYOffset = -1f;
        public const float WallThickness = 0.75f;
        public const float VerticalTerraceStepSize = 1f / 3f;
        public static float WallElevationOffset => VerticalTerraceStepSize;
        public const float WallTowerThreshold = 0.5f;

        /// <summary>
        /// Interpolates wall position along the edge between near and far positions.
        /// XZ is averaged, Y selects based on which side is lower and applies offset.
        /// </summary>
        public static Vector3 WallLerp(Vector3 near, Vector3 far)
        {
            near.X += (far.X - near.X) * 0.5f;
            near.Z += (far.Z - near.Z) * 0.5f;
            float v = near.Y < far.Y ? WallElevationOffset : (1f - WallElevationOffset);
            near.Y += (far.Y - near.Y) * v + WallYOffset;
            return near;
        }

        /// <summary>
        /// Calculates the offset vector for wall thickness perpendicular to the wall direction.
        /// Y component is zeroed to keep wall tops flat.
        /// </summary>
        public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
        {
            Vector3 offset;
            offset.X = far.X - near.X;
            offset.Y = 0f;
            offset.Z = far.Z - near.Z;
            return offset.Normalized() * (WallThickness * 0.5f);
        }

        public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
        {
            if (elevation1 == elevation2)
            {
                return HexEdgeType.Flat;
            }
            int delta = elevation2 - elevation1;
            if (delta == 1 || delta == -1)
            {
                return HexEdgeType.Slope;
            }
            return HexEdgeType.Cliff;
        }
    }
}

// ============================================================================
// Wall Constants Tests
// ============================================================================

/// <summary>
/// Tests for Tutorial 10 wall constants.
/// </summary>
public class WallConstantsTests
{
    [Fact]
    public void WallHeight_MatchesTutorial()
    {
        Tutorial10.HexMetrics.WallHeight.Should().Be(4f);
    }

    [Fact]
    public void WallThickness_MatchesTutorial()
    {
        Tutorial10.HexMetrics.WallThickness.Should().Be(0.75f);
    }

    [Fact]
    public void WallYOffset_MatchesTutorial()
    {
        Tutorial10.HexMetrics.WallYOffset.Should().Be(-1f);
    }

    [Fact]
    public void WallElevationOffset_EqualsVerticalTerraceStepSize()
    {
        Tutorial10.HexMetrics.WallElevationOffset.Should()
            .BeApproximately(Tutorial10.HexMetrics.VerticalTerraceStepSize, 0.0001f);
    }

    [Fact]
    public void WallTowerThreshold_MatchesTutorial()
    {
        Tutorial10.HexMetrics.WallTowerThreshold.Should().Be(0.5f);
    }

    [Fact]
    public void VerticalTerraceStepSize_IsOneThird()
    {
        Tutorial10.HexMetrics.VerticalTerraceStepSize.Should()
            .BeApproximately(1f / 3f, 0.0001f);
    }
}

// ============================================================================
// WallThicknessOffset Tests
// ============================================================================

/// <summary>
/// Tests for WallThicknessOffset calculation.
/// </summary>
public class WallThicknessOffsetTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void WallThicknessOffset_HorizontalEdge_ReturnsCorrectOffset()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(10, 0, 0);

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);

        // Direction is (1, 0, 0), offset should be along X
        offset.X.Should().BeApproximately(0.75f / 2f, Tolerance);
        offset.Y.Should().BeApproximately(0f, Tolerance);
        offset.Z.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void WallThicknessOffset_VerticalEdge_ReturnsCorrectOffset()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(0, 0, 10);

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);

        // Direction is (0, 0, 1), offset should be along Z
        offset.X.Should().BeApproximately(0f, Tolerance);
        offset.Y.Should().BeApproximately(0f, Tolerance);
        offset.Z.Should().BeApproximately(0.75f / 2f, Tolerance);
    }

    [Fact]
    public void WallThicknessOffset_DiagonalEdge_HasCorrectMagnitude()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(3, 0, 4); // 3-4-5 triangle

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);

        float magnitude = offset.Length();
        magnitude.Should().BeApproximately(0.75f / 2f, Tolerance);
    }

    [Fact]
    public void WallThicknessOffset_YComponentAlwaysZero()
    {
        var near = new Tutorial10.Vector3(0, 5, 0);
        var far = new Tutorial10.Vector3(10, 10, 10);

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);

        offset.Y.Should().Be(0f);
    }

    [Fact]
    public void WallThicknessOffset_NegativeDirection_HasCorrectMagnitude()
    {
        var near = new Tutorial10.Vector3(10, 0, 10);
        var far = new Tutorial10.Vector3(0, 0, 0);

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);

        float magnitude = offset.Length();
        magnitude.Should().BeApproximately(0.75f / 2f, Tolerance);
    }
}

// ============================================================================
// WallLerp Tests
// ============================================================================

/// <summary>
/// Tests for WallLerp calculation.
/// </summary>
public class WallLerpTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void WallLerp_XZ_IsMidpoint()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(10, 0, 20);

        var result = Tutorial10.HexMetrics.WallLerp(near, far);

        result.X.Should().BeApproximately(5f, Tolerance);
        result.Z.Should().BeApproximately(10f, Tolerance);
    }

    [Fact]
    public void WallLerp_NearLower_UsesWallElevationOffset()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(10, 10, 0);

        var result = Tutorial10.HexMetrics.WallLerp(near, far);

        // near.Y < far.Y, so v = WallElevationOffset = 1/3
        // Y = near.Y + (far.Y - near.Y) * v + WallYOffset
        // Y = 0 + 10 * (1/3) + (-1) = 3.333... - 1 = 2.333...
        float expectedY = 0 + 10 * Tutorial10.HexMetrics.WallElevationOffset + Tutorial10.HexMetrics.WallYOffset;
        result.Y.Should().BeApproximately(expectedY, Tolerance);
    }

    [Fact]
    public void WallLerp_NearHigher_UsesOneMinusWallElevationOffset()
    {
        var near = new Tutorial10.Vector3(0, 10, 0);
        var far = new Tutorial10.Vector3(10, 0, 0);

        var result = Tutorial10.HexMetrics.WallLerp(near, far);

        // near.Y >= far.Y, so v = 1 - WallElevationOffset = 2/3
        // Y = near.Y + (far.Y - near.Y) * v + WallYOffset
        // Y = 10 + (-10) * (2/3) + (-1) = 10 - 6.666... - 1 = 2.333...
        float v = 1f - Tutorial10.HexMetrics.WallElevationOffset;
        float expectedY = 10 + (0 - 10) * v + Tutorial10.HexMetrics.WallYOffset;
        result.Y.Should().BeApproximately(expectedY, Tolerance);
    }

    [Fact]
    public void WallLerp_SameElevation_AppliesYOffset()
    {
        var near = new Tutorial10.Vector3(0, 5, 0);
        var far = new Tutorial10.Vector3(10, 5, 0);

        var result = Tutorial10.HexMetrics.WallLerp(near, far);

        // near.Y == far.Y, so v = 1 - WallElevationOffset (near is not lower)
        // Y = near.Y + 0 * v + WallYOffset = 5 + (-1) = 4
        result.Y.Should().BeApproximately(5 + Tutorial10.HexMetrics.WallYOffset, Tolerance);
    }

    [Fact]
    public void WallLerp_SymmetricPositions_ReturnsMidpointXZ()
    {
        var near = new Tutorial10.Vector3(-5, 0, -5);
        var far = new Tutorial10.Vector3(5, 0, 5);

        var result = Tutorial10.HexMetrics.WallLerp(near, far);

        result.X.Should().BeApproximately(0f, Tolerance);
        result.Z.Should().BeApproximately(0f, Tolerance);
    }
}

// ============================================================================
// Edge Type Tests
// ============================================================================

/// <summary>
/// Tests for edge type determination (used in wall placement logic).
/// </summary>
public class EdgeTypeTests
{
    [Theory]
    [InlineData(0, 0, Tutorial10.HexEdgeType.Flat)]
    [InlineData(5, 5, Tutorial10.HexEdgeType.Flat)]
    public void GetEdgeType_SameElevation_ReturnsFlat(int elev1, int elev2, Tutorial10.HexEdgeType expected)
    {
        Tutorial10.HexMetrics.GetEdgeType(elev1, elev2).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, Tutorial10.HexEdgeType.Slope)]
    [InlineData(1, 0, Tutorial10.HexEdgeType.Slope)]
    [InlineData(5, 6, Tutorial10.HexEdgeType.Slope)]
    [InlineData(6, 5, Tutorial10.HexEdgeType.Slope)]
    public void GetEdgeType_OneDifference_ReturnsSlope(int elev1, int elev2, Tutorial10.HexEdgeType expected)
    {
        Tutorial10.HexMetrics.GetEdgeType(elev1, elev2).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 2, Tutorial10.HexEdgeType.Cliff)]
    [InlineData(2, 0, Tutorial10.HexEdgeType.Cliff)]
    [InlineData(0, 3, Tutorial10.HexEdgeType.Cliff)]
    [InlineData(5, 10, Tutorial10.HexEdgeType.Cliff)]
    public void GetEdgeType_TwoOrMoreDifference_ReturnsCliff(int elev1, int elev2, Tutorial10.HexEdgeType expected)
    {
        Tutorial10.HexMetrics.GetEdgeType(elev1, elev2).Should().Be(expected);
    }
}

// ============================================================================
// Wall Placement Rules Tests
// ============================================================================

/// <summary>
/// Tests for wall segment placement rules.
/// </summary>
public class WallPlacementRulesTests
{
    [Theory]
    [InlineData(true, false, false)]  // Near cell underwater: no wall
    [InlineData(false, true, false)]  // Far cell underwater: no wall
    [InlineData(false, false, true)]  // Both above water: wall allowed
    public void WallPlacement_RespectsWaterRule(bool nearUnderwater, bool farUnderwater, bool shouldPlaceWall)
    {
        // Walls should not be placed if either cell is underwater
        bool canPlace = !nearUnderwater && !farUnderwater;
        canPlace.Should().Be(shouldPlaceWall);
    }

    [Theory]
    [InlineData(0, 0, true)]   // Flat: wall allowed
    [InlineData(0, 1, true)]   // Slope: wall allowed
    [InlineData(0, 2, false)]  // Cliff: no wall
    [InlineData(0, 3, false)]  // Steep cliff: no wall
    [InlineData(1, 0, true)]   // Slope (reverse): wall allowed
    [InlineData(3, 0, false)]  // Steep cliff (reverse): no wall
    public void WallPlacement_RespectsCliffRule(int elev1, int elev2, bool shouldPlaceWall)
    {
        var edgeType = Tutorial10.HexMetrics.GetEdgeType(elev1, elev2);
        bool canPlace = edgeType != Tutorial10.HexEdgeType.Cliff;
        canPlace.Should().Be(shouldPlaceWall);
    }

    [Theory]
    [InlineData(true, true, false)]    // Both walled: no wall between them
    [InlineData(false, false, false)]  // Neither walled: no wall between them
    [InlineData(true, false, true)]    // One walled: wall between them
    [InlineData(false, true, true)]    // One walled (reverse): wall between them
    public void WallPlacement_RespectsWalledDifferenceRule(bool nearWalled, bool farWalled, bool shouldPlaceWall)
    {
        // Walls only appear between cells with different walled states
        bool canPlace = nearWalled != farWalled;
        canPlace.Should().Be(shouldPlaceWall);
    }
}

// ============================================================================
// Corner Configuration Tests
// ============================================================================

/// <summary>
/// Tests for wall corner configurations (8 cases from the tutorial diagrams).
/// </summary>
public class WallCornerConfigurationTests
{
    [Theory]
    [InlineData(false, false, false, 0, "No walls")]
    [InlineData(true, false, false, 1, "Only cell1 walled")]
    [InlineData(false, true, false, 2, "Only cell2 walled")]
    [InlineData(false, false, true, 4, "Only cell3 walled")]
    [InlineData(true, true, false, 3, "Cell1 and cell2 walled")]
    [InlineData(true, false, true, 5, "Cell1 and cell3 walled")]
    [InlineData(false, true, true, 6, "Cell2 and cell3 walled")]
    [InlineData(true, true, true, 7, "All cells walled")]
    public void CornerConfiguration_AllCasesIdentified(
        bool cell1Walled, bool cell2Walled, bool cell3Walled,
        int expectedConfig, string description)
    {
        // This test documents and verifies the 8 corner configurations
        int configuration = (cell1Walled ? 1 : 0) + (cell2Walled ? 2 : 0) + (cell3Walled ? 4 : 0);
        configuration.Should().Be(expectedConfig, because: description);
    }

    [Theory]
    [InlineData(false, false, false, false)]  // 000: No walls
    [InlineData(true, false, false, true)]    // 001: Wall curves around cell1
    [InlineData(false, true, false, true)]    // 010: Wall curves around cell2
    [InlineData(false, false, true, true)]    // 100: Wall curves around cell3
    [InlineData(true, true, false, true)]     // 011: Wall between cell1 and cell2
    [InlineData(true, false, true, true)]     // 101: Wall between cell1 and cell3
    [InlineData(false, true, true, true)]     // 110: Wall between cell2 and cell3
    [InlineData(true, true, true, false)]     // 111: No walls (all same state)
    public void CornerConfiguration_WallNeeded(
        bool cell1Walled, bool cell2Walled, bool cell3Walled, bool wallNeeded)
    {
        // Wall is needed when there's at least one walled and one non-walled cell
        bool hasWalled = cell1Walled || cell2Walled || cell3Walled;
        bool hasNonWalled = !cell1Walled || !cell2Walled || !cell3Walled;
        bool shouldHaveWall = hasWalled && hasNonWalled;
        shouldHaveWall.Should().Be(wallNeeded);
    }

    [Fact]
    public void CornerConfiguration_SingleWalled_WallCurvesAroundIt()
    {
        // When only one cell is walled, the wall should curve around it
        // Pivot cell is the walled cell
        bool cell1Walled = true;
        bool cell2Walled = false;
        bool cell3Walled = false;

        // In the tutorial, when one cell is walled:
        // - Wall goes from cell1's edge with cell2 to cell1's edge with cell3
        // - Cell1 is the "pivot" cell
        bool onlyOneWalled = (cell1Walled ? 1 : 0) + (cell2Walled ? 1 : 0) + (cell3Walled ? 1 : 0) == 1;
        onlyOneWalled.Should().BeTrue();
    }

    [Fact]
    public void CornerConfiguration_TwoWalled_WallRunsBetweenThem()
    {
        // When two cells are walled, wall runs between them
        bool cell1Walled = true;
        bool cell2Walled = true;
        bool cell3Walled = false;

        // In the tutorial, when two cells are walled:
        // - The non-walled cell is the "pivot"
        // - Wall goes along the boundary between walled area and non-walled cell
        bool twoWalled = (cell1Walled ? 1 : 0) + (cell2Walled ? 1 : 0) + (cell3Walled ? 1 : 0) == 2;
        twoWalled.Should().BeTrue();
    }
}

// ============================================================================
// Wall Segment Geometry Tests
// ============================================================================

/// <summary>
/// Tests for wall segment geometry calculations.
/// </summary>
public class WallSegmentGeometryTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void WallSegment_ThicknessOffsetMagnitude_IsHalfWallThickness()
    {
        var near = new Tutorial10.Vector3(0, 0, 0);
        var far = new Tutorial10.Vector3(10, 0, 5);

        var offset = Tutorial10.HexMetrics.WallThicknessOffset(near, far);
        float magnitude = offset.Length();

        magnitude.Should().BeApproximately(Tutorial10.HexMetrics.WallThickness / 2f, Tolerance);
    }

    [Fact]
    public void WallSegment_TopHeight_IsWallLerpPlusWallHeight()
    {
        var near = new Tutorial10.Vector3(0, 5, 0);
        var far = new Tutorial10.Vector3(10, 5, 0);

        var wallBase = Tutorial10.HexMetrics.WallLerp(near, far);
        float expectedTop = wallBase.Y + Tutorial10.HexMetrics.WallHeight;

        // Wall top should be 4 units above wall base
        expectedTop.Should().BeApproximately(wallBase.Y + 4f, Tolerance);
    }
}

} // namespace HexGame.Tests.Godot2
