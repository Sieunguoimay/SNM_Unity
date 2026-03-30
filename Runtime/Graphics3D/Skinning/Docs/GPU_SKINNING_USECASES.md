# GPU Skinning — Use Cases

## 1. Single Character with Procedural Control (Live Bones)

**When:** You need full control over bones — IK, ragdoll blending, hit reactions, procedural animation.

**Component:** `GPUSkinRendererMB`

**Setup:**
1. Assign: Mesh + Material (GPUSkin shader) + SkeletonAsset + bone Transforms

**Process:**
1. `OnEnable` → `GPUSkinRenderer` created
   - `GPUSkinUploader.UploadMeshData()` → pack bone weights/indices into TEXCOORD1/2
   - `GPUSkinUploader.UploadBlendShapeData()` → extract deltas into StructuredBuffer (if any)
2. Every `LateUpdate`:
   - Frustum cull check (skip if off-screen)
   - `OnBeforeSkinningUpdate` event → game code moves bones (IK, ragdoll, etc.)
   - `UploadBlendShapeWeights()` → send dirty weights to GPU
   - Check `Transform.hasChanged` on each bone
     - If any changed: `ComputeBoneMatrices()` → `bone.localToWorldMatrix * bindpose`
     - `UploadBoneMatrices()` → `MaterialPropertyBlock.SetMatrixArray`
   - `OnAfterSkinningUpdate` event
   - `Graphics.DrawMesh()` → GPU does vertex skinning via `_Bones[]` array

**Shader path:** `GPU_SKINNING_ON` → `SkinLive()` in `UnifiedSkinning.hlsl`

---

## 2. Baked Animation Characters & Crowds

**When:** Characters playing pre-baked animation clips. No per-bone control needed. Automatically batches identical characters into instanced draw calls.

**Component:** `BakedAnimationRendererMB`

**Setup:**
1. Bake animations: **Tools → Snm → Game → AnimInstancingBaker** → select prefab + clips → Bake → save `.asset`
2. Add `BakedAnimationRendererMB` to a GameObject
3. Assign: Mesh + Material + baked `AnimationInstancingData` asset
4. Done. Instancing is on by default.

**Process:**
1. `OnEnable`:
   - Get or create a shared material (pooled by base material + baked data)
   - Material gets `BAKED_SKINNING_ON` keyword + bone texture data pre-configured
   - `BakedAnimationPlayer` created, plays `defaultAnimation`
2. Every `LateUpdate`:
   - `BakedAnimationPlayer.Update(deltaTime)` → advance frame, handle wrap/crossfade
   - If `useInstancing` (default): submit to `GPUSkinInstanceBatcher`
     - Batcher groups by mesh+material, calls `Graphics.DrawMeshInstanced` (up to 200 per call)
   - If not instancing: `Graphics.DrawMesh` with per-instance property block

**Runtime control:**
```csharp
var renderer = GetComponent<BakedAnimationRendererMB>();
renderer.Player.Play("Walk");
renderer.Player.CrossFade("Run", 0.3f);
renderer.Player.Pause();
```

**Inspector buttons:**
- **Bake...** → opens AnimationBakerWindow
- **Rebake** → re-bakes using existing asset (when clips change)

**Shader path:** `BAKED_SKINNING_ON` → `SkinBaked()` reads `_boneTexture` using frame index + bone index, interpolates between frames.

---

## 3. Hybrid LOD (Live Close + Baked Far)

**When:** A character needs full procedural control up close (IK, ragdoll) but should be cheap when far away.

**Component:** `HybridSkinRendererMB`

**Setup:**
1. Bake animations (same as use case 2)
2. Add `HybridSkinRendererMB` to a GameObject
3. Assign: Mesh + Material + bone Transforms + baked `AnimationInstancingData`
4. Set `lodSwitchDistance` (default 30m)

**Process:**
1. `OnEnable`:
   - Create `GPUSkinRenderer` (live bones, `GPU_SKINNING_ON` material)
   - Create `BakedAnimationPlayer` (baked texture, `BAKED_SKINNING_ON` material)
2. Every `LateUpdate`:
   - Measure distance to `Camera.main`
   - Compare against `lodSwitchDistance` (with hysteresis to prevent flickering)
   - If close → live-bone path (full bone control, `OnBeforeSkinningUpdate` events)
   - If far → baked path (submit to `GPUSkinInstanceBatcher` if `useBatchedInstancing` is on)

**Benefit:** Best of both worlds. Close-up: full IK/ragdoll quality. Far away: near-zero CPU cost + instanced draw calls.

---

## Summary

| Use Case | Component | Setup Steps | CPU Cost/Frame | Draw Calls | Bone Control |
|----------|-----------|-------------|----------------|------------|--------------|
| Single character | `GPUSkinRendererMB` | Assign mesh, material, bones | Low (dirty-flag) | 1 per mesh | Full |
| Baked crowd | `BakedAnimationRendererMB` | Bake + assign 3 fields | Near zero | 1 per 200 (instanced) | None (clips only) |
| Hybrid LOD | `HybridSkinRendererMB` | Bake + assign bones + baked data | Adaptive | 1 per mesh or instanced | Close: full, Far: none |
