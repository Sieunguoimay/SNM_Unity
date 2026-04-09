#ifndef WATER_INTERSECTION_BANDS_INCLUDED
#define WATER_INTERSECTION_BANDS_INCLUDED

float ComputeIntersectionBands(float thickness)
{
    // Continuous depth ratio — not clamped so lines extend across wide area
    float depth = thickness / _BandMaxDepth;

    // Repeating contour lines that scroll inward toward shore
    float pattern = sin(depth * (float)_BandCount * 2.0 * PI + _Time.y * _BandSpeed);

    // Sharpen sine into thin lines (higher sharpness = thinner lines)
    float contour = pow(saturate(pattern), _BandSharpness);

    // Fade out at deep water
    float fade = 1.0 - saturate(depth);

    return contour * fade * _BandStrength;
}

#endif // WATER_INTERSECTION_BANDS_INCLUDED
