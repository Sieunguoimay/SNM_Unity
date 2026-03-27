# GPU Skinning — Use Cases

## 1. Single Character with Procedural Control (Live Bones)

**When:** You need full control over bones — IK, ragdoll blending, hit reactions, procedural animation.

**Component:** `GPUSkinRendererMB`

**Process:**
1. Assign: Mesh + Material (GPUSkin shader) + SkeletonAsset + bone Transforms
2. `OnEnable` → `GPUSkinRenderer` created
   - `GPUSkinUploader.UploadMeshData()` → pack bone weights/indices into TEXCOORD1/2
   - `GPUSkinUploader.UploadBlendShapeData()` → extract deltas into StructuredBuffer (if any)
3. Every `LateUpdate`:
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

## 2. Replace Unity's SkinnedMeshRenderer

**When:** You have an existing character using Unity's CPU-based `SkinnedMeshRenderer` and want to switch to GPU skinning without restructuring the hierarchy.

**Component:** `GPUSkinReplacementRendererMB`

**Process:**
1. Assign: reference to the existing `SkinnedMeshRenderer` + GPU skinning shader
2. `OnEnable`:
   - Clone the SMR's material, swap shader to GPUSkin
   - Read mesh + bones + bindposes from the SMR
   - Create `GPUSkinRenderer` (same as use case 1)
   - Disable the original `SkinnedMeshRenderer`
3. Every `LateUpdate`: same as use case 1
4. `OnDisable`: re-enable the original SMR (graceful fallback)

**Benefit:** Drop-in replacement. Unity's Animator still drives the bone Transforms — this component just reads them on GPU instead of CPU.

---

## 3. Baked Animation Crowds (Zero CPU Skinning)

**When:** Many identical characters playing looped animations (NPCs, crowds, background characters). You don't need per-bone control.

**Components:** `BakedAnimationSkinRenderer` + `BakedAnimationPlayer`

**Process:**
1. **Offline (Editor):**
   - `AnimationBakerWindow`: select prefab + clips + FPS
   - `AnimationBaker.BakeWithAnimator()`:
     - Instantiate prefab, play each animation clip frame-by-frame
     - At each frame: compute `bone.localToWorldMatrix * bindpose` for all bones
   - `AnimationTextureBaker`: pack all pose matrices into `Texture2D` (RGBAHalf)
   - Save as `AnimationInstancingData` ScriptableObject (contains textures + clip metadata)
2. **Runtime setup:**
   - `BakedAnimationSkinRenderer(mesh, material, instancingData)`
   - Enable `BAKED_SKINNING_ON` keyword
   - `BakedAnimationPlayer.Play(clipIndex)`
3. **Every frame:**
   - `BakedAnimationPlayer.Update(deltaTime)`
     - Advance `_curFrame` by `fps * deltaTime`
     - Handle wrap mode (Loop, PingPong, Once)
     - Handle crossfade transitions (blend between preFrame and curFrame)
   - Set per-instance properties via `MaterialPropertyBlock`:
     - `_boneTexture` (the baked pose texture)
     - `frameIndex` (current frame)
     - `preFrameIndex` (previous animation frame, for crossfade)
     - `transitionProgress` (0→1 blend weight)
   - `Graphics.DrawMesh()` → GPU samples bone matrices from texture, no CPU bone computation

**Shader path:** `BAKED_SKINNING_ON` → `SkinBaked()` reads `_boneTexture` using frame index + bone index to reconstruct the 4x4 matrix, interpolates between frames.

---

## 4. Batched Baked Crowds (Instanced Draw Calls)

**When:** 10–200 identical baked characters sharing the same mesh+material. Minimizes draw calls.

**Component:** `GPUSkinInstanceBatcher` (singleton)

**Process:**
1. Each character has its own `BakedAnimationSkinRenderer` + `BakedAnimationPlayer`
2. Instead of calling `Render()` individually, each submits to the batcher:
   `GPUSkinInstanceBatcher.Instance.Submit(renderer, mesh, material, localToWorld)`
3. Batcher groups by `mesh + material` hash:
   - Collects world matrices + `frameIndex` + `preFrameIndex` + `transitionProgress`
   - When batch hits 200 or `LateUpdate` fires:
     - Pack per-instance data into float arrays
     - `Graphics.DrawMeshInstanced(mesh, material, matrices[], count, propertyBlock)`
   - 200 characters = 1 draw call instead of 200

**Benefit:** Massive reduction in draw calls. Each character can play a different animation/frame — the per-instance `frameIndex` ensures correct pose sampling.

---

## 5. Hybrid LOD (Live Close + Baked Far)

**When:** A character needs full procedural control up close (IK, ragdoll) but should be cheap when far away.

**Component:** `HybridSkinRendererMB`

**Process:**
1. Assign: Mesh + Material + bone Transforms + `AnimationInstancingData` + `lodSwitchDistance`
2. `OnEnable`:
   - Create `GPUSkinRenderer` (live bones, `GPU_SKINNING_ON` material clone)
   - Create `BakedAnimationSkinRenderer` (baked texture, `BAKED_SKINNING_ON` material clone)
3. Every `LateUpdate`:
   - Measure distance to `Camera.main`
   - Compare against `lodSwitchDistance` (with hysteresis to prevent flickering)
   - If close → `_activeRenderer = liveRenderer` (full bone control)
   - If far → `_activeRenderer = bakedRenderer` (zero CPU skinning)
   - `_activeRenderer.UpdateSkinning()` + `Render()`

**Benefit:** Best of both worlds. Close-up: full IK/ragdoll quality. Far away: near-zero CPU cost.

---

## Summary

| Use Case | Component | Skinning Mode | CPU Cost/Frame | Draw Calls | Bone Control |
|----------|-----------|---------------|----------------|------------|--------------|
| Single character | `GPUSkinRendererMB` | Live | Low (dirty-flag) | 1 per mesh | Full |
| Replace Unity SMR | `GPUSkinReplacementRendererMB` | Live | Low | 1 per mesh | Full (via Animator) |
| Baked crowd | `BakedAnimationSkinRenderer` | Baked texture | Near zero | 1 per mesh | None (clips only) |
| Instanced crowd | `GPUSkinInstanceBatcher` | Baked texture | Near zero | 1 per 200 | None (clips only) |
| Hybrid LOD | `HybridSkinRendererMB` | Both | Adaptive | 1 per mesh | Close: full, Far: none |
