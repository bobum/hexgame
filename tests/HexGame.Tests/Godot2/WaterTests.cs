using FluentAssertions;
using Xunit;

namespace HexGame.Tests.Godot2
{

// Local copies of godot2 types for testing Tutorial 8 without Godot dependencies
namespace Tutorial8
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
        public const float ElevationStep = 3f;

        // Tutorial 8: Water constants
        public const float WaterElevationOffset = -0.5f;
        public const float WaterFactor = 0.6f;
        public const float WaterBlendFactor = 1f - WaterFactor;

        // Tutorial 6: River constant (shared with water)
        public const float StreamBedElevationOffset = -1.75f;

        public const float OuterRadius = 10f;
        public const float InnerRadius = OuterRadius * 0.866025404f;

        public static Vector3[] Corners =
        {
            new Vector3(0f, 0f, OuterRadius),
            new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(0f, 0f, -OuterRadius),
            new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
            new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
            new Vector3(0f, 0f, OuterRadius)
        };

        public static Vector3 GetFirstWaterCorner(int direction)
        {
            return Corners[direction] * WaterFactor;
        }

        public static Vector3 GetSecondWaterCorner(int direction)
        {
            return Corners[direction + 1] * WaterFactor;
        }

        public static Vector3 GetWaterBridge(int direction)
        {
            return (Corners[direction] + Corners[direction + 1]) * WaterBlendFactor;
        }
    }

    /// <summary>
    /// Simplified HexCell for testing water calculations
    /// </summary>
    public class HexCell
    {
        public int Elevation { get; set; }
        public int WaterLevel { get; set; }

        public bool IsUnderwater => WaterLevel > Elevation;

        public float WaterSurfaceY =>
            (WaterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

        public float StreamBedY =>
            (Elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

        public float RiverSurfaceY =>
            (Elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;
    }

    /// <summary>
    /// Waterfall interpolation helper matching the tutorial formula
    /// </summary>
    public static class WaterfallHelper
    {
        /// <summary>
        /// Calculates the interpolation factor for clipping a waterfall at water surface.
        /// Formula: t = (waterY - y2) / (y1 - y2)
        /// </summary>
        public static float CalculateClipFactor(float waterY, float y1, float y2)
        {
            return (waterY - y2) / (y1 - y2);
        }

        /// <summary>
        /// Clips a vertex position to the water surface using interpolation.
        /// </summary>
        public static Vector3 ClipToWaterSurface(Vector3 high, Vector3 low, float t)
        {
            return low.Lerp(high, t);
        }
    }
}

/// <summary>
/// Tests for Tutorial 8 water constants and calculations.
/// </summary>
public class WaterConstantsTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void WaterElevationOffset_MatchesTutorial()
    {
        // Tutorial 8 specifies WaterElevationOffset = -0.5f
        Tutorial8.HexMetrics.WaterElevationOffset.Should().Be(-0.5f);
    }

    [Fact]
    public void WaterFactor_MatchesTutorial()
    {
        // Tutorial 8 specifies WaterFactor = 0.6f
        Tutorial8.HexMetrics.WaterFactor.Should().Be(0.6f);
    }

    [Fact]
    public void WaterBlendFactor_IsOneMinusWaterFactor()
    {
        // WaterBlendFactor = 1 - WaterFactor = 0.4
        Tutorial8.HexMetrics.WaterBlendFactor.Should().BeApproximately(0.4f, Tolerance);
    }

    [Fact]
    public void WaterFactor_Plus_WaterBlendFactor_EqualsOne()
    {
        var sum = Tutorial8.HexMetrics.WaterFactor + Tutorial8.HexMetrics.WaterBlendFactor;
        sum.Should().BeApproximately(1f, Tolerance);
    }
}

/// <summary>
/// Tests for HexCell water properties.
/// </summary>
public class HexCellWaterTests
{
    private const float Tolerance = 0.0001f;

    [Theory]
    [InlineData(0, 1, true)]   // Water level above elevation
    [InlineData(0, 2, true)]   // Water level significantly above
    [InlineData(1, 1, false)]  // Water level equals elevation
    [InlineData(2, 1, false)]  // Water level below elevation
    [InlineData(5, 3, false)]  // High elevation, low water
    public void IsUnderwater_ReturnsCorrectValue(int elevation, int waterLevel, bool expectedUnderwater)
    {
        var cell = new Tutorial8.HexCell
        {
            Elevation = elevation,
            WaterLevel = waterLevel
        };

        cell.IsUnderwater.Should().Be(expectedUnderwater);
    }

    [Theory]
    [InlineData(0, -1.5f)]   // WaterLevel 0: (0 + -0.5) * 3 = -1.5
    [InlineData(1, 1.5f)]    // WaterLevel 1: (1 + -0.5) * 3 = 1.5
    [InlineData(2, 4.5f)]    // WaterLevel 2: (2 + -0.5) * 3 = 4.5
    [InlineData(5, 13.5f)]   // WaterLevel 5: (5 + -0.5) * 3 = 13.5
    public void WaterSurfaceY_CalculatesCorrectly(int waterLevel, float expectedY)
    {
        var cell = new Tutorial8.HexCell { WaterLevel = waterLevel };

        cell.WaterSurfaceY.Should().BeApproximately(expectedY, Tolerance);
    }

    [Theory]
    [InlineData(0, -1.5f)]   // Elevation 0: (0 + -0.5) * 3 = -1.5
    [InlineData(1, 1.5f)]    // Elevation 1: (1 + -0.5) * 3 = 1.5
    [InlineData(3, 7.5f)]    // Elevation 3: (3 + -0.5) * 3 = 7.5
    public void RiverSurfaceY_UsesWaterElevationOffset(int elevation, float expectedY)
    {
        var cell = new Tutorial8.HexCell { Elevation = elevation };

        cell.RiverSurfaceY.Should().BeApproximately(expectedY, Tolerance);
    }

    [Theory]
    [InlineData(0, -5.25f)]  // Elevation 0: (0 + -1.75) * 3 = -5.25
    [InlineData(1, -2.25f)]  // Elevation 1: (1 + -1.75) * 3 = -2.25
    [InlineData(2, 0.75f)]   // Elevation 2: (2 + -1.75) * 3 = 0.75
    public void StreamBedY_CalculatesCorrectly(int elevation, float expectedY)
    {
        var cell = new Tutorial8.HexCell { Elevation = elevation };

        cell.StreamBedY.Should().BeApproximately(expectedY, Tolerance);
    }
}

/// <summary>
/// Tests for water corner calculations.
/// </summary>
public class WaterCornerTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void GetFirstWaterCorner_ScalesByWaterFactor()
    {
        // Direction 0 (NE): Corner is (0, 0, OuterRadius)
        // Water corner = corner * 0.6
        var waterCorner = Tutorial8.HexMetrics.GetFirstWaterCorner(0);

        waterCorner.X.Should().BeApproximately(0f, Tolerance);
        waterCorner.Y.Should().BeApproximately(0f, Tolerance);
        waterCorner.Z.Should().BeApproximately(10f * 0.6f, Tolerance); // 6
    }

    [Fact]
    public void GetSecondWaterCorner_ScalesByWaterFactor()
    {
        // Direction 0 (NE): Second corner is (InnerRadius, 0, 0.5 * OuterRadius)
        // Water corner = corner * 0.6
        var waterCorner = Tutorial8.HexMetrics.GetSecondWaterCorner(0);
        var innerRadius = 10f * 0.866025404f;

        waterCorner.X.Should().BeApproximately(innerRadius * 0.6f, Tolerance);
        waterCorner.Y.Should().BeApproximately(0f, Tolerance);
        waterCorner.Z.Should().BeApproximately(5f * 0.6f, Tolerance); // 3
    }

    [Fact]
    public void GetWaterBridge_ScalesByWaterBlendFactor()
    {
        // Direction 0: Bridge = (corner0 + corner1) * WaterBlendFactor
        var bridge = Tutorial8.HexMetrics.GetWaterBridge(0);

        // Corner0 = (0, 0, 10), Corner1 = (InnerRadius, 0, 5)
        // Sum = (InnerRadius, 0, 15)
        // Bridge = Sum * 0.4
        var innerRadius = 10f * 0.866025404f;
        bridge.X.Should().BeApproximately(innerRadius * 0.4f, Tolerance);
        bridge.Z.Should().BeApproximately(15f * 0.4f, Tolerance); // 6
    }

    [Fact]
    public void WaterCorners_AreSmallerThanSolidCorners()
    {
        // Water corners use WaterFactor (0.6), solid corners use SolidFactor (0.8)
        var waterCorner = Tutorial8.HexMetrics.GetFirstWaterCorner(0);
        var solidCornerZ = 10f * 0.8f; // OuterRadius * SolidFactor

        // Water corner Z should be smaller
        waterCorner.Z.Should().BeLessThan(solidCornerZ);
    }
}

