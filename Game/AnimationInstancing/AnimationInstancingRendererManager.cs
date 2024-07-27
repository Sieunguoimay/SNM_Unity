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
        public static readonly Dictionary<int, VertexCache> VertexCacheDic = new();
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

                    var blockUnit = block.clonedMaterialBlocks[aniTextureIndex];

                    var instanceIndex = blockUnit.NextInstanceIndex();

                    // if (package.instancingCount > 0) -> always true
                    // {
                    // var instanceIndex = topPackage.instancingCount - 1;

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

                    blockUnit.TopPackage.worldMatrixArray[instanceIndex] = instanceRenderer.RootTransform.localToWorldMatrix;
                    blockUnit.TopPackage.frameIndexArray[instanceIndex] = instanceAnimator.FrameIndex;
                    blockUnit.TopPackage.preFrameIndexArray[instanceIndex] = instanceAnimator.PreFrameIndex;
                    blockUnit.TopPackage.transitionProgressArray[instanceIndex] = instanceAnimator.TransitionProgress;
                    // }
                }
            }
        }


        private void Render()
        {
            foreach (var obj in VertexCacheDic)
            {
                var vertexCache = obj.Value;
                foreach (var blockItem in vertexCache.InstanceBlockDic)
                {
                    var block = blockItem.Value;
                    for (var i = 0; i < block.clonedMaterialBlocks.Length; i++)
                    {
                        var blockUnit = block.clonedMaterialBlocks[i];

                        for (var j = 0; j < blockUnit.PackageStack.Count; j++)
                        {
                            var package = blockUnit.PackageStack[j];

                            if (package.instancingCount > 0)
                            {
                                DrawMeshInstanced(vertexCache, package, 
                                    blockUnit.clonedMaterials, i);

                                package.instancingCount = 0;
                            }
                        }

                        blockUnit.ResetStack();
                    }
                }
            }
        }

        private static void DrawMeshInstanced(VertexCache vertexCache,
            InstancingPackage package, Material[] materials, int textureIndex)
        {
            for (int i = 0; i != vertexCache.BoneAndMesh.mesh.subMeshCount; ++i)
            {
#if UNITY_EDITOR
                PreparePackageMaterial(materials[i], vertexCache.textureData, textureIndex);
#endif
                DrawMeshInstanced(vertexCache, package, materials[i], i);
            }
        }

        private static void DrawMeshInstanced(
            VertexCache vertexCache,
            InstancingPackage package,
            Material material,
            int subMeshIndex)
        {
            package.propertyBlock.SetFloatArray("frameIndex", package.frameIndexArray);
            package.propertyBlock.SetFloatArray("preFrameIndex", package.preFrameIndexArray);
            package.propertyBlock.SetFloatArray("transitionProgress", package.transitionProgressArray);

            Graphics.DrawMeshInstanced(vertexCache.BoneAndMesh.mesh,
                subMeshIndex,
                material,
                package.worldMatrixArray,
                package.instancingCount,
                package.propertyBlock,
                vertexCache.renderingConfig.shadowcastingMode,
                vertexCache.renderingConfig.receiveShadow,
                vertexCache.renderingConfig.layer);
        }

        public static void PreparePackageMaterial(Material material, AnimationTextureData textureData, int aniTextureIndex)
        {
            if (textureData == null)
                return;

            material.SetTexture("_boneTexture", textureData.bakedBoneTextures[aniTextureIndex]);
            material.SetInt("_boneTextureWidth", textureData.bakedBoneTextures[aniTextureIndex].width);
            material.SetInt("_boneTextureHeight", textureData.bakedBoneTextures[aniTextureIndex].height);
            material.SetInt("_boneTextureBlockWidth", textureData.textureBlockWidth);
            material.SetInt("_boneTextureBlockHeight", textureData.textureBlockHeight);
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(AnimationInstancingRendererManager))]
        private class _Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                var mng = target as AnimationInstancingRendererManager;

                foreach (var vc in VertexCacheDic)
                {
                    UnityEditor.EditorGUILayout.LabelField($"VertexCache {vc.Key} [{vc.Value.GetHashCode()}]:");
                }
            }
        }
#endif
    }
}