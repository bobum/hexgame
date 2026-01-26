shader_type spatial;

// Tutorial 16: Cell highlight shader
// Renders cell outlines above all terrain using depth_test_disabled
// Equivalent to Unity's ZTest Always in the original tutorial
// Uses blend_mix for proper alpha transparency

render_mode unshaded, depth_test_disabled, cull_disabled, blend_mix;

uniform sampler2D albedo_texture : source_color;
uniform vec4 highlight_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec4 tex = texture(albedo_texture, UV);
    ALBEDO = tex.rgb * highlight_color.rgb;
    ALPHA = tex.a * highlight_color.a;
}
