#version 330 core
layout(location = 0) in vec3 aPosition;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 UV;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    UV = aPosition.xy * 0.5 + 0.5;
}