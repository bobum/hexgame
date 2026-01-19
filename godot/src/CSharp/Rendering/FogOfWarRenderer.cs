using HexGame.Core;

namespace HexGame.Rendering;

/// <summary>
/// Renders fog of war overlay on unexplored/non-visible terrain.
/// Supports three visibility states: unexplored (black), explored (dimmed), visible (clear).
/// </summary>
public partial class FogOfWarRenderer : ChunkedRendererBase
{
    private readonly HexGrid _grid;
    private readonly Dictionary<(int Q, int R), VisibilityState> _visibility = new();
    private ShaderMaterial? _fogMaterial;

    /// <summary>
    /// Color for completely unexplored cells.
    /// </summary>
    public Color UnexploredColor { get; set; } = new(0f, 0f, 0f, 1f);

    /// <summary>
    /// Color for explored but not currently visible cells.
    /// </summary>
    public Color ExploredColor { get; set; } = new(0f, 0f, 0f, 0.6f);

    /// <summary>
    /// Creates a new fog of war renderer.
    /// </summary>
    /// <param name="grid">The hex grid.</param>
    public FogOfWarRenderer(HexGrid grid)
    {
        _grid = grid;
        MaxRenderDistance = RenderingConfig.TerrainRenderDistance;

        // Initialize all cells as unexplored
        foreach (var cell in _grid.GetAllCells())
        {
            _visibility[(cell.Q, cell.R)] = VisibilityState.Unexplored;
        }
    }

    /// <summary>
    /// Gets the visibility state of a cell.
    /// </summary>
    public VisibilityState GetVisibility(int q, int r)
    {
        return _visibility.TryGetValue((q, r), out var state) ? state : VisibilityState.Unexplored;
    }

    /// <summary>
    /// Sets the visibility state of a cell.
    /// </summary>
    public void SetVisibility(int q, int r, VisibilityState state)
    {
        var key = (q, r);
        if (_visibility.TryGetValue(key, out var current))
        {
            // Once explored, can't become unexplored
            if (current == VisibilityState.Unexplored || state == VisibilityState.Visible)
            {
                _visibility[key] = state;
            }
            else if (state == VisibilityState.Explored && current == VisibilityState.Visible)
            {
                // Going from visible to explored (moved away)
                _visibility[key] = state;
            }
        }
    }

    /// <summary>
    /// Reveals cells visible from a position with given sight range.
    /// </summary>
    public void RevealArea(int centerQ, int centerR, int sightRange)
    {
        // First, set all currently visible cells to explored
        foreach (var key in _visibility.Keys.ToList())
        {
            if (_visibility[key] == VisibilityState.Visible)
            {
                _visibility[key] = VisibilityState.Explored;
            }
        }

        // Then reveal new visible area
        var visibleCells = GetCellsInRange(centerQ, centerR, sightRange);
        foreach (var (q, r) in visibleCells)
        {
            SetVisibility(q, r, VisibilityState.Visible);
        }
    }

    /// <summary>
    /// Updates visibility for a player's units.
    /// </summary>
    public void UpdateVisibilityForPlayer(IEnumerable<(int Q, int R, int SightRange)> unitPositions)
    {
        // Set all visible cells to explored
        foreach (var key in _visibility.Keys.ToList())
        {
            if (_visibility[key] == VisibilityState.Visible)
            {
                _visibility[key] = VisibilityState.Explored;
            }
        }

        // Reveal around each unit
        foreach (var (q, r, sightRange) in unitPositions)
        {
            var visibleCells = GetCellsInRange(q, r, sightRange);
            foreach (var cell in visibleCells)
            {
                SetVisibility(cell.Q, cell.R, VisibilityState.Visible);
            }
        }

        // Rebuild chunks that changed
        RebuildDirtyChunks();
    }

