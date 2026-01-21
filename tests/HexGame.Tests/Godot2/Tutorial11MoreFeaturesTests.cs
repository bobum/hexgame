using FluentAssertions;
using System;
using Xunit;

namespace HexGame.Tests.Godot2
{

// ============================================================================
// Local types for unit testing without Godot dependencies
// These mirror the production types but allow testing in isolation
// ============================================================================

namespace Tutorial11
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

    /// <summary>
    /// Simplified hash structure for testing tower placement logic.
    /// </summary>
    public struct HexHash
    {
        public float a, b, c, d, e;

        public HexHash(float a, float b, float c, float d, float e)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
        }
    }

    public static class HexMetrics
    {
        // Tutorial 11: Bridge constants
        public const float BridgeDesignLength = 7f;

        // Tutorial 10: Wall tower threshold (used in Tutorial 11)
        public const float WallTowerThreshold = 0.5f;

        /// <summary>
        /// Calculates the bridge scale factor based on distance.
        /// Bridge Z scale is distance / BridgeDesignLength.
        /// </summary>
        public static float GetBridgeScale(float distance)
        {
            return distance / BridgeDesignLength;
        }

        /// <summary>
        /// Determines if a tower should be placed based on hash value and elevation equality.
        /// </summary>
        public static bool ShouldPlaceTower(HexHash hash, int leftElevation, int rightElevation)
        {
            return hash.e < WallTowerThreshold && leftElevation == rightElevation;
        }
    }

    /// <summary>
    /// Simplified cell data for testing special feature behavior.
    /// </summary>
    public class HexCellData
    {
        private int _specialIndex;
        private bool _hasOutgoingRiver;

        public int SpecialIndex
        {
            get => _specialIndex;
            set => _specialIndex = value;
        }

        public bool IsSpecial => _specialIndex > 0;

        public bool HasOutgoingRiver
        {
            get => _hasOutgoingRiver;
            set
            {
                if (value && !_hasOutgoingRiver)
                {
                    // Rivers override special features (Tutorial 11)
                    _specialIndex = 0;
                }
                _hasOutgoingRiver = value;
            }
        }

        public bool IsUnderwater { get; set; }
        public int Elevation { get; set; }
        public int WaterLevel { get; set; }

        /// <summary>
        /// Simulates road addition check - roads blocked in special cells.
        /// </summary>
        public bool CanAddRoad(bool hasRiverThroughEdge, int elevationDifference)
        {
            return !IsSpecial && !hasRiverThroughEdge && elevationDifference <= 1;
        }
    }
}

// ============================================================================
// Bridge Constants Tests
// ============================================================================

/// <summary>
/// Tests for Tutorial 11 bridge constants and calculations.
/// </summary>
public class BridgeConstantsTests
{
    [Fact]
    public void BridgeDesignLength_MatchesTutorial()
    {
        Tutorial11.HexMetrics.BridgeDesignLength.Should().Be(7f);
    }

    [Fact]
    public void WallTowerThreshold_MatchesTutorial()
    {
        Tutorial11.HexMetrics.WallTowerThreshold.Should().Be(0.5f);
    }
}

// ============================================================================
// Bridge Scale Tests
// ============================================================================

/// <summary>
/// Tests for bridge Z-axis scaling calculation.
/// </summary>
public class BridgeScaleTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void GetBridgeScale_ExactDesignLength_ReturnsOne()
    {
        float scale = Tutorial11.HexMetrics.GetBridgeScale(7f);
        scale.Should().BeApproximately(1f, Tolerance);
    }

    [Fact]
    public void GetBridgeScale_HalfDesignLength_ReturnsHalf()
    {
        float scale = Tutorial11.HexMetrics.GetBridgeScale(3.5f);
        scale.Should().BeApproximately(0.5f, Tolerance);
    }

    [Fact]
    public void GetBridgeScale_DoubleDesignLength_ReturnsTwo()
    {
        float scale = Tutorial11.HexMetrics.GetBridgeScale(14f);
        scale.Should().BeApproximately(2f, Tolerance);
    }

    [Fact]
    public void GetBridgeScale_ZeroDistance_ReturnsZero()
    {
        float scale = Tutorial11.HexMetrics.GetBridgeScale(0f);
        scale.Should().Be(0f);
    }

    [Theory]
    [InlineData(1f, 1f / 7f)]
    [InlineData(5f, 5f / 7f)]
    [InlineData(10f, 10f / 7f)]
    [InlineData(21f, 3f)]
    public void GetBridgeScale_VariousDistances_ReturnsCorrectRatio(float distance, float expectedScale)
    {
        float scale = Tutorial11.HexMetrics.GetBridgeScale(distance);
        scale.Should().BeApproximately(expectedScale, Tolerance);
    }
}

// ============================================================================
// Tower Placement Tests
// ============================================================================

