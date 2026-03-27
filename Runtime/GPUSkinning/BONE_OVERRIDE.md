# Bone Override — Per-Bone Procedural Control on Baked Animations

## What It Does

Bone Override lets you override specific bones on top of a baked animation. The baked texture provides the base pose for all bones (free, GPU-only), while the CPU computes only the few bones you need to adjust (IK, look-at, etc.).

```
50 bones per enemy:
  46 bones → baked texture (zero CPU)
  4 bones  → CPU override (foot IK + spine look-at)
```

## When to Use

- Enemies on terrain that need foot placement
- Characters aiming/looking at a target
- Any baked-animation character that needs a few bones adjusted procedurally
- When you have many such characters and full live-bone skinning would be too expensive

## API

### SetBoneOverrideWorld (recommended)

Provide a desired world-space bone transform. The system converts it to the correct internal space automatically.

```csharp
var renderer = GetComponent<BakedAnimationRendererMB>();

// Foot IK: place left foot at terrain hit point
var footWorldMatrix = Matrix4x4.TRS(terrainHitPoint, footRotation, Vector3.one);
renderer.SetBoneOverrideWorld(leftFootBoneIndex, footWorldMatrix, weight: 1.0f);

// Look-at: rotate spine toward target
var lookDir = (target.position - transform.position).normalized;
var spineRotation = Quaternion.LookRotation(lookDir, Vector3.up);
var spineWorldMatrix = Matrix4x4.TRS(spineWorldPosition, spineRotation, Vector3.one);
renderer.SetBoneOverrideWorld(spineBoneIndex, spineWorldMatrix, weight: 0.8f);
```

### SetBoneOverride (advanced)

Provide a matrix already in root-local space (same space as baked matrices):

```csharp
// Root-local space: character.worldToLocalMatrix * desiredBoneWorldMatrix * mesh.bindposes[boneIndex]
renderer.SetBoneOverride(boneIndex, rootLocalMatrix, weight);
```

### ClearBoneOverride / ClearAllBoneOverrides

```csharp
renderer.ClearBoneOverride(leftFootBoneIndex);  // single bone returns to baked pose
renderer.ClearAllBoneOverrides();                // all bones return to baked pose
```

## Weight Parameter

The `weight` parameter controls blending between the baked pose and the override:

| Weight | Result |
|--------|--------|
| 0.0 | Fully baked (override ignored) |
| 0.5 | 50/50 blend between baked and override |
| 1.0 | Fully overridden |

Use intermediate weights for smooth blending, e.g. gradually increasing look-at weight as a target enters view.

## How It Works Internally

### Shader

`BoneOverride.hlsl` defines up to 8 override slots. When the shader samples a bone matrix from the baked texture, it checks if that bone has an override:

```hlsl
half4x4 GetBoneMatrix(uint frame, uint boneIndex)
{
    half4x4 baked = LoadBoneMatFromTexture(frame, boneIndex);
    // BONE_OVERRIDE macro checks override list, returns lerp if matched
    return BONE_OVERRIDE(baked, boneIndex);
}
```

The loop is at most 8 iterations. Most vertices have 4 bone influences, and most bones have no override, so the early-exit is fast.

### Material Management

- **No overrides active** → uses shared instanced material → `DrawMeshInstanced` via batcher
- **Any override active** → creates a per-instance material with `BONE_OVERRIDE_ON` keyword → individual `DrawMesh`
- **Overrides cleared** → automatically returns to shared instanced material

This means you can dynamically toggle overrides. Enemies far from the player can be pure baked (instanced), and switch to override mode when they get close.

### Matrix Space

Baked matrices are stored as: `rootWorldToLocal * bone.localToWorldMatrix * bindpose`

`SetBoneOverrideWorld()` handles the conversion automatically:
```csharp
rootLocalMatrix = transform.worldToLocalMatrix * desiredWorldMatrix * mesh.bindposes[boneIndex]
```

## Finding Bone Indices

Bone indices correspond to the skeleton hierarchy in the source prefab's SkinnedMeshRenderer. To find the index of a specific bone:

```csharp
// Option 1: from the SkinnedMeshRenderer at edit time
var smr = sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>();
for (int i = 0; i < smr.bones.Length; i++)
    Debug.Log($"{i}: {smr.bones[i].name}");

// Option 2: store bone names → indices in a lookup
var boneMap = new Dictionary<string, int>();
for (int i = 0; i < smr.bones.Length; i++)
    boneMap[smr.bones[i].name] = i;
int leftFoot = boneMap["LeftFoot"];
```

## Limits

- **Max 8 overrides per instance** — enough for 2 feet + 2 knees + spine + chest + head + 1 extra
- **Not instanced** — characters with active overrides use individual `DrawMesh` calls
- **Per-bone only** — overrides a full bone matrix, not individual position/rotation components
- **No child bone cascade** — overriding a spine bone does NOT automatically adjust child bones (arms, head). For a full upper-body override, you'd need to override each bone in the chain.

## Example: Enemy with Foot IK + Look-At

```csharp
public class EnemyAnimController : MonoBehaviour
{
    [SerializeField] private BakedAnimationRendererMB renderer;
    [SerializeField] private Transform target;
    [SerializeField] private int leftFootBone = 3;
    [SerializeField] private int rightFootBone = 7;
    [SerializeField] private int spineBone = 1;
    [SerializeField] private float footRayLength = 1.5f;

    private void LateUpdate()
    {
        // Foot IK
        AdjustFoot(leftFootBone, transform.TransformPoint(-0.15f, 0, 0));
        AdjustFoot(rightFootBone, transform.TransformPoint(0.15f, 0, 0));

        // Look-at
        var toTarget = (target.position - transform.position).normalized;
        var lookRot = Quaternion.LookRotation(toTarget, Vector3.up);
        var spineWorld = Matrix4x4.TRS(transform.position + Vector3.up * 1.2f, lookRot, Vector3.one);
        renderer.SetBoneOverrideWorld(spineBone, spineWorld, 0.7f);
    }

    private void AdjustFoot(int boneIndex, Vector3 worldOrigin)
    {
        if (Physics.Raycast(worldOrigin + Vector3.up, Vector3.down, out var hit, footRayLength))
        {
            var footRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            var footMatrix = Matrix4x4.TRS(hit.point, footRot, Vector3.one);
            renderer.SetBoneOverrideWorld(boneIndex, footMatrix, 1.0f);
        }
        else
        {
            renderer.ClearBoneOverride(boneIndex);
        }
    }
}
```

## Performance

| Scenario (30 enemies) | CPU bone work | Draw calls |
|------------------------|---------------|------------|
| Full live-bone (GPUSkinRendererMB) | 30 x 50 = 1500 bones | 30 |
| Baked + 4 overrides each | 30 x 4 = 120 bones | 30 |
| Pure baked, no override | 0 bones | 1 (instanced) |
| Mixed: 10 with override, 20 pure baked | 10 x 4 = 40 bones | 11 (10 + 1 instanced) |
