#ifndef SPRING_MOTION_INCLUDED
#define SPRING_MOTION_INCLUDED

// Applies inertia-based vertex displacement (jiggle/spring effect).
// Vertices farther from the pivot point are displaced more strongly.
//
// Parameters:
//   posOS            - vertex position in object space
//   pivotOS          - attachment/pivot point in object space
//   displacement     - current spring displacement vector (object space)
//   maxDistance       - reference distance for normalizing falloff (object-space units)
//   falloffPower     - curve exponent (1 = linear, 2 = quadratic, etc.)
//
// Returns: displaced position in object space

float3 ApplySpringMotion(float3 posOS, float3 pivotOS, float3 displacement,
                         float maxDistance, float falloffPower)
{
    float dist = distance(posOS, pivotOS);
    float normalizedDist = saturate(dist / max(maxDistance, 0.001));
    float influence = pow(normalizedDist, falloffPower);
    return posOS + displacement * influence;
}

// Variant that also displaces normals for correct lighting on deformed mesh.
// Uses the same offset so shading stays consistent with geometry.
void ApplySpringMotionFull(inout float3 posOS, inout float3 normalOS,
                           float3 pivotOS, float3 displacement,
                           float maxDistance, float falloffPower)
{
    float dist = distance(posOS, pivotOS);
    float normalizedDist = saturate(dist / max(maxDistance, 0.001));
    float influence = pow(normalizedDist, falloffPower);
    float3 offset = displacement * influence;

    posOS += offset;

    // Approximate normal adjustment: tilt normal away from displacement direction.
    // This gives a subtle but visible shading response to the deformation.
    float3 displacementDir = normalize(displacement + 0.0001);
    float tilt = length(offset) / max(maxDistance, 0.001);
    normalOS = normalize(normalOS - displacementDir * tilt * 0.3);
}

#endif // SPRING_MOTION_INCLUDED
