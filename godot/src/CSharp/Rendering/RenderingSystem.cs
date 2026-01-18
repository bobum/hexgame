namespace HexGame.Rendering;

/// <summary>
/// Central coordinator for all game renderers.
/// Manages renderer lifecycle, updates, and visibility.
/// </summary>
public class RenderingSystem : IService
{
    private readonly List<IRenderer> _renderers = new();
    private Camera3D? _camera;
    private bool _initialized;

    /// <summary>
    /// Gets whether the system is initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the number of registered renderers.
    /// </summary>
    public int RendererCount => _renderers.Count;

    #region IService Implementation

    public void Initialize()
    {
        _initialized = true;
    }

    public void Shutdown()
    {
        foreach (var renderer in _renderers)
        {
            renderer.Cleanup();
            renderer.Dispose();
        }
        _renderers.Clear();
        _initialized = false;
    }

    #endregion

    /// <summary>
    /// Sets the camera for visibility culling.
    /// </summary>
    /// <param name="camera">The camera.</param>
    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Registers a renderer with the system.
    /// </summary>
    /// <param name="renderer">The renderer to register.</param>
    public void Register(IRenderer renderer)
    {
        if (!_renderers.Contains(renderer))
        {
            _renderers.Add(renderer);
        }
    }

    /// <summary>
    /// Unregisters a renderer from the system.
    /// </summary>
    /// <param name="renderer">The renderer to unregister.</param>
    public void Unregister(IRenderer renderer)
    {
        _renderers.Remove(renderer);
    }

    /// <summary>
    /// Builds all registered renderers.
    /// </summary>
    public void BuildAll()
    {
        foreach (var renderer in _renderers)
        {
            renderer.Build();
        }
    }

    /// <summary>
    /// Updates all renderers. Call this from _Process().
    /// </summary>
    /// <param name="delta">Time since last frame.</param>
    public void Update(double delta)
    {
        foreach (var renderer in _renderers)
        {
            renderer.Update(delta);
            renderer.UpdateAnimation(delta);

            if (_camera != null)
            {
                renderer.UpdateVisibility(_camera);
            }
        }
    }

    /// <summary>
    /// Rebuilds all renderers.
    /// </summary>
    public void RebuildAll()
    {
        foreach (var renderer in _renderers)
        {
            renderer.Rebuild();
        }
    }

    /// <summary>
    /// Cleans up all renderers.
    /// </summary>
    public void CleanupAll()
    {
        foreach (var renderer in _renderers)
        {
            renderer.Cleanup();
        }
    }

    /// <summary>
    /// Gets a renderer of a specific type.
    /// </summary>
    /// <typeparam name="T">The renderer type.</typeparam>
    /// <returns>The renderer, or null if not found.</returns>
    public T? GetRenderer<T>() where T : class, IRenderer
    {
        return _renderers.OfType<T>().FirstOrDefault();
    }
}
