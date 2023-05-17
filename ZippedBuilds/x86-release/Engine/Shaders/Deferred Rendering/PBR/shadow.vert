#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormals;

uniform mat4 model;
uniform mat4 lightSpaceMatrix;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * lightSpaceMatrix;
}