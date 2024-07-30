using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
            for (int i = 0; i != instancingRenderers.Count; ++i)
            {
                var instanceRenderer = instancingRenderers[i];
                var instanceAnimator = instancingRenderers[i].InstancingAnimator;

                instanceAnimator.UpdateAnimation();

                var materialBlockList = instanceRenderer.MaterialBlockList;
                int aniTextureIndex = instanceAnimator.AniTextureIndex;

                for (int j = 0; j != materialBlockList.Count; ++j)
                {
                    var block = materialBlockList[j];
                    Debug.Assert(block != null);

                    var blockUnit = block.clonedMaterialBlocks[aniTextureIndex];

                    var instanceIndex = blockUnit.NextInstanceIndex();

                    blockUnit.TopPackage.worldMatrixArray[instanceIndex] = instanceRenderer.RootTransform.localToWorldMatrix;
                    blockUnit.TopPackage.frameIndexArray[instanceIndex] = instanceAnimator.FrameIndex;
                    blockUnit.TopPackage.preFrameIndexArray[instanceIndex] = instanceAnimator.PreFrameIndex;
                    blockUnit.TopPackage.transitionProgressArray[instanceIndex] = instanceAnimator.TransitionProgress;
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

                            blockUnit.totalInstancingCount = package.instancingCount;

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
            for (int i = 0; i != vertexCache.mesh.subMeshCount; ++i)
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

            Graphics.DrawMeshInstanced(vertexCache.mesh,
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
            public override VisualElement CreateInspectorGUI()
            {
                // return base.CreateInspectorGUI();
                var ve = new VisualElement();
                ve.Add(new IMGUIContainer(OnInspectorGUI));
                ve.Add(new AnimationInstancingRenderer.VertexCacheListVE(VertexCacheDic.Values, null));
                return ve;
            }
        }
#endif
    }
}