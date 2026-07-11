// Shared HLSL for GrassSystemV2: instance layout, packing, procedural wind
// noise and bend math. Included by GrassV2.shader and GrassV2Cull.compute.
#ifndef SNM_GRASS_V2_COMMON_INCLUDED
#define SNM_GRASS_V2_COMMON_INCLUDED

// Mirrors Snm.GrassSystemV2.GrassInstance (20 bytes, see GrassInstance.cs).
struct GrassInstanceData
{
    float3 position;
    uint packedYawScale;      // low 16 = yaw [0..2pi), high 16 = scale * 1000
    uint packedTypeSeedFlags; // low 8 = type, next 8 = seed, high 16 = flags
};

#define GRASS_FLAG_CUT 1u

float GrassUnpackYaw(GrassInstanceData instance)
{
    return (instance.packedYawScale & 0xFFFF) * (6.2831853 / 65535.0);
}

float GrassUnpackScale(GrassInstanceData instance)
{
    return (instance.packedYawScale >> 16) * 0.001;
}

float GrassUnpackSeed01(GrassInstanceData instance)
{
    return ((instance.packedTypeSeedFlags >> 8) & 0xFF) / 255.0;
}

uint GrassUnpackFlags(GrassInstanceData instance)
{
    return instance.packedTypeSeedFlags >> 16;
}

// ---------------------------------------------------------------------------
// Procedural wind — value noise, two octaves. No texture, so there is nothing
// to import wrong (V1's wind map required exact sRGB/format settings).
// ---------------------------------------------------------------------------

float GrassHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float GrassValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // smoothstep interpolation

    float a = GrassHash21(cell);
    float b = GrassHash21(cell + float2(1, 0));
    float c = GrassHash21(cell + float2(0, 1));
    float d = GrassHash21(cell + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Signed 2D wind vector at a world position. Gusts travel along _GrassWindDirection.
float2 GrassWindVector(float2 worldXZ, float time, float2 windDirection, float noiseScale)
{
    float2 travel = windDirection * time;
    float coarse = GrassValueNoise(worldXZ * noiseScale - travel);
    float fine = GrassValueNoise(worldXZ * noiseScale * 3.7 - travel * 1.8);
    float gust = coarse * 0.75 + fine * 0.25;          // [0..1]
    float side = GrassValueNoise(worldXZ * noiseScale * 1.3 + travel * 0.6) - 0.5;
    // Mostly along the wind direction, with a little perpendicular wobble.
    float2 perp = float2(-windDirection.y, windDirection.x);
    return windDirection * (gust * 2.0 - 0.6) + perp * side * 0.6;
}

// ---------------------------------------------------------------------------
// Bend helpers (carried over from V1 — proven look)
// ---------------------------------------------------------------------------

// Rotates vector v so that direction `from` maps to direction `to`.
float3 GrassRotateFromTo(float3 v, float3 from, float3 to)
{
    float3 axis = cross(from, to);
    float cosA = dot(from, to);
    float qw = 1.0 + cosA;

    if (qw < 1e-4)
    {
        axis = abs(from.y) < 0.999 ? cross(from, float3(0, 1, 0)) : cross(from, float3(1, 0, 0));
        qw = 0.0;
    }

    float3 t1 = cross(axis, v);
    float3 t2 = cross(axis, t1 + qw * v);
    return v + 2.0 * t2 / dot(float4(axis, qw), float4(axis, qw));
}

float GrassEaseOutCubic(float x)
{
    float inv = 1.0 - x;
    return 1.0 - inv * inv * inv;
}

float GrassEaseInCubic(float x)
{
    return x * x * x;
}

#endif // SNM_GRASS_V2_COMMON_INCLUDED
