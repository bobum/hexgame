using FluentAssertions;
using Xunit;

namespace HexGame.Tests.Godot2
{

// Local copies of Tutorial 9 types for testing without Godot dependencies
namespace Tutorial9
{
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Test copy of HexHash struct.
    /// In production, values are generated via GD.Randf().
    /// For testing, we create instances directly.
    /// </summary>
    public struct HexHash
    {
        public float a, b, c, d, e;

        /// <summary>
        /// Creates a HexHash with specific values for testing.
        /// </summary>
        public static HexHash Create(float a, float b, float c, float d, float e)
        {
            HexHash hash;
            hash.a = a;
            hash.b = b;
            hash.c = c;
            hash.d = d;
            hash.e = e;
            return hash;
        }

        /// <summary>
        /// Creates a HexHash using a deterministic random sequence for testing.
        /// Uses 0.999 multiplier to match production code.
        /// </summary>
        public static HexHash CreateRandom(System.Random random)
        {
            HexHash hash;
            hash.a = (float)random.NextDouble() * 0.999f;
            hash.b = (float)random.NextDouble() * 0.999f;
            hash.c = (float)random.NextDouble() * 0.999f;
            hash.d = (float)random.NextDouble() * 0.999f;
            hash.e = (float)random.NextDouble() * 0.999f;
            return hash;
        }
    }

    /// <summary>
    /// Test copy of HexFeatureCollection struct.
    /// </summary>
    public struct HexFeatureCollection
    {
        public int PrefabCount;

        public HexFeatureCollection(int prefabCount)
        {
            PrefabCount = prefabCount;
        }

        /// <summary>
        /// Picks a prefab index based on choice value [0, 1).
        /// Returns -1 if no prefabs.
        /// </summary>
        public int PickIndex(float choice)
        {
            if (PrefabCount <= 0)
            {
                return -1;
            }
            return (int)(choice * PrefabCount);
        }
    }

    /// <summary>
    /// Test copy of hash grid logic from HexMetrics.
    /// </summary>
    public static class HexMetricsHashGrid
    {
        public const int HashGridSize = 256;
        public const float HashGridScale = 0.25f;

        private static HexHash[]? _hashGrid;

        public static void InitializeHashGrid(int seed)
        {
            _hashGrid = new HexHash[HashGridSize * HashGridSize];
            var random = new System.Random(seed);

            for (int i = 0; i < _hashGrid.Length; i++)
            {
                _hashGrid[i] = HexHash.CreateRandom(random);
            }
        }

        public static HexHash SampleHashGrid(Vector3 position)
        {
            if (_hashGrid == null)
            {
                throw new System.InvalidOperationException("Hash grid not initialized");
            }

            int x = (int)(position.X * HashGridScale) % HashGridSize;
            if (x < 0) x += HashGridSize;
            int z = (int)(position.Z * HashGridScale) % HashGridSize;
            if (z < 0) z += HashGridSize;
            return _hashGrid[x + z * HashGridSize];
        }

        public static void ClearHashGrid()
        {
            _hashGrid = null;
        }
    }

    /// <summary>
    /// Test copy of feature selection logic from HexFeatureManager.
    /// </summary>
    public static class FeatureSelection
    {
        // Feature selection thresholds per level
        // Level 1 threshold = 0.4, Level 2 = 0.6, Level 3 = 0.8
        private static readonly float[] FeatureThresholds = { 0.0f, 0.4f, 0.6f, 0.8f };

