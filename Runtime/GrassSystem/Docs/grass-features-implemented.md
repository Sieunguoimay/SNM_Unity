# Grass System — Implemented Features

---

## Feature 1: Color Variation

Each blade gets a deterministic random value from its `instanceID` using a Knuth hash. This random value blends between two configurable colors (`colorA` and `colorB`), then multiplies into the base tint. This gives each blade a unique hue without any extra textures. Adjacent blades end up with subtly different tints, breaking up the uniform look of a single grass color.

Set `colorA` and `colorB` to your desired range — for example, a darker green and a lighter yellow-green. Both default to white, which is a no-op until you change them.

---

## Feature 2: Ambient Occlusion at Base

Darkens grass near the root using a power curve on `heightFactor` (0 at root, 1 at tip). The base of each blade becomes darker, giving the grass field visual depth and grounding.

When trampled, extra darkening is applied since more of the blade is near the ground — a flattened blade should look darker overall.

**Tuning tips:**
- `strength` (default 0.3) — How dark the base gets. 0 = no AO, 1 = fully black at root.
- `power` (default 2.0) — Controls the falloff curve. Higher values concentrate the darkening tighter to the root.

---

## Feature 7: Frustum Culling

Each frame, the camera's 6 frustum planes are extracted and every blade's world position is tested against them. Blades outside the frustum are excluded. The surviving matrices are re-uploaded to the GPU buffer with an updated instance count, so only visible blades are drawn.

The `margin` config adds extra padding (in world units) around the frustum. This prevents blades from popping in and out at screen edges when they're swaying from wind. Set it to roughly your blade height plus max wind displacement.

The full matrix array is always preserved — only the GPU buffer contents and draw count change each frame.

---

## Feature 8: Per-Blade Sway Variation

Without this feature, all blades sample the wind map at the same phase, so they sway in perfect unison like a rigid sheet. This feature adds two types of per-blade variation to make the grass feel organic and alive.

**Phase offset** — Each blade gets a unique offset derived from its world position, added to the wind UV. This shifts where each blade samples the noise texture, so adjacent blades sway slightly out of sync. Each blade has its own rhythm.

**Amplitude variation** — Each blade gets a scale factor (0.7x to 1.3x), also derived from world position with different hash constants. Some blades sway more, some less.

**Tuning tips:**
- `swayVariation` (default 0.1, range 0–0.5) — Phase offset amount. Start low (0.05–0.15). Too high makes adjacent blades look chaotic rather than natural.
- `amplitudeVariation` (default 0.5, range 0–1) — How much amplitude varies per blade. 0 = all blades sway uniformly, 1 = full 0.7–1.3x range.

---

## Feature 9: Recovery Spring

When a disturber leaves the grass, the blade doesn't just linearly fade back upright — it overshoots and oscillates like a spring snapping back.

**How the spring detects recovery:** The trample map's `z` channel is the hold buffer (stays active while a disturber is present), and `w` is the fading intensity. When `z` is near 0 (disturber gone) but `w` is still above 0 (still fading), the blade is recovering.

**The oscillation:** `recoveryProgress` goes from 0 to 1 as the blade straightens. The formula `exp(-progress * damping) * sin(progress * frequency * 2pi)` gives a classic damped spring — fast wobble that quickly settles to rest. The slight negative clamp (-0.1) allows the blade to overshoot past upright, which sells the springy feel.

**Tuning tips:**
- `springFrequency` (default 8) — Number of wobbles during recovery. Higher = faster vibration.
- `springDamping` (default 3) — How fast the oscillation dies out. Higher = less bouncy, settles faster.
- `springAmplitude` (default 0.15) — Overshoot strength. Keep low or it looks rubbery.
- The oscillation speed is tied to `trampleFadeSpeed` since `recoveryProgress` is driven by how fast the trample fades. Faster fade = faster spring. Tune both together.

---

## Shadow Receiving

Grass receives shadows from the main directional light via URP's shadow map. The shadow attenuation is clamped to a minimum of 0.5 so shadowed grass is darkened but never fully black.

Grass does **not** cast shadows — the Shadow Caster pass was removed. For small grass blades, shadow maps are low resolution relative to the blade size, so self-shadowing creates ugly blocky artifacts that flicker across the grass. The base AO (Feature 2) provides a better-looking, more consistent substitute for that darkening.

---

## Unlit Rendering

The shader uses no N dot L diffuse lighting. With lighting enabled, blades facing the light would be brighter and blades facing away would be darker — but since blade normals point in many directions, this creates a noisy, sparkly look. Without it, the grass looks softer and more stylized.

The visual depth instead comes from:
- `TopColor` / `BottomColor` gradient from tip to root
- Color variation (Feature 1) for per-blade hue differences
- Ambient occlusion (Feature 2) for root darkening
- Shadow receiving (clamped to 0.5 minimum) for environmental shadows

---

## Wind + Trample Combination

Wind and trample each produce a bend direction. They are combined with trample taking priority — `trampleStrength` acts as the blend weight:

- **No trample** — Wind fully controls the blade (swaying).
- **Partial trample** — Blade is pushed down, wind still affects it but with less influence. A half-bent blade doesn't sway as freely.
- **Full trample** — Blade is flat on the ground, wind has no effect. You can't sway a blade that's pinned down.

The transition is smooth and physically intuitive — the heavier the trample, the less the wind matters.

---

## Wind Map Texture Import

The wind map texture must be imported correctly in Unity or the values will be wrong:

- **Texture Type** must be **Default**, not Normal Map. Unity's Normal Map import re-encodes the channels into a packed format (DXT5nm), which mangles the directional data.
- **sRGB must be unchecked.** sRGB is a gamma curve that matches how monitors display color. When checked, Unity converts from sRGB to linear space on sample, applying a ~2.2 power curve that squashes values downward — a texture value of 0.5 becomes about 0.21 in linear. Since the wind map stores directional data (not color), sRGB off gives you the raw values as-is.

Rule of thumb: sRGB on for color/albedo textures (things authored to look correct on a monitor). sRGB off for data textures — normal maps, noise, masks, wind maps, height maps.
