using System;
using System.Linq;
using Xunit;
using HexGame.Region;

namespace HexGame.Tests.Region;

/// <summary>
/// Tests for RegionMap world-level data management.
/// </summary>
public class RegionMapTests
{
    #region RegionMap Creation Tests

    [Fact]
    public void RegionMap_DefaultValues_AreCorrect()
    {
        // Act
        var map = new RegionMap();

        // Assert
        Assert.Equal("New World", map.WorldName);
        Assert.NotEqual(Guid.Empty, map.WorldId);
        Assert.Equal(Guid.Empty, map.CurrentRegionId);
        Assert.Empty(map.Regions);
    }

    [Fact]
    public void CreateNew_WithStartingRegion_InitializesCorrectly()
    {
        // Arrange
        var startRegion = new RegionMapEntry
        {
            RegionId = Guid.NewGuid(),
            Name = "Starting Island"
        };

        // Act
        var map = RegionMap.CreateNew("Test World", startRegion);

        // Assert
        Assert.Equal("Test World", map.WorldName);
        Assert.Equal(startRegion.RegionId, map.CurrentRegionId);
        Assert.Single(map.Regions);
        Assert.True(startRegion.IsDiscovered);
        Assert.NotNull(startRegion.DiscoveredAt);
    }

    #endregion

    #region Region Lookup Tests

    [Fact]
    public void GetRegionById_ExistingRegion_ReturnsRegion()
    {
        // Arrange
        var map = new RegionMap();
        var entry = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Test" };
        map.AddRegion(entry);

        // Act
        var result = map.GetRegionById(entry.RegionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void GetRegionById_NonExistentRegion_ReturnsNull()
    {
        // Arrange
        var map = new RegionMap();

        // Act
        var result = map.GetRegionById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentRegion_WhenSet_ReturnsCorrectRegion()
    {
        // Arrange
        var map = new RegionMap();
        var entry = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Current" };
        map.AddRegion(entry);
        map.CurrentRegionId = entry.RegionId;

        // Act
        var result = map.GetCurrentRegion();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Current", result.Name);
    }

    [Fact]
    public void GetDiscoveredRegions_ReturnsOnlyDiscovered()
    {
        // Arrange
        var map = new RegionMap();
        var discovered1 = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Discovered 1", IsDiscovered = true };
        var undiscovered = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Undiscovered", IsDiscovered = false };
        var discovered2 = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Discovered 2", IsDiscovered = true };
        map.AddRegion(discovered1);
        map.AddRegion(undiscovered);
        map.AddRegion(discovered2);

        // Act
        var result = map.GetDiscoveredRegions().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.IsDiscovered));
    }

    #endregion

    #region Region Connection Tests

    [Fact]
    public void ConnectRegions_CreatesBidirectionalConnection()
    {
        // Arrange
        var map = new RegionMap();
        var regionA = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Region A" };
        var regionB = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Region B" };
        map.AddRegion(regionA);
        map.AddRegion(regionB);

        // Act
        map.ConnectRegions(regionA.RegionId, regionB.RegionId, travelTime: 120f, dangerLevel: 0.5f);

        // Assert
        Assert.Contains(regionB.RegionId, regionA.ConnectedRegionIds);
        Assert.Contains(regionA.RegionId, regionB.ConnectedRegionIds);
        Assert.Single(regionA.ConnectionDetails);
        Assert.Single(regionB.ConnectionDetails);
    }

    [Fact]
    public void GetConnectedRegions_ReturnsConnectedEntries()
    {
        // Arrange
        var map = new RegionMap();
        var center = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Center" };
        var connected1 = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Connected 1" };
        var connected2 = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Connected 2" };
        var unconnected = new RegionMapEntry { RegionId = Guid.NewGuid(), Name = "Unconnected" };
        map.AddRegion(center);
        map.AddRegion(connected1);
        map.AddRegion(connected2);
        map.AddRegion(unconnected);
        map.ConnectRegions(center.RegionId, connected1.RegionId);
        map.ConnectRegions(center.RegionId, connected2.RegionId);

        // Act
        var result = map.GetConnectedRegions(center.RegionId).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "Connected 1");
        Assert.Contains(result, r => r.Name == "Connected 2");
        Assert.DoesNotContain(result, r => r.Name == "Unconnected");
    }

    [Fact]
    public void CanTravelTo_ConnectedRegion_ReturnsTrue()
    {
        // Arrange
        var map = new RegionMap();
        var regionA = new RegionMapEntry { RegionId = Guid.NewGuid() };
        var regionB = new RegionMapEntry { RegionId = Guid.NewGuid() };
        map.AddRegion(regionA);
        map.AddRegion(regionB);
        map.ConnectRegions(regionA.RegionId, regionB.RegionId);

        // Act & Assert
        Assert.True(map.CanTravelTo(regionA.RegionId, regionB.RegionId));
        Assert.True(map.CanTravelTo(regionB.RegionId, regionA.RegionId));
    }

