using Godot;

namespace HexGame.Rendering;

/// <summary>
/// Base class for all renderers providing common functionality.
///
/// Lifecycle:
/// - Cleanup() releases rendering resources, allows Build() again
/// - Dispose() is permanent disposal, object cannot be reused
/// </summary>
public abstract partial class RendererBase : Node3D, IRenderer
{
    private bool _visible = true;
    private bool _isBuilt;
    protected bool _disposed;

    /// <summary>
    /// Gets or sets whether the renderer is visible.
    /// </summary>
    public new bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            base.Visible = value;
        }
    }

    /// <summary>
    /// Gets whether the renderer has been built.
    /// </summary>
    public bool IsBuilt => _isBuilt;

    /// <summary>
    /// Gets whether the renderer has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Builds the renderer's visual resources.
    /// </summary>
    public void Build()
    {
        if (_disposed)
        {
            GD.PrintErr($"{GetType().Name}: Cannot build disposed renderer");
            return;
        }

        if (_isBuilt)
        {
            Cleanup();
        }

        DoBuild();
        _isBuilt = true;
    }

    /// <summary>
    /// Rebuilds the renderer from scratch.
    /// </summary>
    public void Rebuild()
    {
        if (_disposed)
        {
            GD.PrintErr($"{GetType().Name}: Cannot rebuild disposed renderer");
            return;
        }

        Cleanup();
        _isBuilt = false;
        Build();
    }

    /// <summary>
    /// Updates the renderer (called every frame).
    /// </summary>
    public virtual void Update(double delta)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Updates visibility based on camera position.
    /// </summary>
    public virtual void UpdateVisibility(Camera3D camera)
    {
        // Override in derived classes for distance culling
    }

    /// <summary>
    /// Updates animation state.
    /// </summary>
    public virtual void UpdateAnimation(double delta)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Cleans up rendering resources. Can be called multiple times.
    /// After cleanup, Build() can be called again.
    /// </summary>
    public virtual void Cleanup()
    {
        // Override in derived classes
        _isBuilt = false;
    }

    /// <summary>
    /// Performs the actual build logic. Override in derived classes.
    /// </summary>
    protected abstract void DoBuild();

    /// <summary>
    /// Disposes of the renderer. Must be called from the main thread.
    /// After disposal, the renderer cannot be reused.
    /// </summary>
    /// <remarks>
    /// Note: Do NOT use a finalizer (~RendererBase) as it runs on the GC thread,
    /// and Godot's API is not thread-safe.
    /// </remarks>
    public new void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cleanup();
        _disposed = true;
        base.Dispose();
    }

    /// <summary>
    /// Called when the node exits the scene tree. Ensures cleanup.
    /// </summary>
    public override void _ExitTree()
    {
        if (!_disposed)
        {
            Cleanup();
        }
        base._ExitTree();
    }
}

/// <summary>
/// Base class for chunked renderers with visibility culling and LOD support.
/// Uses tuple keys for chunk dictionary to avoid string allocation.
/// </summary>
public abstract partial class ChunkedRendererBase : RendererBase
{
    /// <summary>
    /// Size of each chunk in world units.
    /// </summary>
    protected float ChunkSize { get; set; } = RenderingConfig.ChunkSize;

    /// <summary>
    /// Maximum distance at which chunks are rendered.
    /// </summary>
    protected float MaxRenderDistance { get; set; } = RenderingConfig.MaxRenderDistance;

    /// <summary>
    /// Distance for HIGH to MEDIUM LOD transition.
    /// </summary>
    protected float LodHighToMedium { get; set; } = RenderingConfig.LodHighToMedium;

    /// <summary>
    /// Distance for MEDIUM to LOW LOD transition.
    /// </summary>
    protected float LodMediumToLow { get; set; } = RenderingConfig.LodMediumToLow;

    /// <summary>
    /// Hysteresis margin to prevent LOD flickering at threshold boundaries.
    /// </summary>
    protected float LodHysteresis { get; set; } = 2.0f;

    /// <summary>
    /// Dictionary of chunks keyed by (cx, cz) tuple to avoid string allocation.
    /// </summary>
    protected Dictionary<(int, int), ChunkData> Chunks { get; } = new();

