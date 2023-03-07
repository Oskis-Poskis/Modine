#version 330 core

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in vec3 Normal[];

uniform float lineWidth = 0.02;
uniform mat4 projection;

void main()
{
    for (int i = 0; i < gl_in.length(); i++)
    {
        gl_Position = (gl_in[0].gl_Position + vec4(Normal[0] * lineWidth, 1)) * projection;
        EmitVertex();
        gl_Position = (gl_in[1].gl_Position + vec4(Normal[1] * lineWidth, 1)) * projection;
        EmitVertex();
        gl_Position = (gl_in[2].gl_Position + vec4(Normal[2] * lineWidth, 1)) * projection;
        EmitVertex();
    }

    EndPrimitive();
}