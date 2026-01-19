using HexGame.Core;

namespace HexGame.Rendering;

/// <summary>
/// Renders map features (trees, rocks, resources) using instanced meshes.
/// Uses GPU instancing for efficient rendering of many similar objects.
/// </summary>
public partial class FeatureRenderer : ChunkedRendererBase
{
    private readonly HexGrid _grid;
    private Mesh? _treeMesh;
    private Mesh? _rockMesh;
    private StandardMaterial3D? _treeMaterial;
    private StandardMaterial3D? _rockMaterial;

    /// <summary>
    /// Creates a new feature renderer.
    /// </summary>
    /// <param name="grid">The hex grid containing features.</param>
    public FeatureRenderer(HexGrid grid)
    {
        _grid = grid;
        MaxRenderDistance = RenderingConfig.FeatureRenderDistance;
    }

    /// <summary>
    /// Builds all feature chunks.
    /// </summary>
    protected override void DoBuild()
    {
        CreateBaseMeshes();

        int chunksX = (_grid.Width + ChunkSize - 1) / ChunkSize;
        int chunksY = (_grid.Height + ChunkSize - 1) / ChunkSize;

        for (int cy = 0; cy < chunksY; cy++)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                var chunkPos = new Vector2I(cx, cy);
                var chunkNode = BuildChunk(chunkPos);

                if (chunkNode.GetChildCount() > 0)
                {
                    Chunks[chunkPos] = chunkNode;
                    AddChild(chunkNode);
                }
                else
                {
                    chunkNode.QueueFree();
                }
            }
        }

        GD.Print($"FeatureRenderer: Built {Chunks.Count} feature chunks");
    }

    private void CreateBaseMeshes()
    {
        // Tree mesh (simple cone + cylinder trunk)
        _treeMesh = CreateTreeMesh();
        _treeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 0.5f, 0.15f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel
        };

        // Rock mesh (irregular box-ish shape)
        _rockMesh = CreateRockMesh();
        _rockMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.5f, 0.5f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel
        };
    }

    private static Mesh CreateTreeMesh()
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        // Trunk (cylinder approximation with 6 sides)
        float trunkRadius = 0.05f;
        float trunkHeight = 0.2f;
        var trunkColor = new Color(0.4f, 0.25f, 0.1f);

        AddCylinder(surfaceTool, Vector3.Zero, trunkRadius, trunkHeight, 6, trunkColor);

        // Foliage (cone)
        float foliageRadius = 0.25f;
        float foliageHeight = 0.5f;
        var foliageColor = new Color(0.2f, 0.5f, 0.15f);

        AddCone(surfaceTool, new Vector3(0, trunkHeight, 0), foliageRadius, foliageHeight, 8, foliageColor);

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    private static Mesh CreateRockMesh()
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        var color = new Color(0.5f, 0.5f, 0.5f);

        // Simple irregular shape (squashed octahedron)
        var top = new Vector3(0, 0.15f, 0);
        var bottom = new Vector3(0, -0.02f, 0);

        // Middle ring vertices with variation
        var ring = new Vector3[]
        {
            new(0.12f, 0.05f, 0),
            new(0.06f, 0.08f, 0.1f),
            new(-0.08f, 0.04f, 0.08f),
            new(-0.1f, 0.06f, -0.04f),
            new(-0.02f, 0.03f, -0.12f),
            new(0.08f, 0.07f, -0.06f)
        };

        // Top triangles
        for (int i = 0; i < ring.Length; i++)
        {
            int next = (i + 1) % ring.Length;
            AddTriangle(surfaceTool, top, ring[i], ring[next], color);
        }

        // Bottom triangles
        for (int i = 0; i < ring.Length; i++)
        {
            int next = (i + 1) % ring.Length;
            AddTriangle(surfaceTool, bottom, ring[next], ring[i], color);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    private static void AddCylinder(SurfaceTool st, Vector3 center, float radius, float height, int segments, Color color)
    {
        var bottom = center;
        var top = center + new Vector3(0, height, 0);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.Tau;
            float angle2 = (float)(i + 1) / segments * Mathf.Tau;

            var p1 = bottom + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            var p2 = bottom + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            var p3 = top + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            var p4 = top + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            // Side quad
            AddTriangle(st, p1, p3, p2, color);
            AddTriangle(st, p2, p3, p4, color);
        }
    }

    private static void AddCone(SurfaceTool st, Vector3 baseCenter, float radius, float height, int segments, Color color)
    {
        var apex = baseCenter + new Vector3(0, height, 0);

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.Tau;
            float angle2 = (float)(i + 1) / segments * Mathf.Tau;

            var p1 = baseCenter + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            var p2 = baseCenter + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            AddTriangle(st, apex, p1, p2, color);
            AddTriangle(st, baseCenter, p2, p1, color);
        }
    }

    private static void AddTriangle(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        st.SetColor(color);
        st.AddVertex(a);
        st.SetColor(color);
        st.AddVertex(b);
        st.SetColor(color);
        st.AddVertex(c);
    }

    private Node3D BuildChunk(Vector2I chunkPos)
    {
        var chunkNode = new Node3D
        {
            Name = $"FeatureChunk_{chunkPos.X}_{chunkPos.Y}"
        };

        int startQ = chunkPos.X * ChunkSize;
        int startR = chunkPos.Y * ChunkSize;
        int endQ = Math.Min(startQ + ChunkSize, _grid.Width);
        int endR = Math.Min(startR + ChunkSize, _grid.Height);

        var treeInstances = new List<Transform3D>();
        var rockInstances = new List<Transform3D>();

        for (int r = startR; r < endR; r++)
        {
            for (int q = startQ; q < endQ; q++)
            {
                var cell = _grid.GetCell(q, r);
                if (cell == null || HexMetrics.IsWaterElevation(cell.Elevation))
                {
                    continue;
                }

                // Collect feature transforms from the cell
                foreach (var feature in cell.Features)
                {
                    var pos = feature.Position;
                    var scale = Vector3.One * feature.Scale;
                    var rotation = feature.Rotation;

                    var transform = new Transform3D(
                        Basis.Identity.Scaled(scale).Rotated(Vector3.Up, rotation),
                        pos
                    );

                    if (feature.Type == Feature.FeatureType.Tree)
                    {
                        treeInstances.Add(transform);
                    }
                    else if (feature.Type == Feature.FeatureType.Rock)
                    {
                        rockInstances.Add(transform);
                    }
                }
            }
        }

        // Create instanced meshes
        if (treeInstances.Count > 0 && _treeMesh != null)
        {
            var treeMI = CreateMultiMeshInstance(treeInstances, _treeMesh, _treeMaterial);
            chunkNode.AddChild(treeMI);
        }

        if (rockInstances.Count > 0 && _rockMesh != null)
        {
            var rockMI = CreateMultiMeshInstance(rockInstances, _rockMesh, _rockMaterial);
            chunkNode.AddChild(rockMI);
        }

        return chunkNode;
    }

    private static MultiMeshInstance3D CreateMultiMeshInstance(
        List<Transform3D> transforms, Mesh mesh, StandardMaterial3D? material)
    {
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = transforms.Count
        };

        for (int i = 0; i < transforms.Count; i++)
        {
            multiMesh.SetInstanceTransform(i, transforms[i]);
        }

        return new MultiMeshInstance3D
        {
            Multimesh = multiMesh,
            MaterialOverride = material
        };
    }

    public override void Cleanup()
    {
        _treeMesh?.Dispose();
        _rockMesh?.Dispose();
        _treeMaterial?.Dispose();
        _rockMaterial?.Dispose();

        _treeMesh = null;
        _rockMesh = null;
        _treeMaterial = null;
        _rockMaterial = null;

        base.Cleanup();
    }
}
