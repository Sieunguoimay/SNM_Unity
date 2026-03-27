using System;
using System.Linq;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Replaces a standard Unity SkinnedMeshRenderer with GPU skinning at runtime.
    /// Clones the SMR's material, swaps to GPU skinning shader, and disables the original SMR.
    /// </summary>
    [ExecuteInEditMode]
    public partial class GPUSkinReplacementRendererMB : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer unitySMR;
        [SerializeField] private Shader gpuSkinningShader;

        private IGPUSkinRenderer _renderer;
        private Material _material;
        private Bounds _localBounds;

        /// <summary>Fired before bone matrices are computed.</summary>
        public event Action OnBeforeSkinningUpdate;
        /// <summary>Fired after bone matrices are uploaded to GPU.</summary>
        public event Action OnAfterSkinningUpdate;

        /// <summary>Number of blend shapes available on the mesh.</summary>
        public int BlendShapeCount => _renderer?.BlendShapeCount ?? 0;

        /// <summary>
        /// Sets the weight for a blend shape by index.
        /// </summary>
        public void SetBlendShapeWeight(int shapeIndex, float weight)
        {
            _renderer?.SetBlendShapeWeight(shapeIndex, weight);
        }

        private void OnEnable()
        {
            TryCreate();
        }

        private void OnDisable()
        {
            TryDestroy();
        }

        private void TryCreate()
        {
            if (unitySMR == null || gpuSkinningShader == null) return;
            if (unitySMR.sharedMesh == null || unitySMR.sharedMaterial == null) return;

            _material = Instantiate(unitySMR.sharedMaterial);
            _material.shader = gpuSkinningShader;

            var mesh = unitySMR.sharedMesh;
            var boneTransforms = unitySMR.bones.Where(t => t != null).ToArray();

            var renderer = new GPUSkinRenderer(mesh, mesh.bindposes, _material, boneTransforms, unitySMR.transform);
            renderer.SetupMesh();
            _renderer = renderer;
            _localBounds = mesh.bounds;

            unitySMR.enabled = false;
        }

        private void TryDestroy()
        {
            if (_renderer == null) return;

            _renderer.Dispose();
            UnityEngineUtility.DestroyObject(_material);

            _material = null;
            _renderer = null;

            if (unitySMR != null)
                unitySMR.enabled = true;
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;
            if (!IsVisible()) return;

            OnBeforeSkinningUpdate?.Invoke();
            _renderer.UpdateSkinning();
            OnAfterSkinningUpdate?.Invoke();
            _renderer.Render();
        }

        private bool IsVisible()
        {
            if (Camera.main == null) return true;
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            var worldBounds = TransformBounds(_localBounds);
            return GeometryUtility.TestPlanesAABB(planes, worldBounds);
        }

        private Bounds TransformBounds(Bounds localBounds)
        {
            var center = transform.TransformPoint(localBounds.center);
            var extents = localBounds.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);
            var worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, worldExtents * 2f);
        }
    }
}
