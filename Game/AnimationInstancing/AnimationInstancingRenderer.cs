using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static AnimationInstancing_v2.AnimationInstancingHelper;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingRenderer : MonoBehaviour
    {
        [SerializeField] private InstanceAnimationData animationData;
        [SerializeField] private int bonePerVertex = 2;
        [SerializeField] private ShadowCastingMode shadowCastingMode;
        [SerializeField] private bool receiveShadow;

        private LodInfo[] _lodInfoList;
        public IReadOnlyList<LodInfo> LodInfoList => _lodInfoList;

        private void Start()
        {
            _lodInfoList = GetLodInfoList(gameObject);

            DisableDefaultRenderersAndAnimator(_lodInfoList);

            //Todo: CullingGroup
            var radius = CalcBoundingSphere(_lodInfoList[0]);

            InitializeAnimation(_lodInfoList, GetBonePerVertex());

            UpdateLodVertexCaches(_lodInfoList);

            AnimationInstancingRendererManager.Instance.RegisterAnimationInstancingRenderer(this);
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
            var vertexCachePool = AnimationInstancingRendererManager.Instance.vertexCachePool;

            GetAllBones(lodInfoList, animationData.extraBoneInfo, gameObject,
                out var bones,
                out var bindPose);

            AnimationInstancingHelper.AddToVertexCachePool(
                    vertexCachePool,
                    lodInfoList,
                    bones?.ToArray(),
                    bindPose?.ToArray(),
                    animationData.animationTextureData,
                    bonePerVertex,
                    null);

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
            LodInfo[] lodInfoList,
            ExtraBoneInfo extraBoneInfo,
            GameObject gameObject,
            out List<Transform> boneList,
            out List<Matrix4x4> bindPoseList)
        {
            if (lodInfoList[0].skinnedMeshRenderer.Length == 0)
            {
                boneList = null;
                bindPoseList = null;
                return;
            }

            RuntimeHelper.MergeBone(lodInfoList[0].skinnedMeshRenderer, out boneList, out bindPoseList);

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


    public class VertexCache
    {
        public int nameCode;
        public Mesh mesh = null;
        public Dictionary<int, MaterialBlock> instanceBlockList;
        public Vector4[] weight;
        public Vector4[] boneIndex;
        public Material[] materials = null;
        public Matrix4x4[] bindPose;
        public Transform[] bonePose;
        //public int boneTextureIndex = -1;

        // these are temporary, should be moved to InstancingPackage
        public ShadowCastingMode shadowcastingMode;
        public bool receiveShadow;
        public int layer;
    }
    public class MaterialBlock
    {
        public InstanceData instanceData;
        public int[] runtimePackageIndex;
        // array[index base on texture][package index]
        public List<InstancingPackage>[] packageList;
    }

    public class InstanceData
    {
        public List<Matrix4x4[]>[] worldMatrix;
        public List<float[]>[] frameIndex;
        public List<float[]>[] preFrameIndex;
        public List<float[]>[] transitionProgress;
    }

    public class InstancingPackage
    {
        public Material[] material;
        public int animationTextureIndex = 0;
        public int subMeshCount = 1;
        public int instancingCount;
        public int size;
        public MaterialPropertyBlock propertyBlock;
    }
}