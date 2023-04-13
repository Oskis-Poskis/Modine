#version 330 core

in vec2 UV;

uniform sampler2D gAlbedo;
uniform sampler2D depth;
uniform sampler2D gNormal;
uniform sampler2D gPosition;
uniform sampler2D gMetallicRough;

uniform vec3 ambient;
uniform vec3 viewPos;
uniform vec3 direction;
uniform float dirStrength = 1;
uniform int countPL = 0;
uniform float shadowFactor = 0.75;

const float constant = 1;
const float linear = 0.09;
const float quadratic = 0.032;

struct PointLight {
    vec3 lightPos;
    vec3 lightColor;
    float strength;
};

uniform PointLight pointLights[128];

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
    float k = (r * r) / 8.0;

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

vec3 CalcPointLight(PointLight pl, vec3 V, vec3 N, vec3 F0, vec3 alb, float rough, float metal, vec3 fragPos)
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

out vec4 fragColor;

void main()
{
    vec3 albedo = texture(gAlbedo, UV).rgb;
    float _depth = texture(depth, UV).r;
    vec3 N = texture(gNormal, UV).rgb;
    vec3 fragPos = texture(gPosition, UV).rgb;
    vec3 MetRoughShadow = texture(gMetallicRough, UV).rgb;

    float metallic = MetRoughShadow.r;
    float roughness = MetRoughShadow.g;
    float shadow = MetRoughShadow.b;

    vec3 result = vec3(0);
    vec3 V = normalize(viewPos - fragPos);

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 dirLighting = vec3(0.0);
    dirLighting += CalcDirectionalLight(direction, V, N, F0, albedo, roughness, metallic);
    dirLighting = pow(dirLighting, vec3(1 / 2.2));

    vec3 pointLighting = vec3(0);
    for (int i = 0; i < countPL; i++) pointLighting += CalcPointLight(pointLights[i], V, N, F0, albedo, roughness, metallic, fragPos);
    pointLighting = pow(pointLighting, vec3(1 / 2.2));

    result = dirLighting * (1 - shadow * shadowFactor) + (albedo * ambient);
    result += pointLighting;

    if (_depth == 1) discard;
    fragColor = vec4(result, 1);
}