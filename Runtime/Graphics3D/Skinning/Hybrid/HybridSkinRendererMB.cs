using System;
using System.Linq;
using Snm.Runtime.Foundation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.Graphics3D.GPUSkinning
{
    /// <summary>
    /// Hybrid GPU skinning MonoBehaviour that switches between live-bone and baked-texture modes
    /// based on camera distance. Close = full procedural control. Far = baked texture, near-zero CPU.
    /// </summary>
    [ExecuteInEditMode]
    public class HybridSkinRendererMB : MonoBehaviour
    {
        IMainCameraProvider _cameraProvider;

        /// <summary>
        /// Inject a camera provider to avoid calling <see cref="Camera.main"/> directly
        /// every frame. If not set, falls back to <see cref="MainCameraProvider.Default"/>.
        /// Pragmatic compromise: MonoBehaviours have no constructor DI seam.
        /// </summary>
        public void SetMainCameraProvider(IMainCameraProvider provider)
        {
            _cameraProvider = provider;
        }

        IMainCameraProvider CameraProvider => _cameraProvider ??= MainCameraProvider.Default;

        [Header("Mesh & Material")]
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;

        [Header("Live Bones (close-up mode)")]
        [SerializeField] private SkeletonAsset skeleton;
        [SerializeField] private Transform[] boneTransforms;

        [Header("Baked Animation (crowd mode)")]
        [SerializeField] private AnimationInstancingData bakedAnimationData;
        [SerializeField] private int defaultAnimation;
        [SerializeField] private bool useBatchedInstancing = true;

        [Header("LOD Switching")]
        [SerializeField] private float lodSwitchDistance = 30f;
        [SerializeField] private float switchHysteresis = 2f;

        [Header("Rendering")]
        [SerializeField] private ShadowCastingMode shadowCasting = ShadowCastingMode.On;
        [SerializeField] private bool receiveShadows = true;

        private GPUSkinRenderer _liveRenderer;
        private BakedAnimationPlayer _bakedPlayer;
        private AnimationTextureData _bakedTextureData;
        private SkinningMode _currentMode = SkinningMode.None;
        private Material _liveMaterial;
        private Material _bakedMaterial;
        private MaterialPropertyBlock _bakedPropertyBlock;

        private static readonly int BoneTextureId = Shader.PropertyToID("_boneTexture");
        private static readonly int BoneTextureWidthId = Shader.PropertyToID("_boneTextureWidth");
        private static readonly int BoneTextureHeightId = Shader.PropertyToID("_boneTextureHeight");
        private static readonly int BoneTextureBlockWidthId = Shader.PropertyToID("_boneTextureBlockWidth");
        private static readonly int BoneTextureBlockHeightId = Shader.PropertyToID("_boneTextureBlockHeight");
        private static readonly int FrameIndexId = Shader.PropertyToID("frameIndex");
        private static readonly int PreFrameIndexId = Shader.PropertyToID("preFrameIndex");
        private static readonly int TransitionProgressId = Shader.PropertyToID("transitionProgress");

        /// <summary>Fired before bone matrices are computed (live mode only).</summary>
        public event Action OnBeforeSkinningUpdate;
        /// <summary>Fired after skinning update.</summary>
        public event Action OnAfterSkinningUpdate;

        /// <summary>Access the baked animation player for playback control (Play, CrossFade, etc.).</summary>
        public BakedAnimationPlayer BakedPlayer => _bakedPlayer;
        public SkinningMode CurrentMode => _currentMode;

        private void OnEnable()
        {
            TryCreateRenderers();
        }

        private void OnDisable()
        {
            DisposeAll();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            DisposeAll();
            TryCreateRenderers();
        }

        private void TryCreateRenderers()
        {
            if (mesh == null || material == null) return;

            // Live-bone renderer
            if (boneTransforms != null && boneTransforms.Length > 0)
            {
                var bindposes = skeleton != null
                    ? skeleton.skeleton.bones.Select(b => b.bindpose).ToArray()
                    : mesh.bindposes;
                var safeBones = new Transform[boneTransforms.Length];
                for (int i = 0; i < boneTransforms.Length; i++)
                    safeBones[i] = boneTransforms[i] != null ? boneTransforms[i] : transform;

                _liveMaterial = Instantiate(material);
                _liveMaterial.EnableKeyword("GPU_SKINNING_ON");
                _liveMaterial.DisableKeyword("BAKED_SKINNING_ON");

                _liveRenderer = new GPUSkinRenderer(mesh, bindposes, _liveMaterial, safeBones, transform);
                _liveRenderer.SetupMesh();
            }

            // Baked-texture path
            if (bakedAnimationData != null)
            {
                _bakedTextureData = bakedAnimationData.animationTextureData;

                _bakedMaterial = Instantiate(material);
                _bakedMaterial.EnableKeyword("BAKED_SKINNING_ON");
                _bakedMaterial.DisableKeyword("GPU_SKINNING_ON");
                _bakedMaterial.enableInstancing = true;

                if (_bakedTextureData?.bakedBoneTextures != null && _bakedTextureData.bakedBoneTextures.Length > 0)
                {
                    var tex = _bakedTextureData.bakedBoneTextures[0];
                    _bakedMaterial.SetTexture(BoneTextureId, tex);
                    _bakedMaterial.SetInt(BoneTextureWidthId, tex.width);
                    _bakedMaterial.SetInt(BoneTextureHeightId, tex.height);
                    _bakedMaterial.SetInt(BoneTextureBlockWidthId, _bakedTextureData.textureBlockWidth);
                    _bakedMaterial.SetInt(BoneTextureBlockHeightId, _bakedTextureData.textureBlockHeight);
                }

                _bakedPlayer = new BakedAnimationPlayer(bakedAnimationData);
                _bakedPlayer.Play(defaultAnimation);
                _bakedPropertyBlock = new MaterialPropertyBlock();
            }

            SwitchMode(GetDesiredMode());
        }

        private void LateUpdate()
        {
            if (_currentMode == SkinningMode.None) return;

            var desiredMode = GetDesiredMode();
            if (desiredMode != _currentMode)
                SwitchMode(desiredMode);

            if (_currentMode == SkinningMode.LiveBones)
            {
                OnBeforeSkinningUpdate?.Invoke();
                _liveRenderer.UpdateSkinning();
                OnAfterSkinningUpdate?.Invoke();
                _liveRenderer.Render();
            }
            else if (_currentMode == SkinningMode.BakedTexture)
            {
                _bakedPlayer.Update(Time.deltaTime);

                if (useBatchedInstancing)
                {
                    GPUSkinInstanceBatcher.Instance.Submit(
                        mesh, _bakedMaterial,
                        transform.localToWorldMatrix,
                        _bakedPlayer.FrameIndex, _bakedPlayer.PreFrameIndex, _bakedPlayer.TransitionProgress,
                        gameObject.layer, shadowCasting, receiveShadows);
                }
                else
                {
                    SetBakedPropertyBlock();
                    Graphics.DrawMesh(mesh, transform.localToWorldMatrix, _bakedMaterial,
                        gameObject.layer, null, 0, _bakedPropertyBlock,
                        shadowCasting, receiveShadows);
                }
            }
        }

        private void SetBakedPropertyBlock()
        {
            int texIdx = _bakedPlayer.TextureIndex;
            if (_bakedTextureData?.bakedBoneTextures != null && texIdx < _bakedTextureData.bakedBoneTextures.Length)
            {
                var tex = _bakedTextureData.bakedBoneTextures[texIdx];
                _bakedPropertyBlock.SetTexture(BoneTextureId, tex);
                _bakedPropertyBlock.SetInt(BoneTextureWidthId, tex.width);
                _bakedPropertyBlock.SetInt(BoneTextureHeightId, tex.height);
                _bakedPropertyBlock.SetInt(BoneTextureBlockWidthId, _bakedTextureData.textureBlockWidth);
                _bakedPropertyBlock.SetInt(BoneTextureBlockHeightId, _bakedTextureData.textureBlockHeight);
            }
            _bakedPropertyBlock.SetFloat(FrameIndexId, _bakedPlayer.FrameIndex);
            _bakedPropertyBlock.SetFloat(PreFrameIndexId, _bakedPlayer.PreFrameIndex);
            _bakedPropertyBlock.SetFloat(TransitionProgressId, _bakedPlayer.TransitionProgress);
        }

        private SkinningMode GetDesiredMode()
        {
            if (_liveRenderer == null && _bakedPlayer == null)
                return SkinningMode.None;
            if (_liveRenderer == null)
                return SkinningMode.BakedTexture;
            if (_bakedPlayer == null)
                return SkinningMode.LiveBones;

            var cam = CameraProvider.Current;
            if (cam == null)
                return SkinningMode.LiveBones;

            float dist = Vector3.Distance(transform.position, cam.transform.position);

            if (_currentMode == SkinningMode.LiveBones)
                return dist > lodSwitchDistance + switchHysteresis ? SkinningMode.BakedTexture : SkinningMode.LiveBones;
            else
                return dist < lodSwitchDistance - switchHysteresis ? SkinningMode.LiveBones : SkinningMode.BakedTexture;
        }

        private void SwitchMode(SkinningMode mode)
        {
            _currentMode = mode;
        }

        private void DisposeAll()
        {
            _liveRenderer?.Dispose();
            _liveRenderer = null;
            _bakedPlayer = null;
            _bakedPropertyBlock = null;
            _currentMode = SkinningMode.None;

            if (_liveMaterial != null) { SafeDestroy(_liveMaterial); _liveMaterial = null; }
            if (_bakedMaterial != null) { SafeDestroy(_bakedMaterial); _bakedMaterial = null; }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
