#version 330 core

uniform sampler2D frameBufferTexture;
uniform bool ssaoOnOff = true;

uniform int gaussianRadius = 3;

in vec2 UV;
layout(location = 3) out vec4 blurao;

void main()
{
    if (ssaoOnOff)
    {
        vec2 offset;
        vec2 texelSize = 1.0 / vec2(textureSize(frameBufferTexture, 0));
        float result = 0.0;
        for (int x = -gaussianRadius; x <= gaussianRadius; ++x) 
        {
            for (int y = -gaussianRadius; y <= gaussianRadius; ++y) 
            {
                offset = vec2(float(x) * texelSize.x, float(y) * texelSize.y);
                result += texture(frameBufferTexture, UV + offset).a;
            }
        }
        float blur = result / ((gaussianRadius * 2 + 1) * (gaussianRadius * 2 + 1));
        
        blurao = vec4(blur);
    }

    else blurao = vec4(1);
}