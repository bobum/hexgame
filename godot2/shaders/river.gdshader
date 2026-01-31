shader_type spatial;
render_mode cull_disabled, blend_mix, depth_draw_always;

// River shader - Catlike Coding Hex Map Tutorial 6
// Uses UV for flow animation

// Caribbean river water - slightly more green than ocean
uniform vec4 water_color : source_color = vec4(0.12, 0.50, 0.65, 0.55);
uniform sampler2D noise_texture : hint_default_white, filter_linear_mipmap, repeat_enable;

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
    float river = River(UV, noise_texture);

    // Additive blending: base color + river brightness
    vec4 c = clamp(water_color + vec4(vec3(river), 0.0), vec4(0.0), vec4(1.0));

    ALBEDO = c.rgb;
    ALPHA = c.a;
    ROUGHNESS = 0.2;
    METALLIC = 0.0;
}
