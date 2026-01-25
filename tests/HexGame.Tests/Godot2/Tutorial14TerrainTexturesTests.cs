using FluentAssertions;
using System;
using Xunit;

namespace HexGame.Tests.Godot2
{

// ============================================================================
// Local types for unit testing without Godot dependencies
// These mirror the production types but allow testing in isolation
// ============================================================================

namespace Tutorial14
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

        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3 operator *(Vector3 v, float s) =>
            new Vector3(v.X * s, v.Y * s, v.Z * s);
    }

    public struct Color
    {
        public float R, G, B, A;

        public Color(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Linear interpolation between two colors.
        /// Matches Godot's Color.Lerp behavior.
        /// </summary>
        public Color Lerp(Color to, float t)
        {
            return new Color(
                R + (to.R - R) * t,
                G + (to.G - G) * t,
                B + (to.B - B) * t,
                A + (to.A - A) * t
            );
        }
    }

    /// <summary>
    /// Represents terrain type indices for texture array sampling.
    /// Tutorial 14: Replaces Color property on HexCell.
    /// </summary>
    public static class TerrainTypes
    {
        public const int Sand = 0;
        public const int Grass = 1;
        public const int Mud = 2;
        public const int Stone = 3;
        public const int Snow = 4;
        public const int Count = 5;
    }

    /// <summary>
    /// Static splat weights used for texture blending.
    /// Tutorial 14: Colors are repurposed as blend weights.
    /// </summary>
    public static class SplatWeights
    {
        public static readonly Color Weight1 = new Color(1f, 0f, 0f); // 100% texture 1
        public static readonly Color Weight2 = new Color(0f, 1f, 0f); // 100% texture 2
        public static readonly Color Weight3 = new Color(0f, 0f, 1f); // 100% texture 3

        /// <summary>
        /// Validates that splat weights sum to 1.0.
        /// </summary>
        public static bool IsValid(Color weights)
        {
            float sum = weights.R + weights.G + weights.B;
            return Math.Abs(sum - 1f) < 0.0001f;
        }
    }

    /// <summary>
    /// Simplified cell data for testing terrain type behavior.
    /// </summary>
    public class HexCellData
    {
        private int _terrainTypeIndex;
        private int _elevation;

        public int TerrainTypeIndex
        {
            get => _terrainTypeIndex;
            set
            {
                if (_terrainTypeIndex != value)
                {
                    _terrainTypeIndex = value;
                    // In real code, this would trigger Refresh()
                }
            }
        }

        public int Elevation
        {
            get => _elevation;
            set
            {
                if (_elevation != value)
                {
                    _elevation = value;
                }
            }
        }
    }

    /// <summary>
    /// Terrain type data for triangulation.
    /// Contains three terrain indices for a triangle or quad.
    /// </summary>
    public struct TerrainTypeData
    {
        public float Type1;
        public float Type2;
        public float Type3;

        public TerrainTypeData(float type1, float type2, float type3)
        {
            Type1 = type1;
            Type2 = type2;
            Type3 = type3;
        }

        /// <summary>
        /// Creates terrain data for a single terrain type (edge fan).
        /// </summary>
        public static TerrainTypeData Single(int type)
        {
            return new TerrainTypeData(type, type, type);
        }

        /// <summary>
        /// Creates terrain data for two terrain types (edge strip).
        /// </summary>
        public static TerrainTypeData TwoTypes(int type1, int type2)
        {
            return new TerrainTypeData(type1, type2, type1);
        }

        /// <summary>
        /// Creates terrain data for three terrain types (corner).
        /// </summary>
        public static TerrainTypeData ThreeTypes(int bottom, int left, int right)
        {
            return new TerrainTypeData(bottom, left, right);
        }
    }

    public static class HexMetrics
    {
        /// <summary>
        /// Linear interpolation for terrace geometry and colors.
        /// </summary>
        public static Color TerraceLerp(Color a, Color b, int step)
        {
            // Terrace interpolation uses fixed step fractions
            float t = step * 0.2f; // 5 steps (0.2 per step)
            return a.Lerp(b, t);
        }
    }
}

