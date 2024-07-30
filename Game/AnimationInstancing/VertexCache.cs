using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class VertexCache
    {
        public readonly int nameHash;
        public readonly string name;
        public readonly Dictionary<int, MaterialBlock> InstanceBlockDic = new();
        public readonly Mesh mesh;
        public readonly AnimationTextureData textureData;
        public readonly RenderingConfig renderingConfig;

        public VertexCache(
            string name,
            Mesh mesh,
            AnimationTextureData textureData,
            RenderingConfig renderingConfig)
        {
            nameHash = name.GetHashCode();
            this.name = name;
            this.mesh = mesh;
            this.textureData = textureData;
            this.renderingConfig = renderingConfig;
        }
    }

    public class RenderingConfig
    {
        public UnityEngine.Rendering.ShadowCastingMode shadowcastingMode;
        public bool receiveShadow;
        public int layer;
    }
}