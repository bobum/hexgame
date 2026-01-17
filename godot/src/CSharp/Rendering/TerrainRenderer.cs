using HexGame.Core;

namespace HexGame.Rendering;

/// <summary>
/// Renders hex terrain using chunked mesh generation.
/// Creates terraced terrain with proper elevation transitions.
/// </summary>
public partial class TerrainRenderer : ChunkedRendererBase
{
    private readonly HexGrid _grid;
    private readonly Dictionary<TerrainType, Color> _terrainColors;

    /// <summary>
    /// Creates a new terrain renderer.
    /// </summary>
    /// <param name="grid">The hex grid to render.</param>
    public TerrainRenderer(HexGrid grid)
    {
        _grid = grid;
        MaxRenderDistance = RenderingConfig.TerrainRenderDistance;

        _terrainColors = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Ocean, new Color(0.1f, 0.2f, 0.5f) },
            { TerrainType.Coast, new Color(0.2f, 0.4f, 0.6f) },
            { TerrainType.Plains, new Color(0.6f, 0.8f, 0.3f) },
            { TerrainType.Grassland, new Color(0.4f, 0.7f, 0.2f) },
            { TerrainType.Forest, new Color(0.2f, 0.5f, 0.15f) },
            { TerrainType.Hills, new Color(0.5f, 0.45f, 0.3f) },
            { TerrainType.Mountains, new Color(0.5f, 0.5f, 0.5f) },
            { TerrainType.Desert, new Color(0.9f, 0.85f, 0.5f) },
            { TerrainType.Snow, new Color(0.95f, 0.95f, 1.0f) },
            { TerrainType.Tundra, new Color(0.7f, 0.75f, 0.7f) },
            { TerrainType.Jungle, new Color(0.1f, 0.4f, 0.1f) },
            { TerrainType.Swamp, new Color(0.3f, 0.35f, 0.2f) },
            { TerrainType.Savanna, new Color(0.8f, 0.7f, 0.3f) },
            { TerrainType.Taiga, new Color(0.3f, 0.45f, 0.35f) },
        };
    }

    /// <summary>
    /// Builds all terrain chunks.
    /// </summary>
    protected override void DoBuild()
    {
        // Calculate chunk count based on grid size
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

        GD.Print($"TerrainRenderer: Built {Chunks.Count} chunks");
    }

    private Node3D BuildChunk(Vector2I chunkPos)
    {
        var chunkNode = new Node3D
        {
            Name = $"TerrainChunk_{chunkPos.X}_{chunkPos.Y}"
        };

        // Calculate cell range for this chunk
        int startQ = chunkPos.X * ChunkSize;
        int startR = chunkPos.Y * ChunkSize;
        int endQ = Math.Min(startQ + ChunkSize, _grid.Width);
        int endR = Math.Min(startR + ChunkSize, _grid.Height);

        // Build mesh for this chunk
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        for (int r = startR; r < endR; r++)
        {
            for (int q = startQ; q < endQ; q++)
            {
                var cell = _grid.GetCell(q, r);
                if (cell != null)
                {
                    AddCellToMesh(surfaceTool, cell);
                }
            }
        }

        surfaceTool.GenerateNormals();
        var mesh = surfaceTool.Commit();

        if (mesh != null)
        {
            var meshInstance = new MeshInstance3D
            {
                Mesh = mesh,
                MaterialOverride = CreateTerrainMaterial()
            };
            chunkNode.AddChild(meshInstance);
        }

        return chunkNode;
    }

    private void AddCellToMesh(SurfaceTool st, HexCell cell)
    {
        var center = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep);
        var color = GetTerrainColor(cell.TerrainType);
        var corners = HexMetrics.GetCorners();

        // Build center triangles (solid region)
        for (int i = 0; i < 6; i++)
        {
            int nextI = (i + 1) % 6;

            // Inner solid triangle
            var v1 = center;
            var v2 = center + corners[i] * HexMetrics.SolidFactor;
            var v3 = center + corners[nextI] * HexMetrics.SolidFactor;

            st.SetColor(color);
            st.AddVertex(v1);
            st.SetColor(color);
            st.AddVertex(v2);
            st.SetColor(color);
            st.AddVertex(v3);
        }

        // Build edge connections to neighbors
        for (int dir = 0; dir < 6; dir++)
        {
            var neighbor = _grid.GetNeighbor(cell, (HexDirection)dir);
            if (neighbor != null)
            {
                AddEdgeConnection(st, cell, neighbor, dir, color);
            }
        }
    }

    private void AddEdgeConnection(SurfaceTool st, HexCell cell, HexCell neighbor, int direction, Color cellColor)
    {
        var corners = HexMetrics.GetCorners();
        int nextDir = (direction + 1) % 6;

        var center = cell.Coordinates.ToWorldPosition(cell.Elevation * HexMetrics.ElevationStep);
        var neighborCenter = neighbor.Coordinates.ToWorldPosition(neighbor.Elevation * HexMetrics.ElevationStep);

        // Edge vertices on our cell
        var v1 = center + corners[direction] * HexMetrics.SolidFactor;
        var v2 = center + corners[nextDir] * HexMetrics.SolidFactor;

        // Bridge midpoint
        var bridge1 = center + corners[direction];
        var bridge2 = center + corners[nextDir];

        // Blend colors at edges
        var neighborColor = GetTerrainColor(neighbor.TerrainType);
        var midColor = cellColor.Lerp(neighborColor, 0.5f);

        // Check elevation difference for terracing
        int elevDiff = neighbor.Elevation - cell.Elevation;

        if (Math.Abs(elevDiff) <= 1)
        {
            // Simple edge connection or gentle slope
            AddEdgeQuad(st, v1, v2, bridge1, bridge2, cellColor, midColor);
        }
        else
        {
            // Terraced connection for steeper slopes
            AddTerracedEdge(st, v1, v2, bridge1, bridge2, cellColor, midColor, elevDiff);
        }
    }

    private void AddEdgeQuad(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2)
    {
        // First triangle
        st.SetColor(c1);
        st.AddVertex(v1);
        st.SetColor(c2);
        st.AddVertex(v3);
        st.SetColor(c1);
        st.AddVertex(v2);

        // Second triangle
        st.SetColor(c1);
        st.AddVertex(v2);
        st.SetColor(c2);
        st.AddVertex(v3);
        st.SetColor(c2);
        st.AddVertex(v4);
    }

    private void AddTerracedEdge(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        Color c1, Color c2, int elevationDiff)
    {
        int steps = HexMetrics.TerraceSteps;

        var prev1 = v1;
        var prev2 = v2;
        var prevColor = c1;

        for (int step = 1; step <= steps; step++)
        {
            var next1 = HexMetrics.TerraceLerp(v1, v3, step);
            var next2 = HexMetrics.TerraceLerp(v2, v4, step);
            var nextColor = HexMetrics.TerraceColorLerp(c1, c2, step);

            AddEdgeQuad(st, prev1, prev2, next1, next2, prevColor, nextColor);

            prev1 = next1;
            prev2 = next2;
            prevColor = nextColor;
        }
    }

    private Color GetTerrainColor(TerrainType terrain)
    {
        return _terrainColors.TryGetValue(terrain, out var color) ? color : Colors.Magenta;
    }

    private static StandardMaterial3D CreateTerrainMaterial()
    {
        return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            CullMode = BaseMaterial3D.CullModeEnum.Back
        };
    }

    /// <summary>
    /// Updates a single cell (for incremental updates).
    /// </summary>
    public void UpdateCell(HexCell cell)
    {
        var chunkPos = GetChunkPos(cell.Q, cell.R);
        if (Chunks.TryGetValue(chunkPos, out var chunk))
        {
            // For now, rebuild entire chunk
            // TODO: Implement incremental mesh updates
            chunk.QueueFree();
            var newChunk = BuildChunk(chunkPos);
            Chunks[chunkPos] = newChunk;
            AddChild(newChunk);
        }
    }
}
