#version 330 core

in vec3 normals;
in vec3 position;
out vec4 fragColor;

uniform bool smoothShading;

void main()
{
    vec3 normal;
    if (smoothShading) normal = normals;
    else normal = normal = normalize(cross(dFdx(position), dFdy(position)));

    vec3 norm = normalize(normal);
    vec3 lightDir = normalize(vec3(1, -1, 1));  
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = vec3(diff) + 0.05;

    fragColor = vec4(diffuse, 1);
}