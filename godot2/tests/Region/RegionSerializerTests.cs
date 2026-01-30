using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using HexGame.Region;
using HexGame.Generation;

namespace HexGame.Tests.Region;

/// <summary>
/// Tests for RegionSerializer binary serialization/deserialization.
/// </summary>
public class RegionSerializerTests : IDisposable
{
    private readonly string _testDir;
    private readonly RegionSerializer _serializer;

    public RegionSerializerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"RegionTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _serializer = new RegionSerializer();
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
    public async Task SaveAndLoad_EmptyRegion_PreservesMetadata()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Test Region", 50, 50, 12345);
        region.GeneratedAt = new DateTime(2026, 1, 30, 12, 0, 0, DateTimeKind.Utc);
        var path = Path.Combine(_testDir, "empty.region");

        // Act
        var saveResult = await _serializer.SaveAsync(region, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.True(saveResult);
        Assert.NotNull(loaded);
        Assert.Equal(region.RegionId, loaded.RegionId);
        Assert.Equal(region.Name, loaded.Name);
        Assert.Equal(region.Width, loaded.Width);
        Assert.Equal(region.Height, loaded.Height);
        Assert.Equal(region.Seed, loaded.Seed);
        Assert.Equal(region.GeneratedAt, loaded.GeneratedAt);
    }

    [Fact]
    public async Task SaveAndLoad_WithCellData_PreservesAllFields()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Cell Test", 10, 10, 42);

        // Set varied cell data
        var cell = region.Cells[0];
        cell.Elevation = 5;
        cell.WaterLevel = 2;
        cell.TerrainTypeIndex = 3;
        cell.UrbanLevel = 2;
        cell.FarmLevel = 1;
        cell.PlantLevel = 3;
        cell.SpecialIndex = 1;
        cell.Walled = true;
        cell.HasIncomingRiver = true;
        cell.HasOutgoingRiver = true;
        cell.IncomingRiverDirection = 2;
        cell.OutgoingRiverDirection = 5;
        cell.Moisture = 0.75f;
        cell.HasRoadNE = true;
        cell.HasRoadSW = true;
        region.Cells[0] = cell;

        var path = Path.Combine(_testDir, "cells.region");

        // Act
        await _serializer.SaveAsync(region, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        var loadedCell = loaded.Cells[0];

        Assert.Equal(cell.X, loadedCell.X);
        Assert.Equal(cell.Z, loadedCell.Z);
        Assert.Equal(cell.Elevation, loadedCell.Elevation);
        Assert.Equal(cell.WaterLevel, loadedCell.WaterLevel);
        Assert.Equal(cell.TerrainTypeIndex, loadedCell.TerrainTypeIndex);
        Assert.Equal(cell.UrbanLevel, loadedCell.UrbanLevel);
        Assert.Equal(cell.FarmLevel, loadedCell.FarmLevel);
        Assert.Equal(cell.PlantLevel, loadedCell.PlantLevel);
        Assert.Equal(cell.SpecialIndex, loadedCell.SpecialIndex);
        Assert.Equal(cell.Walled, loadedCell.Walled);
        Assert.Equal(cell.HasIncomingRiver, loadedCell.HasIncomingRiver);
        Assert.Equal(cell.HasOutgoingRiver, loadedCell.HasOutgoingRiver);
        Assert.Equal(cell.IncomingRiverDirection, loadedCell.IncomingRiverDirection);
        Assert.Equal(cell.OutgoingRiverDirection, loadedCell.OutgoingRiverDirection);
        Assert.Equal(cell.Moisture, loadedCell.Moisture, precision: 2); // Half-precision tolerance
        Assert.Equal(cell.HasRoadNE, loadedCell.HasRoadNE);
        Assert.Equal(cell.HasRoadE, loadedCell.HasRoadE);
        Assert.Equal(cell.HasRoadSE, loadedCell.HasRoadSE);
        Assert.Equal(cell.HasRoadSW, loadedCell.HasRoadSW);
        Assert.Equal(cell.HasRoadW, loadedCell.HasRoadW);
        Assert.Equal(cell.HasRoadNW, loadedCell.HasRoadNW);
    }

    [Fact]
    public async Task SaveAndLoad_WithConnections_PreservesConnections()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Connected", 20, 20, 1);
        var targetId = Guid.NewGuid();
        region.Connections.Add(new RegionConnection
        {
            TargetRegionId = targetId,
            TargetRegionName = "Target Island",
            DeparturePortIndex = 100,
            ArrivalPortIndex = 50,
            TravelTimeMinutes = 120.5f,
            DangerLevel = 0.3f
        });

