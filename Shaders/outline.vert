#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormals;

uniform mat4 view;
uniform mat4 model;

out vec3 Normal;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * view;
    Normal =  aNormals *  mat3(transpose(inverse(model)));
}