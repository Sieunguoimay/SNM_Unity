# Future Grass Features

## Visual Polish (low-medium effort)
- **Color variation** — per-instance random tint using the instance ID hash already computed. Breaks the uniform look cheaply.
- **Ambient occlusion at base** — darken blades near the root more when trampled/dense. Already have `heightFactor`, just modulate it.
- **Subsurface scattering fake** — when the camera looks toward the sun through grass tips, brighten them. Single dot product in the frag shader.

## Interaction (medium effort)
- **Cut/destroyed grass** — write a "cut" channel in the trample map (or a separate map). Blades below a threshold scale to zero. Lets you mow or burn patches.
- **Snow/rain accumulation** — similar stamp approach but affects color and bends blades downward uniformly. Could reuse the trample pipeline with a different blend mode.

## Performance (medium effort)
- **Distance-based LOD** — fade out or reduce blade density beyond a distance. Cull instances on the CPU or use a visibility buffer.
- **Frustum culling** — skip instances outside the camera frustum. Currently all instances render every frame.

## Motion Quality (medium-high effort)
- **Per-blade sway variation** — use the instance hash to offset wind phase per blade so they don't all sway in sync. One line change in the shader.
- **Recovery spring** — instead of snapping back linearly when trample fades, add a slight overshoot/wobble. Makes the grass feel alive after something walks through it.
