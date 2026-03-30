using UnityEngine;

namespace Snm.Graphics3D.GPUSkinning
{
    public enum SkinningMode
    {
        None,
        LiveBones,
        BakedTexture
    }

    /// <summary>
    /// Interface for GPU skinning renderers.
    /// Implementations handle mesh setup, per-frame bone matrix computation, and rendering.
    /// </summary>
    public interface IGPUSkinRenderer
    {
        SkinningMode Mode { get; }
        int BlendShapeCount { get; }
        void SetupMesh();
        void UpdateSkinning();
        void Render();
        void SetBlendShapeWeight(int shapeIndex, float weight);
        void Dispose();
    }
}
