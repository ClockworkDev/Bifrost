Texture2D colorMap_ : register(t0);
SamplerState colorSampler_ : register(s0);

// Per-pixel color data passed through the pixel shader.
struct PixelShaderInput
{
    min16float4 pos   : SV_POSITION;
    min16float2 tex0 : TEXCOORD0;
};

// The pixel shader passes through the color data. The color data from 
// is interpolated and assigned to a pixel at the rasterization step.
min16float4 main(PixelShaderInput input) : SV_TARGET
{
	return colorMap_.Sample(colorSampler_, input.tex0);
    //return min16float4(input.color, 1.0f);
}