/// <summary>
/// Tests for waterfall interpolation calculations.
/// </summary>
public class WaterfallInterpolationTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void CalculateClipFactor_WhenWaterAtMidpoint_ReturnsHalf()
    {
        float y1 = 10f;  // High point (river surface)
        float y2 = 0f;   // Low point (below water)
        float waterY = 5f; // Water surface at midpoint

        // t = (5 - 0) / (10 - 0) = 0.5
        var t = Tutorial8.WaterfallHelper.CalculateClipFactor(waterY, y1, y2);

        t.Should().BeApproximately(0.5f, Tolerance);
    }

    [Fact]
    public void CalculateClipFactor_WhenWaterAtHigh_ReturnsOne()
    {
        float y1 = 10f;
        float y2 = 0f;
        float waterY = 10f; // Water at high point

        // t = (10 - 0) / (10 - 0) = 1.0
        var t = Tutorial8.WaterfallHelper.CalculateClipFactor(waterY, y1, y2);

        t.Should().BeApproximately(1f, Tolerance);
    }

    [Fact]
    public void CalculateClipFactor_WhenWaterAtLow_ReturnsZero()
    {
        float y1 = 10f;
        float y2 = 0f;
        float waterY = 0f; // Water at low point

        // t = (0 - 0) / (10 - 0) = 0.0
        var t = Tutorial8.WaterfallHelper.CalculateClipFactor(waterY, y1, y2);

        t.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void CalculateClipFactor_WithRealElevations_CalculatesCorrectly()
    {
        // Simulating river flowing from elevation 2 into water at level 1
        // y1 = river surface at elevation 2 = (2 - 0.5) * 3 = 4.5
        // y2 = river surface at elevation 0 = (0 - 0.5) * 3 = -1.5
        // waterY = water surface at level 1 = (1 - 0.5) * 3 = 1.5
        float y1 = 4.5f;
        float y2 = -1.5f;
        float waterY = 1.5f;

        // t = (1.5 - (-1.5)) / (4.5 - (-1.5)) = 3.0 / 6.0 = 0.5
        var t = Tutorial8.WaterfallHelper.CalculateClipFactor(waterY, y1, y2);

        t.Should().BeApproximately(0.5f, Tolerance);
    }

    [Fact]
    public void ClipToWaterSurface_InterpolatesCorrectly()
    {
        var high = new Tutorial8.Vector3(0f, 10f, 0f);
        var low = new Tutorial8.Vector3(0f, 0f, 10f);
        float t = 0.5f;

        var clipped = Tutorial8.WaterfallHelper.ClipToWaterSurface(high, low, t);

        // Lerp from low to high by 0.5
        // X: 0 + (0-0) * 0.5 = 0
        // Y: 0 + (10-0) * 0.5 = 5
        // Z: 10 + (0-10) * 0.5 = 5
        clipped.X.Should().BeApproximately(0f, Tolerance);
        clipped.Y.Should().BeApproximately(5f, Tolerance);
        clipped.Z.Should().BeApproximately(5f, Tolerance);
    }

    [Fact]
    public void ClipToWaterSurface_WhenTIsZero_ReturnsLow()
    {
        var high = new Tutorial8.Vector3(10f, 10f, 10f);
        var low = new Tutorial8.Vector3(0f, 0f, 0f);

        var clipped = Tutorial8.WaterfallHelper.ClipToWaterSurface(high, low, 0f);

        clipped.X.Should().BeApproximately(0f, Tolerance);
        clipped.Y.Should().BeApproximately(0f, Tolerance);
        clipped.Z.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void ClipToWaterSurface_WhenTIsOne_ReturnsHigh()
    {
        var high = new Tutorial8.Vector3(10f, 10f, 10f);
        var low = new Tutorial8.Vector3(0f, 0f, 0f);

        var clipped = Tutorial8.WaterfallHelper.ClipToWaterSurface(high, low, 1f);

        clipped.X.Should().BeApproximately(10f, Tolerance);
        clipped.Y.Should().BeApproximately(10f, Tolerance);
        clipped.Z.Should().BeApproximately(10f, Tolerance);
    }
}

/// <summary>
/// Tests for river validation with water bodies.
/// </summary>
public class RiverWaterValidationTests
{
    [Theory]
    [InlineData(2, 1, 0, true)]   // Elevation 2 to 1 (downhill): valid
    [InlineData(2, 2, 0, true)]   // Elevation 2 to 2 (flat): valid
    [InlineData(1, 2, 0, false)]  // Elevation 1 to 2 (uphill): invalid
    [InlineData(2, 0, 0, true)]   // Elevation 2 to 0 (downhill into water): valid
    [InlineData(1, 0, 0, true)]   // Elevation 1 to 0 (downhill into water): valid - rivers can flow into lakes
    [InlineData(0, 2, 2, true)]   // Elevation 0 to 2, source water level 2: valid (waterLevel matches dest elevation)
    [InlineData(0, 3, 2, false)]  // Elevation 0 to 3, source water level 2: invalid (uphill, water doesn't match)
    public void IsValidRiverDestination_ConsidersWaterLevel(
        int sourceElevation, int destElevation, int sourceWaterLevel, bool expectedValid)
    {
        // Tutorial validation logic:
        // return neighbor != null && (elevation >= neighbor.Elevation || waterLevel == neighbor.Elevation);
        // A river can flow if:
        // 1. Source elevation >= destination elevation (downhill or flat)
        // 2. OR source's water level equals destination's elevation (river flowing from water body)
        bool actualValid = (sourceElevation >= destElevation) || (sourceWaterLevel == destElevation);

        actualValid.Should().Be(expectedValid);
    }
}

/// <summary>
/// Tests for underwater cell behavior.
/// </summary>
public class UnderwaterCellTests
{
    [Fact]
    public void UnderwaterCell_RiverSurface_IsBelowWaterSurface()
    {
        var cell = new Tutorial8.HexCell
        {
            Elevation = 0,
            WaterLevel = 2
        };

        // River surface: (0 + -0.5) * 3 = -1.5
        // Water surface: (2 + -0.5) * 3 = 4.5
        cell.RiverSurfaceY.Should().BeLessThan(cell.WaterSurfaceY);
    }

    [Fact]
    public void UnderwaterCell_StreamBed_IsBelowRiverSurface()
    {
        var cell = new Tutorial8.HexCell
        {
            Elevation = 1,
            WaterLevel = 2
        };

        // Stream bed: (1 + -1.75) * 3 = -2.25
        // River surface: (1 + -0.5) * 3 = 1.5
        cell.StreamBedY.Should().BeLessThan(cell.RiverSurfaceY);
    }

    [Fact]
    public void UnderwaterCell_HeightRelationship_IsCorrect()
    {
        var cell = new Tutorial8.HexCell
        {
            Elevation = 1,
            WaterLevel = 3
        };

        // From lowest to highest:
        // StreamBedY < RiverSurfaceY < WaterSurfaceY (when underwater)
        cell.StreamBedY.Should().BeLessThan(cell.RiverSurfaceY);
        cell.RiverSurfaceY.Should().BeLessThan(cell.WaterSurfaceY);
    }
}

} // namespace HexGame.Tests.Godot2
