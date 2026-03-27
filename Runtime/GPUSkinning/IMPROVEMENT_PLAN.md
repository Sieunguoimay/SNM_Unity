# GPU Skinning System — Improvement Plan

## Current Architecture Summary

The system has 3 layers:
- **Runtime**: `GPUSkinnedMeshRendererCore` → `GPUSkinnedMeshRenderer` → two MonoBehaviour wrappers
- **Data**: `SkeletonAsset` / `Skeleton` / `Bone` (ScriptableObject) + `RuntimeBone` (editor-only)
- **Editor Tools**: `BoneTool` orchestrator + selection tools + weight painting + conversion utilities

**Shader pipeline**: Bone weights/indices packed into TEXCOORD1/2, bone matrices uploaded per-frame via `Material.SetMatrixArray`, rendered with `Graphics.DrawMesh`.

---

## Problems Identified

### P1 — Performance

| Issue | Location | Impact |
|-------|----------|--------|
| **Per-frame `Material.SetMatrixArray` call** uploads up to 256 matrices every frame, even when bones haven't moved | `GPUSkinnedMeshRendererCore.UploadBoneMatricesViaMaterial()` | CPU overhead per renderer, GC pressure from matrix array allocation |
| **No batching / instancing support** — each renderer calls `Graphics.DrawMesh` individually | `GPUSkinnedMeshRendererCore.Render()` | 1 draw call per mesh, no GPU instancing |
| **Bone matrices stored in material properties** — prevents SRP Batcher compatibility and shared material usage | `UploadBoneMatricesViaMaterial()` | Each instance needs its own material, breaks batching |
| **`Matrix4x4[]` allocated every setup** — `_skinningMatrices` is `new Matrix4x4[MAX_BONES]` (256) regardless of actual bone count | `GPUSkinnedMeshRenderer` constructor | Wastes memory for simple meshes with few bones |
| **No LOD or culling awareness** — skinning matrices are computed even for off-screen or distant objects | `LateUpdate()` runs unconditionally | Wasted CPU for invisible objects |
| **`ConvertToRaw()` creates new lists every call** — allocates two `List<Vector4>` on every mesh setup | `GPUSkinnedMeshRendererCore.ConvertToRaw()` | GC pressure during setup |

### P2 — Code Clarity & Maintainability

| Issue | Location | Impact |
|-------|----------|--------|
| **Two classes in one file** — `GPUSkinnedMeshRendererCore` and `GPUSkinnedMeshRenderer` share `GPUSkinnedMeshRenderer.cs` | `GPUSkinnedMeshRenderer.cs` | Hard to find, violates single-file-per-class convention |
| **`SkeletonAsset`, `Skeleton`, and `Bone`** all in one file | `SkeletonAsset.cs` | Minor but inconsistent with Unity conventions |
| **Unclear naming** — `GPUSkinnedMeshRendererMB_FromUnitySMR` is long and unclear; `Core` vs non-Core distinction is confusing | Multiple files | New developers won't understand the layering |
| **Magic shader name check** — `TryCreateRenderer()` validates `material.shader.name == "Custom/GpuSkin"` as a string literal | `GPUSkinnedMeshRendererMB.cs` | Fragile, breaks silently if shader is renamed |
| **Mixed responsibilities in MonoBehaviours** — `GPUSkinnedMeshRendererMB` handles validation, creation, update, and rendering | `GPUSkinnedMeshRendererMB.cs` | Hard to test or extend |
| **No documentation or comments** on the core skinning math or data flow | All files | Steep onboarding curve |

### P3 — Extensibility

| Issue | Location | Impact |
|-------|----------|--------|
| **No animation system integration** — bones must be driven by external Transform manipulation, no clip/state machine support | Entire runtime | Users must write their own animation playback |
| **Hardcoded to single shader** — can't swap to URP/HDRP lit shaders or custom variants | Shader name check + single .shader file | Locked to built-in RP with basic diffuse lighting |
| **No event hooks** — no callbacks for "before skinning", "after skinning", "on skeleton changed" | Runtime layer | Can't attach gameplay logic (hit reactions, procedural animation) |
| **No per-instance material property block support** — would enable shared materials with per-instance bone data | `Render()` uses raw material | Prerequisite for instancing |
| **Editor tools tightly coupled to runtime** — `BoneToolWindow` directly creates `GPUSkinnedMeshRendererCore` for preview | Editor layer | Can't swap preview renderer or test editor tools independently |

