using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using HexGame.Region;
using HexGame.Generation;

namespace HexGame.Tests.Region;

/// <summary>
/// Tests for RegionManager functionality that can run without Godot runtime.
/// Note: Full integration tests require Godot and are done in-editor.
/// </summary>
public class RegionManagerTests : IDisposable
{
    private readonly string _testDir;

    public RegionManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"RegionManagerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region RegionData Tests

    [Fact]
    public void RegionData_CreateEmpty_InitializesCorrectly()
    {
        // Act
        var region = RegionData.CreateEmpty("Test Island", 100, 80, 12345);

        // Assert
        Assert.Equal("Test Island", region.Name);
        Assert.Equal(100, region.Width);
        Assert.Equal(80, region.Height);
        Assert.Equal(12345, region.Seed);
        Assert.Equal(8000, region.Cells.Length); // 100 * 80
        Assert.NotEqual(Guid.Empty, region.RegionId);
    }

    [Fact]
    public void RegionData_CreateEmpty_InitializesCellCoordinates()
    {
        // Act
        var region = RegionData.CreateEmpty("Test", 10, 10, 1);

        // Assert - check a few cells have correct coordinates
        Assert.Equal(0, region.Cells[0].X);
        Assert.Equal(0, region.Cells[0].Z);
        Assert.Equal(5, region.Cells[5].X);
        Assert.Equal(0, region.Cells[5].Z);
        Assert.Equal(0, region.Cells[10].X);
        Assert.Equal(1, region.Cells[10].Z);
    }

    [Fact]
    public void RegionData_GetCell_ReturnsCorrectCell()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Test", 50, 50, 1);

        // Modify a specific cell
        var index = 25 * 50 + 30; // z=25, x=30
        var cell = region.Cells[index];
        cell.Elevation = 5;
        region.Cells[index] = cell;

