using HexGame.Core;

namespace HexGame.Rendering;

/// <summary>
/// Renders water surfaces with animated effects.
/// Creates a separate water plane for each water cell with wave animation.
/// </summary>
public partial class WaterRenderer : ChunkedRendererBase
{
    private readonly HexGrid _grid;
    private float _waveTime;
    private ShaderMaterial? _waterMaterial;

    /// <summary>
    /// Water surface color (shallow).
    /// </summary>
    public Color ShallowColor { get; set; } = new(0.2f, 0.5f, 0.7f, 0.8f);

    /// <summary>
    /// Water surface color (deep).
    /// </summary>
    public Color DeepColor { get; set; } = new(0.05f, 0.15f, 0.4f, 0.9f);

    /// <summary>
    /// Wave animation speed.
    /// </summary>
    public float WaveSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Wave amplitude (height variation).
    /// </summary>
    public float WaveAmplitude { get; set; } = 0.05f;

    /// <summary>
    /// Creates a new water renderer.
    /// </summary>
    /// <param name="grid">The hex grid to render.</param>
    public WaterRenderer(HexGrid grid)
    {
        _grid = grid;
        MaxRenderDistance = RenderingConfig.WaterRenderDistance;
    }

    /// <summary>
    /// Builds all water chunks.
    /// </summary>
    protected override void DoBuild()
    {
        _waterMaterial = CreateWaterMaterial();

        int chunksX = (_grid.Width + ChunkSize - 1) / ChunkSize;
        int chunksY = (_grid.Height + ChunkSize - 1) / ChunkSize;

        for (int cy = 0; cy < chunksY; cy++)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                var chunkPos = new Vector2I(cx, cy);
                var chunkNode = BuildChunk(chunkPos);

                // Only add chunk if it has water cells
                if (chunkNode.GetChildCount() > 0 || HasMesh(chunkNode))
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

        GD.Print($"WaterRenderer: Built {Chunks.Count} water chunks");
    }

    private static bool HasMesh(Node3D node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D)
            {
                return true;
            }
        }
        return false;
    }

    private Node3D BuildChunk(Vector2I chunkPos)
    {
        var chunkNode = new Node3D
        {
            Name = $"WaterChunk_{chunkPos.X}_{chunkPos.Y}"
        };

        int startQ = chunkPos.X * ChunkSize;
        int startR = chunkPos.Y * ChunkSize;
        int endQ = Math.Min(startQ + ChunkSize, _grid.Width);
        int endR = Math.Min(startR + ChunkSize, _grid.Height);

        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        bool hasWater = false;

        for (int r = startR; r < endR; r++)
        {
            for (int q = startQ; q < endQ; q++)
            {
                var cell = _grid.GetCell(q, r);
                if (cell != null && HexMetrics.IsWaterElevation(cell.Elevation))
                {
                    AddWaterCell(surfaceTool, cell);
                    hasWater = true;
                }
            }
        }

        if (hasWater)
        {
            surfaceTool.GenerateNormals();
            var mesh = surfaceTool.Commit();

            if (mesh != null)
            {
                var meshInstance = new MeshInstance3D
                {
                    Mesh = mesh,
                    MaterialOverride = _waterMaterial
                };
                chunkNode.AddChild(meshInstance);
            }
        }

        return chunkNode;
    }

    private void AddWaterCell(SurfaceTool st, HexCell cell)
    {
        // Water surface is always at sea level
        float waterHeight = HexMetrics.SeaLevel * HexMetrics.ElevationStep;
        var center = cell.Coordinates.ToWorldPosition(waterHeight);
        var corners = HexMetrics.GetCorners();

        // Calculate depth-based color (deeper = darker)
        float depth = HexMetrics.SeaLevel - cell.Elevation;
        float depthFactor = Math.Clamp(depth / (float)HexMetrics.SeaLevel, 0f, 1f);
        var color = ShallowColor.Lerp(DeepColor, depthFactor);

        // Build hex surface
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

    /// <summary>
    /// Updates wave animation.
    /// </summary>
    public override void UpdateAnimation(double delta)
    {
        _waveTime += (float)delta * WaveSpeed;

        if (_waterMaterial != null)
        {
            _waterMaterial.SetShaderParameter("wave_time", _waveTime);
        }
    }

    private ShaderMaterial CreateWaterMaterial()
    {
        var shader = new Shader();
        shader.Code = WaterShaderCode;

        var material = new ShaderMaterial
        {
            Shader = shader
        };

        material.SetShaderParameter("shallow_color", ShallowColor);
        material.SetShaderParameter("deep_color", DeepColor);
        material.SetShaderParameter("wave_amplitude", WaveAmplitude);
        material.SetShaderParameter("wave_time", 0.0f);

        return material;
    }

    private const string WaterShaderCode = @"
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back;

uniform vec4 shallow_color : source_color = vec4(0.2, 0.5, 0.7, 0.8);
uniform vec4 deep_color : source_color = vec4(0.05, 0.15, 0.4, 0.9);
uniform float wave_amplitude = 0.05;
uniform float wave_time = 0.0;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Simple wave animation
    float wave1 = sin(world_pos.x * 2.0 + wave_time * 2.0) * wave_amplitude;
    float wave2 = sin(world_pos.z * 3.0 + wave_time * 1.5) * wave_amplitude * 0.5;
    VERTEX.y += wave1 + wave2;
}

void fragment() {
    // Use vertex color for depth-based coloring
    ALBEDO = COLOR.rgb;
    ALPHA = COLOR.a;

    // Add slight specular for water shine
    SPECULAR = 0.5;
    ROUGHNESS = 0.1;
    METALLIC = 0.0;
}
";

    public override void Cleanup()
    {
        _waterMaterial?.Dispose();
        _waterMaterial = null;
        base.Cleanup();
    }
}
