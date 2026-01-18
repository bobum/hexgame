namespace HexGame.Rendering;

using HexGame.Core;

/// <summary>
/// Renders rivers using chunked meshes for distance culling.
/// Direct port of chunked_river_renderer.gd
/// </summary>
public partial class ChunkedRiverRenderer : Node3D
{
    private const float ChunkSize = 16.0f;
    private const float MaxRenderDistance = 50.0f;
    private const float RiverWidth = 0.15f;
    private const float HeightOffset = 0.02f;

    private HexGrid? _grid;
    private ShaderMaterial? _material;
    private float _time = 0.0f;

    private readonly Dictionary<string, RiverChunk> _chunks = new();

    // Direction to corner mapping
    private static readonly Vector2I[] DirectionToCorners = {
        new(5, 0), new(4, 5), new(3, 4),
        new(2, 3), new(1, 2), new(0, 1)
    };

    private class RiverChunk
    {
        public MeshInstance3D? MeshInstance;
        public int ChunkX;
        public int ChunkZ;
        public Vector3 Center = Vector3.Zero;
    }

    private string GetChunkKey(int cx, int cz) => $"{cx},{cz}";

    private Vector2I GetCellChunkCoords(HexCell cell)
    {
        var worldPos = cell.GetWorldPosition();
        return new Vector2I((int)Mathf.Floor(worldPos.X / ChunkSize), (int)Mathf.Floor(worldPos.Z / ChunkSize));
    }

    private Vector3 GetChunkCenter(int cx, int cz)
    {
        return new Vector3((cx + 0.5f) * ChunkSize, 0, (cz + 0.5f) * ChunkSize);
    }

    public void Setup(HexGrid grid)
    {
        _grid = grid;
    }

    public void Build()
    {
        Dispose();
        _material = CreateRiverMaterial();

        if (_grid == null) return;

        // Build incoming rivers map
        var incomingRivers = new Dictionary<string, List<int>>();
        foreach (var cell in _grid.GetAllCells())
        {
            foreach (int dir in cell.RiverDirections)
            {
                var neighbor = _grid.GetNeighbor(cell, (HexDirection)dir);
                if (neighbor != null)
                {
                    var key = $"{neighbor.Q},{neighbor.R}";
                    if (!incomingRivers.ContainsKey(key))
                        incomingRivers[key] = new List<int>();
                    incomingRivers[key].Add((int)((HexDirection)dir).Opposite());
                }
            }
        }

        // Group river cells by chunk
        var chunkRiverCells = new Dictionary<string, List<RiverCellData>>();

        foreach (var cell in _grid.GetAllCells())
        {
            var outgoing = cell.RiverDirections;
            var incomingKey = $"{cell.Q},{cell.R}";
            var incoming = incomingRivers.GetValueOrDefault(incomingKey, new List<int>());

            if (outgoing.Count == 0 && incoming.Count == 0)
                continue;

            var chunkCoords = GetCellChunkCoords(cell);
            var key = GetChunkKey(chunkCoords.X, chunkCoords.Y);

            if (!_chunks.ContainsKey(key))
            {
                var newChunk = new RiverChunk
                {
                    ChunkX = chunkCoords.X,
                    ChunkZ = chunkCoords.Y,
                    Center = GetChunkCenter(chunkCoords.X, chunkCoords.Y)
                };
                _chunks[key] = newChunk;
            }

            if (!chunkRiverCells.ContainsKey(key))
                chunkRiverCells[key] = new List<RiverCellData>();

            chunkRiverCells[key].Add(new RiverCellData(cell, outgoing.ToList(), incoming));
        }

        // Build mesh for each chunk
        int totalVerts = 0;
        foreach (var key in _chunks.Keys)
        {
            var chunk = _chunks[key];
            var riverData = chunkRiverCells.GetValueOrDefault(key, new List<RiverCellData>());

            if (riverData.Count > 0)
            {
                var mesh = BuildRiverMesh(riverData);
                if (mesh != null)
                {
                    chunk.MeshInstance = new MeshInstance3D();
                    chunk.MeshInstance.Mesh = mesh;
                    chunk.MeshInstance.MaterialOverride = _material;
                    chunk.MeshInstance.Name = $"River_{key}";
                    AddChild(chunk.MeshInstance);
                    totalVerts += mesh.GetFaces().Length;
                }
            }
        }

        GD.Print($"Built river mesh: {totalVerts} vertices");
    }

    private record struct RiverCellData(HexCell Cell, List<int> Outgoing, List<int> Incoming);