        var path = Path.Combine(_testDir, "connected.region");

        // Act
        await _serializer.SaveAsync(region, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.Single(loaded.Connections);
        var conn = loaded.Connections[0];
        Assert.Equal(targetId, conn.TargetRegionId);
        Assert.Equal("Target Island", conn.TargetRegionName);
        Assert.Equal(100, conn.DeparturePortIndex);
        Assert.Equal(50, conn.ArrivalPortIndex);
        Assert.Equal(120.5f, conn.TravelTimeMinutes);
        Assert.Equal(0.3f, conn.DangerLevel, precision: 4);
    }

    [Fact]
    public async Task SaveAndLoad_LargeRegion_CompletesSuccessfully()
    {
        // Arrange - 200x200 = 40,000 cells
        var region = RegionData.CreateEmpty("Large Region", 200, 200, 9999);

        // Set some varied data
        var rng = new Random(42);
        for (int i = 0; i < region.Cells.Length; i++)
        {
            var cell = region.Cells[i];
            cell.Elevation = rng.Next(0, 7);
            cell.TerrainTypeIndex = rng.Next(0, 5);
            cell.Moisture = (float)rng.NextDouble();
            region.Cells[i] = cell;
        }

        var path = Path.Combine(_testDir, "large.region");

        // Act
        await _serializer.SaveAsync(region, path);
        var loaded = await _serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(40000, loaded.Cells.Length);

        // Spot check some cells
        Assert.Equal(region.Cells[0].Elevation, loaded.Cells[0].Elevation);
        Assert.Equal(region.Cells[20000].TerrainTypeIndex, loaded.Cells[20000].TerrainTypeIndex);
        Assert.Equal(region.Cells[39999].Moisture, loaded.Cells[39999].Moisture, precision: 2);
    }

    #endregion

    #region Metadata-Only Load Tests

    [Fact]
    public async Task LoadMetadata_ReturnsMetadataWithoutCells()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Metadata Test", 100, 100, 777);
        region.Connections.Add(new RegionConnection
        {
            TargetRegionId = Guid.NewGuid(),
            TargetRegionName = "Nearby Island"
        });

        var path = Path.Combine(_testDir, "metadata.region");
        await _serializer.SaveAsync(region, path);

