#ifndef WATER_RAIN_INCLUDED
#define WATER_RAIN_INCLUDED

TEXTURE2D(_RainRippleTex);
SAMPLER(sampler_RainRippleTex);

// Hash for random ripple positions
float2 RainHash(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float3 ComputeRainRipples(float3 worldPos)
{
    float2 uv = worldPos.xz * _RainScale;

    // Tile into cells for ripple placement
    float2 cellID = floor(uv * _RainDensity);
    float2 cellFrac = frac(uv * _RainDensity);

    float3 rippleNormal = float3(0, 0, 0);

    // Check 3x3 neighborhood for overlapping ripples
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 id = cellID + neighbor;
            float2 randOffset = RainHash(id);

            // Random position within cell
            float2 rippleCenter = neighbor + randOffset - cellFrac;
            float dist = length(rippleCenter);

            // Expanding ring animation
            float phase = frac(randOffset.x * 7.0 + _Time.y * _RainSpeed);
            float ring = 1.0 - saturate(abs(dist - phase * 0.5) * 10.0);
            float fade = 1.0 - phase; // fade out as ripple expands

            // Radial normal from ring center
            float2 dir = dist > 0.001 ? rippleCenter / dist : float2(0, 0);
            rippleNormal.xz += dir * ring * fade;
        }
    }

    rippleNormal *= _RainIntensity;
    rippleNormal.y = 1.0;
    return normalize(rippleNormal);
}

#endif // WATER_RAIN_INCLUDED
