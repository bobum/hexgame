shader_type spatial;
// Road shader ported from Catlike Coding Hex Map Tutorial 7
// Renders roads with UV-based alpha blending at edges

render_mode blend_mix, depth_draw_opaque, cull_disabled;

uniform vec4 road_color : source_color = vec4(0.6, 0.3, 0.1, 1.0);  // Brown/dirt color
uniform sampler2D noise_texture : hint_default_white;
uniform float noise_scale : hint_range(0.01, 0.1) = 0.025;

// Smoothstep edge parameters (from tutorial)
uniform float edge_start : hint_range(0.0, 1.0) = 0.4;
uniform float edge_end : hint_range(0.0, 1.0) = 0.7;

void fragment() {
    // Sample noise at world position (like Unity's worldPos.xz * 0.025)
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 noise = texture(noise_texture, world_pos.xz * noise_scale);

    // Color variation using noise.y (creates weathered dirt look)
    // Range: 25% to 100% of base color
    vec3 varied_color = road_color.rgb * (noise.y * 0.75 + 0.25);

    // Edge blending using UV.x (U coordinate)
    // UV.x = 1.0 at road center, 0.0 at edges
    float blend = UV.x;

    // Perturb edges with noise for natural ragged appearance
    blend *= noise.x + 0.5;

    // Smoothstep creates soft transition at edges
    blend = smoothstep(edge_start, edge_end, blend);

    ALBEDO = varied_color;
    ALPHA = blend;

    // Dirt road appearance
    ROUGHNESS = 0.8;
    METALLIC = 0.0;
}
