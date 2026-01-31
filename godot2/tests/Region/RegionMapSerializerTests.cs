using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using HexGame.Region;

namespace HexGame.Tests.Region;

/// <summary>
/// Tests for RegionMapSerializer JSON serialization/deserialization.
/// </summary>
public class RegionMapSerializerTests : IDisposable
{
    private readonly string _testDir;
    private readonly RegionMapSerializer _serializer;

    public RegionMapSerializerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"RegionMapTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _serializer = new RegionMapSerializer();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region Round-Trip Tests

    [Fact]
    public async Task SaveAndLoad_EmptyWorld_PreservesMetadata()
    {
        // Arrange
        var map = new RegionMap
        {
            WorldName = "Test World",
            WorldId = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 1, 30, 12, 0, 0, DateTimeKind.Utc)
        };
        var path = Path.Combine(_testDir, "empty.world");

        // Act
        var saveResult = await _serializer.SaveAsync(map, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.True(saveResult);
        Assert.NotNull(loaded);
        Assert.Equal(map.WorldName, loaded.WorldName);
        Assert.Equal(map.WorldId, loaded.WorldId);
        Assert.Equal(map.CreatedAt, loaded.CreatedAt);
    }

    [Fact]
    public async Task SaveAndLoad_WithRegions_PreservesAllData()
    {
        // Arrange
        var region1 = new RegionMapEntry
        {
            RegionId = Guid.NewGuid(),
            Name = "Starting Island",
            MapX = 100f,
            MapY = 200f,
            PrimaryBiome = RegionBiome.Tropical,
            DifficultyRating = 2,
            IsDiscovered = true
        };
        var region2 = new RegionMapEntry
        {
            RegionId = Guid.NewGuid(),
            Name = "Distant Shore",
            MapX = 300f,
            MapY = 150f,
            PrimaryBiome = RegionBiome.Desert,
            DifficultyRating = 4
        };
        var map = RegionMap.CreateNew("Caribbean", region1);
        map.AddRegion(region2);
        map.ConnectRegions(region1.RegionId, region2.RegionId, travelTime: 180f, dangerLevel: 0.5f);

        var path = Path.Combine(_testDir, "caribbean.world");

        // Act
        await _serializer.SaveAsync(map, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Regions.Count);
        Assert.Equal(region1.RegionId, loaded.CurrentRegionId);

        var loadedRegion1 = loaded.GetRegionById(region1.RegionId);
        Assert.NotNull(loadedRegion1);
        Assert.Equal("Starting Island", loadedRegion1.Name);
        Assert.Equal(100f, loadedRegion1.MapX);
        Assert.Equal(200f, loadedRegion1.MapY);
        Assert.Equal(RegionBiome.Tropical, loadedRegion1.PrimaryBiome);
        Assert.True(loadedRegion1.IsDiscovered);
    }

    [Fact]
    public async Task SaveAndLoad_WithConnections_PreservesGraph()
    {
        // Arrange - create a triangle of connected regions
        var regionA = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "A" };
        var regionB = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "B" };
        var regionC = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "C" };

        var map = RegionMap.CreateNew("Triangle World", regionA);
        map.AddRegion(regionB);
        map.AddRegion(regionC);
        map.ConnectRegions(regionA.RegionId, regionB.RegionId, 60f);
        map.ConnectRegions(regionB.RegionId, regionC.RegionId, 90f);
        map.ConnectRegions(regionC.RegionId, regionA.RegionId, 120f);

        var path = Path.Combine(_testDir, "triangle.world");

        // Act
        await _serializer.SaveAsync(map, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.True(loaded.CanTravelTo(regionA.RegionId, regionB.RegionId));
        Assert.True(loaded.CanTravelTo(regionB.RegionId, regionC.RegionId));
        Assert.True(loaded.CanTravelTo(regionC.RegionId, regionA.RegionId));

        // Check connection details preserved
        var connInfo = loaded.GetConnectionInfo(regionA.RegionId, regionB.RegionId);
        Assert.NotNull(connInfo);
        Assert.Equal(60f, connInfo.TravelTimeMinutes);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesDiscoveryState()
    {
        // Arrange
        var discovered = new RegionMapEntry
        {
            RegionId = Guid.NewGuid(),
            Name = "Discovered",
            IsDiscovered = true,
            DiscoveredAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };
        var undiscovered = new RegionMapEntry
        {
            RegionId = Guid.NewGuid(),
            Name = "Hidden",
            IsDiscovered = false
        };

        var map = RegionMap.CreateNew("Discovery Test", discovered);
        map.AddRegion(undiscovered);

        var path = Path.Combine(_testDir, "discovery.world");

        // Act
        await _serializer.SaveAsync(map, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        var loadedDiscovered = loaded.GetRegionById(discovered.RegionId);
        var loadedUndiscovered = loaded.GetRegionById(undiscovered.RegionId);

        Assert.True(loadedDiscovered?.IsDiscovered);
        Assert.NotNull(loadedDiscovered?.DiscoveredAt);
        Assert.False(loadedUndiscovered?.IsDiscovered);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Load_NonexistentFile_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_testDir, "nonexistent.world");

        // Act
        var result = await _serializer.LoadAsync(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Load_InvalidJson_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_testDir, "invalid.world");
        await File.WriteAllTextAsync(path, "not valid json {{{");

        // Act
        var result = await _serializer.LoadAsync(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Save_WithCancellation_ReturnsFalse()
    {
        // Arrange
        var map = new RegionMap { WorldName = "Cancel Test" };
        var path = Path.Combine(_testDir, "cancel.world");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _serializer.SaveAsync(map, path, cts.Token);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Synchronous Methods Tests

    [Fact]
    public void SyncSaveAndLoad_WorksCorrectly()
    {
        // Arrange
        var map = new RegionMap { WorldName = "Sync Test" };
        var path = Path.Combine(_testDir, "sync.world");

        // Act
        var saveResult = _serializer.Save(map, path);
        var loaded = _serializer.Load(path);

        // Assert
        Assert.True(saveResult);
        Assert.NotNull(loaded);
        Assert.Equal("Sync Test", loaded.WorldName);
    }

    #endregion

    #region File Format Tests

    [Fact]
    public async Task Save_ProducesReadableJson()
    {
        // Arrange
        var map = new RegionMap { WorldName = "Readable Test" };
        var path = Path.Combine(_testDir, "readable.world");

        // Act
        await _serializer.SaveAsync(map, path);
        var json = await File.ReadAllTextAsync(path);

        // Assert - should be indented (pretty printed)
        Assert.Contains("\n", json);
        Assert.Contains("worldName", json); // camelCase naming
    }

    #endregion

    #region Path Helper Tests

    [Theory]
    [InlineData("Test World", "regions/test_world.world")]
    [InlineData("Caribbean Adventures", "regions/caribbean_adventures.world")]
    [InlineData("simple", "regions/simple.world")]
    public void GetDefaultWorldPath_FormatsCorrectly(string worldName, string expectedPath)
    {
        // Act
        var result = RegionMapSerializer.GetDefaultWorldPath(worldName);

        // Assert - normalize separators for cross-platform
        var normalizedResult = result.Replace("\\", "/");
        Assert.Equal(expectedPath, normalizedResult);
    }

    #endregion
}
