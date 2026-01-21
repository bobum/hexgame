shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_always;

// Estuary shader - river-water transitions
// Catlike Coding Hex Map Tutorial 8
// UV.x = river influence (0 = shore, 1 = river)
// UV.y = shore distance (0 = water, 1 = land)
// UV2 = river flow UVs

uniform vec4 water_color : source_color = vec4(0.2, 0.5, 0.8, 0.5);
uniform sampler2D noise_texture : hint_default_white, filter_linear_mipmap, repeat_enable;

// Hex metrics for tiling scale
const float TILING_SCALE = 0.00866;

// Foam function - direct port from Water.cginc
float Foam(float shore, vec2 worldXZ, sampler2D noiseTex) {
    shore = sqrt(shore) * 0.9;

    vec2 noiseUV = worldXZ + TIME * 0.25;
    vec4 noise = texture(noiseTex, noiseUV * (2.0 * TILING_SCALE));

    float distortion1 = noise.x * (1.0 - shore);
    float foam1 = sin((shore + distortion1) * 10.0 - TIME);
    foam1 *= foam1;

    float distortion2 = noise.y * (1.0 - shore);
    float foam2 = sin((shore + distortion2) * 10.0 + TIME + 2.0);
    foam2 *= foam2 * 0.7;

    return max(foam1, foam2) * shore;
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

// River function - direct port from Water.cginc
float River(vec2 riverUV, sampler2D noiseTex) {
    vec2 uv = riverUV;
    uv.x = uv.x * 0.0625 + TIME * 0.005;  // Very slow horizontal drift
    uv.y -= TIME * 0.25;  // Flows downstream
    vec4 noise = texture(noiseTex, uv);

    vec2 uv2 = riverUV;
    uv2.x = uv2.x * 0.0625 - TIME * 0.0052;  // Counter-drift
    uv2.y -= TIME * 0.23;  // Slightly different speed
    vec4 noise2 = texture(noiseTex, uv2);

    return noise.r * noise2.a;  // Multiply creates dark/light interference
}

void fragment() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    float shore = UV.y;
    float foam = Foam(shore, world_pos.xz, noise_texture);
    float waves = Waves(world_pos.xz, noise_texture);
    waves *= (1.0 - shore);  // Waves fade near shore
    float shoreWater = max(foam, waves);

    float river = River(UV2, noise_texture);

    // Blend between shore effects and river based on UV.x
    float water = mix(shoreWater, river, UV.x);

    vec4 c = clamp(water_color + vec4(vec3(water), 0.0), vec4(0.0), vec4(1.0));

    ALBEDO = c.rgb;
    ALPHA = c.a;
    ROUGHNESS = 0.2;
    METALLIC = 0.0;
}
