#version 440 core

uniform sampler2D frameBufferTexture;
uniform sampler2D depth;
uniform usampler2D stencilTexture;

uniform bool ACES = true;
uniform bool showDepth = false;

uniform int numSteps = 32;
uniform float radius = 3;

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

// Outline taken from: https://www.shadertoy.com/view/sltcRf

void main()
{
    vec3 color = texture(frameBufferTexture, UV).rgb;
    if (ACES && !showDepth) color = ACESFilm(color);
    if (showDepth) color = vec3(LinearizeDepth(texture(depth, UV).r) / far);

    float stencil = texture(stencilTexture, UV).r;

    const vec3 target = vec3(0);
    const float TAU = 6.28318530;
    
    // Correct aspect ratio
    vec2 aspect = 1.0 / vec2(textureSize(stencilTexture, 0));
    
	vec4 outline = vec4(0, 0, 0, 1.0);
	for (float i = 0.0; i < TAU; i += TAU / numSteps)
    {
		// Sample image in a circular pattern
        vec2 offset = vec2(sin(i), cos(i)) * aspect * radius;
		vec4 col = texture(stencilTexture, clamp(UV + offset, 0.0, 1.0));
		
		// Mix outline with background
		float alpha = smoothstep(0.5, 0.7, distance(col.rgb, target));
		outline = mix(outline, vec4(1.0), alpha);
	}
	
    // Overlay original video
	vec4 mat = texture(stencilTexture, UV);
	float factor = smoothstep(0.5, 0.7, distance(mat.rgb, target));
	outline = mix(outline, vec4(0), factor);

    fragColor = mix(vec4(color, 1), vec4(1, 0.35, 0.0, 1), outline);
}