namespace HexGame.Rendering;

/// <summary>
/// Base class for all renderers providing common functionality.
/// </summary>
public abstract partial class RendererBase : Node3D, IRenderer
{
    private bool _visible = true;
    private bool _isBuilt;
    private bool _disposed;

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
    /// Builds the renderer's visual resources.
    /// </summary>
    public void Build()
    {
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
    /// Cleans up rendering resources.
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
    /// Disposes of the renderer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the renderer.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Cleanup();
        }

        _disposed = true;
    }

    ~RendererBase()
    {
        Dispose(false);
    }
}

/// <summary>
/// Base class for chunked renderers with visibility culling.
/// </summary>
public abstract partial class ChunkedRendererBase : RendererBase
{
    /// <summary>
    /// Chunk size in cells.
    /// </summary>
    protected int ChunkSize { get; set; } = RenderingConfig.ChunkSize;

    /// <summary>
    /// Maximum render distance.
    /// </summary>
    protected float MaxRenderDistance { get; set; } = RenderingConfig.TerrainRenderDistance;

    /// <summary>
    /// Dictionary of chunks keyed by their position.
    /// </summary>
    protected Dictionary<Vector2I, Node3D> Chunks { get; } = new();

    /// <summary>
    /// Updates visibility based on camera distance.
    /// </summary>
    public override void UpdateVisibility(Camera3D camera)
    {
        if (camera == null || Chunks.Count == 0)
        {
            return;
        }

        var cameraPos = camera.GlobalPosition;

        foreach (var (chunkPos, chunkNode) in Chunks)
        {
            // Calculate chunk center in world coordinates
            var chunkWorldPos = GetChunkWorldCenter(chunkPos);

            // Distance-based visibility
            float distance = cameraPos.DistanceTo(chunkWorldPos);
            chunkNode.Visible = distance < MaxRenderDistance;
        }
    }

    /// <summary>
    /// Gets the world center position of a chunk.
    /// </summary>
    protected virtual Vector3 GetChunkWorldCenter(Vector2I chunkPos)
    {
        // Calculate center cell of chunk
        int centerQ = chunkPos.X * ChunkSize + ChunkSize / 2;
        int centerR = chunkPos.Y * ChunkSize + ChunkSize / 2;
        return new HexCoordinates(centerQ, centerR).ToWorldPosition(0);
    }

    /// <summary>
    /// Gets the chunk position for a cell.
    /// </summary>
    protected Vector2I GetChunkPos(int q, int r)
    {
        return new Vector2I(q / ChunkSize, r / ChunkSize);
    }

    /// <summary>
    /// Cleans up all chunks.
    /// </summary>
    public override void Cleanup()
    {
        foreach (var chunk in Chunks.Values)
        {
            chunk.QueueFree();
        }
        Chunks.Clear();
        base.Cleanup();
    }
}
