// Deprecated: This file is kept for backwards compatibility.
// New code should use UnifiedSkinning.cginc with BAKED_SKINNING_ON keyword.

#ifndef ANIMATION_INSTANCING_BASE
#define ANIMATION_INSTANCING_BASE

#include "../../GPUSkinning/Shader/UnifiedSkinning.cginc"

// Legacy vert function — delegates to unified skinning
void vert(inout appdata_full v)
{
#ifdef BAKED_SKINNING_ON
    #ifdef UNITY_PASS_SHADOWCASTER
    v.vertex = SkinBakedShadow(v.vertex, v.texcoord1, v.texcoord2);
    #else
    v.vertex = SkinBaked(v.vertex, v.normal.xyz, v.tangent.xyz, v.texcoord1, v.texcoord2);
    #endif
#endif
}

#endif
