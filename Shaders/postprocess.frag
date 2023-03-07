#version 330 core

uniform sampler2D pptexture;
uniform bool ACES = true;

in vec2 UV;
out vec4 fragColor;

vec3 ACESFilm(vec3 x) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

void main()
{
    vec3 color = texture(pptexture, UV).rgb;
    if (ACES) color = ACESFilm(color);

    fragColor = vec4(color, 1);
}