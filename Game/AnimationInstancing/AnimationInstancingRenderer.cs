using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimationInstancing_v2
{
    public partial class AnimationInstancingRenderer : MonoBehaviour
    {
        [SerializeField] private AnimationInstancingData instancingData;
        [SerializeField] private Transform root;
        [SerializeField] private int bonePerVertex = 2;
        [SerializeField] private ShadowCastingMode shadowCastingMode;
        [SerializeField] private bool receiveShadow;

        [NonSerialized] private LodInfo[] _lodInfoList;
        [NonSerialized] private AnimationInstancingAnimator _instancingAnimator;

        // public IReadOnlyList<VertexCache> VertexCacheList => _lodInfoList[0].vertexCacheList;
        public IReadOnlyList<MaterialBlock> MaterialBlockList => _lodInfoList[0].materialBlockList;
        public Transform RootTransform => root;
        public AnimationInstancingAnimator InstancingAnimator => _instancingAnimator;
        public AnimationInstancingData InstancingData => instancingData;

        private void Start()
        {
            _instancingAnimator = GetComponent<AnimationInstancingAnimator>();
            _lodInfoList = GetLodInfoList(gameObject);

            DisableDefaultRenderersAndAnimator(_lodInfoList);

            //Todo: CullingGroup
            var radius = CalcBoundingSphere(_lodInfoList[0]);

            GetAllBones(_lodInfoList[0].skinnedMeshRenderers, instancingData.boneData, RootTransform,
                out var bones,
                out var bindPose);

            AddToVertexCachePool(
                    _lodInfoList,
                    bones?.ToArray(),
                    bindPose?.ToArray(),
                    instancingData.animationTextureData,
                    GetBonePerVertex(),
                    null);

            AnimationInstancingRendererManager.Instance.RegisterAnimationInstancingRenderer(this);
        }

        private void OnDestroy()
        {
            AnimationInstancingRendererManager.Instance?.UnregisterAnimationInstancingRenderer(this);
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

        private void AddToVertexCachePool(
            LodInfo[] lodInfoList,
            Transform[] allBones,
            Matrix4x4[] bindPose,
            AnimationTextureData textureData,
            int bonePerVertex,
            string alias)
        {
            var renderingConfig = new RenderingConfig
            {
                shadowcastingMode = shadowCastingMode,
                receiveShadow = receiveShadow,
                layer = gameObject.layer,
            };
            UnityEngine.Profiling.Profiler.BeginSample("AddMeshVertex()");
            for (int lodIndex = 0; lodIndex != lodInfoList.Length; ++lodIndex)
            {
                var lod = lodInfoList[lodIndex];
                for (int smrIndex = 0; smrIndex != lod.skinnedMeshRenderers.Length; ++smrIndex)
                {
                    var sharedMesh = lod.skinnedMeshRenderers[smrIndex].sharedMesh;
                    if (sharedMesh == null) continue;

                    var render = lod.skinnedMeshRenderers[smrIndex];
                    int renderName = lod.skinnedMeshRenderers[smrIndex].name.GetHashCode();
                    int aliasName = 0;
                    var sharedMaterials = lod.skinnedMeshRenderers[smrIndex].sharedMaterials;
                    var rendererIndex = smrIndex;
                    int identify = RuntimeHelper.GetIdentify(sharedMaterials);

                    var key = renderName + aliasName;

                    AddToVertexCachePool(lod, rendererIndex, allBones, bindPose,
                        textureData, renderingConfig, sharedMesh,
                        render, sharedMaterials, identify, key);

                    // lod.vertexCacheList[rendererIndex] = vertexCache;
                }

                for (int mrIndex = 0; mrIndex != lod.meshRenderers.Length; ++mrIndex)
                {
                    var sharedMesh = lod.meshFilters[mrIndex].sharedMesh;
                    if (sharedMesh == null) continue;

                    var render = lod.meshRenderers[mrIndex];
                    int renderName = render.name.GetHashCode();
                    int aliasName = alias != null ? alias.GetHashCode() : 0;
                    var sharedMaterials = render.sharedMaterials;
                    var rendererIndex = lod.skinnedMeshRenderers.Length + mrIndex;
                    int identify = RuntimeHelper.GetIdentify(sharedMaterials);

                    var key = renderName + aliasName;

                    AddToVertexCachePool(lod, rendererIndex, allBones, bindPose,
                        textureData, renderingConfig, sharedMesh,
                        render, sharedMaterials, identify, key);
                    // lod.vertexCacheList[rendererIndex] = vertexCache;
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void AddToVertexCachePool(
            LodInfo lod, int index,
            Transform[] allBones,
            Matrix4x4[] bindPose,
            AnimationTextureData textureData,
            RenderingConfig renderingConfig,
            Mesh sharedMesh,
            Renderer render,
            Material[] sharedMaterials,
            int identify, int key)
        {
            if (!AnimationInstancingRendererManager.VertexCacheDic.TryGetValue(key, out var vertexCache))
            {
                var boneAndMesh = new BoneAndMesh(sharedMesh, allBones, bindPose, render, bonePerVertex);
                
                vertexCache = new VertexCache(boneAndMesh, textureData, sharedMaterials, renderingConfig);

                AnimationInstancingRendererManager.VertexCacheDic[key] = vertexCache;
            }

            if (!vertexCache.InstanceBlockDic.TryGetValue(identify, out MaterialBlock matBlock))
            {
                vertexCache.InstanceBlockDic.Add(identify, 
                    new MaterialBlock(sharedMaterials, sharedMesh.subMeshCount, textureData));
            }
            lod.materialBlockList[index] = matBlock;
        }

        private void DisableDefaultRenderersAndAnimator(LodInfo[] lodInfoList)
        {
            foreach (var lod in lodInfoList)
            {
                foreach (var v in lod.meshRenderers)
                {
                    v.enabled = false;
                }
                foreach (var v1 in lod.skinnedMeshRenderers)
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
                    if (lods[i].renderers == null) continue;

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

                    lodInfo[i] = new LodInfo
                    {
                        lodLevel = i,
                        skinnedMeshRenderers = listSkinnedMeshRenderer.ToArray(),
                        meshRenderers = listMeshRenderer.ToArray(),
                        meshFilters = listMeshRenderer.Select(mr => mr.GetComponent<MeshFilter>()).ToArray(),
                        // vertexCacheList = new VertexCache[n],
                        materialBlockList = new MaterialBlock[n],
                    };
                }
            }
            else
            {
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                var mrs = go.GetComponentsInChildren<MeshRenderer>();
                var mfs = go.GetComponentsInChildren<MeshFilter>();
                var n = smrs.Length + mrs.Length;
                lodInfo = new LodInfo[1] {
                    new() {
                        lodLevel = 0,
                        skinnedMeshRenderers = smrs,
                        meshRenderers = mrs,
                        meshFilters = mfs,
                        // vertexCacheList = new VertexCache[n],
                        materialBlockList = new MaterialBlock[n]
                    }
                };
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return lodInfo;
        }

        private static float CalcBoundingSphere(LodInfo info)
        {
            UnityEngine.Profiling.Profiler.BeginSample("CalcBoundingSphere()");
            var bound = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
            for (int i = 0; i != info.meshRenderers.Length; ++i)
            {
                var meshRenderer = info.meshRenderers[i];
                bound.Encapsulate(meshRenderer.bounds);
            }
            for (int i = 0; i != info.skinnedMeshRenderers.Length; ++i)
            {
                var skinnedMeshRenderer = info.skinnedMeshRenderers[i];
                bound.Encapsulate(skinnedMeshRenderer.bounds);
            }
            var radius = bound.size.x > bound.size.y ? bound.size.x : bound.size.y;
            radius = radius > bound.size.z ? radius : bound.size.z;
            UnityEngine.Profiling.Profiler.EndSample();
            return radius;
        }

        public static void GetAllBones(
            SkinnedMeshRenderer[] skinnedMeshRenderers,
            ExtraBoneData extraBoneInfo,
            Transform root,
            out List<Transform> boneList,
            out List<Matrix4x4> bindPoseList)
        {
            RuntimeHelper.MergeBone(skinnedMeshRenderers, out boneList, out bindPoseList);

            if (extraBoneInfo != null)
            {
                foreach (string path in extraBoneInfo.extraBones)
                {
                    var found = RuntimeHelper.GetTransformAtPath(root, path.Split("/"));
                    if (found != null)
                    {
                        boneList.Add(found);
                    }
                }

                bindPoseList.AddRange(extraBoneInfo.extraBindPoses);

                Debug.Assert(bindPoseList.Count == boneList.Count, $"GetAllBones: Bone data lists size error bindPoseList={bindPoseList.Count} boneList={boneList.Count}");
            }
        }

        private class LodInfo
        {
            public int lodLevel;
            public SkinnedMeshRenderer[] skinnedMeshRenderers;
            public MeshRenderer[] meshRenderers;
            public MeshFilter[] meshFilters;
            // public VertexCache[] vertexCacheList;
            public MaterialBlock[] materialBlockList;
        }
    }
}