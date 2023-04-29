#version 330 core
layout(location = 0) out vec4 fragColor;

uniform vec3 lightColor;
uniform sampler2D pltex;

in vec2 UV;

void main()
{
    float alpha = texture(pltex, UV).a;
    vec3 col = lightColor;

    fragColor = vec4(col, 1.0);
}