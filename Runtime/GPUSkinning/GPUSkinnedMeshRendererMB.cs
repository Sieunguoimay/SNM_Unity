using System.Linq;
using Snm.Runtime.GPUSkinning.Serialize;
using System;

#if UNITY_EDITOR
using Snm.GPUSkinning.BoneWeightTool;
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    [ExecuteInEditMode]
    public class GPUSkinnedMeshRendererMB : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private BoneHierarchyAsset boneHierarchy;
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

#if UNITY_EDITOR
        [ContextMenu("Create Bone Transforms")]
        private void CreateBoneTransforms()
        {
            foreach (var bt in boneTransforms) bt.name += "_OBSOLETE";
            var hierarchy = boneHierarchy != null
                ? boneHierarchy.boneHierarchy.parents
                : Array.Empty<int>();
            boneTransforms = BindposeTransformsTool.CreateBoneHierarchy(
                mesh.bindposes,
                transform.localToWorldMatrix,
                hierarchy);

            EditorUtility.SetDirty(this);
            OnValidate();
        }
#endif
    }
}