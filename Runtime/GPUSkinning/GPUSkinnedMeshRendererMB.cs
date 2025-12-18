using System.Linq;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    [ExecuteInEditMode]
    public class GPUSkinnedMeshRendererMB : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private Transform[] boneTransforms;

        private GPUSkinnedMeshRenderer _renderer;

        private void OnEnable()
        {
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            _renderer = null;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            TryCreateRenderer();
        }

        private void TryCreateRenderer()
        {
            _renderer = null;
            if (mesh == null || material == null) return;
            if (material.shader.name != "Custom/GpuSkin")
            {
                Debug.LogError("GPUSkinnedMeshRenderer: Require material with shader Custom/GpuSkin!", this);
                return;
            }
            _renderer = new GPUSkinnedMeshRenderer(mesh, material, boneTransforms.Where(t => t != null).ToArray(), transform);
            _renderer.SetupMesh();
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;
            _renderer.SetupMaterial();
            _renderer.Render();
        }
    }
}