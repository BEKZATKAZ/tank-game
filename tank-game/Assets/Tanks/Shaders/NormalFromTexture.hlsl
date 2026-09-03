float SampleGrayscale(UnityTexture2D Texture, float2 UV)
{
    float3 sampled = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, UV);
    return dot(sampled, float3(0.299, 0.587, 0.114));
}

void NormalFromTexture_float(float2 UV, UnityTexture2D Texture, float Strength, out float3 Out)
{
    float2 texelSize = Texture.texelSize.xy;

    float L = SampleGrayscale(Texture, UV - float2(texelSize.x, 0));
    float R = SampleGrayscale(Texture, UV + float2(texelSize.x, 0));

    float D = SampleGrayscale(Texture, UV - float2(0, texelSize.y));
    float U = SampleGrayscale(Texture, UV + float2(0, texelSize.y));

    float2 gradient = -float2(R - L, U - D) * Strength;
    Out = normalize(float3(gradient, 1));
}
