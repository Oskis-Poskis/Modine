#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormals;
layout(location = 2) in vec2 aUVs;
layout(location = 3) in vec3 aTangents;
layout(location = 4) in vec3 aBiTangents;

out vec2 UVs;
out vec3 normals;
out vec3 fragPos;
out vec3 fragPosViewSpace;
out vec4 fragPosLightSpace;
out mat3 TBN;

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

    vec3 T = normalize(vec3(aTangents * mat3(transpose(inverse(model)))));
    vec3 B = normalize(vec3(aBiTangents * mat3(transpose(inverse(model)))));
    vec3 N = normalize(normals);
    TBN = mat3(T, B, N);
}