        // Act
        var metadata = await _serializer.LoadMetadataAsync(path);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(region.RegionId, metadata.RegionId);
        Assert.Equal(region.Name, metadata.Name);
        Assert.Equal(region.Width, metadata.Width);
        Assert.Equal(region.Height, metadata.Height);
        Assert.Equal(region.Seed, metadata.Seed);
        Assert.Single(metadata.Connections);
    }

    [Fact]
    public async Task LoadMetadata_ReadsLessDataThanFullLoad()
    {
        // Arrange - large region
        var region = RegionData.CreateEmpty("Speed Test", 200, 200, 1);
        var path = Path.Combine(_testDir, "speed.region");
        await _serializer.SaveAsync(region, path);

        // Act - verify both complete successfully
        var metadata = await _serializer.LoadMetadataAsync(path);
        var full = await _serializer.LoadAsync(path);

        // Assert - metadata load works and doesn't include cells
        Assert.NotNull(metadata);
        Assert.NotNull(full);
        Assert.Equal(full.RegionId, metadata.RegionId);
        Assert.Equal(full.Name, metadata.Name);
        // Full load has cells, metadata doesn't have a Cells property
        Assert.Equal(40000, full.Cells.Length);
    }

    #endregion

    #region PackedCellData Tests

    [Fact]
    public void PackedCellData_PackUnpack_PreservesAllFields()
    {
        // Arrange
        var original = new CellData(10, 20)
        {
            Elevation = 4,
            WaterLevel = 1,
            TerrainTypeIndex = 2,
            UrbanLevel = 3,
            FarmLevel = 2,
            PlantLevel = 1,
            SpecialIndex = 2,
            Walled = true,
            HasIncomingRiver = true,
            HasOutgoingRiver = true,
            IncomingRiverDirection = 3,
            OutgoingRiverDirection = 0,
            Moisture = 0.5f,
            HasRoadNE = true,
            HasRoadE = false,
            HasRoadSE = true,
            HasRoadSW = false,
            HasRoadW = true,
            HasRoadNW = false
        };

        // Act
        var packed = PackedCellData.Pack(in original);
        var unpacked = packed.Unpack();

        // Assert
        Assert.Equal(original.X, unpacked.X);
        Assert.Equal(original.Z, unpacked.Z);
        Assert.Equal(original.Elevation, unpacked.Elevation);
        Assert.Equal(original.WaterLevel, unpacked.WaterLevel);
        Assert.Equal(original.TerrainTypeIndex, unpacked.TerrainTypeIndex);
        Assert.Equal(original.UrbanLevel, unpacked.UrbanLevel);
        Assert.Equal(original.FarmLevel, unpacked.FarmLevel);
        Assert.Equal(original.PlantLevel, unpacked.PlantLevel);
        Assert.Equal(original.SpecialIndex, unpacked.SpecialIndex);
        Assert.Equal(original.Walled, unpacked.Walled);
        Assert.Equal(original.HasIncomingRiver, unpacked.HasIncomingRiver);
        Assert.Equal(original.HasOutgoingRiver, unpacked.HasOutgoingRiver);
        Assert.Equal(original.IncomingRiverDirection, unpacked.IncomingRiverDirection);
        Assert.Equal(original.OutgoingRiverDirection, unpacked.OutgoingRiverDirection);
        Assert.Equal(original.Moisture, unpacked.Moisture, precision: 2);
        Assert.Equal(original.HasRoadNE, unpacked.HasRoadNE);
        Assert.Equal(original.HasRoadE, unpacked.HasRoadE);
        Assert.Equal(original.HasRoadSE, unpacked.HasRoadSE);
        Assert.Equal(original.HasRoadSW, unpacked.HasRoadSW);
        Assert.Equal(original.HasRoadW, unpacked.HasRoadW);
        Assert.Equal(original.HasRoadNW, unpacked.HasRoadNW);
    }

    [Fact]
    public void PackedCellData_Size_Is16Bytes()
    {
        // Assert
        Assert.Equal(16, RegionConfig.PackedCellDataSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PackedCellData_FeatureLevels_PreserveRange(int level)
    {
        // Arrange
        var cell = new CellData(0, 0)
        {
            UrbanLevel = level,
            FarmLevel = level,
            PlantLevel = level
        };

        // Act
        var packed = PackedCellData.Pack(in cell);
        var unpacked = packed.Unpack();

        // Assert
        Assert.Equal(level, unpacked.UrbanLevel);
        Assert.Equal(level, unpacked.FarmLevel);
        Assert.Equal(level, unpacked.PlantLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void PackedCellData_RiverDirections_PreserveRange(int direction)
    {
        // Arrange
        var cell = new CellData(0, 0)
        {
            HasIncomingRiver = true,
            HasOutgoingRiver = true,
            IncomingRiverDirection = direction,
            OutgoingRiverDirection = direction
        };

        // Act
        var packed = PackedCellData.Pack(in cell);
        var unpacked = packed.Unpack();

        // Assert
        Assert.Equal(direction, unpacked.IncomingRiverDirection);
        Assert.Equal(direction, unpacked.OutgoingRiverDirection);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Load_NonexistentFile_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_testDir, "nonexistent.region");

        // Act
        var result = await _serializer.LoadAsync(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Load_InvalidMagicNumber_ReturnsNull()
    {
        // Arrange
        var path = Path.Combine(_testDir, "invalid.region");
        await File.WriteAllBytesAsync(path, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });

        // Act
        var result = await _serializer.LoadAsync(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Save_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Cancel Test", 200, 200, 1);
        var path = Path.Combine(_testDir, "cancel.region");
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var result = await _serializer.SaveAsync(region, path, cts.Token);
        Assert.False(result);
    }

    #endregion

    #region File Size Tests

    [Fact]
    public async Task Save_200x200Region_ProducesReasonableFileSize()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Size Test", 200, 200, 1);
        var path = Path.Combine(_testDir, "size.region");

        // Act
        await _serializer.SaveAsync(region, path);
        var fileInfo = new FileInfo(path);

        // Assert - should be around 640KB (40000 cells * 16 bytes + metadata)
        // Allow some margin for metadata
        Assert.True(fileInfo.Length < 1_000_000, $"File size {fileInfo.Length} bytes exceeds 1MB limit");
        Assert.True(fileInfo.Length > 600_000, $"File size {fileInfo.Length} bytes is suspiciously small");
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public async Task SaveAndLoad_SameRegion_ProducesDeterministicOutput()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Determinism Test", 50, 50, 42);
        var path1 = Path.Combine(_testDir, "det1.region");
        var path2 = Path.Combine(_testDir, "det2.region");

        // Act
        await _serializer.SaveAsync(region, path1);
        await _serializer.SaveAsync(region, path2);

        // Assert
        var bytes1 = await File.ReadAllBytesAsync(path1);
        var bytes2 = await File.ReadAllBytesAsync(path2);
        Assert.Equal(bytes1, bytes2);
    }

    #endregion
}
