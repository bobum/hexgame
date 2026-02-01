shader_type spatial;
render_mode cull_disabled;

// Wind shader for vegetation (trees, bushes, plants)
// Applies vertex displacement based on height for natural swaying

// Texture inputs
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform sampler2D normal_texture : hint_normal, filter_linear_mipmap;
uniform bool use_normal_map = false;

// Wind parameters
uniform float wind_strength : hint_range(0.0, 1.0) = 0.15;
uniform float wind_speed : hint_range(0.0, 5.0) = 1.5;
uniform float wind_turbulence : hint_range(0.0, 2.0) = 0.5;
uniform vec2 wind_direction = vec2(1.0, 0.5);

// Material parameters
uniform float alpha_scissor : hint_range(0.0, 1.0) = 0.5;
uniform float roughness : hint_range(0.0, 1.0) = 0.8;

// Height-based sway control
uniform float sway_height_start : hint_range(0.0, 10.0) = 0.5;
uniform float sway_height_factor : hint_range(0.0, 5.0) = 1.0;

varying vec3 world_vertex;

void vertex() {
    // Get world position for consistent wind across instances
    world_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Calculate height factor (more sway at top of plant)
    float height = max(0.0, VERTEX.y - sway_height_start);
    float height_factor = pow(height * sway_height_factor, 1.5);

    // Main wind wave
    float time = TIME * wind_speed;
    vec2 wind_dir = normalize(wind_direction);
    float wind_dot = dot(world_vertex.xz, wind_dir);
    float main_wave = sin(time + wind_dot * 0.5) * wind_strength;

    // Secondary turbulence for natural variation
    float turb1 = sin(time * 1.3 + world_vertex.x * 2.0) * wind_turbulence * 0.3;
    float turb2 = cos(time * 0.7 + world_vertex.z * 1.5) * wind_turbulence * 0.2;

    // Combine waves
    float total_sway = (main_wave + turb1 + turb2) * height_factor;

    // Apply displacement in wind direction (XZ plane)
    VERTEX.x += total_sway * wind_dir.x;
    VERTEX.z += total_sway * wind_dir.y;

    // Subtle vertical movement
    VERTEX.y += abs(total_sway) * 0.1;
}

void fragment() {
    vec4 albedo = texture(albedo_texture, UV);

    // Alpha cutoff for leaves
    if (albedo.a < alpha_scissor) {
        discard;
    }

    ALBEDO = albedo.rgb;
    ALPHA = albedo.a;
    ALPHA_SCISSOR_THRESHOLD = alpha_scissor;

    // Normal mapping
    if (use_normal_map) {
        NORMAL_MAP = texture(normal_texture, UV).rgb;
    }

    ROUGHNESS = roughness;
    METALLIC = 0.0;
}
