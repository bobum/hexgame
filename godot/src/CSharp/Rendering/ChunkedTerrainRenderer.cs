namespace HexGame.Rendering;

using HexGame.Core;

/// <summary>
/// Renders hex terrain using a chunk-based system with LOD and culling.
/// Direct port of chunked_terrain_renderer.gd
/// </summary>
public partial class ChunkedTerrainRenderer : Node3D
{
    private const float ChunkSize = 16.0f;
    private const float MaxRenderDistance = 60.0f;
    private const float LodHighToMedium = 30.0f;
    private const float LodMediumToLow = 60.0f;
    private const float ReferenceZoom = 30.0f;
    private static readonly float SkirtBaseY = HexMetrics.MinElevation * HexMetrics.ElevationStep - 1.0f;

    private ShaderMaterial? _terrainMaterial;
    private Shader? _terrainShader;
    private readonly Dictionary<string, TerrainChunk> _chunks = new();
    private int _totalChunkCount = 0;

    private class TerrainChunk
    {
        public MeshInstance3D? MeshHigh;
        public MeshInstance3D? MeshMedium;
        public MeshInstance3D? MeshLow;
        public MeshInstance3D? MeshSkirt;
        public List<HexCell> Cells = new();
        public int ChunkX;
        public int ChunkZ;
        public Vector3 Center = Vector3.Zero;
    }

    public override void _Ready()
    {
        _terrainShader = GD.Load<Shader>("res://src/rendering/terrain_shader.gdshader");
        _terrainMaterial = new ShaderMaterial();
        _terrainMaterial.Shader = _terrainShader;
        _terrainMaterial.SetShaderParameter("depth_bias", 0.001f);
    }

    private string GetChunkKey(int cx, int cz) => $"{cx},{cz}";

    private Vector2I GetCellChunkCoords(HexCell cell)
    {
        var worldPos = cell.GetWorldPosition();
        int cx = (int)Mathf.Floor(worldPos.X / ChunkSize);
        int cz = (int)Mathf.Floor(worldPos.Z / ChunkSize);
        return new Vector2I(cx, cz);
    }

    private Vector3 GetChunkCenter(int cx, int cz)
    {
        return new Vector3((cx + 0.5f) * ChunkSize, 0, (cz + 0.5f) * ChunkSize);
    }

    /// <summary>
    /// Build terrain from grid
    /// </summary>
    public void Build(HexGrid grid)
    {
        Dispose();

        // Group cells into chunks
        foreach (var cell in grid.GetAllCells())
        {
            var chunkCoords = GetCellChunkCoords(cell);
            var key = GetChunkKey(chunkCoords.X, chunkCoords.Y);

            if (!_chunks.ContainsKey(key))
            {
                var newChunk = new TerrainChunk
                {
                    ChunkX = chunkCoords.X,
                    ChunkZ = chunkCoords.Y,
                    Center = GetChunkCenter(chunkCoords.X, chunkCoords.Y)
                };
                _chunks[key] = newChunk;
            }

            _chunks[key].Cells.Add(cell);
        }

        // Build meshes for all chunks
        foreach (var chunk in _chunks.Values)
        {
            BuildChunkMeshes(chunk, grid);
        }

        _totalChunkCount = _chunks.Count;
        GD.Print($"Built {_totalChunkCount} terrain chunks");
    }

    private void BuildChunkMeshes(TerrainChunk chunk, HexGrid grid)
    {
        if (chunk.Cells.Count == 0)
            return;

        // HIGH detail - full hex with terraces
        var builderHigh = new HexMeshBuilder();
        foreach (var cell in chunk.Cells)
        {
            builderHigh.BuildCell(cell, grid);
        }
        var meshHigh = builderHigh.CommitMesh();

        chunk.MeshHigh = new MeshInstance3D();
        chunk.MeshHigh.Mesh = meshHigh;
        chunk.MeshHigh.MaterialOverride = _terrainMaterial;
        chunk.MeshHigh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        chunk.MeshHigh.Name = $"Chunk_{chunk.ChunkX}_{chunk.ChunkZ}_HIGH";
        AddChild(chunk.MeshHigh);

        // MEDIUM detail - flat hexes
        var meshMedium = BuildFlatHexMesh(chunk.Cells);
        chunk.MeshMedium = new MeshInstance3D();
        chunk.MeshMedium.Mesh = meshMedium;
        chunk.MeshMedium.MaterialOverride = _terrainMaterial;
        chunk.MeshMedium.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        chunk.MeshMedium.Name = $"Chunk_{chunk.ChunkX}_{chunk.ChunkZ}_MED";
        chunk.MeshMedium.Visible = false;
        AddChild(chunk.MeshMedium);

        // LOW detail - simple quads
        var meshLow = BuildSimpleQuadMesh(chunk.Cells);
        chunk.MeshLow = new MeshInstance3D();
        chunk.MeshLow.Mesh = meshLow;
        chunk.MeshLow.MaterialOverride = _terrainMaterial;
        chunk.MeshLow.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        chunk.MeshLow.Name = $"Chunk_{chunk.ChunkX}_{chunk.ChunkZ}_LOW";
        chunk.MeshLow.Visible = false;
        AddChild(chunk.MeshLow);
    }

    private ArrayMesh BuildFlatHexMesh(List<HexCell> cells)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var corners = HexMetrics.GetCorners();
        var upNormal = new Vector3(0, 1, 0);

