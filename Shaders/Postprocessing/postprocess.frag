#version 330 core

uniform sampler2D frameBufferTexture;
uniform sampler2D depth;
uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D texNoise;

uniform bool showDepth = false;

uniform vec3 samples[128];
uniform mat4 projection;

uniform bool ssaoOnOff = true;
uniform float SSAOpower = 0.5;
uniform float radius = 0.8;
uniform int kernelSize = 16;
float bias = 0.025;

in vec2 UV;
out vec4 fragColor;

float near = 0.1; 
float far  = 100.0; 

float LinearizeDepth(float depth) 
{
    float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * near * far) / (far + near - z * (far - near));	
}

void main()
{
    vec3 color = texture(frameBufferTexture, UV).rgb;
    if (showDepth) color = vec3(LinearizeDepth(texture(depth, UV).r) / far);
    if (ssaoOnOff)
    {
        vec2 noiseScale = vec2(textureSize(gNormal, 0).x / 4, textureSize(gNormal, 0).y / 4);

        vec3 fragPos = texture(gPosition, UV).xyz;
        vec3 norm = normalize(texture(gNormal, UV).rgb);
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

            float sampleDepth = texture(gPosition, offset.xy).z;
            float rangeCheck = smoothstep(0.0, 1.0, radius / abs(fragPos.z - sampleDepth));
            occlusion += (sampleDepth >= samplePos.z + bias ? 1.0 : 0.0) * rangeCheck;
        }

        occlusion = 1.0 - (occlusion / kernelSize);
        occlusion = pow(occlusion, SSAOpower);
        fragColor = vec4(color, 1);
    }

    else fragColor = vec4(color, 1);
}