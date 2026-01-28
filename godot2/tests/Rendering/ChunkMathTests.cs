using FluentAssertions;
using HexGame.Rendering;
using Xunit;
using static HexGame.Rendering.ChunkMath;

namespace HexMapTutorial.Tests.Rendering;

/// <summary>
/// Tests for ChunkMath pure functions.
/// </summary>
public class ChunkMathTests
{
    private const float DefaultChunkSize = 16.0f;

    #region GetChunkCoords Tests

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(8, 8, 0, 0)]       // Center of chunk 0,0
    [InlineData(15.9f, 15.9f, 0, 0)] // Just inside chunk 0,0
    [InlineData(16, 16, 1, 1)]     // Start of chunk 1,1
    [InlineData(32, 32, 2, 2)]     // Start of chunk 2,2
    public void GetChunkCoords_PositivePositions_ReturnsCorrectChunk(
        float worldX, float worldZ, int expectedCx, int expectedCz)
    {
        var (cx, cz) = ChunkMath.GetChunkCoords(worldX, worldZ, DefaultChunkSize);

        cx.Should().Be(expectedCx);
        cz.Should().Be(expectedCz);
    }

    [Theory]
    [InlineData(-1, -1, -1, -1)]
    [InlineData(-0.1f, -0.1f, -1, -1)]
    [InlineData(-16, -16, -1, -1)]
    [InlineData(-16.1f, -16.1f, -2, -2)]
    [InlineData(-32, -32, -2, -2)]
    public void GetChunkCoords_NegativePositions_ReturnsCorrectChunk(
        float worldX, float worldZ, int expectedCx, int expectedCz)
    {
        var (cx, cz) = ChunkMath.GetChunkCoords(worldX, worldZ, DefaultChunkSize);

        cx.Should().Be(expectedCx);
        cz.Should().Be(expectedCz);
    }

    [Fact]
    public void GetChunkCoords_MixedPositiveNegative_ReturnsCorrectChunk()
    {
        var (cx, cz) = ChunkMath.GetChunkCoords(20, -5, DefaultChunkSize);

        cx.Should().Be(1);  // 20 / 16 = 1.25 -> floor = 1
        cz.Should().Be(-1); // -5 / 16 = -0.3125 -> floor = -1
    }

    [Fact]
    public void GetChunkCoords_DifferentChunkSize_CalculatesCorrectly()
    {
        var (cx, cz) = ChunkMath.GetChunkCoords(50, 50, 10.0f);

        cx.Should().Be(5);  // 50 / 10 = 5
        cz.Should().Be(5);
    }

    [Fact]
    public void GetChunkCoords_ZeroChunkSize_ThrowsArgumentException()
    {
        var act = () => ChunkMath.GetChunkCoords(10, 10, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("chunkSize");
    }

    [Fact]
    public void GetChunkCoords_NegativeChunkSize_ThrowsArgumentException()
    {
        var act = () => ChunkMath.GetChunkCoords(10, 10, -5);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("chunkSize");
    }

    #endregion

    #region GetChunkCenter Tests

    [Fact]
    public void GetChunkCenter_ChunkZeroZero_ReturnsCenterOfFirstChunk()
    {
        var (x, z) = ChunkMath.GetChunkCenter(0, 0, DefaultChunkSize);

        x.Should().Be(8.0f);  // (0 + 0.5) * 16 = 8
        z.Should().Be(8.0f);
    }

    [Fact]
    public void GetChunkCenter_ChunkOneOne_ReturnsCorrectCenter()
    {
        var (x, z) = ChunkMath.GetChunkCenter(1, 1, DefaultChunkSize);

        x.Should().Be(24.0f);  // (1 + 0.5) * 16 = 24
        z.Should().Be(24.0f);
    }

    [Fact]
    public void GetChunkCenter_NegativeChunk_ReturnsNegativeCenter()
    {
        var (x, z) = ChunkMath.GetChunkCenter(-1, -1, DefaultChunkSize);

        x.Should().Be(-8.0f);  // (-1 + 0.5) * 16 = -8
        z.Should().Be(-8.0f);
    }

    [Fact]
    public void GetChunkCenter_ZeroChunkSize_ThrowsArgumentException()
    {
        var act = () => ChunkMath.GetChunkCenter(0, 0, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("chunkSize");
    }

    [Fact]
    public void GetChunkCenter_RoundTrip_ReturnsOriginalChunk()
    {
        // Get center of chunk (3, 5), then get chunk coords of that center
        var (centerX, centerZ) = ChunkMath.GetChunkCenter(3, 5, DefaultChunkSize);
        var (cx, cz) = ChunkMath.GetChunkCoords(centerX, centerZ, DefaultChunkSize);

        cx.Should().Be(3);
        cz.Should().Be(5);
    }

    #endregion

    #region DistanceSquared Tests

    [Fact]
    public void DistanceSquared_SamePoint_ReturnsZero()
    {
        var distSq = ChunkMath.DistanceSquared(10, 20, 10, 20);

        distSq.Should().Be(0);
    }

    [Fact]
    public void DistanceSquared_HorizontalDistance_ReturnsCorrectValue()
    {
        var distSq = ChunkMath.DistanceSquared(0, 0, 10, 0);

        distSq.Should().Be(100); // 10^2 = 100
    }

    [Fact]
    public void DistanceSquared_VerticalDistance_ReturnsCorrectValue()
    {
        var distSq = ChunkMath.DistanceSquared(0, 0, 0, 5);

        distSq.Should().Be(25); // 5^2 = 25
    }

    [Fact]
    public void DistanceSquared_DiagonalDistance_ReturnsCorrectValue()
    {
        var distSq = ChunkMath.DistanceSquared(0, 0, 3, 4);

        distSq.Should().Be(25); // 3^2 + 4^2 = 9 + 16 = 25
    }

    [Fact]
    public void DistanceSquared_NegativeCoordinates_ReturnsCorrectValue()
    {
        var distSq = ChunkMath.DistanceSquared(-5, -5, 5, 5);

        distSq.Should().Be(200); // 10^2 + 10^2 = 200
    }

    #endregion

    #region SelectLod Tests

    [Theory]
    [InlineData(0, LodLevel.High)]
    [InlineData(10, LodLevel.High)]
    [InlineData(29, LodLevel.High)]
    public void SelectLod_NearCamera_ReturnsHigh(float distance, LodLevel expected)
    {
        var lod = ChunkMath.SelectLod(
            distance,
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        lod.Should().Be(expected);
    }

    [Theory]
    [InlineData(30, LodLevel.Medium)]
    [InlineData(45, LodLevel.Medium)]
    [InlineData(59, LodLevel.Medium)]
    public void SelectLod_MediumDistance_ReturnsMedium(float distance, LodLevel expected)
    {
        var lod = ChunkMath.SelectLod(
            distance,
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        lod.Should().Be(expected);
    }

    [Theory]
    [InlineData(60, LodLevel.Low)]
    [InlineData(70, LodLevel.Low)]
    [InlineData(80, LodLevel.Low)]
    public void SelectLod_FarDistance_ReturnsLow(float distance, LodLevel expected)
    {
        var lod = ChunkMath.SelectLod(
            distance,
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        lod.Should().Be(expected);
    }

    [Fact]
    public void SelectLod_BeyondMaxDistance_ReturnsCulled()
    {
        var lod = ChunkMath.SelectLod(
            distance: 81,
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        lod.Should().Be(LodLevel.Culled);
    }

    [Fact]
    public void SelectLod_Hysteresis_DelaysTransitionToHigherDetail()
    {
        // At distance 29 (just below threshold 30), starting from Medium LOD
        // With hysteresis of 2, should stay Medium until distance < 28
        var lod = ChunkMath.SelectLod(
            distance: 29,
            currentLod: LodLevel.Medium,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80,
            hysteresis: 2.0f);

        lod.Should().Be(LodLevel.Medium, "hysteresis should prevent switching to High");
    }

    [Fact]
    public void SelectLod_Hysteresis_AllowsTransitionWhenFarEnough()
    {
        // At distance 27 (below threshold 30 - hysteresis 2 = 28)
        var lod = ChunkMath.SelectLod(
            distance: 27,
            currentLod: LodLevel.Medium,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80,
            hysteresis: 2.0f);

        lod.Should().Be(LodLevel.High, "should switch when past hysteresis margin");
    }

    [Fact]
    public void SelectLod_NoHysteresis_WhenDecreasingDetail()
    {
        // When going from High to Medium (decreasing detail), no hysteresis applied
        var lod = ChunkMath.SelectLod(
            distance: 30,
            currentLod: LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80,
            hysteresis: 2.0f);

        lod.Should().Be(LodLevel.Medium);
    }

    [Fact]
    public void SelectLod_ExactlyAtThreshold_TransitionsCorrectly()
    {
        var lod = ChunkMath.SelectLod(
            distance: 30,
            currentLod: LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        lod.Should().Be(LodLevel.Medium);
    }

    #endregion

    #region IsChunkVisible Tests

    [Fact]
    public void IsChunkVisible_WithinDistance_ReturnsTrue()
    {
        var visible = ChunkMath.IsChunkVisible(distanceSquared: 100, maxRenderDistance: 20);

        visible.Should().BeTrue(); // sqrt(100) = 10 < 20
    }

    [Fact]
    public void IsChunkVisible_ExactlyAtDistance_ReturnsTrue()
    {
        var visible = ChunkMath.IsChunkVisible(distanceSquared: 400, maxRenderDistance: 20);

        visible.Should().BeTrue(); // sqrt(400) = 20 == 20
    }

    [Fact]
    public void IsChunkVisible_BeyondDistance_ReturnsFalse()
    {
        var visible = ChunkMath.IsChunkVisible(distanceSquared: 500, maxRenderDistance: 20);

        visible.Should().BeFalse(); // sqrt(500) ≈ 22.4 > 20
    }

    [Fact]
    public void IsChunkVisible_ZeroDistance_ReturnsTrue()
    {
        var visible = ChunkMath.IsChunkVisible(distanceSquared: 0, maxRenderDistance: 80);

        visible.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_ChunkAtOrigin_VisibleAndHighLod()
    {
        float cameraX = 8, cameraZ = 8; // Camera at chunk 0,0 center
        var (chunkCenterX, chunkCenterZ) = ChunkMath.GetChunkCenter(0, 0, DefaultChunkSize);

        var distSq = ChunkMath.DistanceSquared(cameraX, cameraZ, chunkCenterX, chunkCenterZ);
        var visible = ChunkMath.IsChunkVisible(distSq, maxRenderDistance: 80);
        var lod = ChunkMath.SelectLod(
            (float)Math.Sqrt(distSq),
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        visible.Should().BeTrue();
        lod.Should().Be(LodLevel.High);
    }

    [Fact]
    public void Integration_DistantChunk_CulledAndNotVisible()
    {
        float cameraX = 0, cameraZ = 0;
        var (chunkCenterX, chunkCenterZ) = ChunkMath.GetChunkCenter(10, 10, DefaultChunkSize); // Far away chunk

        var distSq = ChunkMath.DistanceSquared(cameraX, cameraZ, chunkCenterX, chunkCenterZ);
        var visible = ChunkMath.IsChunkVisible(distSq, maxRenderDistance: 80);
        var lod = ChunkMath.SelectLod(
            (float)Math.Sqrt(distSq),
            LodLevel.High,
            lodHighToMedium: 30,
            lodMediumToLow: 60,
            maxRenderDistance: 80);

        // Chunk center is at (168, 168), distance ≈ 237.6
        visible.Should().BeFalse();
        lod.Should().Be(LodLevel.Culled);
    }

    #endregion
}