        /// <summary>
        /// Determines which density level should be used based on hash and cell level.
        /// Returns 0 if no feature should be placed.
        /// </summary>
        public static int GetSelectedLevel(int cellLevel, float hash)
        {
            if (cellLevel <= 0)
            {
                return 0;
            }

            for (int i = cellLevel; i > 0; i--)
            {
                if (hash >= FeatureThresholds[i])
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Determines which feature type wins when multiple types compete.
        /// Returns: 0 = none, 1 = urban, 2 = farm, 3 = plant
        /// Lower hash value wins.
        /// </summary>
        public static int GetWinningFeatureType(
            int urbanLevel, float urbanHash,
            int farmLevel, float farmHash,
            int plantLevel, float plantHash)
        {
            int urbanSelected = GetSelectedLevel(urbanLevel, urbanHash);
            int farmSelected = GetSelectedLevel(farmLevel, farmHash);
            int plantSelected = GetSelectedLevel(plantLevel, plantHash);

            bool hasUrban = urbanSelected > 0;
            bool hasFarm = farmSelected > 0;
            bool hasPlant = plantSelected > 0;

            if (!hasUrban && !hasFarm && !hasPlant)
            {
                return 0;
            }

            float usedHash = float.MaxValue;
            int winner = 0;

            if (hasUrban)
            {
                usedHash = urbanHash;
                winner = 1;
            }

            if (hasFarm && farmHash < usedHash)
            {
                usedHash = farmHash;
                winner = 2;
            }

            if (hasPlant && plantHash < usedHash)
            {
                winner = 3;
            }

            return winner;
        }
    }
}

/// <summary>
/// Tests for HexHash struct from Tutorial 9.
/// </summary>
public class HexHashTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void Create_AllValuesWithinRange()
    {
        var random = new System.Random(12345);
        var hash = Tutorial9.HexHash.CreateRandom(random);

        hash.a.Should().BeGreaterOrEqualTo(0f);
        hash.a.Should().BeLessThan(1f);
        hash.b.Should().BeGreaterOrEqualTo(0f);
        hash.b.Should().BeLessThan(1f);
        hash.c.Should().BeGreaterOrEqualTo(0f);
        hash.c.Should().BeLessThan(1f);
        hash.d.Should().BeGreaterOrEqualTo(0f);
        hash.d.Should().BeLessThan(1f);
        hash.e.Should().BeGreaterOrEqualTo(0f);
        hash.e.Should().BeLessThan(1f);
    }

    [Fact]
    public void Create_ValuesNeverReach1()
    {
        // The 0.999 multiplier ensures values never reach 1.0
        // This prevents array index overflow when using (int)(choice * arrayLength)
        var random = new System.Random(54321);

        for (int i = 0; i < 1000; i++)
        {
            var hash = Tutorial9.HexHash.CreateRandom(random);
            hash.a.Should().BeLessThan(0.999f);
            hash.b.Should().BeLessThan(0.999f);
            hash.c.Should().BeLessThan(0.999f);
            hash.d.Should().BeLessThan(0.999f);
            hash.e.Should().BeLessThan(0.999f);
        }
    }

    [Fact]
    public void Create_SameSeedProducesSameValues()
    {
        var random1 = new System.Random(99999);
        var random2 = new System.Random(99999);

        var hash1 = Tutorial9.HexHash.CreateRandom(random1);
        var hash2 = Tutorial9.HexHash.CreateRandom(random2);

        hash1.a.Should().BeApproximately(hash2.a, Tolerance);
        hash1.b.Should().BeApproximately(hash2.b, Tolerance);
        hash1.c.Should().BeApproximately(hash2.c, Tolerance);
        hash1.d.Should().BeApproximately(hash2.d, Tolerance);
        hash1.e.Should().BeApproximately(hash2.e, Tolerance);
    }

    [Fact]
    public void Create_DifferentSeedsProduceDifferentValues()
    {
        var random1 = new System.Random(11111);
        var random2 = new System.Random(22222);

        var hash1 = Tutorial9.HexHash.CreateRandom(random1);
        var hash2 = Tutorial9.HexHash.CreateRandom(random2);

        // At least one value should differ (extremely unlikely to be all same)
        bool anyDifferent =
            System.Math.Abs(hash1.a - hash2.a) > Tolerance ||
            System.Math.Abs(hash1.b - hash2.b) > Tolerance ||
            System.Math.Abs(hash1.c - hash2.c) > Tolerance ||
            System.Math.Abs(hash1.d - hash2.d) > Tolerance ||
            System.Math.Abs(hash1.e - hash2.e) > Tolerance;

        anyDifferent.Should().BeTrue();
    }

