#version 330 core

uniform sampler2D frameBufferTexture;
uniform bool ACES = true;

vec3 ACESFilm(vec3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 5.0);
}

in vec2 UV;
layout(location = 0) out vec4 fragColor;

void main()
{
    vec3 color = texture(frameBufferTexture, UV).rgb;

    if (ACES) color = ACESFilm(color);
    fragColor = vec4(color, 1);
}