#ifndef BLEND_SHAPES_INCLUDED
#define BLEND_SHAPES_INCLUDED

// =============================================================================
// GPU Blend Shapes
//
// Keyword: BLEND_SHAPES_ON
//
// Reads per-vertex position and normal deltas from a StructuredBuffer,
// blends them by weight, and applies to the vertex before skinning.
//
// Requires SV_VertexID to index into the delta buffer.
// =============================================================================

#if defined(BLEND_SHAPES_ON)

#define MAX_BLEND_SHAPES 8

struct BlendShapeDelta
{
    float3 positionDelta;
    float3 normalDelta;
};

StructuredBuffer<BlendShapeDelta> _BlendShapeBuffer;
int _BlendShapeCount;
int _BlendShapeVertexCount;
float _BlendShapeWeights[MAX_BLEND_SHAPES];

void ApplyBlendShapes(uint vertexId, inout float3 position, inout float3 normal)
{
    int count = min(_BlendShapeCount, MAX_BLEND_SHAPES);
    for (int i = 0; i < count; i++)
    {
        float w = _BlendShapeWeights[i];
        if (abs(w) < 0.001)
            continue;
        uint idx = (uint)i * (uint)_BlendShapeVertexCount + vertexId;
        BlendShapeDelta d = _BlendShapeBuffer[idx];
        position += d.positionDelta * w;
        normal += d.normalDelta * w;
    }
    normal = normalize(normal);
}

#define BLEND_SHAPE_VERTEX_INPUT uint vertexId : SV_VertexID;
#define APPLY_BLEND_SHAPES(v) ApplyBlendShapes(v.vertexId, v.positionOS.xyz, v.normalOS)

#else

#define BLEND_SHAPE_VERTEX_INPUT
#define APPLY_BLEND_SHAPES(v)

#endif // BLEND_SHAPES_ON

#endif // BLEND_SHAPES_INCLUDED
