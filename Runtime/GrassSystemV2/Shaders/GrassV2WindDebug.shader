// Debug overlay for procedural wind, drawn as a transparent quad over the
// ground (GrassDebugOverlay.DrawWindField). One arrow per grid cell showing the
// live wind vector from GrassWindVector() — the SAME function the blades use —
// so gusts visibly travel across the field over time.
//
//   arrow direction = instantaneous wind direction at that cell
//   arrow length + color = wind strength (blue calm -> yellow gust)
//
// Reads the _GrassWindGlobal global published by GrassWorld every frame, so it
// always matches the current Wind Direction / Speed / Noise Scale config.
Shader "Hidden/Snm/GrassV2WindDebug"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "WindDebug"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GrassV2Common.hlsl"

            float4 _GrassWindGlobal;  // xy = direction, z = speed, w = noise scale
            float4 _GrassWindGlobal2; // x = lean, y = coherence
            float4 _WindRect;         // xy = world min, zw = world size of the debug patch
            float _CellCount;         // arrows per patch edge

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
                return length(p - (a + ab * t));
            }

            float4 frag(Varyings input) : SV_Target
            {
                float cells = max(_CellCount, 4.0);

                // World position at the center of this cell.
                float2 cellUV = (floor(input.uv * cells) + 0.5) / cells;
                float2 worldXZ = _WindRect.xy + cellUV * _WindRect.zw;

                // Same wind as the blades: direction gusts travel along, time scaled by speed.
                float2 windDir = normalize(_GrassWindGlobal.xy + 1e-5);
                float windTime = _Time.y * _GrassWindGlobal.z;
                float2 wind = GrassWindVector(worldXZ, windTime, windDir, _GrassWindGlobal.w);
                wind += windDir * _GrassWindGlobal2.x; // match the blades' steady lean

                float strength = saturate(length(wind) * 0.6);
                float2 dir = length(wind) > 0.001 ? normalize(wind) : float2(1.0, 0.0);

                // Cell-local point, arrow length scaled by strength so calm cells are short.
                float2 p = frac(input.uv * cells) - 0.5;
                float len = lerp(0.12, 0.4, strength);
                float2 tip = dir * len;
                float shaft = SegmentDistance(p, -tip, tip);
                const float HC = -0.8660254; // cos(150deg)
                const float HS = 0.5;        // sin(150deg)
                float2 headA = float2(dir.x * HC - dir.y * HS, dir.x * HS + dir.y * HC);
                float2 headB = float2(dir.x * HC + dir.y * HS, -dir.x * HS + dir.y * HC);
                float head = min(
                    SegmentDistance(p, tip, tip + headA * (len * 0.55)),
                    SegmentDistance(p, tip, tip + headB * (len * 0.55)));
                float lineMask = 1.0 - smoothstep(0.04, 0.08, min(shaft, head));

                float3 color = lerp(float3(0.25, 0.55, 1.0), float3(1.0, 0.85, 0.2), strength);
                float alpha = max(lineMask * 0.9, strength * 0.10);
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