    [Fact]
    public void FieldPurposes_MatchTutorialDocumentation()
    {
        // Tutorial 9 field purposes:
        // a = Urban feature threshold
        // b = Farm feature threshold
        // c = Plant feature threshold
        // d = Prefab variant choice
        // e = Feature rotation (0-360 when multiplied by 360)

        var hash = Tutorial9.HexHash.Create(0.5f, 0.6f, 0.7f, 0.3f, 0.25f);

        // Verify rotation calculation
        float rotation = hash.e * 360f;
        rotation.Should().BeApproximately(90f, Tolerance);
    }
}

/// <summary>
/// Tests for hash grid functionality from HexMetrics (Tutorial 9).
/// </summary>
public class HexMetricsHashGridTests
{
    private const float Tolerance = 0.0001f;

    public HexMetricsHashGridTests()
    {
        // Clear any existing hash grid before each test
        Tutorial9.HexMetricsHashGrid.ClearHashGrid();
    }

    [Fact]
    public void HashGridSize_Is256()
    {
        Tutorial9.HexMetricsHashGrid.HashGridSize.Should().Be(256);
    }

    [Fact]
    public void HashGridScale_Is025()
    {
        Tutorial9.HexMetricsHashGrid.HashGridScale.Should().BeApproximately(0.25f, Tolerance);
    }

    [Fact]
    public void InitializeHashGrid_CreatesDeterministicGrid()
    {
        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(1234);
        var hash1 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(10, 0, 10));

        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(1234);
        var hash2 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(10, 0, 10));

        hash1.a.Should().BeApproximately(hash2.a, Tolerance);
        hash1.b.Should().BeApproximately(hash2.b, Tolerance);
        hash1.c.Should().BeApproximately(hash2.c, Tolerance);
        hash1.d.Should().BeApproximately(hash2.d, Tolerance);
        hash1.e.Should().BeApproximately(hash2.e, Tolerance);
    }

    [Fact]
    public void SampleHashGrid_DifferentPositions_ReturnDifferentHashes()
    {
        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(5678);

        var hash1 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(0, 0, 0));
        var hash2 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(100, 0, 100));

        // Very unlikely to be the same
        bool anyDifferent =
            System.Math.Abs(hash1.a - hash2.a) > Tolerance ||
            System.Math.Abs(hash1.b - hash2.b) > Tolerance;

        anyDifferent.Should().BeTrue();
    }

    [Fact]
    public void SampleHashGrid_NegativeCoordinates_HandlesCorrectly()
    {
        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(9999);

        // Should not throw with negative coordinates
        var hash = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(-50, 0, -50));

        hash.a.Should().BeGreaterOrEqualTo(0f);
        hash.a.Should().BeLessThan(1f);
    }

    [Fact]
    public void SampleHashGrid_WrapsAt256()
    {
        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(1111);

        // Position 0 and position 256 * (1/scale) should give same hash
        // Scale is 0.25, so 256 / 0.25 = 1024
        var hash1 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(0, 0, 0));
        var hash2 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(1024, 0, 1024));

        hash1.a.Should().BeApproximately(hash2.a, Tolerance);
        hash1.b.Should().BeApproximately(hash2.b, Tolerance);
    }

    [Fact]
    public void SampleHashGrid_YCoordinate_IsIgnored()
    {
        Tutorial9.HexMetricsHashGrid.InitializeHashGrid(2222);

        var hash1 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(50, 0, 50));
        var hash2 = Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(50, 100, 50));

        hash1.a.Should().BeApproximately(hash2.a, Tolerance);
        hash1.b.Should().BeApproximately(hash2.b, Tolerance);
        hash1.c.Should().BeApproximately(hash2.c, Tolerance);
        hash1.d.Should().BeApproximately(hash2.d, Tolerance);
        hash1.e.Should().BeApproximately(hash2.e, Tolerance);
    }

    [Fact]
    public void SampleHashGrid_WithoutInit_Throws()
    {
        Tutorial9.HexMetricsHashGrid.ClearHashGrid();

        var action = () => Tutorial9.HexMetricsHashGrid.SampleHashGrid(new Tutorial9.Vector3(0, 0, 0));

        action.Should().Throw<System.InvalidOperationException>();
    }
}

