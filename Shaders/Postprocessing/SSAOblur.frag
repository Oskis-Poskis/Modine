#version 330 core

uniform sampler2D frameBufferTexture;
uniform bool ssaoOnOff = true;

in vec2 UV;
layout(location = 3) out vec4 blurao;

void main()
{
    if (ssaoOnOff)
    {
        vec2 offset;
        vec2 texelSize = 1.0 / vec2(textureSize(frameBufferTexture, 0));
        float result = 0.0;
        for (int x = -3; x <= 3; ++x) 
        {
            for (int y = -3; y <= 3; ++y) 
            {
                offset = vec2(float(x) * texelSize.x, float(y) * texelSize.y);
                result += texture(frameBufferTexture, UV + offset).a;
            }
        }
        float blur = result / (7.0 * 7.0);
        
        blurao = vec4(blur);
    }

    else blurao = vec4(1);
}