        foreach (var cell in cells)
        {
            if (cell.IsUnderwater && cell.Elevation >= HexMetrics.SeaLevel - 2)
                continue;

            var center = cell.GetWorldPosition();
            var baseColor = cell.GetColor();

            // Boost saturation slightly
            float h = baseColor.H;
            float s = Mathf.Min(baseColor.S * 1.15f, 1.0f);
            float v = baseColor.V;
            var color = Color.FromHsv(h, s, v);
            var skirtColor = color;

            // Build hex top as 6 triangles from center
            for (int i = 0; i < 6; i++)
            {
                var c1 = corners[i];
                var c2 = corners[(i + 1) % 6];

                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(center);
                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z));
                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z));
            }

            // Build hex skirt
            for (int i = 0; i < 6; i++)
            {
                var c1 = corners[i];
                var c2 = corners[(i + 1) % 6];

                var topLeft = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
                var topRight = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);
                var bottomLeft = new Vector3(center.X + c1.X, SkirtBaseY, center.Z + c1.Z);
                var bottomRight = new Vector3(center.X + c2.X, SkirtBaseY, center.Z + c2.Z);

                var edge = topRight - topLeft;
                var outward = new Vector3(edge.Z, 0, -edge.X).Normalized();

                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(topLeft);
                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(bottomLeft);
                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(bottomRight);

                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(topLeft);
                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(bottomRight);
                st.SetNormal(outward);
                st.SetColor(skirtColor);
                st.AddVertex(topRight);
            }
        }

        return st.Commit();
    }

    private ArrayMesh BuildSimpleQuadMesh(List<HexCell> cells)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var upNormal = new Vector3(0, 1, 0);

        foreach (var cell in cells)
        {
            if (cell.IsUnderwater && cell.Elevation >= HexMetrics.SeaLevel - 2)
                continue;

            var center = cell.GetWorldPosition();
            var baseColor = cell.GetColor();

            float h = baseColor.H;
            float s = Mathf.Min(baseColor.S * 1.15f, 1.0f);
            float v = baseColor.V;
            var color = Color.FromHsv(h, s, v);
            var skirtColor = color;
            float size = HexMetrics.OuterRadius * 0.85f;

            var v1 = new Vector3(center.X - size, center.Y, center.Z - size);
            var v2 = new Vector3(center.X + size, center.Y, center.Z - size);
            var v3 = new Vector3(center.X + size, center.Y, center.Z + size);
            var v4 = new Vector3(center.X - size, center.Y, center.Z + size);

            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v1);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v2);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v3);

            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v1);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v3);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v4);

            // Box skirt
            var quadCorners = new[] { v1, v2, v3, v4 };
            var wallNormals = new[]
            {
                new Vector3(0, 0, -1),
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(-1, 0, 0)
            };

            for (int i = 0; i < 4; i++)
            {
                var c1 = quadCorners[i];
                var c2 = quadCorners[(i + 1) % 4];
                var wallNormal = wallNormals[i];

                var topLeft = c1;
                var topRight = c2;
                var bottomLeft = new Vector3(c1.X, SkirtBaseY, c1.Z);
                var bottomRight = new Vector3(c2.X, SkirtBaseY, c2.Z);

                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(topLeft);
                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(bottomLeft);
                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(bottomRight);

                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(topLeft);
                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(bottomRight);
                st.SetNormal(wallNormal);
                st.SetColor(skirtColor);
                st.AddVertex(topRight);
            }
        }

        return st.Commit();
    }

    /// <summary>
    /// Update visibility and LOD based on camera
    /// </summary>
    public void Update(Camera3D camera)
    {
        var cameraPos = camera.GlobalPosition;
        var cameraXz = new Vector3(cameraPos.X, 0, cameraPos.Z);

        float effectiveMaxDist = MaxRenderDistance;
        float maxDistSq = effectiveMaxDist * effectiveMaxDist;

        int visibleCount = 0;
        int culledCount = 0;

        foreach (var kvp in _chunks)
        {
            var chunk = kvp.Value;
            if (chunk.MeshHigh == null)
                continue;

            float dx = chunk.Center.X - cameraXz.X;
            float dz = chunk.Center.Z - cameraXz.Z;
            float distSq = dx * dx + dz * dz;

            if (distSq > maxDistSq)
            {
                chunk.MeshHigh.Visible = false;
                chunk.MeshMedium!.Visible = false;
                chunk.MeshLow!.Visible = false;
                culledCount++;
                continue;
            }

            visibleCount++;
            float dist = Mathf.Sqrt(distSq);

            if (dist < LodHighToMedium)
            {
                chunk.MeshHigh.Visible = true;
                chunk.MeshMedium!.Visible = false;
                chunk.MeshLow!.Visible = false;
            }
            else if (dist < LodMediumToLow)
            {
                chunk.MeshHigh.Visible = false;
                chunk.MeshMedium!.Visible = true;
                chunk.MeshLow!.Visible = false;
            }
            else
            {
                chunk.MeshHigh.Visible = false;
                chunk.MeshMedium!.Visible = false;
                chunk.MeshLow!.Visible = true;
            }
        }
    }

    public int GetChunkCount() => _totalChunkCount;

    public new void Dispose()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.MeshHigh?.QueueFree();
            chunk.MeshMedium?.QueueFree();
            chunk.MeshLow?.QueueFree();
            chunk.MeshSkirt?.QueueFree();
        }
        _chunks.Clear();
        _totalChunkCount = 0;
    }
}
