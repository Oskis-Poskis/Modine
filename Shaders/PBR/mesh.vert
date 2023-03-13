#version 330 core
in vec3 aPosition;
in vec3 aNormals;
in vec2 aUVs;

out vec2 UVs;

out vec3 fragPosViewSpace;
out vec3 normalsViewSpace;

out vec3 normals;
out vec3 fragPos;
out vec4 fragPosLightSpace;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 lightSpaceMatrix;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;

    UVs = aUVs;
    normals = aNormals * mat3(transpose(inverse(model)));
    fragPos = vec3(vec4(aPosition, 1.0) * model);
    fragPosLightSpace = vec4(fragPos, 1.0) * lightSpaceMatrix;

    fragPosViewSpace = (vec4(aPosition, 1.0) * model * view).xyz;
    normalsViewSpace = aNormals * mat3(transpose(inverse(model * view)));
}