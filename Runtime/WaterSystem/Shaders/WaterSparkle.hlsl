#ifndef WATER_SPARKLE_INCLUDED
#define WATER_SPARKLE_INCLUDED

// Simple hash for procedural sparkle positions
float SparkleHash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float ComputeSparkle(float3 worldPos, float3 normalWS, float3 viewDir)
{
    // Grid-based sparkle cells
    float2 cellUV = worldPos.xz * _SparkleDensity;

    // Animate cell offset over time
    float timePhase = floor(_Time.y * _SparkleSpeed * 3.0);
    cellUV += timePhase * 0.37;

    float2 cellID = floor(cellUV);
    float2 cellFrac = frac(cellUV);

    // Random value per cell
    float h = SparkleHash(cellID);

    // Only some cells sparkle (threshold)
    float sparkleActive = step(0.85, h);

    // View-dependent flicker: sparkle when view angle aligns
    float NdotV = saturate(dot(normalWS, viewDir));
    float viewFlicker = pow(NdotV, 4.0);

    // Time-based flicker for twinkling
    float flicker = sin(h * 100.0 + _Time.y * _SparkleSpeed * 10.0);
    flicker = saturate(flicker * 2.0 - 1.0);

    // Sharp point — fade near cell edges
    float2 dist = abs(cellFrac - 0.5);
    float pointMask = 1.0 - saturate(max(dist.x, dist.y) * 4.0);

    return sparkleActive * pointMask * flicker * viewFlicker * _SparkleIntensity;
}

#endif // WATER_SPARKLE_INCLUDED