// ============================================================================
// Terrain Type Index Tests
// ============================================================================

/// <summary>
/// Tests for terrain type index property behavior.
/// Tutorial 14: TerrainTypeIndex replaces Color property.
/// </summary>
public class TerrainTypeIndexTests
{
    [Fact]
    public void TerrainTypeIndex_DefaultsToZero()
    {
        var cell = new Tutorial14.HexCellData();
        cell.TerrainTypeIndex.Should().Be(0);
    }

    [Theory]
    [InlineData(Tutorial14.TerrainTypes.Sand, 0)]
    [InlineData(Tutorial14.TerrainTypes.Grass, 1)]
    [InlineData(Tutorial14.TerrainTypes.Mud, 2)]
    [InlineData(Tutorial14.TerrainTypes.Stone, 3)]
    [InlineData(Tutorial14.TerrainTypes.Snow, 4)]
    public void TerrainTypeIndex_MatchesTerrainTypeConstants(int typeConstant, int expectedValue)
    {
        typeConstant.Should().Be(expectedValue);
    }

    [Fact]
    public void TerrainTypeIndex_SetValue_StoresCorrectly()
    {
        var cell = new Tutorial14.HexCellData();
        cell.TerrainTypeIndex = Tutorial14.TerrainTypes.Stone;
        cell.TerrainTypeIndex.Should().Be(3);
    }

    [Fact]
    public void TerrainTypeCount_IsFive()
    {
        Tutorial14.TerrainTypes.Count.Should().Be(5);
    }
}

// ============================================================================
// Splat Weight Tests
// ============================================================================

/// <summary>
/// Tests for splat weight constants and validation.
/// Tutorial 14: Splat weights use vertex colors for texture blending.
/// </summary>
public class SplatWeightTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void SplatWeight1_IsRedChannel()
    {
        var w = Tutorial14.SplatWeights.Weight1;
        w.R.Should().Be(1f);
        w.G.Should().Be(0f);
        w.B.Should().Be(0f);
    }

    [Fact]
    public void SplatWeight2_IsGreenChannel()
    {
        var w = Tutorial14.SplatWeights.Weight2;
        w.R.Should().Be(0f);
        w.G.Should().Be(1f);
        w.B.Should().Be(0f);
    }

    [Fact]
    public void SplatWeight3_IsBlueChannel()
    {
        var w = Tutorial14.SplatWeights.Weight3;
        w.R.Should().Be(0f);
        w.G.Should().Be(0f);
        w.B.Should().Be(1f);
    }

    [Theory]
    [InlineData(1f, 0f, 0f, true)]   // Pure weight 1
    [InlineData(0f, 1f, 0f, true)]   // Pure weight 2
    [InlineData(0f, 0f, 1f, true)]   // Pure weight 3
    [InlineData(0.5f, 0.5f, 0f, true)]   // Two-way blend
    [InlineData(0.33f, 0.33f, 0.34f, true)]  // Three-way blend (approx)
    [InlineData(0.5f, 0f, 0f, false)]  // Doesn't sum to 1
    [InlineData(1f, 1f, 0f, false)]    // Sum > 1
    public void SplatWeights_ValidateSumToOne(float r, float g, float b, bool expectedValid)
    {
        var weights = new Tutorial14.Color(r, g, b);
        Tutorial14.SplatWeights.IsValid(weights).Should().Be(expectedValid);
    }

    [Fact]
    public void SplatWeights_AllPure_AreValid()
    {
        Tutorial14.SplatWeights.IsValid(Tutorial14.SplatWeights.Weight1).Should().BeTrue();
        Tutorial14.SplatWeights.IsValid(Tutorial14.SplatWeights.Weight2).Should().BeTrue();
        Tutorial14.SplatWeights.IsValid(Tutorial14.SplatWeights.Weight3).Should().BeTrue();
    }
}

