shader_type spatial;
render_mode cull_back;

// Adjustable uniforms for real-time tweaking
uniform float top_noise_strength : hint_range(0.0, 0.5) = 0.35;
uniform float top_noise_scale : hint_range(0.5, 10.0) = 3.0;
uniform float wall_darkening : hint_range(0.0, 0.8) = 0.55;
uniform float roughness_value : hint_range(0.0, 1.0) = 0.95;
uniform float saturation_boost : hint_range(0.0, 0.5) = 0.15;

// Advanced shader uniforms
uniform float triplanar_sharpness : hint_range(1.0, 10.0) = 4.0;
uniform float fresnel_strength : hint_range(0.0, 0.5) = 0.15;
uniform float specular_strength : hint_range(0.0, 1.0) = 0.3;

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

// Triplanar projection weights - avoids texture stretching on walls
vec3 triplanar_weights(vec3 normal) {
	vec3 w = abs(normal);
	w = pow(w, vec3(triplanar_sharpness));
	return w / (w.x + w.y + w.z);
}

// Triplanar noise sampling - projects noise from all 3 axes
vec3 triplanar_sample(vec3 world_pos, vec3 normal, vec3 base_color, float noise_scale, float noise_strength) {
	vec3 weights = triplanar_weights(normal);

	// Sample noise from each axis projection
	float noise_xy = fbm(world_pos.xy * noise_scale);
	float noise_xz = fbm(world_pos.xz * noise_scale);
	float noise_yz = fbm(world_pos.yz * noise_scale);

	// Blend based on normal direction
	float blended_noise = noise_yz * weights.x + noise_xz * weights.y + noise_xy * weights.z;

	// Apply noise to vary the base color
	return base_color * (1.0 + (blended_noise - 0.5) * noise_strength);
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
	vec3 view_dir = normalize(VIEW);

	// Boost saturation for more vibrant colors
	if (saturation_boost > 0.001) {
		color = adjust_saturation(color, saturation_boost);
	}

	// Apply noise based on surface orientation
	if (abs(normal.y) > 0.7) {
		// Hex tops - use top-down projection
		if (top_noise_strength > 0.001) {
			float noiseVal = fbm(world_position.xz * top_noise_scale);
			color = color * (1.0 + (noiseVal - 0.5) * top_noise_strength);
		}
	} else {
		// Walls/cliffs - use triplanar projection for seamless texturing
		if (top_noise_strength > 0.001) {
			color = triplanar_sample(world_position, normal, color, top_noise_scale, top_noise_strength * 0.7);
		}
		// Wall darkening based on normal (makes terraces more visible)
		if (wall_darkening > 0.001) {
			float wall_factor = 1.0 - abs(normal.y);
			color = color * (1.0 - wall_factor * wall_darkening);
		}
	}

	// Fresnel rim lighting - adds subtle edge glow
	if (fresnel_strength > 0.001) {
		float fresnel = pow(1.0 - max(dot(normal, view_dir), 0.0), 3.0);
		color = color + fresnel * fresnel_strength;
	}

	ALBEDO = color;
	ROUGHNESS = roughness_value;
	SPECULAR = specular_strength;
	METALLIC = 0.0;
}
