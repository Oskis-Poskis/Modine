#version 330 core

uniform sampler2D pptexture;

in vec2 UV;
out vec4 fragColor;

void main()
{
    vec3 color = texture(pptexture, UV).rgb;
    fragColor = vec4(color, 1);
}