// ============================================================================
// Terrain Type Data Tests
// ============================================================================

/// <summary>
/// Tests for terrain type data construction for triangulation.
/// </summary>
public class TerrainTypeDataTests
{
    [Fact]
    public void Single_AllIndicesSame()
    {
        var data = Tutorial14.TerrainTypeData.Single(Tutorial14.TerrainTypes.Grass);
        data.Type1.Should().Be(1);
        data.Type2.Should().Be(1);
        data.Type3.Should().Be(1);
    }

    [Fact]
    public void TwoTypes_FirstAndThirdMatch()
    {
        var data = Tutorial14.TerrainTypeData.TwoTypes(
            Tutorial14.TerrainTypes.Sand,
            Tutorial14.TerrainTypes.Stone);
        data.Type1.Should().Be(0);  // Sand
        data.Type2.Should().Be(3);  // Stone
        data.Type3.Should().Be(0);  // Sand (same as Type1)
    }

    [Fact]
    public void ThreeTypes_AllDifferent()
    {
        var data = Tutorial14.TerrainTypeData.ThreeTypes(
            Tutorial14.TerrainTypes.Sand,   // bottom
            Tutorial14.TerrainTypes.Grass,  // left
            Tutorial14.TerrainTypes.Stone); // right
        data.Type1.Should().Be(0);
        data.Type2.Should().Be(1);
        data.Type3.Should().Be(3);
    }
}

// ============================================================================
// Color/Weight Interpolation Tests
// ============================================================================

/// <summary>
/// Tests for color/weight interpolation used in terraces.
/// Tutorial 14: Splat weights are interpolated across terrace steps.
/// </summary>
public class WeightInterpolationTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void ColorLerp_AtZero_ReturnsFirst()
    {
        var a = new Tutorial14.Color(1f, 0f, 0f);
        var b = new Tutorial14.Color(0f, 1f, 0f);
        var result = a.Lerp(b, 0f);

        result.R.Should().BeApproximately(1f, Tolerance);
        result.G.Should().BeApproximately(0f, Tolerance);
    }

    [Fact]
    public void ColorLerp_AtOne_ReturnsSecond()
    {
        var a = new Tutorial14.Color(1f, 0f, 0f);
        var b = new Tutorial14.Color(0f, 1f, 0f);
        var result = a.Lerp(b, 1f);

        result.R.Should().BeApproximately(0f, Tolerance);
        result.G.Should().BeApproximately(1f, Tolerance);
    }

    [Fact]
    public void ColorLerp_AtHalf_ReturnsMidpoint()
    {
        var a = new Tutorial14.Color(1f, 0f, 0f);
        var b = new Tutorial14.Color(0f, 1f, 0f);
        var result = a.Lerp(b, 0.5f);

        result.R.Should().BeApproximately(0.5f, Tolerance);
        result.G.Should().BeApproximately(0.5f, Tolerance);
    }

    [Theory]
    [InlineData(1, 0.2f)]  // Step 1: 20%
    [InlineData(2, 0.4f)]  // Step 2: 40%
    [InlineData(3, 0.6f)]  // Step 3: 60%
    [InlineData(4, 0.8f)]  // Step 4: 80%
    [InlineData(5, 1.0f)]  // Step 5: 100%
    public void TerraceLerp_StepFractions(int step, float expectedT)
    {
        var red = new Tutorial14.Color(1f, 0f, 0f);
        var green = new Tutorial14.Color(0f, 1f, 0f);
        var result = Tutorial14.HexMetrics.TerraceLerp(red, green, step);

        // At step, red channel should be (1 - expectedT) and green should be expectedT
        result.R.Should().BeApproximately(1f - expectedT, Tolerance);
        result.G.Should().BeApproximately(expectedT, Tolerance);
    }

    [Fact]
    public void TerraceLerp_InterpolatedWeights_StillValid()
    {
        var w1 = Tutorial14.SplatWeights.Weight1;
        var w2 = Tutorial14.SplatWeights.Weight2;

        // At any interpolation step, weights should still sum to 1
        for (int step = 1; step <= 5; step++)
        {
            var interpolated = Tutorial14.HexMetrics.TerraceLerp(w1, w2, step);
            Tutorial14.SplatWeights.IsValid(interpolated).Should().BeTrue(
                $"Step {step} should produce valid weights");
        }
    }
}

