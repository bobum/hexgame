using Godot;
using System.Collections.Generic;
using System.Linq;

namespace HexGame.Rendering;

/// <summary>
/// Fog parameter enumeration for type-safe parameter passing.
/// </summary>
public enum FogParameter
{
    Near,
    Far,
    Density
}

/// <summary>
/// Central coordinator for all game renderers.
/// Manages renderer lifecycle, updates, and visibility culling.
/// Thread-safe for registration/unregistration during updates.
/// </summary>
public class RenderingSystem
{
    private readonly List<IRenderer> _renderers = new();
    private readonly HashSet<IRenderer> _renderersSet = new(); // For O(1) contains check
    private readonly List<IRenderer> _pendingAdd = new();
    private readonly List<IRenderer> _pendingRemove = new();
    private readonly object _pendingLock = new();
    private bool _isUpdating;

    private Camera3D? _camera;
    private bool _initialized;

    // Fog settings
    private float _fogNear = RenderingConfig.DefaultFogNear;
    private float _fogFar = RenderingConfig.DefaultFogFar;
    private float _fogDensity = RenderingConfig.DefaultFogDensity;
    private WorldEnvironment? _worldEnvironment;

    /// <summary>
    /// Gets whether the system is initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the number of registered renderers.
    /// </summary>
    public int RendererCount => _renderers.Count;

    /// <summary>
    /// Gets or sets the fog near distance.
    /// </summary>
    public float FogNear
    {
        get => _fogNear;
        set
        {
            _fogNear = value;
            UpdateFog();
        }
    }

    /// <summary>
    /// Gets or sets the fog far distance.
    /// </summary>
    public float FogFar
    {
        get => _fogFar;
        set
        {
            _fogFar = value;
            UpdateFog();
        }
    }

    /// <summary>
    /// Gets or sets the fog density.
    /// </summary>
    public float FogDensity
    {
        get => _fogDensity;
        set
        {
            _fogDensity = value;
            UpdateFog();
        }
    }

    /// <summary>
    /// Initializes the rendering system.
    /// </summary>
    public void Initialize()
    {
        _initialized = true;
        GD.Print("RenderingSystem initialized");
    }

    /// <summary>
    /// Shuts down the rendering system.
    /// </summary>
    public void Shutdown()
    {
        ProcessPendingOperations();

        foreach (var renderer in _renderers)
        {
            renderer.Cleanup();
            renderer.Dispose();
        }
        _renderers.Clear();
        _renderersSet.Clear();
        _initialized = false;
        GD.Print("RenderingSystem shutdown");
    }

    /// <summary>
    /// Sets the camera for visibility culling.
    /// </summary>
    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Sets the world environment for fog control.
    /// </summary>
    public void SetWorldEnvironment(WorldEnvironment env)
    {
        _worldEnvironment = env;
        UpdateFog();
    }

