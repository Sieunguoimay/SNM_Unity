using System.Linq;
using Snm.GPUSkinning.BoneWeightTool;
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
            TryCreateRenderer();
        }

        private void TryCreateRenderer()
        {
            _renderer = null;
            if (mesh == null || material == null) return;
            _renderer = new GPUSkinnedMeshRenderer(mesh, material, boneTransforms.Where(t => t != null).ToArray(), transform);
            _renderer.SetupMesh();
        }

        private void Update()
        {
            if (_renderer == null) return;
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;
            _renderer.SetupMaterial();
            _renderer.Render();
        }
    }
}