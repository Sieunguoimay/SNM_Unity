using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingRenderer : MonoBehaviour
    {
        [SerializeField] private AnimationData animationData;
        [SerializeField] private int bonePerVertex = 2;
        [SerializeField] private ShadowCastingMode shadowCastingMode;
        [SerializeField] private bool receiveShadow;
        [SerializeField] private AnimationInstancingAnimator instancingAnimator;

        private LodInfo[] _lodInfoList;
        public IReadOnlyList<LodInfo> LodInfoList => _lodInfoList;

        private Transform _transform;
        public Transform Transform => _transform ??= GetComponentInChildren<Animator>().transform;
        public AnimationInstancingAnimator InstancingAnimator => instancingAnimator;

        private void Start()
        {
            _lodInfoList = GetLodInfoList(gameObject);

            DisableDefaultRenderersAndAnimator(_lodInfoList);

            //Todo: CullingGroup
            var radius = CalcBoundingSphere(_lodInfoList[0]);

            InitializeAnimation(_lodInfoList, GetBonePerVertex());

            UpdateLodVertexCaches(_lodInfoList);

            AnimationInstancingRendererManager.Instance.RegisterAnimationInstancingRenderer(this);

            instancingAnimator.SetAnimInfoList(animationData.animInfoList);
        }

        private void OnDestroy()
        {
            AnimationInstancingRendererManager.Instance?.UnregisterAnimationInstancingRenderer(this);
        }

        private void UpdateLodVertexCaches(LodInfo[] lodInfoList)
        {
            foreach (var lod in lodInfoList)
            {
                foreach (var cache in lod.vertexCacheList)
                {
                    cache.shadowcastingMode = shadowCastingMode;
                    cache.receiveShadow = receiveShadow;
                    cache.layer = gameObject.layer;
                }
            }
        }

        private int GetBonePerVertex()
        {
            return QualitySettings.skinWeights switch
            {
                SkinWeights.TwoBones => bonePerVertex > 2 ? 2 : bonePerVertex,
                SkinWeights.OneBone => 1,
                _ => 1,
            };
        }

        private void InitializeAnimation(LodInfo[] lodInfoList, int bonePerVertex)
        {
            GetAllBones(lodInfoList[0].skinnedMeshRenderer, animationData.extraBoneInfo, gameObject,
                out var bones,
                out var bindPose);

            AddToVertexCachePool(
                    lodInfoList,
                    bones?.ToArray(),
                    bindPose?.ToArray(),
                    animationData.animationTextureData,
                    bonePerVertex,
                    null);
        }

        public static void AddToVertexCachePool(
            LodInfo[] lodInfoList,
            Transform[] allBones,
            Matrix4x4[] bindPose,
            AnimationTextureData textureData,
            int bonePerVertex,
            string alias)
        {
            UnityEngine.Profiling.Profiler.BeginSample("AddMeshVertex()");
            for (int lodIndex = 0; lodIndex != lodInfoList.Length; ++lodIndex)
            {
                var lod = lodInfoList[lodIndex];
                for (int smrIndex = 0; smrIndex != lod.skinnedMeshRenderer.Length; ++smrIndex)
                {
                    var mesh = lod.skinnedMeshRenderer[smrIndex].sharedMesh;
                    if (mesh == null) continue;

                    var render = lod.skinnedMeshRenderer[smrIndex];
                    int renderName = lod.skinnedMeshRenderer[smrIndex].name.GetHashCode();
                    int aliasName = 0;
                    var materials = lod.skinnedMeshRenderer[smrIndex].sharedMaterials;
                    var rendererIndex = smrIndex;

                    AnimationInstancingPool.CreateMaterialBlockAndVertexCache(
                        allBones, bindPose, textureData, bonePerVertex, mesh, render,
                        renderName, aliasName, materials, out var vertexCache, out var matBlock);

                    lod.materialBlockList[rendererIndex] = matBlock;
                    lod.vertexCacheList[rendererIndex] = vertexCache;
                }

                for (int mrIndex = 0; mrIndex != lod.meshRenderer.Length; ++mrIndex)
                {
                    var mesh = lod.meshFilter[mrIndex].sharedMesh;
                    if (mesh == null) continue;

                    var render = lod.meshRenderer[mrIndex];
                    int renderName = render.name.GetHashCode();
                    int aliasName = alias != null ? alias.GetHashCode() : 0;
                    var materials = render.sharedMaterials;
                    var rendererIndex = lod.skinnedMeshRenderer.Length + mrIndex;

                    AnimationInstancingPool.CreateMaterialBlockAndVertexCache(
                        allBones, bindPose, textureData, bonePerVertex, mesh,
                        render, renderName, aliasName, materials, out var vertexCache, out var matBlock);

                    lod.materialBlockList[rendererIndex] = matBlock;
                    lod.vertexCacheList[rendererIndex] = vertexCache;
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void DisableDefaultRenderersAndAnimator(LodInfo[] lodInfoList)
        {
            foreach (var lod in lodInfoList)
            {
                foreach (var v in lod.meshRenderer)
                {
                    v.enabled = false;
                }
                foreach (var v1 in lod.skinnedMeshRenderer)
                {
                    v1.enabled = false;
                }
            }

            var animator = GetComponentInChildren<Animator>();
            animator.enabled = false;
        }

        private static LodInfo[] GetLodInfoList(GameObject go)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Calculate lod");
            LodInfo[] lodInfo;
            if (go.TryGetComponent<LODGroup>(out var lod))
            {
                lodInfo = new LodInfo[lod.lodCount];
                var lods = lod.GetLODs();
                for (int i = 0; i != lods.Length; ++i)
                {
                    if (lods[i].renderers == null)
                    {
                        continue;
                    }
                    var n = lods[i].renderers.Length;
                    var listSkinnedMeshRenderer = new List<SkinnedMeshRenderer>();
                    var listMeshRenderer = new List<MeshRenderer>();
                    foreach (var render in lods[i].renderers)
                    {
                        if (render is SkinnedMeshRenderer smr)
                            listSkinnedMeshRenderer.Add(smr);
                        if (render is MeshRenderer mr)
                            listMeshRenderer.Add(mr);
                    }

                    var info = new LodInfo
                    {
                        lodLevel = i,
                        skinnedMeshRenderer = listSkinnedMeshRenderer.ToArray(),
                        meshRenderer = listMeshRenderer.ToArray(),
                        meshFilter = listMeshRenderer.Select(mr => mr.GetComponent<MeshFilter>()).ToArray(),
                        vertexCacheList = new VertexCache[n],
                        materialBlockList = new MaterialBlock[n],
                    };
                    lodInfo[i] = info;
                }
            }
            else
            {
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                var mrs = go.GetComponentsInChildren<MeshRenderer>();
                var mfs = go.GetComponentsInChildren<MeshFilter>();
                var n = smrs.Length + mrs.Length;
                var info = new LodInfo
                {
                    lodLevel = 0,
                    skinnedMeshRenderer = smrs,
                    meshRenderer = mrs,
                    meshFilter = mfs,
                    vertexCacheList = new VertexCache[n],
                    materialBlockList = new MaterialBlock[n]
                };
                lodInfo = new LodInfo[1] { info };
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return lodInfo;
        }

        private static float CalcBoundingSphere(LodInfo info)
        {
            UnityEngine.Profiling.Profiler.BeginSample("CalcBoundingSphere()");
            var bound = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            for (int i = 0; i != info.meshRenderer.Length; ++i)
            {
                var meshRenderer = info.meshRenderer[i];
                bound.Encapsulate(meshRenderer.bounds);
            }
            for (int i = 0; i != info.skinnedMeshRenderer.Length; ++i)
            {
                var skinnedMeshRenderer = info.skinnedMeshRenderer[i];
                bound.Encapsulate(skinnedMeshRenderer.bounds);
            }
            var radius = bound.size.x > bound.size.y ? bound.size.x : bound.size.y;
            radius = radius > bound.size.z ? radius : bound.size.z;
            UnityEngine.Profiling.Profiler.EndSample();
            return radius;
        }

        public static void GetAllBones(
            SkinnedMeshRenderer[] skinnedMeshRenderers,
            ExtraBoneInfo extraBoneInfo,
            GameObject gameObject,
            out List<Transform> boneList,
            out List<Matrix4x4> bindPoseList)
        {
            // if (skinnedMeshRenderers.Length == 0)
            // {
            //     boneList = null;
            //     bindPoseList = null;
            //     return;
            // }

            boneList = new List<Transform>();
            bindPoseList = new List<Matrix4x4>();
            
            if (skinnedMeshRenderers.Length > 0)
            {
                RuntimeHelper.MergeBone(skinnedMeshRenderers, out boneList, out bindPoseList);
            }

            if (extraBoneInfo != null)
            {
                var transforms = gameObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i != extraBoneInfo.extraBoneNames.Length; ++i)
                {
                    for (int j = 0; j != transforms.Length; ++j)
                    {
                        if (extraBoneInfo.extraBoneNames[i] == transforms[j].name)
                        {
                            boneList.Add(transforms[j]);
                        }
                    }
                    bindPoseList.Add(extraBoneInfo.extraBindPoseMatrices[i]);
                }
            }
        }

        [UnityEditor.CustomEditor(typeof(AnimationInstancingRenderer))]
        private class _Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                var renderer = target as AnimationInstancingRenderer;
                if (renderer.LodInfoList == null) return;
                foreach (var lod in renderer.LodInfoList)
                {
                    UnityEditor.EditorGUILayout.LabelField($"VertexCacheList:");
                    for (int i = 0; i < lod.vertexCacheList.Length; i++)
                    {
                        var vc = lod.vertexCacheList[i];
                        UnityEditor.EditorGUILayout.LabelField($"VertexCache {i} [{vc.GetHashCode()}]:");
                        UnityEditor.EditorGUILayout.ObjectField("->mesh", vc.mesh, typeof(Mesh), true);
                        foreach (var m in vc.materials)
                        {
                            UnityEditor.EditorGUILayout.ObjectField("->material", m, typeof(Material), true);
                        }
                        for (int i1 = 0; i1 < vc.bonePose.Length; i1++)
                        {
                            var m = vc.bonePose[i1];
                            UnityEditor.EditorGUILayout.ObjectField("->bonePose", m, typeof(Transform), true);
                        }
                        UnityEditor.EditorGUILayout.LabelField($"->bone weights ({vc.weight.Length}) {string.Join(",", vc.weight)}");
                        UnityEditor.EditorGUILayout.LabelField($"->bone indices ({vc.boneIndex.Length}) {string.Join(",", vc.boneIndex)}");
                        UnityEditor.EditorGUILayout.LabelField($"->instanceBlockList: {string.Join(",", vc.instanceBlockList.Select(b => $"({b.Key}={b.Value.GetHashCode()})"))}");
                    }
                    UnityEditor.EditorGUILayout.LabelField($"MaterialBlockList:");
                    for (int i = 0; i < lod.materialBlockList.Length; i++)
                    {
                        var block = lod.materialBlockList[i];
                        UnityEditor.EditorGUILayout.LabelField($"Block {i}-{block.GetHashCode()}:");
                        UnityEditor.EditorGUILayout.LabelField($"->runtimePackageIndex: {string.Join(",", block.runtimePackageIndex)}");
                        UnityEditor.EditorGUILayout.LabelField($"->packageLists: {block.packageLists.Length}");
                        foreach (var pl in block.packageLists)
                        {
                            UnityEditor.EditorGUILayout.LabelField($"->->packageList: {pl.Count}");
                            foreach (var p in pl)
                            {
                                UnityEditor.EditorGUILayout.LabelField($"->->->package: textureIndex={p.animationTextureIndex} instancingCount={p.instancingCount} subMeshCount={p.subMeshCount}");
                            }
                        }
                    }
                }
            }
        }
    }

    public class LodInfo
    {
        public int lodLevel;
        public SkinnedMeshRenderer[] skinnedMeshRenderer;
        public MeshRenderer[] meshRenderer;
        public MeshFilter[] meshFilter;
        public VertexCache[] vertexCacheList;
        public MaterialBlock[] materialBlockList;
    }
}