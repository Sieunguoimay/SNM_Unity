using System;
using System.Linq;
using Snm.AnimationInstancing;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Hybrid GPU skinning MonoBehaviour that switches between live-bone and baked-texture modes
    /// based on camera distance. Close = full procedural control. Far = baked texture, near-zero CPU.
    /// </summary>
    [ExecuteInEditMode]
    public class HybridSkinRendererMB : MonoBehaviour
    {
        [Header("Mesh & Material")]
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;

        [Header("Live Bones (close-up mode)")]
        [SerializeField] private SkeletonAsset skeleton;
        [SerializeField] private Transform[] boneTransforms;

        [Header("Baked Animation (crowd mode)")]
        [SerializeField] private AnimationInstancingData bakedAnimationData;
        [SerializeField] private int defaultAnimation;

        [Header("LOD Switching")]
        [SerializeField] private float lodSwitchDistance = 30f;
        [SerializeField] private float switchHysteresis = 2f;

        private GPUSkinRenderer _liveRenderer;
        private BakedAnimationSkinRenderer _bakedRenderer;
        private IGPUSkinRenderer _activeRenderer;
        private SkinningMode _currentMode = SkinningMode.None;
        private Material _liveMaterial;
        private Material _bakedMaterial;

        /// <summary>Fired before bone matrices are computed (live mode only).</summary>
        public event Action OnBeforeSkinningUpdate;
        /// <summary>Fired after skinning update.</summary>
        public event Action OnAfterSkinningUpdate;

        /// <summary>Access the baked animation player for playback control (Play, CrossFade, etc.).</summary>
        public BakedAnimationPlayer BakedPlayer => _bakedRenderer?.Player;
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

            // Create live-bone renderer if bones are available
            if (boneTransforms != null && boneTransforms.Length > 0)
            {
                var bindposes = skeleton != null
                    ? skeleton.skeleton.bones.Select(b => b.bindpose).ToArray()
                    : mesh.bindposes;
                var validBones = boneTransforms.Where(t => t != null).ToArray();

                _liveMaterial = Instantiate(material);
                _liveMaterial.EnableKeyword("GPU_SKINNING_ON");
                _liveMaterial.DisableKeyword("BAKED_SKINNING_ON");

                _liveRenderer = new GPUSkinRenderer(mesh, bindposes, _liveMaterial, validBones, transform);
                _liveRenderer.SetupMesh();
            }

            // Create baked-texture renderer if baked data is available
            if (bakedAnimationData != null)
            {
                _bakedMaterial = Instantiate(material);
                _bakedMaterial.EnableKeyword("BAKED_SKINNING_ON");
                _bakedMaterial.DisableKeyword("GPU_SKINNING_ON");

                _bakedRenderer = new BakedAnimationSkinRenderer(mesh, _bakedMaterial, transform, bakedAnimationData);
                _bakedRenderer.SetupMesh();
                _bakedRenderer.Player.Play(defaultAnimation);
            }

            // Start with best available mode
            SwitchMode(GetDesiredMode());
        }

        private void LateUpdate()
        {
            if (_activeRenderer == null) return;

            var desiredMode = GetDesiredMode();
            if (desiredMode != _currentMode)
                SwitchMode(desiredMode);

            OnBeforeSkinningUpdate?.Invoke();
            _activeRenderer.UpdateSkinning();
            OnAfterSkinningUpdate?.Invoke();
            _activeRenderer.Render();
        }

        private SkinningMode GetDesiredMode()
        {
            if (_liveRenderer == null && _bakedRenderer == null)
                return SkinningMode.None;
            if (_liveRenderer == null)
                return SkinningMode.BakedTexture;
            if (_bakedRenderer == null)
                return SkinningMode.LiveBones;

            if (Camera.main == null)
                return SkinningMode.LiveBones;

            float dist = Vector3.Distance(transform.position, Camera.main.transform.position);

            // Hysteresis to prevent mode flickering at boundary
            if (_currentMode == SkinningMode.LiveBones)
                return dist > lodSwitchDistance + switchHysteresis ? SkinningMode.BakedTexture : SkinningMode.LiveBones;
            else
                return dist < lodSwitchDistance - switchHysteresis ? SkinningMode.LiveBones : SkinningMode.BakedTexture;
        }

        private void SwitchMode(SkinningMode mode)
        {
            _currentMode = mode;
            _activeRenderer = mode switch
            {
                SkinningMode.LiveBones => _liveRenderer,
                SkinningMode.BakedTexture => _bakedRenderer,
                _ => null
            };
        }

        private void DisposeAll()
        {
            _liveRenderer?.Dispose();
            _bakedRenderer?.Dispose();
            _liveRenderer = null;
            _bakedRenderer = null;
            _activeRenderer = null;
            _currentMode = SkinningMode.None;

            if (_liveMaterial != null) { DestroyImmediate(_liveMaterial); _liveMaterial = null; }
            if (_bakedMaterial != null) { DestroyImmediate(_bakedMaterial); _bakedMaterial = null; }
        }
    }
}