    [Fact]
    public void CanTravelTo_UnconnectedRegion_ReturnsFalse()
    {
        // Arrange
        var map = new RegionMap();
        var regionA = new RegionMapEntry { RegionId = Guid.NewGuid() };
        var regionB = new RegionMapEntry { RegionId = Guid.NewGuid() };
        map.AddRegion(regionA);
        map.AddRegion(regionB);

        // Act & Assert
        Assert.False(map.CanTravelTo(regionA.RegionId, regionB.RegionId));
    }

    [Fact]
    public void GetConnectionInfo_ReturnsCorrectDetails()
    {
        // Arrange
        var map = new RegionMap();
        var regionA = new RegionMapEntry { RegionId = Guid.NewGuid() };
        var regionB = new RegionMapEntry { RegionId = Guid.NewGuid() };
        map.AddRegion(regionA);
        map.AddRegion(regionB);
        map.ConnectRegions(regionA.RegionId, regionB.RegionId, travelTime: 90f, dangerLevel: 0.3f);

        // Act
        var info = map.GetConnectionInfo(regionA.RegionId, regionB.RegionId);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(90f, info.TravelTimeMinutes);
        Assert.Equal(0.3f, info.DangerLevel, precision: 4);
    }

    #endregion

    #region Discovery Tests

    [Fact]
    public void DiscoverRegion_MarksAsDiscovered()
    {
        // Arrange
        var map = new RegionMap();
        var entry = new RegionMapEntry { RegionId = Guid.NewGuid(), IsDiscovered = false };
        map.AddRegion(entry);

        // Act
        map.DiscoverRegion(entry.RegionId);

        // Assert
        Assert.True(entry.IsDiscovered);
        Assert.NotNull(entry.DiscoveredAt);
    }

    [Fact]
    public void SetCurrentRegion_DiscoversThatRegion()
    {
        // Arrange
        var map = new RegionMap();
        var entry = new RegionMapEntry { RegionId = Guid.NewGuid(), IsDiscovered = false };
        map.AddRegion(entry);

        // Act
        map.SetCurrentRegion(entry.RegionId);

        // Assert
        Assert.Equal(entry.RegionId, map.CurrentRegionId);
        Assert.True(entry.IsDiscovered);
    }

    #endregion

    #region AddRegion Tests

    [Fact]
    public void AddRegion_DuplicateId_DoesNotAdd()
    {
        // Arrange
        var map = new RegionMap();
        var id = Guid.NewGuid();
        var entry1 = new RegionMapEntry { RegionId = id, Name = "First" };
        var entry2 = new RegionMapEntry { RegionId = id, Name = "Second" };
        map.AddRegion(entry1);

        // Act
        map.AddRegion(entry2);

        // Assert
        Assert.Single(map.Regions);
        Assert.Equal("First", map.Regions[0].Name);
    }

    #endregion

    #region RegionMapEntry Tests

    [Fact]
    public void RegionMapEntry_FromRegionData_CopiesFields()
    {
        // Arrange
        var data = RegionData.CreateEmpty("Test Island", 100, 80, 12345);

        // Act
        var entry = RegionMapEntry.FromRegionData(data, mapX: 50f, mapY: 100f);

        // Assert
        Assert.Equal(data.RegionId, entry.RegionId);
        Assert.Equal(data.Name, entry.Name);
        Assert.Equal(data.Seed, entry.Seed);
        Assert.Equal(data.Width, entry.Width);
        Assert.Equal(data.Height, entry.Height);
        Assert.Equal(50f, entry.MapX);
        Assert.Equal(100f, entry.MapY);
        Assert.Contains(".region", entry.FilePath);
    }

    [Fact]
    public void RegionMapEntry_DefaultValues_AreCorrect()
    {
        // Act
        var entry = new RegionMapEntry();

        // Assert
        Assert.Equal("Unknown Region", entry.Name);
        Assert.False(entry.IsDiscovered);
        Assert.Equal(RegionBiome.Temperate, entry.PrimaryBiome);
        Assert.Equal(1, entry.DifficultyRating);
        Assert.Equal(RegionConfig.DefaultRegionWidth, entry.Width);
        Assert.Equal(RegionConfig.DefaultRegionHeight, entry.Height);
    }

    #endregion

    #region RegionConnectionInfo Tests

    [Fact]
    public void RegionConnectionInfo_DefaultValues_AreCorrect()
    {
        // Act
        var info = new RegionConnectionInfo();

        // Assert
        Assert.Equal(60f, info.TravelTimeMinutes);
        Assert.Equal(0f, info.DangerLevel);
        Assert.True(info.IsAvailable);
        Assert.Equal("", info.RouteDescription);
    }

    #endregion

    #region RegionBiome Tests

    [Theory]
    [InlineData(RegionBiome.Temperate)]
    [InlineData(RegionBiome.Tropical)]
    [InlineData(RegionBiome.Arctic)]
    [InlineData(RegionBiome.Desert)]
    [InlineData(RegionBiome.Volcanic)]
    [InlineData(RegionBiome.Coastal)]
    [InlineData(RegionBiome.Swamp)]
    public void RegionBiome_AllValues_AreDefined(RegionBiome biome)
    {
        // Assert - just verify the enum value exists and can be assigned
        var entry = new RegionMapEntry { PrimaryBiome = biome };
        Assert.Equal(biome, entry.PrimaryBiome);
    }

    #endregion
}
