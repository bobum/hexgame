shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_always;

// Open water shader - Catlike Coding Hex Map Tutorial 8
// For underwater cells showing wave animation

uniform vec4 water_color : source_color = vec4(0.2, 0.5, 0.8, 0.5);
uniform sampler2D noise_texture : hint_default_white, filter_linear_mipmap, repeat_enable;

// Hex metrics for tiling scale (matches chunk boundaries)
const float TILING_SCALE = 0.00866;

// Waves function - direct port from Water.cginc
float Waves(vec2 worldXZ, sampler2D noiseTex) {
    // Two noise samples moving in perpendicular directions
    vec2 uv1 = worldXZ;
    uv1.y += TIME;
    vec4 noise1 = texture(noiseTex, uv1 * (3.0 * TILING_SCALE));

    vec2 uv2 = worldXZ;
    uv2.x += TIME;
    vec4 noise2 = texture(noiseTex, uv2 * (3.0 * TILING_SCALE));

    // Blend factor oscillates based on position and noise
    float blendWave = sin(
        (worldXZ.x + worldXZ.y) * 0.1 +
        (noise1.y + noise2.z) + TIME
    );
    blendWave *= blendWave;  // Squared for sharper transitions

    // Cross-blend different noise channels
    float waves = mix(noise1.z, noise1.w, blendWave) +
                  mix(noise2.x, noise2.y, blendWave);

    // Remap: values below 0.75 -> 0, up to 2 -> 1
    return smoothstep(0.75, 2.0, waves);
}

void fragment() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    float waves = Waves(world_pos.xz, noise_texture);

    // Additive blending: base color + wave brightness
    vec4 c = clamp(water_color + vec4(vec3(waves), 0.0), vec4(0.0), vec4(1.0));

    ALBEDO = c.rgb;
    ALPHA = c.a;
    ROUGHNESS = 0.2;
    METALLIC = 0.0;
}