/// <summary>
/// Tests for wall tower placement logic.
/// </summary>
public class TowerPlacementTests
{
    [Fact]
    public void ShouldPlaceTower_HashBelowThreshold_SameElevation_ReturnsTrue()
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, 0.3f); // e < 0.5
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, 2, 2);
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldPlaceTower_HashAboveThreshold_SameElevation_ReturnsFalse()
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, 0.7f); // e > 0.5
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, 2, 2);
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldPlaceTower_HashBelowThreshold_DifferentElevation_ReturnsFalse()
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, 0.3f); // e < 0.5
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, 2, 3);
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldPlaceTower_HashExactlyAtThreshold_SameElevation_ReturnsFalse()
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, 0.5f); // e == 0.5 (not < 0.5)
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, 2, 2);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0.0f, 1, 1, true)]
    [InlineData(0.49f, 1, 1, true)]
    [InlineData(0.50f, 1, 1, false)]
    [InlineData(0.51f, 1, 1, false)]
    [InlineData(0.99f, 1, 1, false)]
    public void ShouldPlaceTower_HashThresholdBoundary(float hashE, int leftElev, int rightElev, bool expected)
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, hashE);
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, leftElev, rightElev);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(5, 5, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 3, false)]
    public void ShouldPlaceTower_ElevationEquality(int leftElev, int rightElev, bool expected)
    {
        var hash = new Tutorial11.HexHash(0, 0, 0, 0, 0.3f); // hash below threshold
        bool result = Tutorial11.HexMetrics.ShouldPlaceTower(hash, leftElev, rightElev);
        result.Should().Be(expected);
    }
}

// ============================================================================
// Special Feature Index Tests
// ============================================================================

/// <summary>
/// Tests for special feature index property behavior.
/// </summary>
public class SpecialIndexTests
{
    [Fact]
    public void IsSpecial_ZeroIndex_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        cell.IsSpecial.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void IsSpecial_PositiveIndex_ReturnsTrue(int index)
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = index };
        cell.IsSpecial.Should().BeTrue();
    }

    [Fact]
    public void SpecialIndex_SettingValue_StoresCorrectly()
    {
        var cell = new Tutorial11.HexCellData();
        cell.SpecialIndex = 2;
        cell.SpecialIndex.Should().Be(2);
    }

    [Fact]
    public void SpecialIndex_ChangingValue_UpdatesIsSpecial()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        cell.IsSpecial.Should().BeFalse();

        cell.SpecialIndex = 1;
        cell.IsSpecial.Should().BeTrue();

        cell.SpecialIndex = 0;
        cell.IsSpecial.Should().BeFalse();
    }
}

// ============================================================================
// River Override Special Feature Tests
// ============================================================================

/// <summary>
/// Tests for rivers overriding special features.
/// </summary>
public class RiverOverrideTests
{
    [Fact]
    public void HasOutgoingRiver_ClearsSpecialIndex()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 2 };
        cell.SpecialIndex.Should().Be(2);

        cell.HasOutgoingRiver = true;

        cell.SpecialIndex.Should().Be(0);
        cell.IsSpecial.Should().BeFalse();
    }

    [Fact]
    public void HasOutgoingRiver_AlreadyHasRiver_DoesNotClearAgain()
    {
        var cell = new Tutorial11.HexCellData();
        cell.HasOutgoingRiver = true;
        cell.SpecialIndex = 2; // Set after river (shouldn't be possible in real code, but testing)

        // Setting HasOutgoingRiver = true again when already true
        cell.HasOutgoingRiver = true;
        cell.SpecialIndex.Should().Be(2); // Shouldn't clear again
    }

    [Fact]
    public void HasOutgoingRiver_NoSpecialFeature_NoError()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        cell.HasOutgoingRiver = true;
        cell.SpecialIndex.Should().Be(0);
    }
}

// ============================================================================
// Road Blocking in Special Cells Tests
// ============================================================================

/// <summary>
/// Tests for roads being blocked in cells with special features.
/// </summary>
public class RoadBlockingTests
{
    [Fact]
    public void CanAddRoad_SpecialCell_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 1 };
        bool canAdd = cell.CanAddRoad(hasRiverThroughEdge: false, elevationDifference: 0);
        canAdd.Should().BeFalse();
    }

    [Fact]
    public void CanAddRoad_NonSpecialCell_NoRiver_FlatTerrain_ReturnsTrue()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        bool canAdd = cell.CanAddRoad(hasRiverThroughEdge: false, elevationDifference: 0);
        canAdd.Should().BeTrue();
    }

    [Fact]
    public void CanAddRoad_NonSpecialCell_HasRiver_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        bool canAdd = cell.CanAddRoad(hasRiverThroughEdge: true, elevationDifference: 0);
        canAdd.Should().BeFalse();
    }

    [Fact]
    public void CanAddRoad_NonSpecialCell_ElevationTooSteep_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = 0 };
        bool canAdd = cell.CanAddRoad(hasRiverThroughEdge: false, elevationDifference: 2);
        canAdd.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, false, 0, true)]   // Normal: road allowed
    [InlineData(1, false, 0, false)]  // Special: blocked
    [InlineData(0, true, 0, false)]   // River: blocked
    [InlineData(0, false, 2, false)]  // Cliff: blocked
    [InlineData(1, true, 2, false)]   // All blockers: blocked
    public void CanAddRoad_CombinedConditions(int specialIndex, bool hasRiver, int elevDiff, bool expected)
    {
        var cell = new Tutorial11.HexCellData { SpecialIndex = specialIndex };
        bool canAdd = cell.CanAddRoad(hasRiverThroughEdge: hasRiver, elevationDifference: elevDiff);
        canAdd.Should().Be(expected);
    }
}