    private List<(int Q, int R)> GetCellsInRange(int centerQ, int centerR, int range)
    {
        var result = new List<(int, int)>();

        for (int dq = -range; dq <= range; dq++)
        {
            for (int dr = Math.Max(-range, -dq - range); dr <= Math.Min(range, -dq + range); dr++)
            {
                int q = centerQ + dq;
                int r = centerR + dr;

                if (_grid.GetCell(q, r) != null)
                {
                    result.Add((q, r));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Builds fog of war overlay chunks.
    /// </summary>
    protected override void DoBuild()
    {
        _fogMaterial = CreateFogMaterial();

        int chunksX = (_grid.Width + ChunkSize - 1) / ChunkSize;
        int chunksY = (_grid.Height + ChunkSize - 1) / ChunkSize;

        for (int cy = 0; cy < chunksY; cy++)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                var chunkPos = new Vector2I(cx, cy);
                var chunkNode = BuildChunk(chunkPos);
                Chunks[chunkPos] = chunkNode;
                AddChild(chunkNode);
            }
        }

        GD.Print($"FogOfWarRenderer: Built {Chunks.Count} fog chunks");
    }

    private Node3D BuildChunk(Vector2I chunkPos)
    {
        var chunkNode = new Node3D
        {
            Name = $"FogChunk_{chunkPos.X}_{chunkPos.Y}"
        };

        int startQ = chunkPos.X * ChunkSize;
        int startR = chunkPos.Y * ChunkSize;
        int endQ = Math.Min(startQ + ChunkSize, _grid.Width);
        int endR = Math.Min(startR + ChunkSize, _grid.Height);

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        bool hasFog = false;

        for (int r = startR; r < endR; r++)
        {
            for (int q = startQ; q < endQ; q++)
            {
                var cell = _grid.GetCell(q, r);
                if (cell == null) continue;

                var visibility = GetVisibility(q, r);
                if (visibility == VisibilityState.Visible) continue;

                // Add fog hex
                var color = visibility == VisibilityState.Unexplored ? UnexploredColor : ExploredColor;
                AddFogHex(surfaceTool, cell, color);
                hasFog = true;
            }
        }

        if (hasFog)
        {
            surfaceTool.GenerateNormals();
            var mesh = surfaceTool.Commit();

            if (mesh != null)
            {
                var meshInstance = new MeshInstance3D
                {
                    Mesh = mesh,
                    MaterialOverride = _fogMaterial,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
                };
                chunkNode.AddChild(meshInstance);
            }
        }

        return chunkNode;
    }

    private void AddFogHex(SurfaceTool st, HexCell cell, Color color)
    {
        // Fog sits slightly above terrain
        float height = cell.Elevation * HexMetrics.ElevationStep + 0.1f;
        var center = cell.Coordinates.ToWorldPosition(height);
        var corners = HexMetrics.GetCorners();

        for (int i = 0; i < 6; i++)
        {
            int nextI = (i + 1) % 6;

            var v1 = center;
            var v2 = center + corners[i];
            var v3 = center + corners[nextI];

            st.SetColor(color);
            st.AddVertex(v1);
            st.SetColor(color);
            st.AddVertex(v2);
            st.SetColor(color);
            st.AddVertex(v3);
        }
    }

    private void RebuildDirtyChunks()
    {
        // For now, rebuild all chunks
        // TODO: Track which chunks are dirty and only rebuild those
        foreach (var (chunkPos, chunk) in Chunks.ToList())
        {
            chunk.QueueFree();
            var newChunk = BuildChunk(chunkPos);
            Chunks[chunkPos] = newChunk;
            AddChild(newChunk);
        }
    }

    private ShaderMaterial CreateFogMaterial()
    {
        var shader = new Shader();
        shader.Code = FogShaderCode;

        return new ShaderMaterial
        {
            Shader = shader
        };
    }

    private const string FogShaderCode = @"
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, unshaded;

void fragment() {
    ALBEDO = COLOR.rgb;
    ALPHA = COLOR.a;
}
";

    public override void Cleanup()
    {
        _fogMaterial?.Dispose();
        _fogMaterial = null;
        base.Cleanup();
    }
}

/// <summary>
/// Visibility states for fog of war.
/// </summary>
public enum VisibilityState
{
    /// <summary>
    /// Cell has never been seen.
    /// </summary>
    Unexplored,

    /// <summary>
    /// Cell was seen before but is not currently visible.
    /// </summary>
    Explored,

    /// <summary>
    /// Cell is currently visible to the player.
    /// </summary>
    Visible
}
