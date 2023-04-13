#version 430 core
layout(location = 0) in vec2 aPosition;

out vec2 texCoords;

void main()
{
    gl_Position = vec4(aPosition, 0.0, 1.0);
    texCoords = aPosition * 0.5 + 0.5;
}