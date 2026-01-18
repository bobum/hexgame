using HexGame.Core;

namespace HexGame.Rendering;

/// <summary>
/// Interface that all renderers must implement.
/// Defines the contract for building, updating, and disposing rendering resources.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Gets whether the renderer is visible.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Gets whether the renderer has been built.
    /// </summary>
    bool IsBuilt { get; }

    /// <summary>
    /// Builds the renderer's visual resources.
    /// </summary>
    void Build();

    /// <summary>
    /// Rebuilds the renderer from scratch.
    /// </summary>
    void Rebuild();

    /// <summary>
    /// Updates the renderer (called every frame from _Process).
    /// </summary>
    /// <param name="delta">Time since last frame.</param>
    void Update(double delta);

    /// <summary>
    /// Updates visibility based on camera position.
    /// </summary>
    /// <param name="camera">The camera node.</param>
    void UpdateVisibility(Camera3D camera);

    /// <summary>
    /// Updates animation state.
    /// </summary>
    /// <param name="delta">Time since last frame.</param>
    void UpdateAnimation(double delta);

    /// <summary>
    /// Cleans up rendering resources.
    /// </summary>
    void Cleanup();
}

/// <summary>
/// Configuration for renderer distance culling.
/// </summary>
public readonly record struct RenderDistance(float Near, float Far, float Fade);

/// <summary>
/// Configuration settings for rendering.
/// </summary>
public static class RenderingConfig
{
    /// <summary>
    /// Chunk size for chunked renderers.
    /// </summary>
    public static int ChunkSize => GameConstants.Rendering.ChunkSize;

    /// <summary>
    /// Maximum render distance for terrain.
    /// </summary>
    public static float TerrainRenderDistance => GameConstants.Rendering.TerrainRenderDistance;

    /// <summary>
    /// Maximum render distance for features.
    /// </summary>
    public static float FeatureRenderDistance => GameConstants.Rendering.FeatureRenderDistance;

    /// <summary>
    /// Maximum render distance for units.
    /// </summary>
    public static float UnitRenderDistance => GameConstants.Rendering.UnitRenderDistance;

    /// <summary>
    /// Maximum render distance for water effects.
    /// </summary>
    public static float WaterRenderDistance => GameConstants.Rendering.WaterRenderDistance;

    /// <summary>
    /// Fade start distance (as percentage of max).
    /// </summary>
    public static float FadeStartPercent => GameConstants.Rendering.FadeStartPercentage;

    /// <summary>
    /// Player colors for unit rendering.
    /// </summary>
    public static readonly Color[] PlayerColors =
    {
        new(0.5f, 0.5f, 0.5f), // Neutral (0)
        new(0.2f, 0.4f, 0.8f), // Player 1 (Blue)
        new(0.8f, 0.2f, 0.2f), // Player 2 (Red)
        new(0.2f, 0.8f, 0.2f), // Player 3 (Green)
        new(0.8f, 0.8f, 0.2f), // Player 4 (Yellow)
    };

    /// <summary>
    /// Gets the color for a player ID.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The player's color.</returns>
    public static Color GetPlayerColor(int playerId)
    {
        if (playerId >= 0 && playerId < PlayerColors.Length)
        {
            return PlayerColors[playerId];
        }
        return PlayerColors[0];
    }
}