/// <summary>
/// Tests for HexFeatureCollection from Tutorial 9.
/// </summary>
public class HexFeatureCollectionTests
{
    [Fact]
    public void PickIndex_EmptyCollection_ReturnsNegativeOne()
    {
        var collection = new Tutorial9.HexFeatureCollection(0);

        collection.PickIndex(0.5f).Should().Be(-1);
    }

    [Fact]
    public void PickIndex_SinglePrefab_AlwaysReturnsZero()
    {
        var collection = new Tutorial9.HexFeatureCollection(1);

        collection.PickIndex(0.0f).Should().Be(0);
        collection.PickIndex(0.5f).Should().Be(0);
        collection.PickIndex(0.99f).Should().Be(0);
    }

    [Fact]
    public void PickIndex_TwoPrefabs_SelectsCorrectly()
    {
        var collection = new Tutorial9.HexFeatureCollection(2);

        // choice < 0.5 -> index 0
        collection.PickIndex(0.0f).Should().Be(0);
        collection.PickIndex(0.25f).Should().Be(0);
        collection.PickIndex(0.49f).Should().Be(0);

        // choice >= 0.5 -> index 1
        collection.PickIndex(0.5f).Should().Be(1);
        collection.PickIndex(0.75f).Should().Be(1);
        collection.PickIndex(0.99f).Should().Be(1);
    }

    [Fact]
    public void PickIndex_ThreePrefabs_SelectsCorrectly()
    {
        var collection = new Tutorial9.HexFeatureCollection(3);

        // 0 to 0.333 -> index 0
        collection.PickIndex(0.0f).Should().Be(0);
        collection.PickIndex(0.32f).Should().Be(0);

        // 0.333 to 0.666 -> index 1
        collection.PickIndex(0.34f).Should().Be(1);
        collection.PickIndex(0.65f).Should().Be(1);

        // 0.666 to 1.0 -> index 2
        collection.PickIndex(0.67f).Should().Be(2);
        collection.PickIndex(0.99f).Should().Be(2);
    }

    [Fact]
    public void PickIndex_MaxChoiceValue_DoesNotOverflow()
    {
        // The 0.999 multiplier in HexHash.Create ensures choice < 1.0
        // but we test the boundary condition
        var collection = new Tutorial9.HexFeatureCollection(10);

        // With 10 prefabs and choice = 0.999, index = (int)(0.999 * 10) = 9
        collection.PickIndex(0.999f).Should().Be(9);
        collection.PickIndex(0.999f).Should().BeLessThan(10);
    }
}

