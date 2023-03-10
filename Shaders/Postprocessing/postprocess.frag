#version 440 core

uniform sampler2D frameBufferTexture;
uniform sampler2D depth;

uniform bool ACES = true;
uniform bool showDepth = false;

const float DISTORTION_AMOUNT = 0.5;

in vec2 UV;
out vec4 fragColor;

vec3 ACESFilm( vec3 x) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

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
    if (ACES && !showDepth) color = ACESFilm(color);
    if (showDepth) color = vec3(LinearizeDepth(texture(depth, UV).r) / far);

    fragColor = vec4(color, 1);
}