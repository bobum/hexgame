shader_type spatial;
// DEBUG: Test with simple solid red, no alpha
render_mode unshaded, cull_disabled;

void fragment() {
    // Bright red for maximum visibility
    ALBEDO = vec3(1.0, 0.0, 0.0);
}