    private ArrayMesh? BuildRiverMesh(List<RiverCellData> riverData)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();
        int vertexIndex = 0;

        var corners = HexMetrics.GetCorners();
        float halfWidth = RiverWidth;
        var renderedEdges = new HashSet<string>();

        foreach (var data in riverData)
        {
            var cell = data.Cell;
            var outgoing = data.Outgoing;
            var incoming = data.Incoming;

            var centerPos = cell.GetWorldPosition();
            var worldCorners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                worldCorners[i] = new Vector3(centerPos.X + corners[i].X, 0, centerPos.Z + corners[i].Z);
            }

            // Draw outgoing river edges
            foreach (int outDir in outgoing)
            {
                var neighbor = _grid!.GetNeighbor(cell, (HexDirection)outDir);
                if (neighbor == null)
                    continue;

                var edgeKey = GetEdgeKey(cell.Q, cell.R, neighbor.Q, neighbor.R);
                if (renderedEdges.Contains(edgeKey))
                    continue;
                renderedEdges.Add(edgeKey);

                var cornerPair = DirectionToCorners[outDir];
                var c1 = worldCorners[cornerPair.X];
                var c2 = worldCorners[cornerPair.Y];

                float edgeDx = c2.X - c1.X;
                float edgeDz = c2.Z - c1.Z;
                float edgeLen = Mathf.Sqrt(edgeDx * edgeDx + edgeDz * edgeDz);
                float perpX = -edgeDz / edgeLen * halfWidth;
                float perpZ = edgeDx / edgeLen * halfWidth;

                float highY = Mathf.Max(cell.Elevation, neighbor.Elevation) * HexMetrics.ElevationStep + HeightOffset;
                float lowY = Mathf.Min(cell.Elevation, neighbor.Elevation) * HexMetrics.ElevationStep + HeightOffset;

                // Horizontal quad
                vertices.Add(new Vector3(c1.X - perpX, highY, c1.Z - perpZ));
                vertices.Add(new Vector3(c1.X + perpX, highY, c1.Z + perpZ));
                vertices.Add(new Vector3(c2.X + perpX, highY, c2.Z + perpZ));
                vertices.Add(new Vector3(c2.X - perpX, highY, c2.Z - perpZ));
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                indices.Add(vertexIndex); indices.Add(vertexIndex + 1); indices.Add(vertexIndex + 2);
                indices.Add(vertexIndex); indices.Add(vertexIndex + 2); indices.Add(vertexIndex + 3);
                vertexIndex += 4;

                // Waterfall if downhill
                if (cell.Elevation > neighbor.Elevation)
                {
                    bool neighborHasRiver = neighbor.RiverDirections.Count > 0 || neighbor.Elevation < HexMetrics.SeaLevel;
                    if (neighborHasRiver)
                    {
                        float edgeNormX = edgeDx / edgeLen * halfWidth;
                        float edgeNormZ = edgeDz / edgeLen * halfWidth;
                        vertices.Add(new Vector3(c2.X - edgeNormX, highY, c2.Z - edgeNormZ));
                        vertices.Add(new Vector3(c2.X + edgeNormX, highY, c2.Z + edgeNormZ));
                        vertices.Add(new Vector3(c2.X + edgeNormX, lowY, c2.Z + edgeNormZ));
                        vertices.Add(new Vector3(c2.X - edgeNormX, lowY, c2.Z - edgeNormZ));
                        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                        uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                        indices.Add(vertexIndex); indices.Add(vertexIndex + 1); indices.Add(vertexIndex + 2);
                        indices.Add(vertexIndex); indices.Add(vertexIndex + 2); indices.Add(vertexIndex + 3);
                        vertexIndex += 4;
                    }
                }
            }

