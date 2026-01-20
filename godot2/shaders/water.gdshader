shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_opaque;

// Water shader for open water bodies
// Ported from Catlike Coding Hex Map Tutorial 8
// Renders underwater cells with wave animation

uniform vec4 water_color : source_color = vec4(0.2, 0.6, 0.9, 0.8);
uniform float wave_speed : hint_range(0.0, 2.0) = 0.3;
uniform float wave_scale : hint_range(1.0, 50.0) = 15.0;

void fragment() {
    // Sample world position for consistent wave pattern
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Animate waves using world position
    float wave1 = sin(world_pos.x * wave_scale * 0.1 + TIME * wave_speed);
    float wave2 = sin(world_pos.z * wave_scale * 0.1 + TIME * wave_speed * 0.7);
    float pattern = (wave1 + wave2) * 0.05 + 0.95;

    ALBEDO = water_color.rgb * pattern;
    ALPHA = water_color.a;

    // Water surface properties
    ROUGHNESS = 0.2;
    METALLIC = 0.1;
}