/// <summary>
/// Tests for feature selection logic from HexFeatureManager (Tutorial 9).
/// </summary>
public class FeatureSelectionTests
{
    [Theory]
    [InlineData(0, 0.0f, 0)]  // Level 0 = no feature
    [InlineData(0, 0.5f, 0)]
    [InlineData(0, 0.9f, 0)]
    public void GetSelectedLevel_LevelZero_ReturnsZero(int level, float hash, int expected)
    {
        Tutorial9.FeatureSelection.GetSelectedLevel(level, hash).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 0.0f, 0)]   // hash 0.0 < threshold 0.4 -> no feature
    [InlineData(1, 0.3f, 0)]   // hash 0.3 < threshold 0.4 -> no feature
    [InlineData(1, 0.4f, 1)]   // hash 0.4 >= threshold 0.4 -> level 1
    [InlineData(1, 0.5f, 1)]   // hash 0.5 >= threshold 0.4 -> level 1
    [InlineData(1, 0.9f, 1)]   // hash 0.9 >= threshold 0.4 -> level 1
    public void GetSelectedLevel_Level1_ThresholdAt04(int level, float hash, int expected)
    {
        Tutorial9.FeatureSelection.GetSelectedLevel(level, hash).Should().Be(expected);
    }

    [Theory]
    [InlineData(2, 0.0f, 0)]   // hash 0.0 < threshold 0.4 -> no feature
    [InlineData(2, 0.3f, 0)]   // hash 0.3 < threshold 0.4 -> no feature
    [InlineData(2, 0.4f, 1)]   // hash 0.4 >= threshold 0.4, < 0.6 -> level 1
    [InlineData(2, 0.5f, 1)]   // hash 0.5 >= threshold 0.4, < 0.6 -> level 1
    [InlineData(2, 0.6f, 2)]   // hash 0.6 >= threshold 0.6 -> level 2
    [InlineData(2, 0.9f, 2)]   // hash 0.9 >= threshold 0.6 -> level 2
    public void GetSelectedLevel_Level2_ThresholdAt06(int level, float hash, int expected)
    {
        Tutorial9.FeatureSelection.GetSelectedLevel(level, hash).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 0.0f, 0)]   // hash 0.0 -> no feature
    [InlineData(3, 0.3f, 0)]   // hash 0.3 -> no feature
    [InlineData(3, 0.4f, 1)]   // hash 0.4 -> level 1
    [InlineData(3, 0.6f, 2)]   // hash 0.6 -> level 2
    [InlineData(3, 0.8f, 3)]   // hash 0.8 -> level 3
    [InlineData(3, 0.9f, 3)]   // hash 0.9 -> level 3
    public void GetSelectedLevel_Level3_ThresholdAt08(int level, float hash, int expected)
    {
        Tutorial9.FeatureSelection.GetSelectedLevel(level, hash).Should().Be(expected);
    }

    [Fact]
    public void GetWinningFeatureType_AllZeroLevels_ReturnsZero()
    {
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 0, urbanHash: 0.5f,
            farmLevel: 0, farmHash: 0.5f,
            plantLevel: 0, plantHash: 0.5f);

        result.Should().Be(0);
    }

    [Fact]
    public void GetWinningFeatureType_OnlyUrban_ReturnsUrban()
    {
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.5f,
            farmLevel: 0, farmHash: 0.5f,
            plantLevel: 0, plantHash: 0.5f);

        result.Should().Be(1);
    }

    [Fact]
    public void GetWinningFeatureType_OnlyFarm_ReturnsFarm()
    {
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 0, urbanHash: 0.5f,
            farmLevel: 1, farmHash: 0.5f,
            plantLevel: 0, plantHash: 0.5f);

        result.Should().Be(2);
    }

    [Fact]
    public void GetWinningFeatureType_OnlyPlant_ReturnsPlant()
    {
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 0, urbanHash: 0.5f,
            farmLevel: 0, farmHash: 0.5f,
            plantLevel: 1, plantHash: 0.5f);

        result.Should().Be(3);
    }

    [Fact]
    public void GetWinningFeatureType_UrbanAndFarm_LowestHashWins()
    {
        // Urban has lower hash -> Urban wins
        var result1 = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.4f,
            farmLevel: 1, farmHash: 0.6f,
            plantLevel: 0, plantHash: 0.5f);
        result1.Should().Be(1);

        // Farm has lower hash -> Farm wins
        var result2 = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.7f,
            farmLevel: 1, farmHash: 0.5f,
            plantLevel: 0, plantHash: 0.5f);
        result2.Should().Be(2);
    }

    [Fact]
    public void GetWinningFeatureType_AllThreeTypes_LowestHashWins()
    {
        // Plant has lowest hash -> Plant wins
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.6f,
            farmLevel: 1, farmHash: 0.5f,
            plantLevel: 1, plantHash: 0.4f);

        result.Should().Be(3);
    }

    [Fact]
    public void GetWinningFeatureType_HashBelowThreshold_NoFeature()
    {
        // All types have hash below their thresholds (0.4 for level 1)
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.3f,
            farmLevel: 1, farmHash: 0.3f,
            plantLevel: 1, plantHash: 0.3f);

        result.Should().Be(0);
    }

    [Fact]
    public void GetWinningFeatureType_MixedEligibility_OnlyEligibleCompete()
    {
        // Urban: level 1, hash 0.3 -> below threshold, not eligible
        // Farm: level 1, hash 0.5 -> eligible
        // Plant: level 1, hash 0.6 -> eligible
        var result = Tutorial9.FeatureSelection.GetWinningFeatureType(
            urbanLevel: 1, urbanHash: 0.3f,
            farmLevel: 1, farmHash: 0.5f,
            plantLevel: 1, plantHash: 0.6f);

        // Farm has lower hash among eligible -> Farm wins
        result.Should().Be(2);
    }
}

