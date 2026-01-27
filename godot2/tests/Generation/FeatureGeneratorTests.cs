using System;
using System.Threading;
using FluentAssertions;
using HexGame.Generation;
using Xunit;

namespace HexMapTutorial.Tests.Generation;

/// <summary>
/// Tests for FeatureGenerator - feature placement based on biomes and terrain.
/// </summary>
public class FeatureGeneratorTests
{
    #region Basic Functionality

    [Fact]
    public void Generate_WithValidLand_PlacesFeatures()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateGrasslandTerrain(10, 10);

        generator.Generate(data);

        // At least some cells should have features
        int cellsWithFeatures = 0;
        foreach (var cell in data)
        {
            if (cell.PlantLevel > 0 || cell.FarmLevel > 0 || cell.UrbanLevel > 0 || cell.SpecialIndex > 0)
                cellsWithFeatures++;
        }
        cellsWithFeatures.Should().BeGreaterThan(0, "features should be placed on valid land");
    }

    [Fact]
    public void Generate_SkipsUnderwaterCells()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateMixedTerrain(10, 10);

        generator.Generate(data);

        // Check underwater cells have no features
        foreach (var cell in data)
        {
            if (cell.Elevation < GenerationConfig.WaterLevel)
            {
                cell.PlantLevel.Should().Be(0, "underwater cells should have no plants");
                cell.FarmLevel.Should().Be(0, "underwater cells should have no farms");
                cell.UrbanLevel.Should().Be(0, "underwater cells should have no urban");
                cell.SpecialIndex.Should().Be(0, "underwater cells should have no special features");
            }
        }
    }

    [Fact]
    public void Generate_SkipsCellsWithRivers()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateGrasslandTerrain(10, 10);

        // Add rivers to some cells
        data[25].HasIncomingRiver = true;
        data[25].IncomingRiverDirection = 4;
        data[35].HasOutgoingRiver = true;
        data[35].OutgoingRiverDirection = 1;

        generator.Generate(data);

        // River cells should have no features
        data[25].PlantLevel.Should().Be(0, "cells with incoming rivers should have no plants");
        data[25].FarmLevel.Should().Be(0, "cells with incoming rivers should have no farms");
        data[25].UrbanLevel.Should().Be(0, "cells with incoming rivers should have no urban");
        data[35].PlantLevel.Should().Be(0, "cells with outgoing rivers should have no plants");
        data[35].FarmLevel.Should().Be(0, "cells with outgoing rivers should have no farms");
        data[35].UrbanLevel.Should().Be(0, "cells with outgoing rivers should have no urban");
    }

    [Fact]
    public void Generate_DeterministicWithSameSeed()
    {
        var data1 = CreateGrasslandTerrain(10, 10);
        var data2 = CreateGrasslandTerrain(10, 10);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var generator1 = new FeatureGenerator(rng1, 10, 10);
        var generator2 = new FeatureGenerator(rng2, 10, 10);

        generator1.Generate(data1);
        generator2.Generate(data2);

        // Results should be identical
        for (int i = 0; i < data1.Length; i++)
        {
            data1[i].PlantLevel.Should().Be(data2[i].PlantLevel, $"cell {i} PlantLevel should match");
            data1[i].FarmLevel.Should().Be(data2[i].FarmLevel, $"cell {i} FarmLevel should match");
            data1[i].UrbanLevel.Should().Be(data2[i].UrbanLevel, $"cell {i} UrbanLevel should match");
            data1[i].SpecialIndex.Should().Be(data2[i].SpecialIndex, $"cell {i} SpecialIndex should match");
        }
    }

    #endregion

    #region Biome-based Placement

    [Fact]
    public void PlaceDensityFeatures_SandTerrain_NoPlantsLowUrban()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateTerrainOfType(10, 10, 0); // Sand

        generator.Generate(data);

        // Sand should have no plants
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
            {
                cell.PlantLevel.Should().Be(0, "sand terrain should have no plants");
            }
        }

        // Sand can have some sparse urban/farm (0-1)
        int maxUrban = 0;
        int maxFarm = 0;
        foreach (var cell in data)
        {
            if (cell.UrbanLevel > maxUrban) maxUrban = cell.UrbanLevel;
            if (cell.FarmLevel > maxFarm) maxFarm = cell.FarmLevel;
        }
        maxUrban.Should().BeLessThanOrEqualTo(1, "sand urban should be sparse (0-1)");
        maxFarm.Should().BeLessThanOrEqualTo(1, "sand farm should be sparse (0-1)");
    }

    [Fact]
    public void PlaceDensityFeatures_GrassTerrain_AllFeatureTypes()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 20, 20);
        var data = CreateTerrainOfType(20, 20, 1); // Grass

        // Add moisture variation
        for (int i = 0; i < data.Length; i++)
        {
            data[i].Moisture = (float)(i % 10) / 10f;
        }

        generator.Generate(data);

        // Grass should have all types of features
        bool hasPlants = false;
        bool hasFarms = false;
        bool hasUrban = false;

        foreach (var cell in data)
        {
            if (cell.PlantLevel > 0) hasPlants = true;
            if (cell.FarmLevel > 0) hasFarms = true;
            if (cell.UrbanLevel > 0) hasUrban = true;
        }

        hasPlants.Should().BeTrue("grass terrain should have plants");
        hasFarms.Should().BeTrue("grass terrain should have farms");
        hasUrban.Should().BeTrue("grass terrain should have urban areas");
    }

    [Fact]
    public void PlaceDensityFeatures_MudTerrain_HighPlants()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateTerrainOfType(10, 10, 2); // Mud

        // Set high moisture for jungle
        foreach (ref var cell in data.AsSpan())
        {
            cell.Moisture = 0.8f;
        }

        generator.Generate(data);

        // Mud should have high plant levels (2-3)
        int totalPlants = 0;
        int eligibleCells = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel &&
                !cell.HasIncomingRiver && !cell.HasOutgoingRiver &&
                cell.SpecialIndex == 0)
            {
                eligibleCells++;
                totalPlants += cell.PlantLevel;
            }
        }

        if (eligibleCells > 0)
        {
            float avgPlants = (float)totalPlants / eligibleCells;
            avgPlants.Should().BeGreaterThan(1.5f, "mud terrain should have dense vegetation");
        }

        // Mud should have no urban
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel && cell.SpecialIndex == 0)
            {
                cell.UrbanLevel.Should().Be(0, "mud terrain should have no urban areas");
            }
        }
    }

    [Fact]
    public void PlaceDensityFeatures_StoneTerrain_SparseFeatures()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateTerrainOfType(10, 10, 3); // Stone

        generator.Generate(data);

        // Stone should have sparse features (0-1 for most)
        int maxPlant = 0;
        int maxFarm = 0;
        int maxUrban = 0;

        foreach (var cell in data)
        {
            if (cell.PlantLevel > maxPlant) maxPlant = cell.PlantLevel;
            if (cell.FarmLevel > maxFarm) maxFarm = cell.FarmLevel;
            if (cell.UrbanLevel > maxUrban) maxUrban = cell.UrbanLevel;
        }

        maxPlant.Should().BeLessThanOrEqualTo(1, "stone plant should be sparse (0-1)");
        maxFarm.Should().BeLessThanOrEqualTo(1, "stone farm should be sparse (0-1)");
        maxUrban.Should().BeLessThanOrEqualTo(1, "stone urban should be sparse (0-1)");
    }

    [Fact]
    public void PlaceDensityFeatures_SnowTerrain_NoFeatures()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateTerrainOfType(10, 10, 4); // Snow

        generator.Generate(data);

        // Snow should have no density features
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel)
            {
                cell.PlantLevel.Should().Be(0, "snow terrain should have no plants");
                cell.FarmLevel.Should().Be(0, "snow terrain should have no farms");
                cell.UrbanLevel.Should().Be(0, "snow terrain should have no urban");
            }
        }
    }

    #endregion

    #region Special Features

    [Fact]
    public void PlaceSpecialFeatures_Castle_RequiresHighElevation()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 20, 20);
        var data = new CellData[400];

        // Create grass terrain with varying elevation
        for (int i = 0; i < 400; i++)
        {
            data[i] = new CellData(i % 20, i / 20)
            {
                Elevation = GenerationConfig.WaterLevel + (i % 8), // Elevation 1-8
                TerrainTypeIndex = 1, // Grass
                Moisture = 0.5f
            };
        }

        // Force special feature on all cells (for testing)
        for (int i = 0; i < data.Length; i++)
        {
            ref var cell = ref data[i];
            int special = generator.GetSpecialFeatureIndex(ref cell);
            if (special == 1) // Castle
            {
                cell.Elevation.Should().BeGreaterThanOrEqualTo(GenerationConfig.CastleMinElevation,
                    "castles should only appear on high elevation");
            }
        }
    }

    [Fact]
    public void PlaceSpecialFeatures_Ziggurat_OnSandTerrain()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            TerrainTypeIndex = 0, // Sand
            Moisture = 0.2f
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(2, "sand terrain should produce ziggurat special feature");
    }

    [Fact]
    public void PlaceSpecialFeatures_Megaflora_OnMudTerrain()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            TerrainTypeIndex = 2, // Mud
            Moisture = 0.8f // High moisture
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(3, "high moisture mud terrain should produce megaflora");
    }

    [Fact]
    public void PlaceSpecialFeatures_Megaflora_RequiresHighMoisture()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);

        var lowMoistureCell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            TerrainTypeIndex = 2, // Mud
            Moisture = 0.5f // Not high enough
        };

        int special = generator.GetSpecialFeatureIndex(ref lowMoistureCell);

        special.Should().Be(0, "low moisture mud should not produce megaflora");
    }

    [Fact]
    public void PlaceSpecialFeatures_Snow_NoCastle_EvenAtHighElevation()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.CastleMinElevation + 2, // High elevation
            TerrainTypeIndex = 4, // Snow
            Moisture = 0.5f
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(0, "snow terrain should not produce castles even at high elevation");
    }

    [Fact]
    public void PlaceSpecialFeatures_ClearsOtherFeatures()
    {
        var rng = new Random(42); // Use seed that produces special features
        var generator = new FeatureGenerator(rng, 20, 20);
        var data = CreateTerrainOfType(20, 20, 0); // Sand (for ziggurats)

        generator.Generate(data);

        // Cells with special features should have no density features
        foreach (var cell in data)
        {
            if (cell.SpecialIndex > 0)
            {
                cell.PlantLevel.Should().Be(0, "special feature cells should have no plants");
                cell.FarmLevel.Should().Be(0, "special feature cells should have no farms");
                cell.UrbanLevel.Should().Be(0, "special feature cells should have no urban");
            }
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generate_EmptyData_NoException()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 0, 0);
        var data = Array.Empty<CellData>();

        var act = () => generator.Generate(data);

        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_CancellationToken_Throws()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 50, 50);
        var data = CreateGrasslandTerrain(50, 50);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => generator.Generate(data, cts.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Generate_AllWater_NoFeatures()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 10, 10);
        var data = CreateUnderwaterGrid(10, 10);

        generator.Generate(data);

        // All cells should have no features
        foreach (var cell in data)
        {
            cell.PlantLevel.Should().Be(0);
            cell.FarmLevel.Should().Be(0);
            cell.UrbanLevel.Should().Be(0);
            cell.SpecialIndex.Should().Be(0);
        }
    }

    [Fact]
    public void CanPlaceFeature_UnderwaterCell_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 5, 5);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel - 1
        };

        bool canPlace = generator.CanPlaceFeature(ref cell);

        canPlace.Should().BeFalse("underwater cells should not be eligible for features");
    }

    [Fact]
    public void CanPlaceFeature_CellWithRiver_ReturnsFalse()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 5, 5);

        var cellWithIncoming = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            HasIncomingRiver = true
        };

        var cellWithOutgoing = new CellData(1, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            HasOutgoingRiver = true
        };

        generator.CanPlaceFeature(ref cellWithIncoming).Should().BeFalse(
            "cells with incoming rivers should not be eligible");
        generator.CanPlaceFeature(ref cellWithOutgoing).Should().BeFalse(
            "cells with outgoing rivers should not be eligible");
    }

    [Fact]
    public void CanPlaceFeature_ValidLandCell_ReturnsTrue()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 5, 5);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            HasIncomingRiver = false,
            HasOutgoingRiver = false
        };

        bool canPlace = generator.CanPlaceFeature(ref cell);

        canPlace.Should().BeTrue("valid land cells without rivers should be eligible");
    }

    #endregion

    #region Statistical Tests

    [Fact]
    public void Generate_FeaturePlacementChance_StatisticallyCorrect()
    {
        // Run multiple trials and verify ~70% of eligible cells get features
        int totalEligibleCells = 0;
        int totalCellsWithFeatures = 0;

        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(10000 + trial);
            var generator = new FeatureGenerator(rng, 10, 10);
            var data = CreateGrasslandTerrain(10, 10);

            generator.Generate(data);

            foreach (var cell in data)
            {
                if (cell.Elevation >= GenerationConfig.WaterLevel)
                {
                    totalEligibleCells++;
                    if (cell.PlantLevel > 0 || cell.FarmLevel > 0 ||
                        cell.UrbanLevel > 0 || cell.SpecialIndex > 0)
                        totalCellsWithFeatures++;
                }
            }
        }

        float actualRatio = (float)totalCellsWithFeatures / totalEligibleCells;
        // Should be approximately FeaturePlacementChance (0.7) with tolerance
        // Account for special features also consuming some placement chances
        actualRatio.Should().BeInRange(0.60f, 0.80f,
            $"feature placement should be approximately 70% (was {actualRatio:P1})");
    }

    [Fact]
    public void Generate_SpecialFeatureChance_StatisticallyCorrect()
    {
        // SpecialFeatureChance = 0.02 (2%)
        int totalEligibleCells = 0;
        int totalSpecialFeatures = 0;

        for (int trial = 0; trial < 200; trial++)
        {
            var rng = new Random(20000 + trial);
            var generator = new FeatureGenerator(rng, 20, 20);
            var data = CreateTerrainOfType(20, 20, 0); // Sand for ziggurats

            generator.Generate(data);

            foreach (var cell in data)
            {
                if (cell.Elevation >= GenerationConfig.WaterLevel)
                {
                    totalEligibleCells++;
                    if (cell.SpecialIndex > 0)
                        totalSpecialFeatures++;
                }
            }
        }

        float actualRatio = (float)totalSpecialFeatures / totalEligibleCells;
        actualRatio.Should().BeInRange(0.01f, 0.04f,
            $"special feature rate should be approximately 2% (was {actualRatio:P2})");
    }

    [Fact]
    public void Generate_DifferentSeeds_ProduceDifferentResults()
    {
        var data1 = CreateGrasslandTerrain(15, 15);
        var data2 = CreateGrasslandTerrain(15, 15);

        var rng1 = new Random(11111);
        var rng2 = new Random(22222);

        var generator1 = new FeatureGenerator(rng1, 15, 15);
        var generator2 = new FeatureGenerator(rng2, 15, 15);

        generator1.Generate(data1);
        generator2.Generate(data2);

        // Results should differ significantly
        int differences = 0;
        for (int i = 0; i < data1.Length; i++)
        {
            if (data1[i].PlantLevel != data2[i].PlantLevel ||
                data1[i].FarmLevel != data2[i].FarmLevel ||
                data1[i].UrbanLevel != data2[i].UrbanLevel ||
                data1[i].SpecialIndex != data2[i].SpecialIndex)
                differences++;
        }

        differences.Should().BeGreaterThanOrEqualTo(data1.Length / 10,
            "different seeds should produce significantly different features");
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData(0.29f, 1)]  // Just below medium threshold (0.3) -> level 1
    [InlineData(0.31f, 1)]  // Just above medium threshold -> level 1 or 2 (random)
    [InlineData(0.59f, 1)]  // Just below high threshold (0.6) -> level 1 or 2 (random)
    [InlineData(0.61f, 2)]  // Just above high threshold -> level 2
    public void GetPlantLevel_GrassTerrain_MoistureThresholds(float moisture, int expectedMinLevel)
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            TerrainTypeIndex = 1, // Grass
            Moisture = moisture
        };

        int plantLevel = generator.GetPlantLevel(ref cell);

        plantLevel.Should().BeGreaterThanOrEqualTo(expectedMinLevel,
            $"grass with moisture {moisture} should have at least level {expectedMinLevel} plants");
    }

    [Theory]
    [InlineData(GenerationConfig.CastleMinElevation - 1, 0)] // Just below castle threshold
    [InlineData(GenerationConfig.CastleMinElevation, 1)]     // At threshold (castle)
    [InlineData(GenerationConfig.CastleMinElevation + 1, 1)] // Above threshold (castle)
    public void GetSpecialFeatureIndex_GrassTerrain_ElevationThreshold(
        int elevation, int expectedSpecialIndex)
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng);

        var cell = new CellData(0, 0)
        {
            Elevation = elevation,
            TerrainTypeIndex = 1, // Grass
            Moisture = 0.5f
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(expectedSpecialIndex,
            $"grass terrain at elevation {elevation} should have special index {expectedSpecialIndex}");
    }

    [Theory]
    [InlineData(0.69f, 0)]  // Just below megaflora threshold (0.7) - uses > not >=
    [InlineData(0.70f, 0)]  // At threshold - still no megaflora (uses >)
    [InlineData(0.71f, 3)]  // Just above threshold (megaflora)
    public void GetSpecialFeatureIndex_MudTerrain_MoistureThreshold(
        float moisture, int expectedSpecialIndex)
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.WaterLevel + 1,
            TerrainTypeIndex = 2, // Mud
            Moisture = moisture
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(expectedSpecialIndex,
            $"mud terrain with moisture {moisture} should have special index {expectedSpecialIndex}");
    }

    [Fact]
    public void PlaceSpecialFeatures_Castle_OnStoneTerrain()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng);

        var cell = new CellData(0, 0)
        {
            Elevation = GenerationConfig.CastleMinElevation,
            TerrainTypeIndex = 3, // Stone
            Moisture = 0.5f
        };

        int special = generator.GetSpecialFeatureIndex(ref cell);

        special.Should().Be(1, "high elevation stone terrain should produce castle");
    }

    [Fact]
    public void Generate_AllFeatureLevels_WithinValidRange()
    {
        var rng = new Random(12345);
        var generator = new FeatureGenerator(rng, 30, 30);
        var data = CreateGrasslandTerrain(30, 30);

        // Add moisture variation
        for (int i = 0; i < data.Length; i++)
            data[i].Moisture = (float)(i % 100) / 100f;

        generator.Generate(data);

        foreach (var cell in data)
        {
            cell.PlantLevel.Should().BeInRange(0, 3, "PlantLevel should be 0-3");
            cell.FarmLevel.Should().BeInRange(0, 2, "FarmLevel should be 0-2");
            cell.UrbanLevel.Should().BeInRange(0, 2, "UrbanLevel should be 0-2");
            cell.SpecialIndex.Should().BeInRange(0, 3, "SpecialIndex should be 0-3");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_FullPipeline_LandClimateRiverFeatures_WorkTogether()
    {
        const int seed = 54321;
        const int width = 25;
        const int height = 25;

        // Initialize cell data
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z);
            }
        }

        // Step 1: Land generation
        var landRng = new Random(seed);
        var landGenerator = new LandGenerator(landRng, width, height);
        landGenerator.Generate(data, 0.6f);

        // Step 2: Climate generation
        var climateGenerator = new ClimateGenerator(width, height, seed);
        climateGenerator.Generate(data);

        // Step 3: River generation
        var riverRng = new Random(seed + GenerationConfig.RiverSeedOffset);
        var riverGenerator = new RiverGenerator(riverRng, width, height);
        riverGenerator.Generate(data);

        // Step 4: Feature generation
        var featureRng = new Random(seed + GenerationConfig.FeatureSeedOffset);
        var featureGenerator = new FeatureGenerator(featureRng, width, height);
        featureGenerator.Generate(data);

        // Verify integration invariants
        foreach (var cell in data)
        {
            // Features should not appear on river cells
            if (cell.HasIncomingRiver || cell.HasOutgoingRiver)
            {
                cell.PlantLevel.Should().Be(0, "river cells should have no plants");
                cell.FarmLevel.Should().Be(0, "river cells should have no farms");
                cell.UrbanLevel.Should().Be(0, "river cells should have no urban");
            }

            // Features should not appear underwater
            if (cell.Elevation < GenerationConfig.WaterLevel)
            {
                cell.PlantLevel.Should().Be(0, "underwater cells should have no plants");
                cell.FarmLevel.Should().Be(0, "underwater cells should have no farms");
                cell.UrbanLevel.Should().Be(0, "underwater cells should have no urban");
                cell.SpecialIndex.Should().Be(0, "underwater cells should have no special features");
            }

            // Special features should clear density features
            if (cell.SpecialIndex > 0)
            {
                cell.PlantLevel.Should().Be(0, "special feature cells should have no plants");
                cell.FarmLevel.Should().Be(0, "special feature cells should have no farms");
                cell.UrbanLevel.Should().Be(0, "special feature cells should have no urban");
            }
        }

        // Verify biome-appropriate features exist
        int landCells = 0;
        int cellsWithFeatures = 0;
        foreach (var cell in data)
        {
            if (cell.Elevation >= GenerationConfig.WaterLevel &&
                !cell.HasIncomingRiver && !cell.HasOutgoingRiver)
            {
                landCells++;
                if (cell.PlantLevel > 0 || cell.FarmLevel > 0 ||
                    cell.UrbanLevel > 0 || cell.SpecialIndex > 0)
                    cellsWithFeatures++;
            }
        }

        cellsWithFeatures.Should().BeGreaterThan(0, "integration should produce some features");
        landCells.Should().BeGreaterThan(0, "should have some eligible land cells");
    }

    #endregion

    #region Helper Methods

    private static CellData[] CreateGrasslandTerrain(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z)
                {
                    Elevation = GenerationConfig.WaterLevel + 1,
                    TerrainTypeIndex = 1, // Grass
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    private static CellData[] CreateMixedTerrain(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                // Alternate between underwater and land
                bool isLand = (x + z) % 2 == 0;
                data[index] = new CellData(x, z)
                {
                    Elevation = isLand ? GenerationConfig.WaterLevel + 1 : GenerationConfig.MinElevation,
                    TerrainTypeIndex = 1, // Grass
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    private static CellData[] CreateTerrainOfType(int width, int height, int terrainTypeIndex)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z)
                {
                    Elevation = GenerationConfig.WaterLevel + 1,
                    TerrainTypeIndex = terrainTypeIndex,
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    private static CellData[] CreateUnderwaterGrid(int width, int height)
    {
        var data = new CellData[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                data[index] = new CellData(x, z)
                {
                    Elevation = GenerationConfig.MinElevation,
                    Moisture = 0.5f
                };
            }
        }
        return data;
    }

    #endregion
}
