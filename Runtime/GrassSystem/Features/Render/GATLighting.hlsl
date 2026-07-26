#ifndef GAT_LIGHTING_INCLUDED
#define GAT_LIGHTING_INCLUDED

// ============================================================
// Hue-shifted lighting for Grab and Toss
// Warm highlights, cool shadows, rich midtones.
// Requires: URP Lighting.hlsl included before this file
// ============================================================

int   _GAT_LightingEnabled;
half  _GAT_HighlightTargetHue;
half  _GAT_HighlightHueShift;
half  _GAT_HighlightSatMul;
half  _GAT_ShadowHueShift;
half  _GAT_ShadowTargetHue;
half  _GAT_ShadowSatMul;
half  _GAT_ShadowValMul;
half  _GAT_ShadowStrength;
half  _GAT_MidtoneSatBoost;
half  _GAT_MidtoneWidth;
half  _GAT_LightingMix;

// --- RGB <-> HSV (branchless) ---

half3 GAT_RGBtoHSV(half3 c)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
    half d = q.x - min(q.w, q.y);
    half e = 1.0e-4;
    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

half3 GAT_HSVtoRGB(half3 c)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// Lerp hue along the shortest arc on the color wheel (circular interpolation).
// Fades out near the antipodal point (0.5 distance) where direction is ambiguous.
half GAT_HueLerp(half a, half b, half t)
{
    half delta = frac(b - a + 0.5) - 0.5;
    half antipodalFade = smoothstep(0.5, 0.35, abs(delta));
    return frac(a + delta * t * antipodalFade);
}

// --- Core shading ---

// Full shading: warm highlights, cool shadows, rich midtones.
// NdotL = Lambert term, shadowAtten = distanceAttenuation * shadowAttenuation.
half3 GAT_ApplyLighting(half3 baseColor, half3 lightColor, half NdotL, half shadowAtten)
{
    if (!_GAT_LightingEnabled) return baseColor;

    half softShadow = lerp(1.0, saturate(shadowAtten), _GAT_ShadowStrength);
    half atten = min(NdotL, softShadow);

    // Base HSV (shared by both highlight and shadow)
    half3 hsv = GAT_RGBtoHSV(baseColor);
    half satFade = smoothstep(0.01, 0.1, hsv.y);

    // --- Highlight: shift hue toward warm target ---
    half3 highlightHSV = half3(
        GAT_HueLerp(hsv.x, _GAT_HighlightTargetHue, _GAT_HighlightHueShift * satFade),
        saturate(hsv.y * _GAT_HighlightSatMul),
        hsv.z
    );
    half3 lit = GAT_HSVtoRGB(highlightHSV) * lightColor;

    // --- Shadow: shift hue toward cool target ---
    half3 shadowHSV = half3(
        GAT_HueLerp(hsv.x, _GAT_ShadowTargetHue, _GAT_ShadowHueShift * satFade),
        saturate(hsv.y * _GAT_ShadowSatMul),
        hsv.z * _GAT_ShadowValMul
    );
    half3 shadow = GAT_HSVtoRGB(shadowHSV);

    // --- Blend shadow and lit ---
    half3 color = lerp(shadow, lit, atten);

    // --- Midtone saturation boost ---
    half midtoneMask = 1.0 - abs(atten * 2.0 - 1.0);
    midtoneMask = pow(midtoneMask, _GAT_MidtoneWidth);
    half boost = midtoneMask * _GAT_MidtoneSatBoost;

    if (boost > 0.001)
    {
        half3 colorHSV = GAT_RGBtoHSV(color);
        colorHSV.y = saturate(colorHSV.y + boost);
        color = GAT_HSVtoRGB(colorHSV);
    }

    return lerp(baseColor, color, _GAT_LightingMix);
}

#endif // GAT_LIGHTING_INCLUDED
