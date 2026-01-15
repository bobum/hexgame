shader_type spatial;
render_mode cull_back;

// Adjustable uniforms for real-time tweaking
uniform float top_noise_strength : hint_range(0.0, 0.5) = 0.35;
uniform float top_noise_scale : hint_range(0.5, 10.0) = 3.0;
uniform float wall_darkening : hint_range(0.0, 0.8) = 0.55;
uniform float roughness_value : hint_range(0.0, 1.0) = 0.95;
uniform float saturation_boost : hint_range(0.0, 0.5) = 0.15;

varying vec3 world_position;
varying vec3 world_normal;

// Simple hash function
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Value noise
float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Fractal Brownian Motion - multiple octaves for richer texture
float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	float frequency = 1.0;
	for (int i = 0; i < 4; i++) {
		value += amplitude * noise(p * frequency);
		amplitude *= 0.5;
		frequency *= 2.0;
	}
	return value;
}

// Saturation adjustment
vec3 adjust_saturation(vec3 color, float amount) {
	float luminance = dot(color, vec3(0.299, 0.587, 0.114));
	return mix(vec3(luminance), color, 1.0 + amount);
}

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 normal = normalize(world_normal);
	vec3 color = COLOR.rgb;

	// Boost saturation for more vibrant colors
	if (saturation_boost > 0.001) {
		color = adjust_saturation(color, saturation_boost);
	}

	// Apply noise ONLY to hex tops (horizontal surfaces)
	// Walls/terraces stay clean for crisp edges
	if (abs(normal.y) > 0.7 && top_noise_strength > 0.001) {
		// Hex top - apply FBM noise using top-down XZ projection
		float noiseVal = fbm(world_position.xz * top_noise_scale);
		color = color * (1.0 + (noiseVal - 0.5) * top_noise_strength);
	}

	// Wall darkening based on normal (makes terraces more visible)
	if (wall_darkening > 0.001) {
		float wall_factor = 1.0 - abs(normal.y);
		color = color * (1.0 - wall_factor * wall_darkening);
	}

	ALBEDO = color;
	ROUGHNESS = roughness_value;
	METALLIC = 0.0;
}
