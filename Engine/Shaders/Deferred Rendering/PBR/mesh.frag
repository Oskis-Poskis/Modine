#version 430

layout(location = 0) out vec4 mainTexture;
layout(location = 1) out vec4 gAlbedo;
layout(location = 2) out vec3 gNormal;
layout(location = 3) out vec3 gMetallicRough;

layout(binding = 5) uniform sampler2D shadowMap;

in vec2 UVs;
in vec3 normals;
in vec4 fragPos;
in vec4 fragPosLightSpace;
in mat3 TBN;

uniform float meshID;
uniform vec3 direction;

struct Material {
    vec3 albedo;
    float metallic;
    float roughness;
    float emissionStrength;

    sampler2D albedoTex;
    sampler2D roughnessTex;
    sampler2D metallicTex;
    sampler2D normalTex;
};

uniform Material material;
uniform float shadowBias = 0.0018;

float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;

    float bias = max(shadowBias * (1.0 - abs(dot(normal, lightDir))), shadowBias);

    // PCF
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(float x = -1.5; x <= 1.5; ++x)
    {
        for(float y = -1.5; y <= 1.5; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;
        }    
    }
    shadow /= 16;

    if (projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

void main()
{
    vec3 albedo = material.albedo * texture(material.albedoTex, UVs).rgb;
    float roughness = material.roughness * texture(material.roughnessTex, UVs).r;
    float metallic = material.metallic * texture(material.metallicTex, UVs).r;
    vec3 normal = texture(material.normalTex, UVs).rgb * 2 - 1;
    
    normal = normalize(TBN * normal);

    mainTexture = vec4(0.0);
    float test = meshID;
    gAlbedo = vec4(albedo, 1.0);
    gNormal = normal;

    float shadowCalc = ShadowCalculation(fragPosLightSpace, normal, direction);
    gMetallicRough = vec3(metallic, roughness, shadowCalc);
}