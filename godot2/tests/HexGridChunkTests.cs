using FluentAssertions;
using Xunit;

namespace HexMapTutorial.Tests;

/// <summary>
/// Unit tests for HexMetrics chunk-related constants.
/// Verifies Tutorial 5 math.
/// </summary>
public class HexGridChunkTests
{
    [Fact]
    public void ChunkSizeX_IsCorrect()
    {
        HexMetrics.ChunkSizeX.Should().Be(5);
    }

    [Fact]
    public void ChunkSizeZ_IsCorrect()
    {
        HexMetrics.ChunkSizeZ.Should().Be(5);
    }

    [Fact]
    public void ChunkSize_MatchesTutorial5()
    {
        // Tutorial 5 uses 5x5 chunks
        (HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ).Should().Be(25);
    }

    [Theory]
    [InlineData(1, 1, 5, 5)]   // 1x1 chunks = 5x5 cells
    [InlineData(2, 2, 10, 10)] // 2x2 chunks = 10x10 cells
    [InlineData(4, 3, 20, 15)] // 4x3 chunks = 20x15 cells (default in tutorial)
    public void CellCount_IsChunkCountTimesChunkSize(int chunkCountX, int chunkCountZ, int expectedCellsX, int expectedCellsZ)
    {
        int cellCountX = chunkCountX * HexMetrics.ChunkSizeX;
        int cellCountZ = chunkCountZ * HexMetrics.ChunkSizeZ;

        cellCountX.Should().Be(expectedCellsX);
        cellCountZ.Should().Be(expectedCellsZ);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]   // First cell goes to chunk 0,0 at local index 0
    [InlineData(4, 0, 0, 0, 4)]   // Cell at x=4 still in chunk 0,0 at local index 4
    [InlineData(5, 0, 1, 0, 0)]   // Cell at x=5 starts chunk 1,0 at local index 0
    [InlineData(0, 5, 0, 1, 0)]   // Cell at z=5 starts chunk 0,1 at local index 0
    [InlineData(7, 8, 1, 1, 17)]  // Cell at 7,8 goes to chunk 1,1 at local index 2 + 3*5 = 17
    public void CellToChunkMapping_IsCorrect(int cellX, int cellZ, int expectedChunkX, int expectedChunkZ, int expectedLocalIndex)
    {
        int chunkX = cellX / HexMetrics.ChunkSizeX;
        int chunkZ = cellZ / HexMetrics.ChunkSizeZ;
        int localX = cellX - chunkX * HexMetrics.ChunkSizeX;
        int localZ = cellZ - chunkZ * HexMetrics.ChunkSizeZ;
        int localIndex = localX + localZ * HexMetrics.ChunkSizeX;

        chunkX.Should().Be(expectedChunkX);
        chunkZ.Should().Be(expectedChunkZ);
        localIndex.Should().Be(expectedLocalIndex);
    }

    [Theory]
    [InlineData(1, 1, 1)]   // 1x1 = 1 chunk
    [InlineData(2, 2, 4)]   // 2x2 = 4 chunks
    [InlineData(4, 3, 12)]  // 4x3 = 12 chunks (default in tutorial)
    public void TotalChunkCount_IsCorrect(int chunkCountX, int chunkCountZ, int expectedTotal)
    {
        (chunkCountX * chunkCountZ).Should().Be(expectedTotal);
    }
}
