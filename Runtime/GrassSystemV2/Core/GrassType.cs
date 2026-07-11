using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// One kind of grass: meshes, material and look parameters.
    /// Reusable across worlds. Referenced by index from painted instances,
    /// via <see cref="GrassWorldData.types"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassType", menuName = "Snm/Grass System V2/Grass Type")]
    public class GrassType : ScriptableObject
    {
        [Header("Meshes")]
        [Tooltip("Full-detail blade mesh, drawn near the camera.")]
        public Mesh meshLod0;

        [Tooltip("Optional cheaper mesh for far distances. Leave empty to always use LOD0.")]
        public Mesh meshLod1;

        [Header("Material")]
        [Tooltip("Material using the 'Snm/GrassV2' shader. The system renders with an instanced clone.")]
        public Material material;

        [Header("Placement")]
        [Tooltip("Random uniform scale range applied when painting blades of this type.")]
        public Vector2 scaleRange = new(0.8f, 1.2f);

        [Header("Wind")]
        [Range(0f, 1f)]
        [Tooltip("How strongly wind bends this type. 0 = rigid.")]
        public float swayAmount = 0.5f;

        [Tooltip("Sway oscillation speed multiplier.")]
        public float swayFrequency = 1f;

        [Header("Color")]
        [Tooltip("Per-blade tint is a random blend between Color A and Color B (seeded, stable).")]
        public Color colorA = Color.white;
        public Color colorB = Color.white;

        [Header("Ambient Occlusion")]
        [Range(0f, 1f)]
        [Tooltip("Darkening at the blade root. Substitute for cast shadows.")]
        public float aoStrength = 0.3f;

        [Tooltip("Falloff curve of root darkening. Higher = tighter to the root.")]
        public float aoPower = 2f;

        /// <summary>Blade height of LOD0, used for bend normalization and bounds padding.</summary>
        public float BladeHeight => meshLod0 != null ? meshLod0.bounds.size.y : 1f;

        public bool HasLod1 => meshLod1 != null;

        /// <summary>True when the type has everything required to render.</summary>
        public bool IsValid => meshLod0 != null && material != null;
    }
}
