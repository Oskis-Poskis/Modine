#version 330 core

uniform sampler2D frameBufferTexture;
uniform usampler2D stencilTexture;
uniform int numSteps = 12;
uniform float radius = 3.0;
uniform bool ssaoOnOff = true;

const float TAU = 6.28318530;

in vec2 UV;
out vec4 fragColor;

// Modification of: https://www.shadertoy.com/view/sltcRf
void main()
{
    vec4 color = texture(frameBufferTexture, UV);
    float stencil = texture(stencilTexture, UV).r;
    float alpha;
    
    vec2 aspect = 1.0 / vec2(textureSize(stencilTexture, 0));
    float outlinemask = 0.0;
    for (float i = 0.0; i < TAU; i += TAU / numSteps)
    {
        // Sample image in a circular pattern
        vec2 offset = vec2(sin(i), cos(i)) * aspect * radius;
        float col = texture(stencilTexture, clamp(UV + offset, 0, 1)).r;
        
        outlinemask = mix(outlinemask, 1.0, col);
    }
    outlinemask = mix(outlinemask, 0.0, stencil);

    vec4 _color;
    if (ssaoOnOff)
    {
        vec2 offset;
        vec2 texelSize = 1.0 / vec2(textureSize(frameBufferTexture, 0));
        float result = 0.0;
        for (int x = -2; x < 2; ++x) 
        {
            for (int y = -2; y < 2; ++y) 
            {
                offset = vec2(float(x) * texelSize.x, float(y) * texelSize.y);
                result += texture(frameBufferTexture, UV + offset).a;
            }
        }
        float blur = result / (4.0 * 4.0);
        
        _color = vec4(color.rgb * vec3(blur), 1);
    }

    else _color = vec4(color.rgb, 1);
    fragColor = mix(_color, vec4(0.75, 0.4, 0.0, 1.0), outlinemask);
}