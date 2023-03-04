#version 330 core

in vec3 normals;
in vec3 fragPos;
in vec4 fragPosLightSpace;
out vec4 fragColor;

uniform vec3 albedo;
uniform float metallic;
uniform float roughness;
uniform bool smoothShading;

uniform sampler2D shadowMap;

uniform vec3 viewPos;

const float constant = 1;
const float linear = 0.09;
const float quadratic = 0.032;

uniform highp float NoiseAmount;
highp float NoiseCalc = NoiseAmount / 255;
highp float random(highp vec2 coords)
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

vec3 CalcDirectionalLight(vec3 direction, vec3 V, vec3 N, vec3 F0, vec3 alb, float rough, float metal)
{
    // Calc per light radiance
    vec3 L = direction;
    vec3 H = normalize(V + L);
    vec3 radiance = vec3(1);

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
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;

    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;
    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.0075);
    float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;  

    return shadow;
}

void main()
{
    vec3 normal;
    if (smoothShading) normal = normals;
    else normal = cross(dFdx(fragPos), dFdy(fragPos));

    vec3 N = normalize(normal);
    vec3 V = normalize(viewPos - fragPos);

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);

    Lo += CalcDirectionalLight(vec3(1, 1, 1), V, N, F0, albedo, roughness, metallic);

    float ambient = 0.1;
    vec3 color = ambient + Lo;

    

    vec3 result = vec3(1) - exp(-color);
    result = pow(result, vec3(1 / 2.2));

    float shadow = ShadowCalculation(fragPosLightSpace, N, vec3(1, 1, 1)); 
    result = result * (1 - shadow * 0.9);

    fragColor = vec4(result, 1);
}