#version 330 core

uniform sampler2D frameBufferTexture;
uniform bool fxaaOnOff = true;

in vec2 UV;
out vec4 fragColor;

vec3 tex(vec2 p) {
    return texture(frameBufferTexture, p).rgb; 
}

// FXAA from: https://www.shadertoy.com/view/4tf3D8
vec3 fxaa(vec2 p, vec2 RES)
{
	float FXAA_SPAN_MAX   = 8.0;
    float FXAA_REDUCE_MUL = 1.0 / 8.0;
    float FXAA_REDUCE_MIN = 1.0 / 128.0;

    // 1st stage - Find edge
    vec3 rgbNW = tex(p + (vec2(-1.,-1.) / RES));
    vec3 rgbNE = tex(p + (vec2( 1.,-1.) / RES));
    vec3 rgbSW = tex(p + (vec2(-1., 1.) / RES));
    vec3 rgbSE = tex(p + (vec2( 1., 1.) / RES));
    vec3 rgbM  = tex(p);

    vec3 luma = vec3(0.299, 0.587, 0.114);

    float lumaNW = dot(rgbNW, luma);
    float lumaNE = dot(rgbNE, luma);
    float lumaSW = dot(rgbSW, luma);
    float lumaSE = dot(rgbSE, luma);
    float lumaM  = dot(rgbM,  luma);

    vec2 dir;
    dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
    dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));
    
    float lumaSum   = lumaNW + lumaNE + lumaSW + lumaSE;
    float dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
    float rcpDirMin = 1. / (min(abs(dir.x), abs(dir.y)) + dirReduce);

    dir = min(vec2(FXAA_SPAN_MAX), max(vec2(-FXAA_SPAN_MAX), dir * rcpDirMin)) / RES;

    // 2nd stage - Blur
    vec3 rgbA = .5 * (tex(p + dir * (1./3. - .5)) +
        			  tex(p + dir * (2./3. - .5)));
    vec3 rgbB = rgbA * .5 + .25 * (
        			  tex(p + dir * (0./3. - .5)) +
        			  tex(p + dir * (3./3. - .5)));
    
    float lumaB = dot(rgbB, luma);
    
    float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
    float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

    return ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;
}

// Modification of: https://www.shadertoy.com/view/sltcRf
void main()
{	
    if (fxaaOnOff)
    {
        vec2 size = textureSize(frameBufferTexture, 0);
        fragColor = vec4(fxaa(UV, size), 1);
    }

    else fragColor = vec4(texture(frameBufferTexture, UV).rgb, 1);
}