---

## Improvement Plan

### Phase 1 — Lightweight & High Performance (Priority: High)

#### 1.1 Use `MaterialPropertyBlock` instead of direct material properties
- Replace `material.SetMatrixArray("_Bones", ...)` with `MaterialPropertyBlock.SetMatrixArray()`
- Enables shared material across all GPU-skinned meshes
- Unlocks GPU instancing potential
- **Files**: `GPUSkinnedMeshRendererCore.cs`

#### 1.2 Cache and dirty-flag bone matrices
- Add a `_dirty` flag; only recompute `FillSkinningMatrices()` and upload when bones actually moved
- Compare `Transform.hasChanged` flags on bone transforms
- Reset `hasChanged` after read
- **Files**: `GPUSkinnedMeshRenderer.cs`

#### 1.3 Right-size the matrix array
- Allocate `_skinningMatrices = new Matrix4x4[actualBoneCount]` instead of `MAX_BONES`
- Or use a shared pool / `NativeArray<Matrix4x4>` for zero-GC
- **Files**: `GPUSkinnedMeshRenderer.cs`, `GPUSkinnedMeshRendererCore.cs`

#### 1.4 Add visibility culling
- Check `Renderer.isVisible` or use `GeometryUtility.TestPlanesAABB` before computing skinning
- Skip `SetupMaterial()` + `Render()` for off-screen objects
- **Files**: `GPUSkinnedMeshRendererMB.cs`, `GPUSkinnedMeshRendererMB_FromUnitySMR.cs`

#### 1.5 Reduce setup allocations
- Make `ConvertToRaw()` accept pre-allocated lists or use `Span<T>` / `NativeArray`
- Cache converted mesh data so re-enable doesn't re-allocate
- **Files**: `GPUSkinnedMeshRendererCore.cs`

#### 1.6 (Optional) Compute shader skinning for large crowds
- Move bone matrix multiplication to a compute shader
- Write skinned vertex positions to a structured buffer
- Render with `Graphics.DrawMeshInstancedIndirect`
- Only worth it for 50+ identical characters; keep current path as fallback
- **Files**: New `GPUSkinningCompute.compute`, new `GPUSkinningComputeRenderer.cs`

---

### Phase 2 — Easy to Understand (Priority: Medium)

#### 2.1 One class per file
- Move `GPUSkinnedMeshRendererCore` to its own file
- Move `Skeleton` and `Bone` to their own files (or keep `Bone` inside `Skeleton.cs`)
- **Files**: Split `GPUSkinnedMeshRenderer.cs`, split `SkeletonAsset.cs`

#### 2.2 Rename for clarity
| Current | Proposed | Reason |
|---------|----------|--------|
| `GPUSkinnedMeshRendererCore` | `GPUSkinningData` or `GPUSkinUploader` | Describes what it does: uploads mesh/bone data to GPU |
| `GPUSkinnedMeshRendererMB_FromUnitySMR` | `GPUSkinReplacementRenderer` or `UnitySmrToGpuSkin` | Shorter, clearer intent |
| `GPUSkinnedMeshRendererMB` | `GPUSkinRenderer` | Simpler |
| `FillSkinningMatrices()` | `ComputeBoneMatrices()` | More standard terminology |

#### 2.3 Add XML doc comments to public API
- Document the 3-layer architecture at namespace level
- Document the data flow: setup → per-frame update → GPU
- Document the UV packing convention (TEXCOORD1 = weights, TEXCOORD2 = indices)
- Keep it minimal — only public methods and non-obvious internals

#### 2.4 Replace magic string shader check
- Use `Shader.Find("Custom/GpuSkin")` cached in a static field
- Compare by shader reference, not name string
- Or remove the check entirely and trust the user's material assignment

