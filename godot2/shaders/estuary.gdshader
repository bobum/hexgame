shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_opaque;

// Estuary shader for river-water transitions
// Ported from Catlike Coding Hex Map Tutorial 8
// UV is used for shore blending (like water_shore)
// UV2 is used for river flow direction

uniform vec4 water_color : source_color = vec4(0.2, 0.6, 0.9, 0.8);
uniform vec4 foam_color : source_color = vec4(0.9, 0.95, 1.0, 0.9);
uniform float flow_speed : hint_range(0.0, 2.0) = 0.5;
uniform sampler2D noise_texture : hint_default_white;
uniform float noise_scale : hint_range(0.01, 0.1) = 0.025;

// Shore blending parameters
uniform float shore_start : hint_range(0.0, 1.0) = 0.6;
uniform float shore_end : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    // Sample world position for noise
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 noise = texture(noise_texture, world_pos.xz * noise_scale);

    // River flow animation based on UV2
    // UV2.x controls flow intensity, UV2.y is flow position along river
    vec2 flow_uv = UV2;
    flow_uv.y -= TIME * flow_speed;

    // River flow pattern
    float river_pattern = sin(flow_uv.y * 10.0) * 0.02;
    river_pattern = sin((flow_uv.x + river_pattern) * 20.0 + flow_uv.y * 5.0);
    river_pattern = river_pattern * 0.1 + 0.9;

    // Shore foam pattern (similar to water_shore)
    float foam_pattern = sin(world_pos.x * 10.0 + TIME * flow_speed * 0.5);
    foam_pattern += sin(world_pos.z * 8.0 + TIME * flow_speed * 0.35);
    foam_pattern = foam_pattern * 0.1 + noise.r * 0.3;

    // Shore blend based on UV.y (V coordinate)
    float shore_blend = UV.y;
    shore_blend += foam_pattern * 0.2;
    float foam_factor = smoothstep(shore_start, shore_end, shore_blend);

    // Blend river flow into the estuary
    // UV2.x indicates how much river influence (higher = more river-like)
    float river_influence = clamp(UV2.x * 2.0, 0.0, 1.0);

    // Combine river pattern with foam
    vec3 river_color = water_color.rgb * river_pattern;
    vec3 shore_color = mix(water_color.rgb, foam_color.rgb, foam_factor * 0.5);

    // Final color blends based on river influence and shore distance
    vec3 final_color = mix(shore_color, river_color, river_influence * (1.0 - foam_factor));

    // Alpha handling
    float alpha = mix(water_color.a, 0.0, smoothstep(shore_start, 1.0, shore_blend));

    ALBEDO = final_color;
    ALPHA = alpha;

    ROUGHNESS = 0.3;
    METALLIC = 0.1;
}
