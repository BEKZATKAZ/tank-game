float2 MirrorBackgroundUV(float2 uv)
{
    return 1.0 - abs(frac(uv * 0.5) * 2.0 - 1.0);
}

float SampleMirroredBackground(UnityTexture2D Texture, float2 UV)
{
    float3 color = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, MirrorBackgroundUV(UV)).rgb;
    return dot(color, float3(0.299, 0.587, 0.114));
}

void MirroredBackground_float(float3 Position, UnityTexture2D Texture, float Strength,
    float4 Dimensions, out float3 Out, out float2 UV)
{
    // XY is the background size; ZW is the native sprite size in local units.
    float2 tileUV = (Position.xy + Dimensions.xy * 0.5) / max(Dimensions.zw, 0.0001);
    UV = MirrorBackgroundUV(tileUV);

    float2 texel = Texture.texelSize.xy;
    float L = SampleMirroredBackground(Texture, tileUV - float2(texel.x, 0));
    float R = SampleMirroredBackground(Texture, tileUV + float2(texel.x, 0));
    float D = SampleMirroredBackground(Texture, tileUV - float2(0, texel.y));
    float U = SampleMirroredBackground(Texture, tileUV + float2(0, texel.y));

    float2 gradient = -float2(R - L, U - D) * Strength;
    Out = normalize(float3(gradient, 1));
}
