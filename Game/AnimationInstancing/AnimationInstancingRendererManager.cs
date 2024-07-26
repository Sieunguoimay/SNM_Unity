using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingRendererManager : MonoBehaviour
    {
        private static AnimationInstancingRendererManager _instance;
        public static AnimationInstancingRendererManager Instance
        {
            get
            {
                if (_destroyed) return null;
                if (_instance == null)
                {
                    _instance = new GameObject("#AnimationInstancingRendererManager")
                        .AddComponent<AnimationInstancingRendererManager>();
                }
                return _instance;
            }
        }

        private static bool _destroyed = false;
        private readonly List<AnimationInstancingRenderer> instancingRenderers = new();
        // private Transform cameraTransform;

        // private void OnEnable()
        // {
        //     cameraTransform = Camera.main.transform;
        // }
        private void OnDestroy()
        {
            _destroyed = true;
        }

        private void Update()
        {
            ApplyBoneMatrix();
            Render();
        }

        public void RegisterAnimationInstancingRenderer(AnimationInstancingRenderer renderer)
        {
            instancingRenderers.Add(renderer);
        }

        public void UnregisterAnimationInstancingRenderer(AnimationInstancingRenderer renderer)
        {
            instancingRenderers.Add(renderer);
        }

        private void ApplyBoneMatrix()
        {
            // Vector3 cameraPosition = cameraTransform.position;
            for (int i = 0; i != instancingRenderers.Count; ++i)
            {
                var instanceRenderer = instancingRenderers[i];
                var instanceAnimator = instancingRenderers[i].InstancingAnimator;

                // if (!instanceAnimator.IsPlaying)// && instance.parentInstance == null)
                //     continue;

                // if (instance.applyRootMotion)
                //     ApplyRootMotion(instance);

                instanceAnimator.UpdateCurrentFrame();

                // instance.UpdateAnimation();
                // instance.boundingSpere.position = instance.transform.position;
                // boundingSphere[i] = instance.boundingSpere;

                // if (!instance.visible)
                //     continue;
                // instance.UpdateLod(cameraPosition);

                // AnimationInstancing.LodInfo lod = instance.lodInfo[instance.lodLevel];
                var vertexCacheList = instanceRenderer.VertexCacheList;
                var materialBlockList = instanceRenderer.MaterialBlockList;
                int aniTextureIndex = instanceAnimator.AniTextureIndex;

                // if (instance.parentInstance != null)
                //     aniTextureIndex = instance.parentInstance.aniTextureIndex;
                // else
                // aniTextureIndex = instance.aniTextureIndex;

                for (int j = 0; j != materialBlockList.Count; ++j)
                {
                    var block = materialBlockList[j];
                    Debug.Assert(block != null);

                    var packageList = block.packageLists[aniTextureIndex];
                    int packageIndex = block.runtimePackageIndex[aniTextureIndex];

                    Debug.Assert(packageIndex < packageList.Count);

                    var package = packageList[packageIndex];

                    if (package.instancingCount >= VertexCachePool.InstancingPackageSize)
                    {
                        packageIndex++;

                        block.runtimePackageIndex[aniTextureIndex] = packageIndex;

                        if (packageIndex >= packageList.Count)
                        {
                            VertexCachePool.ExtendMaterialBlockInstanceData(block, vertexCacheList[j], 1, aniTextureIndex);
                        }
                        else
                        {
                            packageList[packageIndex].instancingCount = 1;
                        }
                    }
                    else
                    {
                        package.instancingCount++;
                    }

                    package = packageList[packageIndex];

                    // if (package.instancingCount > 0) -> always true
                    // {
                    var instanceData = block.instanceData;
                    var instanceIndex = package.instancingCount - 1;

                    // if (instance.parentInstance != null)
                    // {
                    //     frameIndex = instance.parentInstance.aniInfo[instance.parentInstance.aniIndex].animationIndex + instance.parentInstance.curFrame;
                    //     if (instance.parentInstance.preAniIndex >= 0)
                    //         preFrameIndex = instance.parentInstance.aniInfo[instance.parentInstance.preAniIndex].animationIndex + instance.parentInstance.preAniFrame;
                    //     transition = instance.parentInstance.transitionProgress;
                    // }
                    // else
                    // {

                    // var preFrameIndex = -1f;
                    // var frameIndex = instance.AnimationData.animInfoList[instanceAnimator.aniIndex].animationIndex
                    //     + instanceAnimator.curFrame;
                    // if (instanceAnimator.preAniIndex >= 0)
                    //     preFrameIndex = instance.AnimationData.animInfoList[instanceAnimator.preAniIndex].animationIndex
                    //     + instanceAnimator.preAniFrame;
                    // var transition = instanceAnimator.transitionProgress;
                    // }

                    instanceData.worldMatrix[aniTextureIndex][packageIndex][instanceIndex]
                        = instanceRenderer.RootTransform.localToWorldMatrix;
                    instanceData.frameIndex[aniTextureIndex][packageIndex][instanceIndex]
                        = instanceAnimator.FrameIndex;
                    instanceData.preFrameIndex[aniTextureIndex][packageIndex][instanceIndex]
                        = instanceAnimator.PreFrameIndex;
                    instanceData.transitionProgress[aniTextureIndex][packageIndex][instanceIndex]
                        = instanceAnimator.TransitionProgress;
                    // }
                }
            }
        }

        private void Render()
        {
            foreach (var obj in VertexCachePool.VertexCacheDic)
            {
                var vertexCache = obj.Value;
                foreach (var block in vertexCache.instanceBlockList)
                {
                    var packageLists = block.Value.packageLists;

                    for (int packageListIndex = 0; packageListIndex != packageLists.Length; ++packageListIndex)
                    {
                        var packageList = packageLists[packageListIndex];

                        for (int packageIndex = 0; packageIndex != packageList.Count; ++packageIndex)
                        {
                            var package = packageList[packageIndex];

                            if (package.instancingCount == 0) continue;

                            for (int subMeshIndex = 0; subMeshIndex != package.subMeshCount; ++subMeshIndex)
                            {
#if UNITY_EDITOR
                                PreparePackageMaterial(package, vertexCache, packageListIndex);
#endif
                                var data = block.Value.instanceData;
                                DrawMeshInstanced(vertexCache, package, data, packageListIndex, packageIndex, subMeshIndex);
                            }
                            package.instancingCount = 0;
                        }
                        block.Value.runtimePackageIndex[packageListIndex] = 0;
                    }
                }
            }
        }

        private static void DrawMeshInstanced(VertexCache vertexCache,
            InstancingPackage package, InstanceData data, int k, int i, int subMeshIndex)
        {
            package.propertyBlock.SetFloatArray("frameIndex", data.frameIndex[k][i]);
            package.propertyBlock.SetFloatArray("preFrameIndex", data.preFrameIndex[k][i]);
            package.propertyBlock.SetFloatArray("transitionProgress", data.transitionProgress[k][i]);

            Graphics.DrawMeshInstanced(vertexCache.mesh,
                subMeshIndex,
                package.material[subMeshIndex],
                data.worldMatrix[k][i],
                package.instancingCount,
                package.propertyBlock,
                vertexCache.shadowcastingMode,
                vertexCache.receiveShadow,
                vertexCache.layer);
        }

        public static void PreparePackageMaterial(
            InstancingPackage package,
            VertexCache vertexCache,
            int aniTextureIndex)
        {
            if (vertexCache.textureData == null)
                return;

            for (int i = 0; i != package.subMeshCount; ++i)
            {
                var texture = vertexCache.textureData;
                package.material[i].SetTexture("_boneTexture", texture.bakedBoneTextures[aniTextureIndex]);
                package.material[i].SetInt("_boneTextureWidth", texture.bakedBoneTextures[aniTextureIndex].width);
                package.material[i].SetInt("_boneTextureHeight", texture.bakedBoneTextures[aniTextureIndex].height);
                package.material[i].SetInt("_boneTextureBlockWidth", texture.textureBlockWidth);
                package.material[i].SetInt("_boneTextureBlockHeight", texture.textureBlockHeight);
            }
        }


        [UnityEditor.CustomEditor(typeof(AnimationInstancingRendererManager))]
        private class _Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                var mng = target as AnimationInstancingRendererManager;

                foreach (var vc in VertexCachePool.VertexCacheDic)
                {
                    UnityEditor.EditorGUILayout.LabelField($"VertexCache {vc.Key} [{vc.Value.GetHashCode()}]:");
                }
            }
        }
    }
}