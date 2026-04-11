#ifndef WATER_SHORELINE_INCLUDED
#define WATER_SHORELINE_INCLUDED

// shorelineUV.x = normalized distance to shore (0 at shore, 1 in deep water)
// shorelineUV.y = normalized along-shore arc length (seamless across closed loops)
// Both are baked into UV1 by WaterMeshGenerator.
float ComputeShoreline(float2 shorelineUV)
{
    float shoreDist = saturate(shorelineUV.x);
    float alongShore = shorelineUV.y;

    float shorelineFoam = 0.0;

    for (int i = 0; i < _ShorelineWaveCount; i++)
    {
        float freq = (float)(i + 1) * _ShorelineFoamScale;
        float phase = _Time.y * _ShorelineSpeed * (1.0 + (float)i * 0.3);

        // Sine band rolling from deep water toward shore. The along-shore term
        // wobbles the band so it doesn't read as a perfectly parallel stripe.
        float wave = sin(shoreDist * freq * PI * 2.0 - phase + alongShore * PI * 2.0);

        wave = saturate(wave);
        wave = pow(wave, 8.0);

        // Concentrate the foam near the shore.
        float shoreFade = 1.0 - shoreDist;

        shorelineFoam += wave * shoreFade;
    }

    return saturate(shorelineFoam) * _ShorelineFoamStrength;
}

#endif // WATER_SHORELINE_INCLUDED
