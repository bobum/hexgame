shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_opaque;

// River water shader for hex map
// Ported from Catlike Coding Hex Map Tutorial 6
// Uses UV coordinates for flow animation

uniform vec4 water_color : source_color = vec4(0.2, 0.5, 0.8, 0.7);
uniform float flow_speed : hint_range(0.0, 2.0) = 0.5;

void fragment() {
    // Animate UV in V direction for water flow effect
    vec2 animated_uv = UV;
    animated_uv.y -= TIME * flow_speed;

    // Simple water pattern using animated UVs
    // Creates subtle wave effect
    float wave = sin(animated_uv.y * 10.0) * 0.02;
    float pattern = sin((animated_uv.x + wave) * 20.0 + animated_uv.y * 5.0);
    pattern = pattern * 0.1 + 0.9;

    ALBEDO = water_color.rgb * pattern;
    ALPHA = water_color.a;

    // Add some specular for water shine
    ROUGHNESS = 0.3;
    METALLIC = 0.1;
}
