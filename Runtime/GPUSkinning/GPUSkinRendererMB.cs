using System;
using System.Linq;
using Snm.Runtime.GPUSkinning.Serialize;

#if UNITY_EDITOR
using Snm.GPUSkinning.BoneWeightTool;
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// MonoBehaviour wrapper for GPU skinning with a custom mesh and skeleton.
    /// Assign a mesh, material (using any GPU skinning-compatible shader), bone transforms, and optionally a SkeletonAsset.
    /// </summary>
    [ExecuteInEditMode]
    public class GPUSkinRendererMB : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private SkeletonAsset skeleton;
        [SerializeField] private Material material;
        [SerializeField] private Transform[] boneTransforms;

        private IGPUSkinRenderer _renderer;
        private Bounds _localBounds;

        /// <summary>Fired before bone matrices are computed. Use for procedural bone manipulation (IK, ragdoll blend).</summary>
        public event Action OnBeforeSkinningUpdate;
        /// <summary>Fired after bone matrices are uploaded to GPU.</summary>
        public event Action OnAfterSkinningUpdate;

        private void OnEnable()
        {
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            _renderer?.Dispose();
            _renderer = null;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            _renderer?.Dispose();
            TryCreateRenderer();
        }

        private void TryCreateRenderer()
        {
            _renderer = null;
            if (mesh == null || material == null) return;

            var bindposes = skeleton != null
                ? skeleton.skeleton.bones.Select(b => b.bindpose).ToArray()
                : mesh.bindposes;

            var validBones = boneTransforms.Where(t => t != null).ToArray();
            var renderer = new GPUSkinRenderer(mesh, bindposes, material, validBones, transform);
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

#if UNITY_EDITOR
        [ContextMenu("Create Bone Transforms")]
        private void CreateBoneTransforms()
        {
            foreach (var bt in boneTransforms) bt.name += "_OBSOLETE";
            var hierarchy = skeleton != null
                ? skeleton.skeleton.bones.Select(b => b.parent).ToArray()
                : Array.Empty<int>();

            boneTransforms = BoneTransformsTool.CreateBoneHierarchy(
                mesh.bindposes,
                transform.localToWorldMatrix,
                hierarchy);

            EditorUtility.SetDirty(this);
            OnValidate();
        }
#endif
    }
}