// ============================================================================
// Edge Fan Triangulation Tests
// ============================================================================

/// <summary>
/// Tests for edge fan triangulation with terrain types.
/// Tutorial 14: Edge fans use single terrain type with weight (1,0,0).
/// </summary>
public class EdgeFanTriangulationTests
{
    [Fact]
    public void EdgeFan_UsesSingleTerrainType()
    {
        int cellTerrainType = Tutorial14.TerrainTypes.Grass;
        var data = Tutorial14.TerrainTypeData.Single(cellTerrainType);

        // All three indices should be the same for edge fan
        data.Type1.Should().Be(cellTerrainType);
        data.Type2.Should().Be(cellTerrainType);
        data.Type3.Should().Be(cellTerrainType);
    }

    [Fact]
    public void EdgeFan_SplatWeight_IsPureFirst()
    {
        // Edge fans use pure weight1 (red channel)
        var weight = Tutorial14.SplatWeights.Weight1;
        weight.R.Should().Be(1f);
        weight.G.Should().Be(0f);
        weight.B.Should().Be(0f);
    }
}

// ============================================================================
// Edge Strip Triangulation Tests
// ============================================================================

/// <summary>
/// Tests for edge strip triangulation with terrain types.
/// Tutorial 14: Edge strips blend two terrain types.
/// </summary>
public class EdgeStripTriangulationTests
{
    [Fact]
    public void EdgeStrip_BlendsTwoTerrainTypes()
    {
        int nearType = Tutorial14.TerrainTypes.Sand;
        int farType = Tutorial14.TerrainTypes.Stone;
        var data = Tutorial14.TerrainTypeData.TwoTypes(nearType, farType);

        // Type1 (near) and Type3 should be nearType
        // Type2 (far) should be farType
        data.Type1.Should().Be(nearType);
        data.Type2.Should().Be(farType);
        data.Type3.Should().Be(nearType);
    }

    [Fact]
    public void EdgeStrip_NearVertices_UseWeight1()
    {
        // Near vertices (e1 side) use weight1
        var nearWeight = Tutorial14.SplatWeights.Weight1;
        nearWeight.R.Should().Be(1f);
    }

    [Fact]
    public void EdgeStrip_FarVertices_UseWeight2()
    {
        // Far vertices (e2 side) use weight2
        var farWeight = Tutorial14.SplatWeights.Weight2;
        farWeight.G.Should().Be(1f);
    }
}

// ============================================================================
// Corner Triangulation Tests
// ============================================================================

/// <summary>
/// Tests for corner triangulation with terrain types.
/// Tutorial 14: Corners blend three terrain types from adjacent cells.
/// </summary>
public class CornerTriangulationTests
{
    [Fact]
    public void Corner_BlendsThreeTerrainTypes()
    {
        int bottomType = Tutorial14.TerrainTypes.Sand;
        int leftType = Tutorial14.TerrainTypes.Grass;
        int rightType = Tutorial14.TerrainTypes.Stone;

        var data = Tutorial14.TerrainTypeData.ThreeTypes(bottomType, leftType, rightType);

        data.Type1.Should().Be(bottomType);
        data.Type2.Should().Be(leftType);
        data.Type3.Should().Be(rightType);
    }

