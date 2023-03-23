#version 330 core

uniform sampler2D frameBufferTexture;
uniform sampler2D depth;
uniform sampler2D gNormal;
uniform sampler2D texNoise;
uniform sampler2D gPosition;

uniform vec3 samples[128];
uniform mat4 projection;
uniform mat4 projMatrixInv;
uniform mat4 viewMatrix;
uniform bool ACES = true;

uniform bool ssaoOnOff = true;
uniform float SSAOpower = 0.5;
uniform float radius = 0.8;
uniform int kernelSize = 16;
float bias = 0.025;

float near = 0.5;
float far = 100;

vec4 ViewPosFromDepth(float depth, vec2 uvs)
{
    float z = depth * 2.0 - 1.0;
    vec4 clipSpacePosition = vec4(uvs * 2.0 - 1.0, z, 1.0);
    vec4 viewSpacePosition = clipSpacePosition * projMatrixInv;
    viewSpacePosition /= viewSpacePosition.w;

    return viewSpacePosition;
}

vec3 WorldPos(float depth, vec2 uvs)
{
    vec4 viewSpacePosition = ViewPosFromDepth(depth, uvs);
    vec4 worldSpacePosition = viewSpacePosition * inverse(viewMatrix);

    return worldSpacePosition.xyz;
}

vec3 ACESFilm(vec3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 5.0);
}

in vec2 UV;
layout(location = 0) out vec4 fragColor;
layout(location = 5) out vec3 outAO;

void main()
{
    vec3 color = texture(frameBufferTexture, UV).rgb;
    float _depth = texture(depth, UV).r;

    if (ACES) color = ACESFilm(color);
    if (ssaoOnOff)
    {
        vec2 noiseScale = vec2(textureSize(gPosition, 0).x / 4, textureSize(gPosition, 0).y / 4);

        vec3 _fragPos = ViewPosFromDepth(_depth, UV).xyz;
        vec3 fragPos = texture(gPosition, UV).rgb;
        vec3 test = normalize(texture(gNormal, UV).rgb);
        vec3 norm = normalize(texture(gNormal, UV).rgb * mat3(inverse(transpose(viewMatrix))));
        vec3 randomVec = normalize(texture(texNoise, UV * noiseScale).xyz);
        
        vec3 tangent = normalize(randomVec - norm * dot(randomVec, norm));
        vec3 bitangent = cross(norm, tangent);
        mat3 TBN = mat3(tangent, bitangent, norm);

        float occlusion = 0.0;
        for (int i = 0; i < kernelSize; i++)
        {
            vec3 samplePos = TBN * samples[i];
            samplePos = fragPos + samplePos * radius;

            vec4 offset = vec4(samplePos, 1.0);
            offset = offset * projection;
            offset.xyz /= offset.w;
            offset.xyz = offset.xyz * 0.5 + 0.5;

            vec3 occluderPos = texture(gPosition, offset.xy).xyz;
            float rangeCheck = smoothstep(0.0, 1.0, radius / length(fragPos - occluderPos));
            occlusion += (occluderPos.z >= samplePos.z + bias ? 1.0 : 0.0) * rangeCheck;
        }

        occlusion = 1.0 - (occlusion / kernelSize);
        occlusion = pow(occlusion, SSAOpower);
        outAO = vec3(occlusion);
    }
    else outAO = vec3(1);
    
    fragColor = vec4(color, 1);
}