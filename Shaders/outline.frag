#version 330 core

in vec3 normals;
in vec3 fragPos;
in vec4 fragPosLightSpace;

out vec4 fragColor;

void main()
{
    fragColor = vec4(1);
}