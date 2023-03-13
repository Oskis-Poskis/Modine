#version 330 core

in vec3 fragPosViewSpace;
in vec3 normalsViewSpace;

in vec2 UVs;
in vec3 normals;
in vec3 fragPos;
in vec4 fragPosLightSpace;
layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec3 gPosition;
layout(location = 2) out vec3 gNormal;

uniform vec3 ambient;

uniform sampler2D shadowMap;
uniform float shadowFactor;
uniform float shadowBias = 0.0018;

uniform bool smoothShading;
uniform vec3 viewPos;

uniform vec3 direction;
uniform float dirStrength = 1;
uniform int countPL = 0;

const float constant = 1;
const float linear = 0.09;
const float quadratic = 0.032;

struct Material {
    vec3 albedo;
    float metallic;
    float roughness;
    float emissionStrength;

    sampler2D albedoTex;
    sampler2D roughnessTex;
    sampler2D metallicTex;
};

struct PointLight {
    vec3 lightPos;
    vec3 lightColor;
    float strength;
};

uniform PointLight pointLights[32];
uniform Material material;

highp float random(vec2 coords)
{
   return fract(sin(dot(coords.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

const float PI = 3.14159265359;
float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

vec3 CalcPointLight(PointLight pl, vec3 V, vec3 N, vec3 F0, vec3 alb, float rough, float metal)
{
    // Calc per light radiance
    vec3 L = normalize(pl.lightPos - fragPos);
    vec3 H = normalize(V + L);
    float distance = length(pl.lightPos - fragPos);
    float attenuation = 1.0 / (distance * distance);
    //float _attenuation = pow(smoothstep(pl.radius, 0, distance), pl.falloff); // Non-PBR attenuation
    vec3 radiance = pl.lightColor * attenuation; // * pl.strength;

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, rough);   
    float G   = GeometrySmith(N, V, L, rough);
    vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);

    vec3 numerator    = NDF * G * F; 
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
    vec3 specular = numerator / denominator;

    vec3 kS = F;
    vec3 kD = vec3(1) - kS;
    kD *= 1 - metal;

    float NDotL = max(dot(N, L), 0.0);

    return (kD * alb / PI + specular) * radiance * NDotL * pl.strength;
}

vec3 CalcDirectionalLight(vec3 direction, vec3 V, vec3 N, vec3 F0, vec3 alb, float rough, float metal)
{
    // Calc per light radiance
    vec3 L = normalize(direction);
    vec3 H = normalize(V + L);
    vec3 radiance = vec3(1) * dirStrength;

    // Cook-Torrance BRDF
    float NDF = DistributionGGX(N, H, rough);   
    float G   = GeometrySmith(N, V, L, rough);
    vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);

    vec3 numerator    = NDF * G * F; 
    float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
    vec3 specular = numerator / denominator;

    vec3 kS = F;
    vec3 kD = vec3(1) - kS;
    kD *= 1 - metal;

    float NDotL = max(dot(N, L), 0.0);

    return (kD * alb / PI + specular) * radiance * NDotL;
}

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

    if(projCoords.z > 1.0)
        shadow = 0.0;
        
    return shadow;
}

void main()
{
    vec3 result = vec3(0);
    if (material.emissionStrength == 0)
    {
        vec3 albedo = material.albedo * texture(material.albedoTex, UVs).rgb;
        float roughness = material.roughness * texture(material.roughnessTex, UVs).r;
        float metallic = material.metallic * texture(material.metallicTex, UVs).r;

        vec3 normal;
        if (smoothShading) normal = normals;
        else normal = cross(dFdx(fragPos), dFdy(fragPos));

        vec3 N = normalize(normal);
        vec3 V = normalize(viewPos - fragPos);

        vec3 F0 = vec3(0.04);
        F0 = mix(F0, albedo, metallic);

        vec3 dirLighting = vec3(0.0);
        dirLighting += CalcDirectionalLight(direction, V, N, F0, albedo, roughness,  metallic);
        dirLighting = pow(dirLighting, vec3(1 / 2.2));

        vec3 pointLighting = vec3(0);
        for (int i = 0; i < countPL; i++) pointLighting += CalcPointLight(pointLights[i], V, N, F0, albedo, roughness, metallic);
        pointLighting = pow(pointLighting, vec3(1 / 2.2));

        float shadow = ShadowCalculation(fragPosLightSpace, N, direction); 
        result = dirLighting * (1 - shadow * shadowFactor) + (albedo * ambient);
        result += pointLighting;
    }

    else result += material.albedo * material.emissionStrength;

    gPosition = fragPosViewSpace;
    gNormal = normalsViewSpace;
    fragColor = vec4(result, 1);
}