    /// <summary>
    /// Total number of chunks.
    /// </summary>
    public int ChunkCount => Chunks.Count;

    /// <summary>
    /// LOD level enumeration.
    /// </summary>
    public enum LodLevel
    {
        High,
        Medium,
        Low,
        Culled
    }

    /// <summary>
    /// Represents a chunk with LOD meshes.
    /// </summary>
    protected class ChunkData
    {
        public int ChunkX;
        public int ChunkZ;
        public Vector3 Center = Vector3.Zero;
        public MeshInstance3D? MeshHigh;
        public MeshInstance3D? MeshMedium;
        public MeshInstance3D? MeshLow;
        public LodLevel CurrentLod = LodLevel.High;
    }

    /// <summary>
    /// Gets the chunk coordinates for a world position.
    /// </summary>
    protected Vector2I GetChunkCoords(Vector3 worldPos)
    {
        var (cx, cz) = ChunkMath.GetChunkCoords(worldPos.X, worldPos.Z, ChunkSize);
        return new Vector2I(cx, cz);
    }

    /// <summary>
    /// Gets the center world position of a chunk.
    /// </summary>
    protected Vector3 GetChunkCenter(int cx, int cz)
    {
        var (x, z) = ChunkMath.GetChunkCenter(cx, cz, ChunkSize);
        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// Selects the appropriate LOD level for a given distance.
    /// Uses hysteresis to prevent flickering at thresholds.
    /// </summary>
    protected LodLevel SelectLod(float distance, LodLevel currentLod)
    {
        var mathLod = ChunkMath.SelectLod(
            distance,
            (ChunkMath.LodLevel)currentLod,
            LodHighToMedium,
            LodMediumToLow,
            MaxRenderDistance,
            LodHysteresis);
        return (LodLevel)mathLod;
    }

    /// <summary>
    /// Updates visibility and LOD based on camera distance.
    /// </summary>
    public override void UpdateVisibility(Camera3D camera)
    {
        if (_disposed || camera == null || Chunks.Count == 0)
        {
            return;
        }

        var cameraPos = camera.GlobalPosition;
        var cameraXz = new Vector3(cameraPos.X, 0, cameraPos.Z);
        float maxDistSq = MaxRenderDistance * MaxRenderDistance;

        foreach (var chunk in Chunks.Values)
        {
            float dx = chunk.Center.X - cameraXz.X;
            float dz = chunk.Center.Z - cameraXz.Z;
            float distSq = dx * dx + dz * dz;

            if (distSq > maxDistSq)
            {
                // Cull - hide all LODs
                if (chunk.CurrentLod != LodLevel.Culled)
                {
                    SetChunkLodVisibility(chunk, LodLevel.Culled);
                    chunk.CurrentLod = LodLevel.Culled;
                }
                continue;
            }

            float dist = Mathf.Sqrt(distSq);
            var newLod = SelectLod(dist, chunk.CurrentLod);

            if (newLod != chunk.CurrentLod)
            {
                SetChunkLodVisibility(chunk, newLod);
                chunk.CurrentLod = newLod;
            }
        }
    }

    /// <summary>
    /// Sets the visibility of LOD meshes for a chunk.
    /// </summary>
    protected void SetChunkLodVisibility(ChunkData chunk, LodLevel lod)
    {
        if (chunk.MeshHigh != null)
            chunk.MeshHigh.Visible = lod == LodLevel.High;
        if (chunk.MeshMedium != null)
            chunk.MeshMedium.Visible = lod == LodLevel.Medium;
        if (chunk.MeshLow != null)
            chunk.MeshLow.Visible = lod == LodLevel.Low;
    }

    /// <summary>
    /// Cleans up all chunks.
    /// </summary>
    public override void Cleanup()
    {
        foreach (var chunk in Chunks.Values)
        {
            chunk.MeshHigh?.QueueFree();
            chunk.MeshMedium?.QueueFree();
            chunk.MeshLow?.QueueFree();
        }
        Chunks.Clear();
        base.Cleanup();
    }
}
