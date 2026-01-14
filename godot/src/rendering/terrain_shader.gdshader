shader_type spatial;
render_mode cull_disabled, vertex_lighting;

// Depth bias to prevent z-fighting
uniform float depth_bias : hint_range(-0.01, 0.01) = 0.0005;

void vertex() {
	// Apply small depth bias by pushing vertex slightly toward camera
	// This prevents z-fighting on coplanar surfaces
	vec3 view_dir = normalize(VERTEX);
	VERTEX -= view_dir * depth_bias;
}

void fragment() {
	ALBEDO = COLOR.rgb;
}
