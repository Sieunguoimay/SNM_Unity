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
        public IReadOnlyList<MaterialBlock> MaterialBlockList => _lodInfoList[0].materialBlockList;
        public Transform RootTransform => root;
        public AnimationInstancingAnimator InstancingAnimator => _instancingAnimator;
        public AnimationInstancingData InstancingData => instancingData;

        private void Start()
        {
            _instancingAnimator = GetComponent<AnimationInstancingAnimator>();
            _lodInfoList = GetLodInfoList(RootTransform.gameObject);

            RootTransform.GetComponentInChildren<Animator>().enabled = false;

            //Todo: CullingGroup
            var radius = CalcBoundingSphere(_lodInfoList[0]);
            GetAllBones(_lodInfoList[0].skinnedMeshRenderers, instancingData.boneData, RootTransform,
                out var bones,
                out _);

            AddAllRenderersToVertexCachePool(
                    _lodInfoList,
                    bones?.ToArray(),
                    instancingData.animationTextureData);

            AnimationInstancingRendererManager.Instance.RegisterAnimationInstancingRenderer(this);
        }

        private void OnDestroy()
        {
            AnimationInstancingRendererManager.Instance?.UnregisterAnimationInstancingRenderer(this);
        }

        private void AddAllRenderersToVertexCachePool(
            LodInfo[] lodInfoList,
            Transform[] allBones,
            AnimationTextureData textureData)
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
                var materialBlocks = new List<MaterialBlock>();
                var vertexCaches = new List<VertexCache>();
                for (int smrIndex = 0; smrIndex != lod.skinnedMeshRenderers.Length; ++smrIndex)
                {
                    var sharedMesh = lod.skinnedMeshRenderers[smrIndex].sharedMesh;
                    if (sharedMesh == null) continue;
                    var renderer = lod.skinnedMeshRenderers[smrIndex];

                    renderer.enabled = !AddToVertexCachePool(materialBlocks, vertexCaches, allBones,
                        textureData, renderingConfig, sharedMesh, renderer);
                }

                for (int mrIndex = 0; mrIndex != lod.meshRenderers.Length; ++mrIndex)
                {
                    var sharedMesh = lod.meshFilters[mrIndex].sharedMesh;
                    if (sharedMesh == null) continue;

                    var renderer = lod.meshRenderers[mrIndex];

                    renderer.enabled = !AddToVertexCachePool(materialBlocks, vertexCaches, allBones,
                        textureData, renderingConfig, sharedMesh, renderer);
                }
                lod.materialBlockList = materialBlocks.ToArray();
#if UNITY_EDITOR
                lod.vertexCacheList = vertexCaches.ToArray();
#endif
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private bool AddToVertexCachePool(
            List<MaterialBlock> materialBlocks,
            List<VertexCache> vertexCaches,
            Transform[] allBones,
            AnimationTextureData textureData,
            RenderingConfig renderingConfig,
            Mesh sharedMesh,
            Renderer renderer)
        {
            var renderName = RuntimeHelper.GetTransformPath(RootTransform, renderer.transform);
            var key = renderName.GetHashCode();

            if (!AnimationInstancingRendererManager.VertexCacheDic.TryGetValue(key, out var vertexCache))
            {
                var mesh = BoneAndMesh.PrepareMeshVertexData(sharedMesh, allBones, renderer,
                    RootTransform, GetBonePerVertex());

                if (mesh == null) return false;//don't add this renderer to vertexcache

                vertexCache = new VertexCache(renderName, mesh, textureData, renderingConfig);

                AnimationInstancingRendererManager.VertexCacheDic[key] = vertexCache;
            }

            var sharedMaterials = renderer.sharedMaterials;
            var identify = RuntimeHelper.GetIdentify(sharedMaterials);

            if (!vertexCache.InstanceBlockDic.TryGetValue(identify, out MaterialBlock matBlock))
            {
                var clonedMaterials = MaterialBlock.CloneMaterialsWithTextures(
                    sharedMaterials, sharedMesh.subMeshCount, textureData);

                matBlock = new MaterialBlock(clonedMaterials, sharedMaterials);
                vertexCache.InstanceBlockDic.Add(identify, matBlock);
            }

            vertexCaches.Add(vertexCache);
#if UNITY_EDITOR
            materialBlocks.Add(matBlock);
#endif
            return true;
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
            public MaterialBlock[] materialBlockList;
#if UNITY_EDITOR
            public VertexCache[] vertexCacheList;
#endif
        }
    }
}