shader_type spatial;
render_mode cull_back;

// Adjustable uniforms for real-time tweaking
uniform float noise_strength : hint_range(0.0, 0.5) = 0.0;
uniform float noise_scale : hint_range(0.5, 10.0) = 2.0;
uniform float wall_darkening : hint_range(0.0, 0.8) = 0.0;
uniform float roughness_value : hint_range(0.0, 1.0) = 0.9;

varying vec3 world_position;
varying vec3 world_normal;

// Simple noise function
float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

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

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 normal = normalize(world_normal);
	vec3 color = COLOR.rgb;

	// Optional noise variation
	if (noise_strength > 0.001) {
		float n = noise(world_position.xz * noise_scale);
		color = color * (1.0 + (n - 0.5) * noise_strength);
	}

	// Optional wall darkening based on normal
	if (wall_darkening > 0.001) {
		float wall_factor = 1.0 - abs(normal.y);
		color = color * (1.0 - wall_factor * wall_darkening);
	}

	ALBEDO = color;
	ROUGHNESS = roughness_value;
	METALLIC = 0.0;
}
