#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormals;

out vec3 normals;
out vec3 position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main(void)
{
    normals = aNormals;
    position = aPosition;
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
}