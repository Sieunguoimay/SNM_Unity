using UnityEngine;

namespace Snm.Runtime.GPUSkinning
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
        void SetupMesh();
        void UpdateSkinning();
        void Render();
        void Dispose();
    }
}