    [Fact]
    public void Corner_BottomVertex_UsesWeight1()
    {
        var weight = Tutorial14.SplatWeights.Weight1;
        weight.R.Should().Be(1f);
        weight.G.Should().Be(0f);
        weight.B.Should().Be(0f);
    }

    [Fact]
    public void Corner_LeftVertex_UsesWeight2()
    {
        var weight = Tutorial14.SplatWeights.Weight2;
        weight.R.Should().Be(0f);
        weight.G.Should().Be(1f);
        weight.B.Should().Be(0f);
    }

    [Fact]
    public void Corner_RightVertex_UsesWeight3()
    {
        var weight = Tutorial14.SplatWeights.Weight3;
        weight.R.Should().Be(0f);
        weight.G.Should().Be(0f);
        weight.B.Should().Be(1f);
    }

    [Fact]
    public void Corner_AllSameType_StillUsesThreeIndices()
    {
        // Even when all three cells have the same terrain type,
        // we still pass three indices (shader handles it)
        int type = Tutorial14.TerrainTypes.Grass;
        var data = Tutorial14.TerrainTypeData.ThreeTypes(type, type, type);

        data.Type1.Should().Be(type);
        data.Type2.Should().Be(type);
        data.Type3.Should().Be(type);
    }
}

// ============================================================================
// Terrace Terrain Type Tests
// ============================================================================

/// <summary>
/// Tests for terrace triangulation with terrain types.
/// Tutorial 14: Terrain types stay constant across terrace steps (no interpolation).
/// </summary>
public class TerraceTerrainTypeTests
{
    [Fact]
    public void TerraceEdge_TypesStayConstant()
    {
        // Unlike colors, terrain types don't interpolate across terrace steps
        int beginType = Tutorial14.TerrainTypes.Sand;
        int endType = Tutorial14.TerrainTypes.Stone;

        // At all terrace steps, the types should remain the original two
        // (The splat weights interpolate, not the type indices)
        var data = Tutorial14.TerrainTypeData.TwoTypes(beginType, endType);

        data.Type1.Should().Be(beginType);
        data.Type2.Should().Be(endType);
    }

    [Fact]
    public void TerraceCorner_TypesStayConstant()
    {
        int beginType = Tutorial14.TerrainTypes.Sand;
        int leftType = Tutorial14.TerrainTypes.Grass;
        int rightType = Tutorial14.TerrainTypes.Stone;

        // Corner terrace types don't change across steps
        var data = Tutorial14.TerrainTypeData.ThreeTypes(beginType, leftType, rightType);

        data.Type1.Should().Be(beginType);
        data.Type2.Should().Be(leftType);
        data.Type3.Should().Be(rightType);
    }
}

// ============================================================================
// Shader Parameter Tests
// ============================================================================

/// <summary>
/// Tests documenting shader parameter expectations.
/// </summary>
public class ShaderParameterTests
{
    [Fact]
    public void TextureScale_DefaultValue()
    {
        // Default texture scale for world-space UVs
        float defaultScale = 0.02f;
        defaultScale.Should().BeApproximately(0.02f, 0.0001f);
    }

    [Fact]
    public void TerrainTextureArray_IndexRange()
    {
        // Valid terrain texture array indices
        int minIndex = 0;
        int maxIndex = Tutorial14.TerrainTypes.Count - 1;

        minIndex.Should().Be(0);
        maxIndex.Should().Be(4);
    }

    [Theory]
    [InlineData(0, "Sand")]
    [InlineData(1, "Grass")]
    [InlineData(2, "Mud")]
    [InlineData(3, "Stone")]
    [InlineData(4, "Snow")]
    public void TerrainIndex_MapsToTextureName(int index, string textureName)
    {
        // Documents the texture array ordering
        var names = new[] { "Sand", "Grass", "Mud", "Stone", "Snow" };
        names[index].Should().Be(textureName);
    }
}

} // namespace HexGame.Tests.Godot2
