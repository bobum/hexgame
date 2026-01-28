using Godot;

namespace HexGame.Rendering;

/// <summary>
/// Interface that all renderers must implement.
/// Defines the contract for building, updating, and disposing rendering resources.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Gets or sets whether the renderer is visible.
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
