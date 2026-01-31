shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_always;

// Water shore shader - Catlike Coding Hex Map Tutorial 8
// UV.y = shore distance: 0 at water, 1 at land

// Caribbean turquoise water
uniform vec4 water_color : source_color = vec4(0.15, 0.55, 0.75, 0.6);
uniform sampler2D noise_texture : hint_default_white, filter_linear_mipmap, repeat_enable;

// Hex metrics for tiling scale
const float TILING_SCALE = 0.00866;

// Foam function - direct port from Water.cginc
float Foam(float shore, vec2 worldXZ, sampler2D noiseTex) {
    shore = sqrt(shore) * 0.9;  // Square root spreads foam, 0.9 caps it

    vec2 noiseUV = worldXZ + TIME * 0.25;  // Animated UV
    vec4 noise = texture(noiseTex, noiseUV * (2.0 * TILING_SCALE));

    float distortion1 = noise.x * (1.0 - shore);  // Distortion fades toward shore
    float foam1 = sin((shore + distortion1) * 10.0 - TIME);  // Animated sine
    foam1 *= foam1;  // Square for sharp peaks (0 to 1 range, peaks at 1)

    float distortion2 = noise.y * (1.0 - shore);  // Second layer uses different channel
    float foam2 = sin((shore + distortion2) * 10.0 + TIME + 2.0);  // Different phase
    foam2 *= foam2 * 0.7;  // Slightly dimmer second layer

    return max(foam1, foam2) * shore;  // Take maximum, fade with shore distance
}

// Waves function - direct port from Water.cginc
float Waves(vec2 worldXZ, sampler2D noiseTex) {
    vec2 uv1 = worldXZ;
    uv1.y += TIME;
    vec4 noise1 = texture(noiseTex, uv1 * (3.0 * TILING_SCALE));

    vec2 uv2 = worldXZ;
    uv2.x += TIME;
    vec4 noise2 = texture(noiseTex, uv2 * (3.0 * TILING_SCALE));

    float blendWave = sin(
        (worldXZ.x + worldXZ.y) * 0.1 +
        (noise1.y + noise2.z) + TIME
    );
    blendWave *= blendWave;

    float waves = mix(noise1.z, noise1.w, blendWave) +
                  mix(noise2.x, noise2.y, blendWave);
    return smoothstep(0.75, 2.0, waves);
}

void fragment() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Shore value from UV.y (0 = deep water, 1 = shore edge)
    float shore = UV.y;

    // Calculate foam and waves
    float foam = Foam(shore, world_pos.xz, noise_texture);
    float waves = Waves(world_pos.xz, noise_texture);
    waves *= (1.0 - shore);  // Waves fade near shore

    // Combine: water base + whichever effect is stronger
    float waterEffect = max(foam, waves);
    vec4 c = clamp(water_color + vec4(vec3(waterEffect), 0.0), vec4(0.0), vec4(1.0));

    ALBEDO = c.rgb;
    ALPHA = c.a;
    ROUGHNESS = 0.2;
    METALLIC = 0.0;
}