---

### Phase 3 — Easy to Extend (Priority: Medium)

#### 3.1 Introduce `IGPUSkinRenderer` interface
```csharp
public interface IGPUSkinRenderer
{
    void Setup(Mesh mesh, Material material, Matrix4x4[] bindposes, Transform[] bones);
    void UpdateSkinning();
    void Render(Matrix4x4 localToWorld);
    void Dispose();
}
```
- `GPUSkinnedMeshRenderer` implements this
- Future compute-shader renderer implements this
- MonoBehaviours depend on interface, not concrete class

#### 3.2 Support `MaterialPropertyBlock` rendering path
- Already needed for Phase 1.1
- Enables per-instance bone data with shared materials
- Foundation for `DrawMeshInstanced` batching later

#### 3.3 Add shader variant / keyword support
- Define `#pragma multi_compile _ GPU_SKINNING_ON` in shader
- Allow non-skinned fallback in same shader
- Support URP/HDRP by providing `.shadergraph` or separate shader variants
- Keep the `.cginc` include file as the shared skinning logic

#### 3.4 Add lifecycle callbacks
```csharp
public event Action OnBeforeSkinningUpdate;
public event Action OnAfterSkinningUpdate;
```
- Enables procedural bone manipulation (IK, ragdoll blend, hit reactions)
- Called from `LateUpdate` before/after matrix computation

#### 3.5 Decouple editor preview from runtime renderer ✅
- `BoneToolWindow` uses dedicated `EditorSkinningPreview` class
- `EditorSkinningPreview` wraps `GPUSkinUploader` for editor-only concerns
- `#if UNITY_EDITOR` blocks extracted from `GPUSkinRendererMB` and `GPUSkinReplacementRendererMB` into partial `._Editor.cs` files
- Runtime `.cs` files are now free of editor-only code

---

### Phase 4 — Optional Future Enhancements

#### 4.1 Animation clip baking ✅
- Bake `AnimationClip` bone matrices into a texture (bone matrix texture animation)
- Sample in vertex shader — zero CPU cost per frame
- Implemented via `BakedAnimationSkinRenderer` + `BakedAnimationPlayer` + BakeTool

#### 4.2 Dual quaternion skinning
- Add DQS option in shader for better volume preservation at joints
- Toggle via material keyword `_SKINNING_DQ`

#### 4.3 Blend shape support ✅
- Blend shape deltas extracted from `Mesh` and uploaded via `ComputeBuffer` (StructuredBuffer)
- Per-frame weights uploaded via `MaterialPropertyBlock` (up to 8 simultaneous shapes)
- Shader keyword `BLEND_SHAPES_ON`, applied before skinning using `SV_VertexID`
- `BlendShapes.hlsl` shader include, integrated into all passes of `GPUSkin.shader`
- API: `GPUSkinUploader.SetBlendShapeWeight()`, exposed through `IGPUSkinRenderer`, `GPUSkinRendererMB`, `GPUSkinReplacementRendererMB`

---

## Execution Priority

```
Phase 1.1 (MaterialPropertyBlock)  ✅ done
Phase 1.2 (dirty flag)             ✅ done
Phase 2.1 (one class per file)     ✅ done
Phase 2.2 (rename)                 ✅ done
Phase 1.3 (right-size arrays)      ✅ done
Phase 1.4 (culling)                ✅ done
Phase 3.1 (interface)              ✅ done
Phase 2.3 (XML docs)               ✅ done
Phase 2.4 (remove magic string)    ✅ done
Phase 3.2 (MaterialPropertyBlock)  ✅ done
Phase 3.3 (shader variants)        ✅ done
Phase 3.4 (callbacks)              ✅ done
Phase 3.5 (decouple editor)        ✅ done
Phase 1.5 (reduce allocations)     ✅ done
Phase 4.1 (animation baking)       ✅ done
Phase 4.3 (blend shapes)           ✅ done
Phase 1.6 (compute shader)         — not implemented (only needed for 50+ identical characters)
Phase 4.2 (dual quaternion)        — not implemented
```
