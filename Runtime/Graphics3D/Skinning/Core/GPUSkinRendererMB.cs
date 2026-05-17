using System;
using System.Linq;
using Snm.Runtime.Foundation;
using UnityEngine;

namespace Snm.Graphics3D.GPUSkinning
{
    /// <summary>
    /// MonoBehaviour wrapper for GPU skinning with a custom mesh and skeleton.
    /// Assign a mesh, material (using any GPU skinning-compatible shader), bone transforms, and optionally a SkeletonAsset.
    /// </summary>
    [ExecuteInEditMode]
    public partial class GPUSkinRendererMB : MonoBehaviour
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

        [SerializeField] private Mesh mesh;
        [SerializeField] private SkeletonAsset skeleton;
        [SerializeField] private Material material;
        [SerializeField] private Transform[] boneTransforms;

        private IGPUSkinRenderer _renderer;
        private Material _runtimeMaterial;
        private Bounds _localBounds;

        /// <summary>Fired before bone matrices are computed. Use for procedural bone manipulation (IK, ragdoll blend).</summary>
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
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            _renderer?.Dispose();
            _renderer = null;
            if (_runtimeMaterial != null) { Destroy(_runtimeMaterial); _runtimeMaterial = null; }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            _renderer?.Dispose();
            _renderer = null;
            if (_runtimeMaterial != null) { Destroy(_runtimeMaterial); _runtimeMaterial = null; }
            TryCreateRenderer();
        }

        private void TryCreateRenderer()
        {
            _renderer = null;
            if (mesh == null || material == null) return;

            var bindposes = skeleton != null
                ? skeleton.skeleton.bones.Select(b => b.bindpose).ToArray()
                : mesh.bindposes;

            // Preserve index alignment: replace null bones with the mesh transform
            // instead of filtering, so bone indices in mesh weights stay correct.
            var safeBones = new Transform[boneTransforms.Length];
            for (int i = 0; i < boneTransforms.Length; i++)
                safeBones[i] = boneTransforms[i] != null ? boneTransforms[i] : transform;

            _runtimeMaterial = Instantiate(material);
            var renderer = new GPUSkinRenderer(mesh, bindposes, _runtimeMaterial, safeBones, transform);
            renderer.SetupMesh();
            _renderer = renderer;
            _localBounds = mesh.bounds;
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
            var cam = CameraProvider.Current;
            if (cam == null) return true;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
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
