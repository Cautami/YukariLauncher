shader_type canvas_item;

uniform float fade_start : hint_range(0.0, 1.0) = 0.6;
uniform float fade_end : hint_range(0.0, 1.0) = 1.0;
uniform sampler2D next_texture : source_color;
uniform float blend : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 current = texture(TEXTURE, UV);
    vec4 next = texture(next_texture, UV);
    vec4 blended = mix(current, next, blend);
    float alpha = smoothstep(fade_end, fade_start, UV.y);
    COLOR = blended * COLOR.a;
    COLOR.a *= alpha;
}