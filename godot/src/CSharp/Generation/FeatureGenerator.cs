using HexGame.Core;

namespace HexGame.Generation;

/// <summary>
/// Generates features (trees, rocks) on terrain based on biome.
/// </summary>
public class FeatureGenerator
{
    private readonly HexGrid _grid;
    private Random _rng;

    /// <summary>
    /// Creates a new feature generator.
    /// </summary>
    /// <param name="grid">The hex grid.</param>
    public FeatureGenerator(HexGrid grid)
    {
        _grid = grid;
        _rng = new Random();
    }

    /// <summary>
    /// Generates features on all cells.
    /// </summary>
    /// <param name="seed">Random seed.</param>
    public void Generate(int seed)
    {
        _rng = new Random(seed + GameConstants.Generation.FeatureSeedOffset);

        // Clear existing features
        foreach (var cell in _grid.GetAllCells())
        {
            cell.Features.Clear();
        }

        int treeCount = 0;
        int rockCount = 0;

        foreach (var cell in _grid.GetAllCells())
        {
            // Skip water cells
            if (cell.IsUnderwater)
            {
                continue;
            }

            // Skip cells with rivers
            if (cell.HasRiver)
            {
                continue;
            }

            // Get feature chances based on terrain
            var (treeChance, rockChance) = GetFeatureChances(cell.TerrainType);

            var center = cell.GetWorldPosition();

            // Try to place trees
            if (treeChance > 0 && _rng.NextDouble() < treeChance)
            {
                int numTrees = _rng.Next(GameConstants.Features.MinTreesPerCell, GameConstants.Features.MaxTreesPerCell);
                for (int i = 0; i < numTrees; i++)
                {
                    var offset = new Vector3(
                        RandomRange(GameConstants.Features.TreeOffsetMin, GameConstants.Features.TreeOffsetMax),
                        0,
                        RandomRange(GameConstants.Features.TreeOffsetMin, GameConstants.Features.TreeOffsetMax)
                    );
                    var feature = new Feature(
                        Feature.FeatureType.Tree,
                        center + offset,
                        (float)(_rng.NextDouble() * Math.Tau),
                        RandomRange(GameConstants.Features.TreeScaleMin, GameConstants.Features.TreeScaleMax)
                    );
                    cell.Features.Add(feature);
                    treeCount++;
                }
            }

            // Try to place rocks
            if (rockChance > 0 && _rng.NextDouble() < rockChance)
            {
                int numRocks = _rng.Next(GameConstants.Features.MinRocksPerCell, GameConstants.Features.MaxRocksPerCell);
                for (int i = 0; i < numRocks; i++)
                {
                    var offset = new Vector3(
                        RandomRange(GameConstants.Features.RockOffsetMin, GameConstants.Features.RockOffsetMax),
                        0,
                        RandomRange(GameConstants.Features.RockOffsetMin, GameConstants.Features.RockOffsetMax)
                    );
                    var feature = new Feature(
                        Feature.FeatureType.Rock,
                        center + offset,
                        (float)(_rng.NextDouble() * Math.Tau),
                        RandomRange(GameConstants.Features.RockScaleMin, GameConstants.Features.RockScaleMax)
                    );
                    cell.Features.Add(feature);
                    rockCount++;
                }
            }
        }

        if (treeCount > 0 || rockCount > 0)
        {
            GD.Print($"Generated {treeCount} trees, {rockCount} rocks");
        }
    }

    private (float TreeChance, float RockChance) GetFeatureChances(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Forest => (GameConstants.Features.ForestTreeChance, GameConstants.Features.ForestRockChance),
            TerrainType.Jungle => (GameConstants.Features.JungleTreeChance, GameConstants.Features.JungleRockChance),
            TerrainType.Plains => (GameConstants.Features.GrasslandTreeChance, GameConstants.Features.GrasslandRockChance),
            TerrainType.Savanna => (GameConstants.Features.SavannaTreeChance, GameConstants.Features.SavannaRockChance),
            TerrainType.Hills => (GameConstants.Features.HillsTreeChance, GameConstants.Features.HillsRockChance),
            TerrainType.Mountains => (GameConstants.Features.MountainTreeChance, GameConstants.Features.MountainRockChance),
            TerrainType.Desert => (GameConstants.Features.DesertTreeChance, GameConstants.Features.DesertRockChance),
            TerrainType.Snow => (GameConstants.Features.SnowTreeChance, GameConstants.Features.SnowRockChance),
            TerrainType.Taiga => (GameConstants.Features.TaigaTreeChance, GameConstants.Features.TaigaRockChance),
            TerrainType.Tundra => (GameConstants.Features.TundraTreeChance, GameConstants.Features.TundraRockChance),
            _ => (0f, 0f)
        };
    }

    private float RandomRange(float min, float max)
    {
        return (float)(_rng.NextDouble() * (max - min) + min);
    }
}
