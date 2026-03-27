# GPUSkinning — Architecture Overview

## What This System Is

GPUSkinning is a **character rendering pipeline** that handles the full lifecycle of rendering animated, deformable meshes on the GPU. "GPU Skinning" refers to the core technique — vertex deformation via bone matrices computed or sampled on the GPU — but the system encompasses all supporting infrastructure around that goal.

## Responsibilities

| Area | Files | What it does |
|------|-------|-------------|
| **Skeleton data** | `SkeletonAsset`, `BoneTool/*` | Define, edit, visualize bone hierarchies and weights |
| **Animation baking** | `Baked/BakeTool/*`, `AnimationInstancingData`, `BakedAnimationPlayer` | Bake animation clips to textures, play them back at runtime |
| **Skinned rendering** | `GPUSkinUploader`, `GPUSkinRenderer`, `BakedAnimationSkinRenderer` | Render meshes deformed by bones (live or baked) |
| **Blend shapes** | `BlendShapes.hlsl`, blend shape code in `GPUSkinUploader` | GPU morph target deformation via StructuredBuffer |
| **Batching / LOD** | `GPUSkinInstanceBatcher`, `HybridSkinRendererMB` | Instanced crowds, distance-based mode switching |
| **Shader** | `UnifiedSkinning.hlsl`, `BlendShapes.hlsl`, `GPUSkin.shader` | Vertex deformation on GPU (live bones + baked texture + blend shapes) |

## Folder Structure

```
GPUSkinning/
├── Shader/
│   ├── UnifiedSkinning.hlsl      # GPU_SKINNING_ON (live) + BAKED_SKINNING_ON (texture) paths
│   ├── BlendShapes.hlsl          # BLEND_SHAPES_ON path (StructuredBuffer + SV_VertexID)
│   └── GPUSkin.shader            # URP shader with all three keywords across 3 passes
├── Baked/
│   ├── AnimationInstancingData.cs        # ScriptableObject: baked bone textures + clip metadata
│   ├── BakedAnimationPlayer.cs           # Frame progression, crossfade, wrap modes (no MonoBehaviour)
│   ├── BakedAnimationRendererMB.cs       # Main MB for baked characters (auto-instancing, shared materials)
│   ├── BakedAnimationRendererMB._Editor.cs # Inspector: Bake/Rebake buttons, playback info
│   ├── BakedAnimationSkinRenderer.cs     # Low-level IGPUSkinRenderer using baked textures
│   ├── GPUSkinInstanceBatcher.cs         # Auto-batches into DrawMeshInstanced (up to 200 per call)
│   ├── HybridSkinRendererMB.cs           # Switches live ↔ baked by camera distance
│   └── BakeTool/
│       ├── AnimationBaker.cs         # Samples Animator frame-by-frame, collects bone matrices
│       ├── AnimationTextureBaker.cs  # Packs pose matrices into Texture2D (RGBAHalf)
│       ├── AnimationBakerWindow.cs   # Editor UI for baking
│       └── RuntimeHelper.cs          # Transform path utilities for bone merging
├── BoneTool/                         # Editor-only skeleton authoring and weight painting
│   ├── BoneToolWindow.cs
│   ├── EditorSkinningPreview.cs
│   └── ... (selection, visualization, import/export)
├── GPUSkinUploader.cs            # Low-level: mesh data packing, bone matrix upload, blend shape buffer
├── GPUSkinnedMeshRenderer.cs     # GPUSkinRenderer: live-bone IGPUSkinRenderer (dirty-flag, Transform[])
├── IGPUSkinRenderer.cs           # Interface: SetupMesh, UpdateSkinning, Render, SetBlendShapeWeight, Dispose
├── GPUSkinRendererMB.cs          # MonoBehaviour wrapper for GPUSkinRenderer (frustum cull, lifecycle)
├── GPUSkinRendererMB._Editor.cs  # Editor-only partial: CreateBoneTransforms context menu
└── SkeletonAsset.cs              # ScriptableObject for serialized bone hierarchy + bindposes
```

## Why the Name Stays "GPUSkinning"

1. **GPU skinning is the core identity.** Everything in the system — baking, batching, blend shapes, bone tools — exists to serve GPU-based mesh deformation. They are supporting infrastructure for that one technique.

2. **A broader name would be vague.** "CharacterRendering" or "AnimatedMeshSystem" wouldn't communicate *how* it works, which is the distinguishing feature versus Unity's built-in SkinnedMeshRenderer.

3. **Consistent with other package modules.** The SNM_Unity package names modules by technique: `WaterSystem`, `GrassSystem`, `GPUSkinning`.

4. **Renaming has real cost.** Renaming a folder in a Unity submodule breaks `.meta` GUIDs, prefab references, and asmdef paths. The cost is real, the benefit is cosmetic.

## When to Use This vs Unity's SkinnedMeshRenderer

- **Use Unity SMR** for characters driven by Animator that need standard tooling (Cloth, mesh colliders, editor preview). Unity's Burst/SIMD CPU skinning is faster than managed C# for live-bone cases.
- **Use GPUSkinning** when you need baked animation crowds (zero CPU cost), instanced rendering (1 draw call per 200 characters), custom bone control (IK/ragdoll hooks), or GPU blend shapes.

See [GPU_SKINNING_USECASES.md](GPU_SKINNING_USECASES.md) for detailed per-use-case workflows.
