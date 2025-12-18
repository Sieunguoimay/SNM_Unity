using System.Linq;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    [ExecuteInEditMode]
    public class GPUSkinnedMeshRendererMB_FromUnitySMR : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer unitySMR;
        [SerializeField] private Shader gpuSkinningShader;

        private GPUSkinnedMeshRenderer _gpuSMR;
        private Material _material;

        private void OnEnable()
        {
            TryCreateGPUSMR();
        }

        private void OnDisable()
        {
            TryDestroyGPUSMR();
        }

        private void TryDestroyGPUSMR()
        {
            if (_gpuSMR == null) return;

            if (Application.IsPlaying(this)) Destroy(_material);
            else DestroyImmediate(_material);

            _material = null;
            _gpuSMR = null;

            unitySMR.enabled = true;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            TryDestroyGPUSMR();
            TryCreateGPUSMR();
        }

        private void TryCreateGPUSMR()
        {
            if (unitySMR == null || gpuSkinningShader == null) return;
            if (unitySMR.sharedMesh == null || unitySMR.sharedMaterial == null) return;

            _material = Instantiate(unitySMR.sharedMaterial);
            _material.shader = gpuSkinningShader;

            var mesh = unitySMR.sharedMesh;
            var boneTransforms = unitySMR.bones;
            var meshTransform = unitySMR.transform;

            _gpuSMR = new GPUSkinnedMeshRenderer(mesh, _material, boneTransforms.Where(t => t != null).ToArray(), meshTransform);
            _gpuSMR.SetupMesh();

            unitySMR.enabled = false;
        }

        private void LateUpdate()
        {
            if (_gpuSMR == null) return;
            _gpuSMR.SetupMaterial();
            _gpuSMR.Render();
        }
    }
}