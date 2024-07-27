using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class VertexCache
    {
        // public int nameCode;
        private readonly Dictionary<int, MaterialBlock> instanceBlockDic = new();
        public Material[] sharedMaterials;
        public AnimationTextureData textureData;
        public BoneAndMesh BoneAndMesh { get; }
        public Dictionary<int, MaterialBlock> InstanceBlockDic => instanceBlockDic;

        // these are temporary, should be moved to InstancingPackage
        public RenderingConfig renderingConfig;

        public VertexCache(
            BoneAndMesh boneAndMesh,
            AnimationTextureData textureData,
            Material[] sharedMaterials,
            RenderingConfig renderingConfig)
        {
            BoneAndMesh = boneAndMesh;
            this.sharedMaterials = sharedMaterials;
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