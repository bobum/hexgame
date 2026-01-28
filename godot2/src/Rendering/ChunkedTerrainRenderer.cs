using Godot;
using System.Collections.Generic;

namespace HexGame.Rendering;

/// <summary>
/// Manages terrain chunks with LOD and distance-based culling.
/// Creates MEDIUM and LOW detail meshes alongside existing HexGridChunk (HIGH).
/// </summary>
public partial class ChunkedTerrainRenderer : ChunkedRendererBase
{
    private HexGrid? _grid;
    private readonly Dictionary<(int, int), ChunkWrapper> _chunkWrappers = new();
    private int _visibleChunkCount;

    // Skirt drops below terrain - just enough to hide gaps
    private static readonly float SkirtBaseY = -5.0f;

    // Shared material for LOD meshes that uses vertex colors
    private static StandardMaterial3D? _lodMaterial;

    private static StandardMaterial3D GetLodMaterial()
    {
        if (_lodMaterial == null)
        {
            _lodMaterial = new StandardMaterial3D();
            _lodMaterial.VertexColorUseAsAlbedo = true;
            _lodMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
        }
        return _lodMaterial;
    }

    /// <summary>
    /// Wraps a HexGridChunk with LOD meshes.
    /// </summary>
    private class ChunkWrapper
    {
        public HexGridChunk? ChunkHigh;      // Original high-detail chunk
        public MeshInstance3D? MeshMedium;   // Flat hexes + skirts
        public MeshInstance3D? MeshLow;      // Simple quads + skirts
        public int ChunkX;
        public int ChunkZ;
        public Vector3 Center;
        public LodLevel CurrentLod = LodLevel.High;
    }

    /// <summary>
    /// Sets up the renderer with a grid reference.
    /// </summary>
    public void Setup(HexGrid grid)
    {
        _grid = grid;
    }

    /// <summary>
    /// Builds LOD meshes for all chunks.
    /// </summary>
    protected override void DoBuild()
    {
        if (_grid == null)
        {
            GD.PrintErr("ChunkedTerrainRenderer: Grid not set");
            return;
        }

        _chunkWrappers.Clear();
        _visibleChunkCount = 0;

        // Collect chunks
        var chunks = CollectChunksIterative(_grid);

        foreach (var chunk in chunks)
        {
            var (chunkX, chunkZ) = ParseChunkName(chunk.Name);
            if (chunkX < 0 || chunkZ < 0)
                continue;

            var key = (chunkX, chunkZ);
            if (_chunkWrappers.ContainsKey(key))
                continue;

            var center = CalculateChunkCenterFromCells(chunkX, chunkZ);

            var wrapper = new ChunkWrapper
            {
                ChunkHigh = chunk,
                ChunkX = chunkX,
                ChunkZ = chunkZ,
                Center = center,
                CurrentLod = LodLevel.High
            };

            // Build MEDIUM and LOW LOD meshes
            if (chunk.Cells != null && chunk.Cells.Length > 0)
            {
                wrapper.MeshMedium = BuildMediumLodMesh(chunk.Cells, chunkX, chunkZ);
                wrapper.MeshLow = BuildLowLodMesh(chunk.Cells, chunkX, chunkZ);

                if (wrapper.MeshMedium != null)
                {
                    wrapper.MeshMedium.Visible = false;
                    AddChild(wrapper.MeshMedium);
                }
                if (wrapper.MeshLow != null)
                {
                    wrapper.MeshLow.Visible = false;
                    AddChild(wrapper.MeshLow);
                }
            }

            _chunkWrappers[key] = wrapper;
            _visibleChunkCount++;
        }

        GD.Print($"ChunkedTerrainRenderer: Built {_chunkWrappers.Count} chunks with LOD meshes");
    }

    /// <summary>
    /// Builds MEDIUM LOD mesh: flat hexes with skirts.
    /// </summary>
    private MeshInstance3D? BuildMediumLodMesh(HexCell[] cells, int chunkX, int chunkZ)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        var corners = HexMetrics.Corners;
        var upNormal = new Vector3(0, 1, 0);
        int validCells = 0;

        foreach (var cell in cells)
        {
            if (cell == null) continue;
            validCells++;

            var center = cell.Position;
            var color = GetCellColor(cell);

            // Flat hex top - 6 triangles from center
            // Winding order: counter-clockwise for front face (Godot default)
            // Corners array goes clockwise, so use c2 -> c1 order
            for (int i = 0; i < 6; i++)
            {
                var c1 = corners[i];
                var c2 = corners[i + 1];

                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(center);

                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z));

