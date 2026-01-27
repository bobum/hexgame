using System;

namespace HexGame.Generation;

/// <summary>
/// Interface for procedural map generators.
/// Supports both synchronous and asynchronous generation with progress reporting.
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// Fired when generation begins.
    /// </summary>
    event Action? GenerationStarted;

    /// <summary>
    /// Fired during generation to report progress.
    /// Parameters: (stage name, progress 0.0-1.0)
    /// </summary>
    event Action<string, float>? GenerationProgress;

    /// <summary>
    /// Fired when generation completes.
    /// Parameter: success (true if completed successfully)
    /// </summary>
    event Action<bool>? GenerationCompleted;

    /// <summary>
    /// Returns true if generation is currently in progress.
    /// </summary>
    bool IsGenerating { get; }

    /// <summary>
    /// Generates a map synchronously (blocking).
    /// </summary>
    /// <param name="grid">The hex grid to populate.</param>
    /// <param name="seed">Random seed (0 for random).</param>
    void Generate(HexGrid grid, int seed = 0);

    /// <summary>
    /// Starts asynchronous map generation.
    /// Use IsGenerationComplete() to poll for completion,
    /// then call FinishAsyncGeneration() to apply results.
    /// </summary>
    /// <param name="grid">The hex grid to populate.</param>
    /// <param name="seed">Random seed (0 for random).</param>
    void GenerateAsync(HexGrid grid, int seed = 0);

    /// <summary>
    /// Checks if async generation has completed.
    /// </summary>
    /// <returns>True if generation is complete and ready to apply.</returns>
    bool IsGenerationComplete();

    /// <summary>
    /// Applies the results of async generation to the grid.
    /// Call this after IsGenerationComplete() returns true.
    /// </summary>
    void FinishAsyncGeneration();

    /// <summary>
    /// Cancels any ongoing async generation.
    /// </summary>
    void CancelGeneration();
}
