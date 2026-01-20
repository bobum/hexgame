shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_opaque;

// Water shore shader for shore foam effects
// Ported from Catlike Coding Hex Map Tutorial 8
// UV.y determines shore distance: V=0 at water, V=1 at land

uniform vec4 water_color : source_color = vec4(0.2, 0.6, 0.9, 0.8);
uniform vec4 foam_color : source_color = vec4(0.9, 0.95, 1.0, 0.9);
uniform float wave_speed : hint_range(0.0, 2.0) = 0.5;
uniform sampler2D noise_texture : hint_default_white;
uniform float noise_scale : hint_range(0.01, 0.1) = 0.025;

// Shore blending parameters
uniform float shore_start : hint_range(0.0, 1.0) = 0.6;
uniform float shore_end : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    // Sample world position for noise
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 noise = texture(noise_texture, world_pos.xz * noise_scale);

    // Animated foam pattern
    float foam_pattern = sin(world_pos.x * 10.0 + TIME * wave_speed);
    foam_pattern += sin(world_pos.z * 8.0 + TIME * wave_speed * 0.7);
    foam_pattern = foam_pattern * 0.1 + noise.r * 0.3;

    // Shore blend based on UV.y (V coordinate)
    // V = 0 at water edge, V = 1 at land
    float shore_blend = UV.y;

    // Add noise perturbation to shore edge
    shore_blend += foam_pattern * 0.2;

    // Smoothstep creates soft foam transition
    float foam_factor = smoothstep(shore_start, shore_end, shore_blend);

    // Blend between water and foam colors
    vec3 final_color = mix(water_color.rgb, foam_color.rgb, foam_factor * 0.5);

    // Alpha fades out toward land
    float alpha = mix(water_color.a, 0.0, smoothstep(shore_start, 1.0, shore_blend));

    ALBEDO = final_color;
    ALPHA = alpha;

    ROUGHNESS = 0.3;
    METALLIC = 0.0;
}