                st.SetNormal(upNormal);
                st.SetColor(color);
                st.AddVertex(new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z));
            }

            // Skirt - vertical walls dropping to SkirtBaseY
            BuildSkirt(st, center, corners, color);
        }

        if (validCells == 0)
            return null;

        var mesh = st.Commit();
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        meshInstance.MaterialOverride = GetLodMaterial();
        meshInstance.Name = $"Chunk_{chunkX}_{chunkZ}_MED";
        meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        return meshInstance;
    }

    /// <summary>
    /// Builds LOW LOD mesh: simple quads with skirts.
    /// </summary>
    private MeshInstance3D? BuildLowLodMesh(HexCell[] cells, int chunkX, int chunkZ)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        var upNormal = new Vector3(0, 1, 0);
        float size = HexMetrics.OuterRadius * 0.85f;
        int validCells = 0;

        foreach (var cell in cells)
        {
            if (cell == null) continue;
            validCells++;

            var center = cell.Position;
            var color = GetCellColor(cell);

            // Simple quad - counter-clockwise winding for front face
            var v1 = new Vector3(center.X - size, center.Y, center.Z - size);
            var v2 = new Vector3(center.X + size, center.Y, center.Z - size);
            var v3 = new Vector3(center.X + size, center.Y, center.Z + size);
            var v4 = new Vector3(center.X - size, center.Y, center.Z + size);

            // Triangle 1: v1 -> v3 -> v2 (counter-clockwise from above)
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v1);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v3);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v2);

            // Triangle 2: v1 -> v4 -> v3 (counter-clockwise from above)
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v1);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v4);
            st.SetNormal(upNormal);
            st.SetColor(color);
            st.AddVertex(v3);

            // Simple skirt for quad
            BuildQuadSkirt(st, v1, v2, v3, v4, color);
        }

        if (validCells == 0)
            return null;

        var mesh = st.Commit();
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = mesh;
        meshInstance.MaterialOverride = GetLodMaterial();
        meshInstance.Name = $"Chunk_{chunkX}_{chunkZ}_LOW";
        meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

        return meshInstance;
    }

    /// <summary>
    /// Builds hex skirt walls.
    /// </summary>
    private void BuildSkirt(SurfaceTool st, Vector3 center, Vector3[] corners, Color color)
    {
        for (int i = 0; i < 6; i++)
        {
            var c1 = corners[i];
            var c2 = corners[i + 1];

            var topLeft = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
            var topRight = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);
            var bottomLeft = new Vector3(center.X + c1.X, SkirtBaseY, center.Z + c1.Z);
            var bottomRight = new Vector3(center.X + c2.X, SkirtBaseY, center.Z + c2.Z);

            var edge = topRight - topLeft;
            var outward = new Vector3(edge.Z, 0, -edge.X).Normalized();

            // Triangle 1
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(topLeft);
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(bottomLeft);
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(bottomRight);

            // Triangle 2
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(topLeft);
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(bottomRight);
            st.SetNormal(outward);
            st.SetColor(color);
            st.AddVertex(topRight);
        }
    }

    /// <summary>
    /// Builds quad skirt walls.
    /// </summary>
    private void BuildQuadSkirt(SurfaceTool st, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color)
    {
        // Four sides of the quad
        BuildSkirtSide(st, v1, v2, color);
        BuildSkirtSide(st, v2, v3, color);
        BuildSkirtSide(st, v3, v4, color);
        BuildSkirtSide(st, v4, v1, color);
    }

    private void BuildSkirtSide(SurfaceTool st, Vector3 topLeft, Vector3 topRight, Color color)
    {
        var bottomLeft = new Vector3(topLeft.X, SkirtBaseY, topLeft.Z);
        var bottomRight = new Vector3(topRight.X, SkirtBaseY, topRight.Z);

        var edge = topRight - topLeft;
        var outward = new Vector3(edge.Z, 0, -edge.X).Normalized();

        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(topLeft);
        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(bottomLeft);
        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(bottomRight);

        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(topLeft);
        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(bottomRight);
        st.SetNormal(outward);
        st.SetColor(color);
        st.AddVertex(topRight);
    }

    /// <summary>
    /// Gets a representative color for a cell.
    /// </summary>
    private Color GetCellColor(HexCell cell)
    {
        // Use terrain type to determine color
        return cell.TerrainTypeIndex switch
        {
            0 => new Color(0.2f, 0.6f, 0.2f),  // Grass - green
            1 => new Color(0.6f, 0.5f, 0.3f),  // Mud - brown
            2 => new Color(0.9f, 0.85f, 0.6f), // Sand - tan
            3 => new Color(0.95f, 0.95f, 0.95f), // Snow - white
            4 => new Color(0.5f, 0.5f, 0.5f),  // Stone - gray
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }

    /// <summary>
    /// Updates visibility and LOD based on camera distance.
    /// </summary>
    public override void UpdateVisibility(Camera3D camera)
    {
        if (_disposed || camera == null || _chunkWrappers.Count == 0)
            return;

        var cameraPos = camera.GlobalPosition;
        var cameraXz = new Vector3(cameraPos.X, 0, cameraPos.Z);

        foreach (var wrapper in _chunkWrappers.Values)
        {
            if (wrapper.ChunkHigh == null)
                continue;

            float dx = wrapper.Center.X - cameraXz.X;
            float dz = wrapper.Center.Z - cameraXz.Z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            var newLod = SelectLod(dist, wrapper.CurrentLod);

            if (newLod != wrapper.CurrentLod)
            {
                SetLodVisibility(wrapper, newLod);
                wrapper.CurrentLod = newLod;
            }
        }
    }

    /// <summary>
    /// Sets visibility for the appropriate LOD level.
    /// </summary>
    private void SetLodVisibility(ChunkWrapper wrapper, LodLevel lod)
    {
        // Hide all first
        if (wrapper.ChunkHigh != null)
            wrapper.ChunkHigh.Visible = false;
        if (wrapper.MeshMedium != null)
            wrapper.MeshMedium.Visible = false;
        if (wrapper.MeshLow != null)
            wrapper.MeshLow.Visible = false;

        // Show appropriate level
        switch (lod)
        {
            case LodLevel.High:
                if (wrapper.ChunkHigh != null)
                    wrapper.ChunkHigh.Visible = true;
                break;
            case LodLevel.Medium:
                if (wrapper.MeshMedium != null)
                    wrapper.MeshMedium.Visible = true;
                else if (wrapper.ChunkHigh != null)
                    wrapper.ChunkHigh.Visible = true; // Fallback
                break;
            case LodLevel.Low:
                if (wrapper.MeshLow != null)
                    wrapper.MeshLow.Visible = true;
                else if (wrapper.MeshMedium != null)
                    wrapper.MeshMedium.Visible = true; // Fallback
                else if (wrapper.ChunkHigh != null)
                    wrapper.ChunkHigh.Visible = true;
                break;
            case LodLevel.Culled:
                // All hidden
                break;
        }

        // Track visible count
        if (lod == LodLevel.Culled)
            _visibleChunkCount--;
        else if (wrapper.CurrentLod == LodLevel.Culled)
            _visibleChunkCount++;
    }

    private List<HexGridChunk> CollectChunksIterative(Node root)
    {
        var chunks = new List<HexGridChunk>();
        var stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is HexGridChunk chunk)
                chunks.Add(chunk);

            var children = current.GetChildren();
            for (int i = children.Count - 1; i >= 0; i--)
                stack.Push(children[i]);
        }

        return chunks;
    }

    private (int x, int z) ParseChunkName(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.StartsWith("Chunk_"))
            return (-1, -1);

        var parts = name.Split('_');
        if (parts.Length != 3)
            return (-1, -1);

        if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
            return (x, z);

        return (-1, -1);
    }

    private Vector3 CalculateChunkCenterFromCells(int chunkX, int chunkZ)
    {
        int startCellX = chunkX * HexMetrics.ChunkSizeX;
        int startCellZ = chunkZ * HexMetrics.ChunkSizeZ;
        int centerCellX = startCellX + HexMetrics.ChunkSizeX / 2;
        int centerCellZ = startCellZ + HexMetrics.ChunkSizeZ / 2;

        float worldX = (centerCellX + centerCellZ * 0.5f - centerCellZ / 2) * (HexMetrics.InnerRadius * 2f);
        float worldZ = centerCellZ * (HexMetrics.OuterRadius * 1.5f);

        return new Vector3(worldX, 0, worldZ);
    }

    public int GetVisibleChunkCount() => _visibleChunkCount;
    public int GetTotalChunkCount() => _chunkWrappers.Count;

    public override void Cleanup()
    {
        foreach (var wrapper in _chunkWrappers.Values)
        {
            wrapper.MeshMedium?.QueueFree();
            wrapper.MeshLow?.QueueFree();
        }
        _chunkWrappers.Clear();
        _visibleChunkCount = 0;
        base.Cleanup();
    }
}
