shader_type spatial;
render_mode cull_disabled;

// Vertex color shader for hex cells
// Ported from Catlike Coding Hex Map Tutorial 1

void fragment() {
    ALBEDO = COLOR.rgb;
}
