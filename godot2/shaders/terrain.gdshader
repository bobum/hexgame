shader_type spatial;

// Tutorial 14: Terrain Textures shader
// Implements splat mapping with texture array for hex terrain
// Ported from Catlike Coding Hex Map Tutorial 14

// Terrain texture array containing all terrain types
// Index 0 = sand, 1 = grass, 2 = mud, 3 = stone, 4 = snow
uniform sampler2DArray terrain_textures : source_color, filter_linear_mipmap, repeat_enable;

// Flag to use textures or fallback to colors
uniform bool use_textures = false;

// Fallback colors for each terrain type (when textures not available)
uniform vec3 color_sand = vec3(0.93, 0.87, 0.73);   // Index 0
uniform vec3 color_grass = vec3(0.4, 0.7, 0.3);    // Index 1
uniform vec3 color_mud = vec3(0.5, 0.4, 0.3);      // Index 2
uniform vec3 color_stone = vec3(0.6, 0.6, 0.6);    // Index 3
uniform vec3 color_snow = vec3(0.95, 0.95, 0.95);  // Index 4

// Scale for world-space UV coordinates (smaller = larger textures)
uniform float texture_scale : hint_range(0.001, 0.1) = 0.02;

// Terrain type indices from CUSTOM0 (set in HexMesh)
varying vec3 terrain_indices;
// World position for UV calculation (passed from vertex to fragment)
varying vec3 world_position;

// Get fallback color for a terrain type index
vec3 get_terrain_color(float index) {
    int i = int(index + 0.5); // Round to nearest int
    if (i == 0) return color_sand;
    if (i == 1) return color_grass;
    if (i == 2) return color_mud;
    if (i == 3) return color_stone;
    return color_snow; // Index 4 or fallback
}

void vertex() {
    // Get terrain type indices from custom vertex attribute
    // CUSTOM0 contains (type1, type2, type3) as RGB floats
    terrain_indices = CUSTOM0.rgb;
    // Calculate world position for seamless UV mapping
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

// Debug mode: 0=normal, 1=show splat weights, 2=show terrain indices
uniform int debug_mode : hint_range(0, 2) = 1;

void fragment() {
    // Get splat weights from vertex color
    vec3 splat = COLOR.rgb;

    vec3 blended;

    if (debug_mode == 1) {
        // DEBUG: Shows splat weights as RGB (red=weight1, green=weight2, blue=weight3)
        blended = splat;
    } else if (debug_mode == 2) {
        // DEBUG: Shows terrain indices as grayscale (0=black, 5=white)
        blended = terrain_indices / 5.0;
    } else if (use_textures) {
        // Use world-space UV coordinates to avoid seams at cell boundaries
        vec2 world_uv = world_position.xz * texture_scale;

        // Sample all three terrain textures based on indices
        vec4 tex1 = texture(terrain_textures, vec3(world_uv, terrain_indices.x));
        vec4 tex2 = texture(terrain_textures, vec3(world_uv, terrain_indices.y));
        vec4 tex3 = texture(terrain_textures, vec3(world_uv, terrain_indices.z));

        // Weighted blend of all three textures
        blended = tex1.rgb * splat.r + tex2.rgb * splat.g + tex3.rgb * splat.b;
    } else {
        // Fallback: use solid colors based on terrain type indices
        vec3 col1 = get_terrain_color(terrain_indices.x);
        vec3 col2 = get_terrain_color(terrain_indices.y);
        vec3 col3 = get_terrain_color(terrain_indices.z);

        // Weighted blend of colors
        blended = col1 * splat.r + col2 * splat.g + col3 * splat.b;
    }

    ALBEDO = blended;
}