    /// <summary>
    /// Registers a renderer with the system.
    /// Safe to call during Update().
    /// </summary>
    public void Register(IRenderer renderer)
    {
        if (_isUpdating)
        {
            lock (_pendingLock)
            {
                if (!_renderersSet.Contains(renderer) && !_pendingAdd.Contains(renderer))
                {
                    _pendingAdd.Add(renderer);
                }
            }
        }
        else
        {
            if (!_renderersSet.Contains(renderer))
            {
                _renderers.Add(renderer);
                _renderersSet.Add(renderer);
                GD.Print($"RenderingSystem: Registered {renderer.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Unregisters a renderer from the system.
    /// Safe to call during Update().
    /// </summary>
    public void Unregister(IRenderer renderer)
    {
        if (_isUpdating)
        {
            lock (_pendingLock)
            {
                if (!_pendingRemove.Contains(renderer))
                {
                    _pendingRemove.Add(renderer);
                }
            }
        }
        else
        {
            if (_renderers.Remove(renderer))
            {
                _renderersSet.Remove(renderer);
                GD.Print($"RenderingSystem: Unregistered {renderer.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// Processes pending add/remove operations.
    /// Called at the start of Update() and during Shutdown().
    /// </summary>
    private void ProcessPendingOperations()
    {
        lock (_pendingLock)
        {
            foreach (var renderer in _pendingAdd)
            {
                if (!_renderersSet.Contains(renderer))
                {
                    _renderers.Add(renderer);
                    _renderersSet.Add(renderer);
                    GD.Print($"RenderingSystem: Registered (deferred) {renderer.GetType().Name}");
                }
            }
            _pendingAdd.Clear();

            foreach (var renderer in _pendingRemove)
            {
                if (_renderers.Remove(renderer))
                {
                    _renderersSet.Remove(renderer);
                    GD.Print($"RenderingSystem: Unregistered (deferred) {renderer.GetType().Name}");
                }
            }
            _pendingRemove.Clear();
        }
    }

    /// <summary>
    /// Builds all registered renderers.
    /// </summary>
    public void BuildAll()
    {
        ProcessPendingOperations();

        foreach (var renderer in _renderers)
        {
            renderer.Build();
        }
        GD.Print($"RenderingSystem: Built {_renderers.Count} renderers");
    }

    /// <summary>
    /// Updates all renderers. Call this from _Process().
    /// </summary>
    public void Update(double delta)
    {
        ProcessPendingOperations();

        _isUpdating = true;
        try
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
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Rebuilds all renderers.
    /// </summary>
    public void RebuildAll()
    {
        ProcessPendingOperations();

        foreach (var renderer in _renderers)
        {
            renderer.Rebuild();
        }
        GD.Print($"RenderingSystem: Rebuilt {_renderers.Count} renderers");
    }

    /// <summary>
    /// Cleans up all renderers.
    /// </summary>
    public void CleanupAll()
    {
        ProcessPendingOperations();

        foreach (var renderer in _renderers)
        {
            renderer.Cleanup();
        }
        GD.Print("RenderingSystem: Cleaned up all renderers");
    }

    /// <summary>
    /// Gets a renderer of a specific type.
    /// </summary>
    public T? GetRenderer<T>() where T : class, IRenderer
    {
        return _renderers.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Updates the fog settings in the world environment.
    /// Uses depth-based fog to create clear near-field with fog at distance.
    /// </summary>
    private void UpdateFog()
    {
        if (_worldEnvironment?.Environment == null)
            return;

        var env = _worldEnvironment.Environment;

        // Enable fog if density > 0
        if (_fogDensity > 0.01f)
        {
            env.FogEnabled = true;
            // Exponential density scaled for visible fog at distance
            env.FogDensity = _fogDensity * 0.008f;
            env.FogAerialPerspective = 0.3f;

            // Depth fog: clear until FogNear, then fade to opaque at FogFar
            env.FogDepthBegin = _fogNear;
            env.FogDepthEnd = _fogFar;
            env.FogDepthCurve = 1.5f;
        }
        else
        {
            env.FogEnabled = false;
        }
    }

    /// <summary>
    /// Handles fog parameter changes from UI using type-safe enum.
    /// </summary>
    public void OnFogParamChanged(FogParameter param, float value)
    {
        switch (param)
        {
            case FogParameter.Near:
                FogNear = value;
                break;
            case FogParameter.Far:
                FogFar = value;
                break;
            case FogParameter.Density:
                FogDensity = value;
                break;
        }
    }

    /// <summary>
    /// Handles fog parameter changes from UI using string (for signal compatibility).
    /// </summary>
    public void OnFogParamChanged(string param, float value)
    {
        switch (param)
        {
            case "fog_near":
                FogNear = value;
                break;
            case "fog_far":
                FogFar = value;
                break;
            case "fog_density":
                FogDensity = value;
                break;
            default:
                GD.PrintErr($"RenderingSystem: Unknown fog parameter '{param}'");
                break;
        }
    }
}
