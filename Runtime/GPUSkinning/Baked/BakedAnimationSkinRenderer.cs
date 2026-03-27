using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// GPU skinning renderer using pre-baked animation textures.
    /// Bone matrices are sampled from texture in the shader — near-zero CPU cost per frame.
    /// Uses MaterialPropertyBlock for per-instance frame data.
    /// </summary>
    public class BakedAnimationSkinRenderer : IGPUSkinRenderer
    {
        private static readonly int BoneTextureId = Shader.PropertyToID("_boneTexture");
        private static readonly int BoneTextureWidthId = Shader.PropertyToID("_boneTextureWidth");
        private static readonly int BoneTextureHeightId = Shader.PropertyToID("_boneTextureHeight");
        private static readonly int BoneTextureBlockWidthId = Shader.PropertyToID("_boneTextureBlockWidth");
        private static readonly int BoneTextureBlockHeightId = Shader.PropertyToID("_boneTextureBlockHeight");
        private static readonly int FrameIndexId = Shader.PropertyToID("frameIndex");
        private static readonly int PreFrameIndexId = Shader.PropertyToID("preFrameIndex");
        private static readonly int TransitionProgressId = Shader.PropertyToID("transitionProgress");

        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly Transform _meshTransform;
        private readonly AnimationTextureData _textureData;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private readonly BakedAnimationPlayer _player;

        public SkinningMode Mode => SkinningMode.BakedTexture;
        public int BlendShapeCount => 0;
        public BakedAnimationPlayer Player => _player;

        /// <summary>
        /// Current frame index for external batching (GPUSkinInstanceBatcher).
        /// </summary>
        public float FrameIndex => _player.FrameIndex;
        public float PreFrameIndex => _player.PreFrameIndex;
        public float TransitionProgress => _player.TransitionProgress;

        public BakedAnimationSkinRenderer(
            Mesh mesh,
            Material material,
            Transform meshTransform,
            AnimationInstancingData instancingData)
        {
            _mesh = mesh;
            _material = material;
            _meshTransform = meshTransform;
            _textureData = instancingData.animationTextureData;
            _player = new BakedAnimationPlayer(instancingData);

            material.EnableKeyword("BAKED_SKINNING_ON");
            material.DisableKeyword("GPU_SKINNING_ON");
        }

        public void SetupMesh()
        {
            // Baked mode reads bone data from texture, but still needs
            // bone weights/indices in TEXCOORD1/TEXCOORD2 on the mesh.
            // These are expected to be pre-baked (via GPUSkinUploader).
        }

        public void UpdateSkinning()
        {
            _player.Update(Time.deltaTime);

            int texIdx = _player.TextureIndex;
            if (_textureData?.bakedBoneTextures != null && texIdx < _textureData.bakedBoneTextures.Length)
            {
                var tex = _textureData.bakedBoneTextures[texIdx];
                _propertyBlock.SetTexture(BoneTextureId, tex);
                _propertyBlock.SetInt(BoneTextureWidthId, tex.width);
                _propertyBlock.SetInt(BoneTextureHeightId, tex.height);
                _propertyBlock.SetInt(BoneTextureBlockWidthId, _textureData.textureBlockWidth);
                _propertyBlock.SetInt(BoneTextureBlockHeightId, _textureData.textureBlockHeight);
            }

            _propertyBlock.SetFloat(FrameIndexId, _player.FrameIndex);
            _propertyBlock.SetFloat(PreFrameIndexId, _player.PreFrameIndex);
            _propertyBlock.SetFloat(TransitionProgressId, _player.TransitionProgress);
        }

        public void Render()
        {
            Graphics.DrawMesh(_mesh, _meshTransform.localToWorldMatrix, _material, 0, null, 0, _propertyBlock);
        }

        public void SetBlendShapeWeight(int shapeIndex, float weight)
        {
            // Blend shapes not supported in baked animation mode.
        }

        public void Dispose()
        {
            // No unmanaged resources.
        }
    }
}
