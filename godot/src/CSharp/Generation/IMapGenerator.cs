namespace HexGame.Generation;

/// <summary>
/// Interface for map generation systems.
/// Extends IService for ServiceLocator registration.
/// </summary>
public interface IMapGenerator : IService
{
    #region Configuration Properties

    /// <summary>
    /// Noise frequency scale. Higher values = more detail.
    /// </summary>
    float NoiseScale { get; set; }

    /// <summary>
    /// Number of noise octaves for fractal detail.
    /// </summary>
    int Octaves { get; set; }

    /// <summary>
    /// Persistence/gain for fractal noise.
    /// </summary>
    float Persistence { get; set; }

    /// <summary>
    /// Lacunarity for fractal noise.
    /// </summary>
    float Lacunarity { get; set; }

    /// <summary>
    /// Sea level threshold (0-1). Values below become water.
    /// </summary>
    float SeaLevel { get; set; }

    /// <summary>
    /// Mountain threshold (0-1). Values above become mountains.
    /// </summary>
    float MountainLevel { get; set; }

    /// <summary>
    /// Percentage of land cells to have rivers (0-1).
    /// </summary>
    float RiverPercentage { get; set; }

    #endregion

    #region Generation Methods

    /// <summary>
    /// Generates a map synchronously.
    /// </summary>
    /// <param name="grid">The grid to populate.</param>
    /// <param name="seed">Random seed (0 for random).</param>
    void Generate(HexGrid grid, int seed = 0);

    /// <summary>
    /// Starts asynchronous map generation.
    /// Check IsGenerationComplete() and call FinishAsyncGeneration() when done.
    /// </summary>
    /// <param name="grid">The grid to populate.</param>
    /// <param name="seed">Random seed (0 for random).</param>
    void GenerateAsync(HexGrid grid, int seed = 0);

    /// <summary>
    /// Checks if async generation has completed.
    /// </summary>
    bool IsGenerationComplete();

    /// <summary>
    /// Checks if generation is currently in progress.
    /// </summary>
    bool IsGenerating { get; }

    /// <summary>
    /// Finishes async generation by applying results to grid.
    /// Must be called from main thread.
    /// </summary>
    /// <returns>Generation timing info.</returns>
    GenerationResult FinishAsyncGeneration();

    /// <summary>
    /// Cancels any ongoing generation.
    /// </summary>
    void CancelGeneration();

    #endregion

    #region Events

    /// <summary>
    /// Fired when generation starts.
    /// </summary>
    event Action? GenerationStarted;

    /// <summary>
    /// Fired during generation with progress updates.
    /// </summary>
    event Action<string, float>? GenerationProgress;

    /// <summary>
    /// Fired when generation completes.
    /// </summary>
    event Action<bool, float, float>? GenerationCompleted;

    #endregion
}

/// <summary>
/// Result of map generation.
/// </summary>
/// <param name="WorkerTimeMs">Time spent in worker thread.</param>
/// <param name="FeatureTimeMs">Time spent generating features.</param>
public readonly record struct GenerationResult(float WorkerTimeMs, float FeatureTimeMs);