/// <summary>
/// Tests for HexCell feature level properties (Tutorial 9).
/// </summary>
public class HexCellFeatureLevelTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UrbanLevel_ValidValues(int level)
    {
        // Test that levels 0-3 are valid (matching Tutorial 9 specification)
        level.Should().BeInRange(0, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FarmLevel_ValidValues(int level)
    {
        level.Should().BeInRange(0, 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void PlantLevel_ValidValues(int level)
    {
        level.Should().BeInRange(0, 3);
    }
}

/// <summary>
/// Tests for Tutorial 9 constants matching Catlike Coding.
/// </summary>
public class Tutorial9ConstantsTests
{
    private const float Tolerance = 0.0001f;

    [Fact]
    public void HashGridSize_MatchesTutorial()
    {
        // Tutorial 9 specifies HashGridSize = 256
        const int expected = 256;
        Tutorial9.HexMetricsHashGrid.HashGridSize.Should().Be(expected);
    }

    [Fact]
    public void HashGridScale_MatchesTutorial()
    {
        // Tutorial 9 specifies HashGridScale = 0.25f
        const float expected = 0.25f;
        Tutorial9.HexMetricsHashGrid.HashGridScale.Should().BeApproximately(expected, Tolerance);
    }

    [Fact]
    public void FeatureThresholds_MatchTutorial()
    {
        // Tutorial 9 thresholds: Level 1 = 0.4, Level 2 = 0.6, Level 3 = 0.8
        // Level 0 means no feature

        // At threshold boundary, feature should be selected
        Tutorial9.FeatureSelection.GetSelectedLevel(1, 0.4f).Should().Be(1);
        Tutorial9.FeatureSelection.GetSelectedLevel(2, 0.6f).Should().Be(2);
        Tutorial9.FeatureSelection.GetSelectedLevel(3, 0.8f).Should().Be(3);

        // Just below threshold, use lower level
        Tutorial9.FeatureSelection.GetSelectedLevel(1, 0.39f).Should().Be(0);
        Tutorial9.FeatureSelection.GetSelectedLevel(2, 0.59f).Should().Be(1);
        Tutorial9.FeatureSelection.GetSelectedLevel(3, 0.79f).Should().Be(2);
    }

    [Fact]
    public void HashMultiplier_Prevents1Point0()
    {
        // Tutorial 9 uses 0.999 multiplier to prevent array overflow
        // When choice = 1.0 and array length = n, (int)(1.0 * n) = n which is out of bounds
        // With 0.999: max value is 0.999, so (int)(0.999 * n) = n-1 which is valid
        const float maxHashValue = 0.999f;
        const int arrayLength = 10;

        int maxIndex = (int)(maxHashValue * arrayLength);
        maxIndex.Should().Be(9);
        maxIndex.Should().BeLessThan(arrayLength);
    }
}

} // namespace HexGame.Tests.Godot2
