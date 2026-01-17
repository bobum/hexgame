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
        _rng = new Random(seed + 2000);

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
                int numTrees = _rng.Next(1, 4);
                for (int i = 0; i < numTrees; i++)
                {
                    var offset = new Vector3(
                        RandomRange(-0.3f, 0.3f),
                        0,
                        RandomRange(-0.3f, 0.3f)
                    );
                    var feature = new Feature(
                        Feature.FeatureType.Tree,
                        center + offset,
                        (float)(_rng.NextDouble() * Math.Tau),
                        RandomRange(0.8f, 1.2f)
                    );
                    cell.Features.Add(feature);
                    treeCount++;
                }
            }

            // Try to place rocks
            if (rockChance > 0 && _rng.NextDouble() < rockChance)
            {
                int numRocks = _rng.Next(1, 3);
                for (int i = 0; i < numRocks; i++)
                {
                    var offset = new Vector3(
                        RandomRange(-0.35f, 0.35f),
                        0,
                        RandomRange(-0.35f, 0.35f)
                    );
                    var feature = new Feature(
                        Feature.FeatureType.Rock,
                        center + offset,
                        (float)(_rng.NextDouble() * Math.Tau),
                        RandomRange(0.6f, 1.4f)
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
            TerrainType.Forest => (0.7f, 0.1f),
            TerrainType.Jungle => (0.85f, 0.05f),
            TerrainType.Plains => (0.15f, 0.1f),
            TerrainType.Savanna => (0.1f, 0.15f),
            TerrainType.Hills => (0.2f, 0.3f),
            TerrainType.Mountains => (0.05f, 0.4f),
            TerrainType.Desert => (0f, 0.2f),
            TerrainType.Snow => (0f, 0.15f),
            TerrainType.Taiga => (0.4f, 0.1f),
            TerrainType.Tundra => (0.05f, 0.2f),
            _ => (0f, 0f)
        };
    }

    private float RandomRange(float min, float max)
    {
        return (float)(_rng.NextDouble() * (max - min) + min);
    }
}
