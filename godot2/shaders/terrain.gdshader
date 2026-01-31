shader_type spatial;

// Tutorial 14: Terrain Textures shader
// Implements splat mapping with texture array for hex terrain
// Ported from Catlike Coding Hex Map Tutorial 14
// Tutorial 15: Added grid overlay support
// Caribbean Theme: Updated colors and added roughness variation

// Terrain texture array containing all terrain types
// Index 0 = sand, 1 = grass, 2 = mud, 3 = stone, 4 = snow
uniform sampler2DArray terrain_textures : source_color, filter_linear_mipmap, repeat_enable;

// Normal map array for terrain depth/detail
uniform sampler2DArray terrain_normals : hint_normal, filter_linear_mipmap, repeat_enable;
uniform bool use_normals = false;
uniform float normal_strength : hint_range(0.0, 2.0) = 0.8;

// Tutorial 15: Grid overlay texture
uniform sampler2D grid_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform bool show_grid = false;

// Flag to use textures or fallback to colors
uniform bool use_textures = false;

// Caribbean color palette for each terrain type
uniform vec3 color_sand = vec3(0.96, 0.91, 0.76);   // Index 0 - Warm beach sand
uniform vec3 color_grass = vec3(0.35, 0.65, 0.30);  // Index 1 - Lush tropical green
uniform vec3 color_mud = vec3(0.45, 0.35, 0.25);    // Index 2 - Rich earth
uniform vec3 color_stone = vec3(0.55, 0.52, 0.48);  // Index 3 - Volcanic gray
uniform vec3 color_snow = vec3(0.95, 0.88, 0.82);   // Index 4 - Coral/white sand

// Per-terrain roughness (0 = shiny/wet, 1 = rough/matte)
uniform float roughness_sand = 0.85;   // Dry granular sand
uniform float roughness_grass = 0.70;  // Leafy vegetation
uniform float roughness_mud = 0.40;    // Wet = more reflective
uniform float roughness_stone = 0.65;  // Rocky surface
uniform float roughness_snow = 0.50;   // Smooth coral sand

// Scale for world-space UV coordinates (smaller = larger textures)
uniform float texture_scale : hint_range(0.001, 0.1) = 0.02;

// Terrain type indices from CUSTOM0 (set in HexMesh)
varying vec3 terrain_indices;
// World position for UV calculation (passed from vertex to fragment)
varying vec3 world_position;
// Tutorial 15: Cell-local UV for grid overlay (passed via UV)
varying vec2 grid_uv;

// Get fallback color for a terrain type index
vec3 get_terrain_color(float index) {
    int i = int(index + 0.5); // Round to nearest int
    if (i == 0) return color_sand;
    if (i == 1) return color_grass;
    if (i == 2) return color_mud;
    if (i == 3) return color_stone;
    return color_snow; // Index 4 or fallback
}

// Get roughness for a terrain type index
float get_terrain_roughness(float index) {
    int i = int(index + 0.5); // Round to nearest int
    if (i == 0) return roughness_sand;
    if (i == 1) return roughness_grass;
    if (i == 2) return roughness_mud;
    if (i == 3) return roughness_stone;
    return roughness_snow; // Index 4 or fallback
}

void vertex() {
    // Get terrain type indices from custom vertex attribute
    // CUSTOM0 contains (type1, type2, type3) as RGB floats
    terrain_indices = CUSTOM0.rgb;
    // Calculate world position for seamless UV mapping
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    // Tutorial 15: Pass UV coordinates for grid overlay
    grid_uv = UV;
}

// Debug mode: 0=normal, 1=show splat weights, 2=show terrain indices
uniform int debug_mode : hint_range(0, 2) = 0;

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

    // Tutorial 15: Apply grid overlay if enabled
    // Uses world-space UV like the tutorial (not mesh UVs)
    // HexMetrics: OuterRadius=10, InnerRadius=8.66
    if (show_grid) {
        vec2 gridUV;
        gridUV.x = world_position.x * (1.0 / (4.0 * 8.66025404));  // 4 * innerRadius
        gridUV.y = world_position.z * (1.0 / (2.0 * 10.0 * 1.5));  // 2 * outerRadius * 1.5 = 30
        vec4 grid = texture(grid_texture, gridUV);
        // Multiplicative blend like tutorial (darken where grid lines are)
        blended = blended * grid.rgb;
    }

    // Calculate blended roughness from terrain types
    float r1 = get_terrain_roughness(terrain_indices.x);
    float r2 = get_terrain_roughness(terrain_indices.y);
    float r3 = get_terrain_roughness(terrain_indices.z);
    float blended_roughness = r1 * splat.r + r2 * splat.g + r3 * splat.b;

    ALBEDO = blended;
    ROUGHNESS = blended_roughness;
    METALLIC = 0.0;

    // Apply normal maps if enabled
    if (use_normals) {
        vec2 world_uv = world_position.xz * texture_scale;

        // Sample normal maps for each terrain type
        vec3 n1 = texture(terrain_normals, vec3(world_uv, terrain_indices.x)).rgb * 2.0 - 1.0;
        vec3 n2 = texture(terrain_normals, vec3(world_uv, terrain_indices.y)).rgb * 2.0 - 1.0;
        vec3 n3 = texture(terrain_normals, vec3(world_uv, terrain_indices.z)).rgb * 2.0 - 1.0;

        // Blend normals using splat weights
        vec3 blended_normal = normalize(n1 * splat.r + n2 * splat.g + n3 * splat.b);

        // Apply normal strength (scale XY, keep Z normalized)
        blended_normal.xy *= normal_strength;
        blended_normal = normalize(blended_normal);

        // Convert back to 0-1 range for NORMAL_MAP
        NORMAL_MAP = blended_normal * 0.5 + 0.5;
    }
}
