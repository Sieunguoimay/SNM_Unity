#ifndef WATER_SHORELINE_INCLUDED
#define WATER_SHORELINE_INCLUDED

float ComputeShoreline(float thickness)
{
    // Normalize depth into [0,1] range
    float depthNorm = saturate(thickness / _ShorelineMaxDepth);

    float shorelineFoam = 0.0;

    // Layer multiple sine-based foam bands rolling toward shore
    for (int i = 0; i < _ShorelineWaveCount; i++)
    {
        float freq = (float)(i + 1) * _ShorelineFoamScale;
        float phase = _Time.y * _ShorelineSpeed * (1.0 + (float)i * 0.3);

        // Sine wave mapped to depth — rolls toward shore (depthNorm=0)
        float wave = sin(depthNorm * freq * PI * 2.0 - phase);

        // Sharpen into a narrow foam band
        wave = saturate(wave);
        wave = pow(wave, 8.0);

        // Fade out at deeper water
        float depthFade = 1.0 - depthNorm;

        shorelineFoam += wave * depthFade;
    }

    return saturate(shorelineFoam) * _ShorelineFoamStrength;
}

#endif // WATER_SHORELINE_INCLUDED
