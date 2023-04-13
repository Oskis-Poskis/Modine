#version 430 core

in vec2 texCoords;
out vec4 fragColor;

uniform sampler2D framebufferTexture;

void main()
{
    fragColor = texture(framebufferTexture, texCoords);
}