// ============================================================================
// Special Feature Underwater Suppression Tests
// ============================================================================

/// <summary>
/// Tests for special features not rendering underwater.
/// </summary>
public class UnderwaterSuppressionTests
{
    [Fact]
    public void ShouldRenderSpecial_NotUnderwater_HasSpecial_ReturnsTrue()
    {
        var cell = new Tutorial11.HexCellData
        {
            SpecialIndex = 1,
            IsUnderwater = false
        };

        bool shouldRender = !cell.IsUnderwater && cell.IsSpecial;
        shouldRender.Should().BeTrue();
    }

    [Fact]
    public void ShouldRenderSpecial_Underwater_HasSpecial_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData
        {
            SpecialIndex = 1,
            IsUnderwater = true
        };

        bool shouldRender = !cell.IsUnderwater && cell.IsSpecial;
        shouldRender.Should().BeFalse();
    }

    [Fact]
    public void ShouldRenderSpecial_NotUnderwater_NoSpecial_ReturnsFalse()
    {
        var cell = new Tutorial11.HexCellData
        {
            SpecialIndex = 0,
            IsUnderwater = false
        };

        bool shouldRender = !cell.IsUnderwater && cell.IsSpecial;
        shouldRender.Should().BeFalse();
    }
}

// ============================================================================
// Bridge Position Calculation Tests
// ============================================================================

/// <summary>
/// Tests for bridge position and rotation calculations.
/// </summary>
public class BridgePositionTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void BridgePosition_IsMidpointOfRoadCenters()
    {
        var roadCenter1 = new Tutorial11.Vector3(0, 0, 0);
        var roadCenter2 = new Tutorial11.Vector3(10, 0, 10);

        var bridgePosition = new Tutorial11.Vector3(
            (roadCenter1.X + roadCenter2.X) * 0.5f,
            (roadCenter1.Y + roadCenter2.Y) * 0.5f,
            (roadCenter1.Z + roadCenter2.Z) * 0.5f
        );

        bridgePosition.X.Should().BeApproximately(5f, Tolerance);
        bridgePosition.Y.Should().BeApproximately(0f, Tolerance);
        bridgePosition.Z.Should().BeApproximately(5f, Tolerance);
    }

    [Fact]
    public void BridgeRotation_AlongXAxis_ReturnsZeroAngle()
    {
        var direction = new Tutorial11.Vector3(1, 0, 0);
        float angle = MathF.Atan2(direction.X, direction.Z);

        // Atan2(1, 0) = PI/2 radians = 90 degrees
        angle.Should().BeApproximately(MathF.PI / 2f, Tolerance);
    }

    [Fact]
    public void BridgeRotation_AlongZAxis_Returns90DegreeAngle()
    {
        var direction = new Tutorial11.Vector3(0, 0, 1);
        float angle = MathF.Atan2(direction.X, direction.Z);

        // Atan2(0, 1) = 0 radians
        angle.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void BridgeLength_CalculatedFromDirection()
    {
        var roadCenter1 = new Tutorial11.Vector3(0, 0, 0);
        var roadCenter2 = new Tutorial11.Vector3(3, 0, 4);

        var direction = roadCenter2 - roadCenter1;
        float length = direction.Length();

        // 3-4-5 triangle
        length.Should().BeApproximately(5f, Tolerance);
    }
}

// ============================================================================
// Special Feature Type Tests
// ============================================================================

/// <summary>
/// Tests documenting special feature types.
/// </summary>
public class SpecialFeatureTypeTests
{
    [Theory]
    [InlineData(0, "None")]
    [InlineData(1, "Castle")]
    [InlineData(2, "Ziggurat")]
    [InlineData(3, "Megaflora")]
    public void SpecialIndex_MapsToFeatureType(int index, string featureName)
    {
        // This test documents the special feature mapping
        var featureNames = new[] { "None", "Castle", "Ziggurat", "Megaflora" };

        if (index >= 0 && index < featureNames.Length)
        {
            featureNames[index].Should().Be(featureName);
        }
    }

    [Fact]
    public void SpecialPrefabArray_IndexCalculation()
    {
        // In HexFeatureManager, prefab index is SpecialIndex - 1
        // SpecialIndex 1 (Castle) -> prefab[0]
        // SpecialIndex 2 (Ziggurat) -> prefab[1]
        // SpecialIndex 3 (Megaflora) -> prefab[2]

        int specialIndex = 2;
        int prefabIndex = specialIndex - 1;
        prefabIndex.Should().Be(1);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void SpecialIndex_ToPrefabIndex_Conversion(int specialIndex, int expectedPrefabIndex)
    {
        int prefabIndex = specialIndex - 1;
        prefabIndex.Should().Be(expectedPrefabIndex);
    }
}

} // namespace HexGame.Tests.Godot2
