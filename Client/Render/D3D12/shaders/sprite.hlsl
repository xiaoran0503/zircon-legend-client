// 精灵批着色器：NDC 四边形 + 纹理 + 色调染色（Tint）。
// 一份源码，DXC 编 DXIL（D3D12）；M3 同份源码 -spirv 编 SPIR-V（Vulkan）。

struct VSIn
{
    float2 pos : POSITION;
    float2 uv  : TEXCOORD0;
};

struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv  : TEXCOORD0;
};

cbuffer SpriteCB : register(b0)
{
    float4x4 Transform;
    float4   Tint;
};

Texture2D<float4> SpriteTex : register(t0);
SamplerState SpriteSamp    : register(s0);

VSOut VS(VSIn i)
{
    VSOut o;
    o.pos = mul(float4(i.pos, 0.0, 1.0), Transform);
    o.uv  = i.uv;
    return o;
}

float4 PS(VSOut i) : SV_TARGET
{
    float4 c = SpriteTex.Sample(SpriteSamp, i.uv);
    return c * Tint;
}
