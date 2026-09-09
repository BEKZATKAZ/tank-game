void DangerZone_float(
    float2 UV, float2 ObjectSize, float ZoneSize,
    out float Edge, out float Alpha)
{
    float maxSize = max(ObjectSize.x, ObjectSize.y);
    float2 size = ZoneSize / maxSize;

    float Thickness = 0.5f;
    float2 thicknessUV = Thickness / ObjectSize;

    float x = smoothstep(size.x - thicknessUV.x, size.x, abs(UV.x - 0.5) * 2);
    float y = smoothstep(size.y - thicknessUV.y, size.y, abs(UV.y - 0.5) * 2);

    Alpha = x + y - x * y;
    Edge = saturate(smoothstep(0.00, 0.05, Alpha) * (1.0 - smoothstep(0.95, 1.0, Alpha)) * 2);
}