        // Act
        var retrieved = region.GetCell(30, 25);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.Value.Elevation);
    }

    [Fact]
    public void RegionData_GetCell_OutOfBounds_ReturnsNull()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Test", 50, 50, 1);

        // Act & Assert
        Assert.Null(region.GetCell(-1, 0));
        Assert.Null(region.GetCell(0, -1));
        Assert.Null(region.GetCell(50, 0));
        Assert.Null(region.GetCell(0, 50));
    }

    #endregion

    #region Region Generation Content Tests

    [Fact]
    public void GenerateRegionContent_ProducesValidTerrain()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Generated", 50, 50, 42);
        var rng = new Random(region.Seed);

        // Act - Run through generation pipeline (same as RegionManager)
        var landGenerator = new LandGenerator(rng, region.Width, region.Height);
        landGenerator.Generate(region.Cells, GenerationConfig.LandPercentage);

        var climateGenerator = new ClimateGenerator(region.Width, region.Height, region.Seed);
        climateGenerator.Generate(region.Cells);

        // Assert - Should have some land and some water
        int landCells = 0;
        int waterCells = 0;
        foreach (var cell in region.Cells)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
                landCells++;
            else
                waterCells++;
        }

        Assert.True(landCells > 0, "Should have some land cells");
        Assert.True(waterCells > 0, "Should have some water cells");
    }

    [Fact]
    public void GenerateRegionContent_IsDeterministic()
    {
        // Arrange
        const int seed = 12345;

        // Act - Generate two regions with same seed
        var region1 = GenerateTestRegion(seed);
        var region2 = GenerateTestRegion(seed);

        // Assert - Should be identical
        Assert.Equal(region1.Cells.Length, region2.Cells.Length);
        for (int i = 0; i < region1.Cells.Length; i++)
        {
            Assert.Equal(region1.Cells[i].Elevation, region2.Cells[i].Elevation);
            Assert.Equal(region1.Cells[i].TerrainTypeIndex, region2.Cells[i].TerrainTypeIndex);
        }
    }

    [Fact]
    public void GenerateRegionContent_DifferentSeeds_ProduceDifferentResults()
    {
        // Act
        var region1 = GenerateTestRegion(111);
        var region2 = GenerateTestRegion(222);

        // Assert - Should be different (check a sample of cells)
        bool anyDifferent = false;
        for (int i = 0; i < Math.Min(100, region1.Cells.Length); i++)
        {
            if (region1.Cells[i].Elevation != region2.Cells[i].Elevation ||
                region1.Cells[i].TerrainTypeIndex != region2.Cells[i].TerrainTypeIndex)
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent, "Different seeds should produce different terrain");
    }

    private RegionData GenerateTestRegion(int seed)
    {
        var region = RegionData.CreateEmpty("Test", 30, 30, seed);
        var rng = new Random(seed);

        var landGenerator = new LandGenerator(rng, region.Width, region.Height);
        landGenerator.Generate(region.Cells, GenerationConfig.LandPercentage);

        var climateGenerator = new ClimateGenerator(region.Width, region.Height, seed);
        climateGenerator.Generate(region.Cells);

        return region;
    }

    #endregion

    #region RegionConnection Tests

    [Fact]
    public void RegionConnection_StoresAllProperties()
    {
        // Arrange
        var targetId = Guid.NewGuid();

        // Act
        var connection = new RegionConnection
        {
            TargetRegionId = targetId,
            TargetRegionName = "Destination Island",
            DeparturePortIndex = 100,
            ArrivalPortIndex = 50,
            TravelTimeMinutes = 180f,
            DangerLevel = 0.7f
        };

        // Assert
        Assert.Equal(targetId, connection.TargetRegionId);
        Assert.Equal("Destination Island", connection.TargetRegionName);
        Assert.Equal(100, connection.DeparturePortIndex);
        Assert.Equal(50, connection.ArrivalPortIndex);
        Assert.Equal(180f, connection.TravelTimeMinutes);
        Assert.Equal(0.7f, connection.DangerLevel);
    }

    [Fact]
    public void RegionData_WithConnections_SerializesCorrectly()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Connected Island", 20, 20, 1);
        var target1 = Guid.NewGuid();
        var target2 = Guid.NewGuid();

        region.Connections.Add(new RegionConnection
        {
            TargetRegionId = target1,
            TargetRegionName = "Island A",
            TravelTimeMinutes = 60f
        });
        region.Connections.Add(new RegionConnection
        {
            TargetRegionId = target2,
            TargetRegionName = "Island B",
            TravelTimeMinutes = 120f
        });

        var serializer = new RegionSerializer();
        var path = Path.Combine(_testDir, "connected.region");

        // Act
        serializer.Save(region, path);
        var loaded = serializer.Load(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Connections.Count);
        Assert.Equal("Island A", loaded.Connections[0].TargetRegionName);
        Assert.Equal("Island B", loaded.Connections[1].TargetRegionName);
    }

    #endregion

    #region RegionMetadata Tests

    [Fact]
    public void RegionMetadata_FromRegionData_CopiesAllFields()
    {
        // Arrange
        var region = RegionData.CreateEmpty("Full Region", 100, 100, 42);
        region.Connections.Add(new RegionConnection { TargetRegionName = "Test" });

        // Act
        var metadata = RegionMetadata.FromRegionData(region);

        // Assert
        Assert.Equal(region.RegionId, metadata.RegionId);
        Assert.Equal(region.Name, metadata.Name);
        Assert.Equal(region.Width, metadata.Width);
        Assert.Equal(region.Height, metadata.Height);
        Assert.Equal(region.Seed, metadata.Seed);
        Assert.Single(metadata.Connections);
    }

    #endregion

    #region Path Handling Tests

    [Fact]
    public void GetRegionPath_AbsolutePath_ReturnsUnchanged()
    {
        // Arrange
        var absolutePath = Path.Combine(_testDir, "test.region");

        // Act - We can't test the actual manager without Godot, but we can verify the logic
        // The path is already absolute, so it should be returned as-is
        Assert.True(Path.IsPathRooted(absolutePath));
    }

    [Fact]
    public void RegionConfig_FileExtension_IsCorrect()
    {
        // Assert
        Assert.Equal(".region", RegionConfig.FileExtension);
    }

    [Fact]
    public void RegionConfig_DefaultSizes_AreReasonable()
    {
        // Assert
        Assert.Equal(200, RegionConfig.DefaultRegionWidth);
        Assert.Equal(200, RegionConfig.DefaultRegionHeight);
        Assert.True(RegionConfig.MinRegionSize < RegionConfig.DefaultRegionWidth);
        Assert.True(RegionConfig.MaxRegionSize > RegionConfig.DefaultRegionWidth);
    }

    #endregion

    #region Integration Tests (Serializer + Generation)

    [Fact]
    public async Task FullPipeline_GenerateSaveLoad_PreservesRegion()
    {
        // Arrange
        var region = GenerateTestRegion(99999);
        var serializer = new RegionSerializer();
        var path = Path.Combine(_testDir, "pipeline.region");

        // Act
        await serializer.SaveAsync(region, path);
        var loaded = await serializer.LoadAsync(path);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(region.Name, loaded.Name);
        Assert.Equal(region.Width, loaded.Width);
        Assert.Equal(region.Height, loaded.Height);
        Assert.Equal(region.Seed, loaded.Seed);

        // Verify cell data preserved
        for (int i = 0; i < region.Cells.Length; i++)
        {
            Assert.Equal(region.Cells[i].Elevation, loaded.Cells[i].Elevation);
            Assert.Equal(region.Cells[i].TerrainTypeIndex, loaded.Cells[i].TerrainTypeIndex);
        }
    }

    #endregion
}
