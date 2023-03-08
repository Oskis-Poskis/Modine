#version 330 core
out vec4 fragColor;

uniform vec3 lightColor = vec3(1, 1, 0);

void main()
{
    fragColor = vec4(lightColor, 1);
}