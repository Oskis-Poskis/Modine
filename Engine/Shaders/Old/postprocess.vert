#version 330 core
layout(location = 0) in vec2 aPosition;

out vec2 UV;

void main(void)
{
    UV = aPosition * 0.5 + 0.5;
    gl_Position = vec4(aPosition, 0.0, 1.0);
}