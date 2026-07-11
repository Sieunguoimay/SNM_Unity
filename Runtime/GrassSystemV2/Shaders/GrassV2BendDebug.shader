// Debug overlay for the bend canvas, drawn as a transparent quad over the
// canvas area (GrassDebugOverlay.DrawBendField). One arrow per grid cell:
//
//   arrow direction = fall direction stored in the bend map (RG)
//   arrow color     = bend energy: green (light) -> red (fully pressed),
//                     WHITE when past _DirectionLock (direction is frozen)
//   faint cell fill = same energy, so pressed areas read as a heat patch
//
// Samples the _GrassBendMap / _GrassCanvasRect globals published by
// GrassInteractionCanvas every frame — no per-frame C# binding needed.
Shader "Hidden/Snm/GrassV2BendDebug"
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
            Name "BendDebug"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_GrassBendMap); SAMPLER(sampler_GrassBendMap);

            float _DirectionLock;   // bend amount at which direction froze
            float _CellCount;       // arrows per canvas edge

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Distance from point p to segment a-b, all in cell-local space.
            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
                return length(p - (a + ab * t));
            }

            float4 frag(Varyings input) : SV_Target
            {
                float cells = max(_CellCount, 4.0);

                // Sample the bend map once per cell so the whole cell shows one arrow.
                float2 cellUV = (floor(input.uv * cells) + 0.5) / cells;
                float4 bend = SAMPLE_TEXTURE2D(_GrassBendMap, sampler_GrassBendMap, cellUV);
                float energy = saturate(bend.a);
                if (energy < 0.02) return 0;

                float2 dir = bend.xy;
                float dirLen = length(dir);
                dir = dirLen > 0.001 ? dir / dirLen : float2(1.0, 0.0);

                // Cell-local point in -0.5..0.5.
                float2 p = frac(input.uv * cells) - 0.5;

                // Arrow: shaft plus two head strokes rotated +/-150 deg off the tip.
                float2 tip = dir * 0.36;
                float shaft = SegmentDistance(p, -tip, tip);
                const float HC = -0.8660254; // cos(150deg)
                const float HS = 0.5;        // sin(150deg)
                float2 headA = float2(dir.x * HC - dir.y * HS, dir.x * HS + dir.y * HC);
                float2 headB = float2(dir.x * HC + dir.y * HS, -dir.x * HS + dir.y * HC);
                float head = min(
                    SegmentDistance(p, tip, tip + headA * 0.22),
                    SegmentDistance(p, tip, tip + headB * 0.22));
                float lineMask = 1.0 - smoothstep(0.045, 0.085, min(shaft, head));

                bool locked = energy >= _DirectionLock - 0.001;
                float3 color = locked
                    ? float3(1.0, 1.0, 1.0)
                    : lerp(float3(0.2, 0.85, 0.3), float3(1.0, 0.25, 0.15), energy);

                float alpha = max(lineMask * 0.9, energy * 0.12);
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
