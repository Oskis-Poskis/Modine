#version 330 core
layout(location = 3) out vec3 fragColor;

uniform vec3 lightColor;

void main()
{
    fragColor = lightColor;
}