            // Connecting paths for through-flow
            if (incoming.Count > 0 && outgoing.Count > 0)
            {
                foreach (int inDir in incoming)
                {
                    foreach (int outDir in outgoing)
                    {
                        var inCorners = DirectionToCorners[inDir];
                        var outCorners = DirectionToCorners[outDir];
                        var path = FindCornerPath(inCorners.X, outCorners.X);

                        if (path.Count >= 2)
                        {
                            float y = cell.Elevation * HexMetrics.ElevationStep + HeightOffset;
                            for (int i = 0; i < path.Count - 1; i++)
                            {
                                var p1 = worldCorners[path[i]];
                                var p2 = worldCorners[path[i + 1]];
                                float segDx = p2.X - p1.X;
                                float segDz = p2.Z - p1.Z;
                                float segLen = Mathf.Sqrt(segDx * segDx + segDz * segDz);
                                if (segLen < 0.001f)
                                    continue;
                                float sPerpX = -segDz / segLen * halfWidth;
                                float sPerpZ = segDx / segLen * halfWidth;

                                vertices.Add(new Vector3(p1.X - sPerpX, y, p1.Z - sPerpZ));
                                vertices.Add(new Vector3(p1.X + sPerpX, y, p1.Z + sPerpZ));
                                vertices.Add(new Vector3(p2.X + sPerpX, y, p2.Z + sPerpZ));
                                vertices.Add(new Vector3(p2.X - sPerpX, y, p2.Z - sPerpZ));
                                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                                uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                                indices.Add(vertexIndex); indices.Add(vertexIndex + 1); indices.Add(vertexIndex + 2);
                                indices.Add(vertexIndex); indices.Add(vertexIndex + 2); indices.Add(vertexIndex + 3);
                                vertexIndex += 4;
                            }
                        }
                    }
                }
            }
        }

        if (vertices.Count == 0)
            return null;

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private List<int> FindCornerPath(int start, int end)
    {
        if (start == end)
            return new List<int> { start };

        var cw = new List<int> { start };
        int curr = start;
        while (curr != end && cw.Count <= 6)
        {
            curr = (curr + 1) % 6;
            cw.Add(curr);
        }

        var ccw = new List<int> { start };
        curr = start;
        while (curr != end && ccw.Count <= 6)
        {
            curr = (curr + 5) % 6;
            ccw.Add(curr);
        }

        return cw.Count <= ccw.Count ? cw : ccw;
    }

    private string GetEdgeKey(int q1, int r1, int q2, int r2)
    {
        if (q1 < q2 || (q1 == q2 && r1 < r2))
            return $"{q1},{r1}-{q2},{r2}";
        return $"{q2},{r2}-{q1},{r1}";
    }

    public void Update(Camera3D camera)
    {
        if (camera == null)
            return;

        var cameraPos = camera.GlobalPosition;
        var cameraXz = new Vector3(cameraPos.X, 0, cameraPos.Z);
        float maxDistSq = MaxRenderDistance * MaxRenderDistance;

        foreach (var chunk in _chunks.Values)
        {
            if (chunk.MeshInstance == null)
                continue;
            float dx = chunk.Center.X - cameraXz.X;
            float dz = chunk.Center.Z - cameraXz.Z;
            chunk.MeshInstance.Visible = (dx * dx + dz * dz) <= maxDistSq;
        }
    }

    public void UpdateAnimation(float delta)
    {
        _time += delta;
        _material?.SetShaderParameter("time", _time);
    }

    private ShaderMaterial CreateRiverMaterial()
    {
        var mat = new ShaderMaterial();
        mat.Shader = CreateRiverShader();
        mat.SetShaderParameter("river_color", new Color(0.176f, 0.545f, 0.788f));
        mat.SetShaderParameter("river_color_deep", new Color(0.102f, 0.361f, 0.557f));
        mat.SetShaderParameter("flow_speed", 1.5f);
        mat.SetShaderParameter("time", 0.0f);
        return mat;
    }

    private Shader CreateRiverShader()
    {
        var shader = new Shader();
        shader.Code = @"
shader_type spatial;
render_mode blend_mix, cull_disabled, depth_draw_opaque;

uniform vec3 river_color : source_color = vec3(0.176, 0.545, 0.788);
uniform vec3 river_color_deep : source_color = vec3(0.102, 0.361, 0.557);
uniform float flow_speed = 1.5;
uniform float time = 0.0;

varying vec2 world_uv;

float noise(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void vertex() {
    world_uv = UV;
}

void fragment() {
    vec2 flow_uv = world_uv;
    flow_uv.y -= time * flow_speed;
    float ripple = noise(flow_uv * 10.0 + time) + noise(flow_uv * 5.0 - time * 0.5) * 0.5;
    ripple = ripple * 0.15;
    float color_mix = sin(VERTEX.x * 0.5 + VERTEX.z * 0.3) * 0.5 + 0.5;
    vec3 base_color = mix(river_color, river_color_deep, color_mix * 0.3);
    vec3 color = base_color + vec3(ripple * 0.2);
    float edge_fade = smoothstep(0.0, 0.15, world_uv.x) * smoothstep(1.0, 0.85, world_uv.x);
    ALBEDO = color;
    ALPHA = 0.85 * edge_fade;
}
";
        return shader;
    }

    public new void Dispose()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.MeshInstance?.QueueFree();
        }
        _chunks.Clear();
    }
}
