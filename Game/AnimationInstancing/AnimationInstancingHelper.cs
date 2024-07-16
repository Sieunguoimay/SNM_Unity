using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingHelper
    {
        private static readonly int instancingPackageSize = 200;

        public static void AddToVertexCachePool(
            Dictionary<int, VertexCache> vertexCachePool,
            LodInfo[] lodInfo,

            Transform[] bones,
            Matrix4x4[] bindPose,
            AnimationTextureData texture,
            int bonePerVertex,
            string alias)
        {
            var packageCount = texture != null ? texture.bakedBoneTextures.Length : 1;

            UnityEngine.Profiling.Profiler.BeginSample("AddMeshVertex()");
            for (int x = 0; x != lodInfo.Length; ++x)
            {
                var lod = lodInfo[x];
                for (int i = 0; i != lod.skinnedMeshRenderer.Length; ++i)
                {
                    var m = lod.skinnedMeshRenderer[i].sharedMesh;
                    if (m == null) continue;

                    int renderName = lod.skinnedMeshRenderer[i].name.GetHashCode();
                    int aliasName = 0;
                    var materials = lod.skinnedMeshRenderer[i].sharedMaterials;
                    int identify = GetIdentify(materials);
                    if (vertexCachePool.TryGetValue(renderName + aliasName, out VertexCache cache))
                    {
                        Found(texture, packageCount, lod, i, materials, identify, cache);
                    }
                    else
                    {
                        MaterialBlock matBlock;
                        VertexCache vertexCache;

                        NotFound(vertexCachePool, bindPose, texture, packageCount, lod, i,
                            m, renderName, aliasName, materials, identify, out matBlock, out vertexCache);

                        SetupVertexCache_ForSkinnedMeshRenderer(
                            texture,
                            vertexCache,
                            matBlock,
                            lod.skinnedMeshRenderer[i],
                            bones,
                            bonePerVertex,
                            instancingPackageSize);
                    }
                }

                for (int i = 0; i != lod.meshRenderer.Length; ++i)
                {
                    var m = lod.meshFilter[i].sharedMesh;
                    if (m == null) continue;

                    int renderName = lod.meshRenderer[i].name.GetHashCode();
                    int aliasName = alias != null ? alias.GetHashCode() : 0;
                    var materials = lod.meshRenderer[i].sharedMaterials;
                    int identify = GetIdentify(materials);

                    if (vertexCachePool.TryGetValue(renderName + aliasName, out VertexCache cache))
                    {
                        Found(texture, packageCount, lod, lod.skinnedMeshRenderer.Length + i, materials, identify, cache);
                    }
                    else
                    {

                        MaterialBlock matBlock;
                        VertexCache vertexCache;

                        NotFound(vertexCachePool, bindPose, texture, packageCount, lod,
                            lod.skinnedMeshRenderer.Length + i,
                            m, renderName, aliasName, materials, identify, out matBlock, out vertexCache);

                        SetupVertexCache_ForMeshRenderer(
                            texture,
                            vertexCache,
                            matBlock,
                            lod.meshRenderer[i],
                            bones,
                            instancingPackageSize);
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void NotFound(Dictionary<int, VertexCache> vertexCachePool,
            Matrix4x4[] bindPose,
            AnimationTextureData texture,
            int packageCount, LodInfo lod, int i, Mesh m, int renderName, int aliasName,
            Material[] materials, int identify,
            out MaterialBlock matBlock,
            out VertexCache vertexCache)
        {
            matBlock = CreateBlock(
                m,
                materials,
                texture,
                packageCount,
                instancingPackageSize);
            vertexCache = new VertexCache
            {
                nameCode = renderName + aliasName,
                mesh = m,
                weight = new Vector4[m.vertexCount],
                boneIndex = new Vector4[m.vertexCount],
                instanceBlockList = new() { { identify, matBlock } },
                bindPose = bindPose,
                materials = materials,
            };
            lod.vertexCacheList[i] = vertexCache;
            lod.materialBlockList[i] = matBlock;
            vertexCachePool[renderName + aliasName] = vertexCache;
        }

        private static void Found(AnimationTextureData texture, int packageCount, LodInfo lod, int i, Material[] materials, int identify, VertexCache cache)
        {
            if (!cache.instanceBlockList.TryGetValue(identify, out MaterialBlock block))
            {
                block = CreateBlock(
                    cache.mesh,
                    materials,
                    texture,
                    packageCount,
                    instancingPackageSize);
                cache.instanceBlockList.Add(identify, block);
            }
            lod.vertexCacheList[i] = cache;
            lod.materialBlockList[i] = block;
        }

        private static int GetIdentify(Material[] materials)
        {
            int hash = 0;
            for (int i = 0; i != materials.Length; ++i)
            {
                hash += materials[i].name.GetHashCode();
            }
            return hash;
        }

        private static MaterialBlock CreateBlock(
            Mesh mesh,
            Material[] materials,
            AnimationTextureData texture,
            int packageCount,
            int instancingPackageSize)
        {
            var block = new MaterialBlock
            {
                instanceData = CreateInstanceData(packageCount),
                packageList = new List<InstancingPackage>[packageCount]
            };
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                block.packageList[i] = new List<InstancingPackage>();

                var package = CreatePackage(block.instanceData,
                    mesh, materials, i,
                    instancingPackageSize);

                block.packageList[i].Add(package);

                if (texture != null)
                {
                    PreparePackageMaterial(package, texture, i);
                }

                package.instancingCount = 1;
            }
            block.runtimePackageIndex = new int[packageCount];
            return block;
        }

        private static InstanceData CreateInstanceData(int packageCount)
        {
            var data = new InstanceData
            {
                worldMatrix = new List<Matrix4x4[]>[packageCount],
                frameIndex = new List<float[]>[packageCount],
                preFrameIndex = new List<float[]>[packageCount],
                transitionProgress = new List<float[]>[packageCount]
            };
            for (int i = 0; i != packageCount; ++i)
            {
                data.worldMatrix[i] = new List<Matrix4x4[]>();
                data.frameIndex[i] = new List<float[]>();
                data.preFrameIndex[i] = new List<float[]>();
                data.transitionProgress[i] = new List<float[]>();
            }
            return data;
        }

        private static InstancingPackage CreatePackage(
            InstanceData data,
            Mesh mesh,
            Material[] originalMaterial,
            int animationIndex,
            int instancingPackageSize)
        {
            var package = new InstancingPackage
            {
                material = new Material[mesh.subMeshCount],
                subMeshCount = mesh.subMeshCount,
                size = 1
            };

            for (int i = 0; i != mesh.subMeshCount; ++i)
            {
                package.material[i] = new Material(originalMaterial[i]);
#if UNITY_5_6_OR_NEWER
                package.material[i].enableInstancing = true;
#endif
                //if (useInstancing)
                package.material[i].EnableKeyword("INSTANCING_ON");
                //else
                //package.material[i].DisableKeyword("INSTANCING_ON");

                package.propertyBlock = new MaterialPropertyBlock();
                package.material[i].EnableKeyword("USE_CONSTANT_BUFFER");
                package.material[i].DisableKeyword("USE_COMPUTE_BUFFER");
            }

            var mat = new Matrix4x4[instancingPackageSize];
            var frameIndex = new float[instancingPackageSize];
            var preFrameIndex = new float[instancingPackageSize];
            var transitionProgress = new float[instancingPackageSize];
            data.worldMatrix[animationIndex].Add(mat);
            data.frameIndex[animationIndex].Add(frameIndex);
            data.preFrameIndex[animationIndex].Add(preFrameIndex);
            data.transitionProgress[animationIndex].Add(transitionProgress);
            return package;
        }

        private static VertexCache CreateVertexCache(
            int nameCode,
            Mesh mesh,
            Matrix4x4[] bindPose)
        {
            var vertexCache = new VertexCache
            {
                nameCode = nameCode,
                mesh = mesh,
                weight = new Vector4[mesh.vertexCount],
                boneIndex = new Vector4[mesh.vertexCount],
                instanceBlockList = new Dictionary<int, MaterialBlock>(),
                bindPose = bindPose
            };

            return vertexCache;
        }

        private static void SetupVertexCache_ForSkinnedMeshRenderer(
            AnimationTextureData texture,
            VertexCache vertexCache,
            MaterialBlock block,
            SkinnedMeshRenderer render,
            Transform[] allBones,
            int bonePerVertex,
            int instancingPackageSize)
        {
            var boneIndicesMap = CalcBoneIndicesMap(render.bones, allBones, render.transform.parent);

            UnityEngine.Profiling.Profiler.BeginSample("Copy the vertex data in SetupVertexCache()");
            CopyBoneWeightsAndBoneIndices(
                render.sharedMesh.boneWeights,
                render.sharedMesh.vertexCount,
                bonePerVertex, boneIndicesMap,
                out var newBoneWeights,
                out var newBoneIndices);
            UnityEngine.Profiling.Profiler.EndSample();

            AddBoneDataToMesh(vertexCache.mesh, newBoneWeights, newBoneIndices);

            GenerateInstancePackages(texture, vertexCache, block, render.sharedMaterials, instancingPackageSize);
        }

        private static int[] CalcBoneIndicesMap(
            Transform[] bones, 
            Transform[] allBones, 
            Transform defaultBone)
        {
            int[] boneIndicesMap = null;
            if (bones.Length != allBones.Length)
            {
                if (bones.Length == 0)
                {
                    boneIndicesMap = new int[1];

                    int hashRenderParentName = defaultBone.name.GetHashCode();

                    for (int k = 0; k != allBones.Length; ++k)
                    {
                        if (hashRenderParentName == allBones[k].name.GetHashCode())
                        {
                            boneIndicesMap[0] = k;
                            break;
                        }
                    }
                }
                else
                {
                    boneIndicesMap = new int[bones.Length];
                    for (int j = 0; j != bones.Length; ++j)
                    {
                        boneIndicesMap[j] = -1;

                        var trans = bones[j];
                        int hashTransformName = trans.name.GetHashCode();

                        for (int k = 0; k != allBones.Length; ++k)
                        {
                            if (hashTransformName == allBones[k].name.GetHashCode())
                            {
                                boneIndicesMap[j] = k;
                                break;
                            }
                        }
                    }
                }
            }

            return boneIndicesMap;
        }

        private static void CopyBoneWeightsAndBoneIndices(
            BoneWeight[] boneWeights,
            int vertexCount,
            int bonePerVertex,
            int[] boneIndicesMap,
            out Vector4[] outBoneWeights,
            out Vector4[] outBoneIndices)
        {
            outBoneWeights = new Vector4[vertexCount];
            outBoneIndices = new Vector4[vertexCount];

            Debug.Assert(vertexCount > 0);
            for (int j = 0; j != vertexCount; ++j)
            {
                outBoneWeights[j].x = boneWeights[j].weight0;
                Debug.Assert(outBoneWeights[j].x > 0.0f);
                outBoneWeights[j].y = boneWeights[j].weight1;
                outBoneWeights[j].z = boneWeights[j].weight2;
                outBoneWeights[j].w = boneWeights[j].weight3;

                outBoneIndices[j].x = boneIndicesMap == null
                    ? boneWeights[j].boneIndex0 : boneIndicesMap[boneWeights[j].boneIndex0];
                outBoneIndices[j].y = boneIndicesMap == null
                    ? boneWeights[j].boneIndex1 : boneIndicesMap[boneWeights[j].boneIndex1];
                outBoneIndices[j].z = boneIndicesMap == null
                    ? boneWeights[j].boneIndex2 : boneIndicesMap[boneWeights[j].boneIndex2];
                outBoneIndices[j].w = boneIndicesMap == null
                    ? boneWeights[j].boneIndex3 : boneIndicesMap[boneWeights[j].boneIndex3];

                Debug.Assert(outBoneIndices[j].x >= 0);

                if (bonePerVertex == 3)
                {
                    float rate = 1.0f / (outBoneWeights[j].x + outBoneWeights[j].y + outBoneWeights[j].z);
                    outBoneWeights[j].x = outBoneWeights[j].x * rate;
                    outBoneWeights[j].y = outBoneWeights[j].y * rate;
                    outBoneWeights[j].z = outBoneWeights[j].z * rate;
                    outBoneWeights[j].w = -0.1f;
                }
                else if (bonePerVertex == 2)
                {
                    float rate = 1.0f / (outBoneWeights[j].x + outBoneWeights[j].y);
                    outBoneWeights[j].x = outBoneWeights[j].x * rate;
                    outBoneWeights[j].y = outBoneWeights[j].y * rate;
                    outBoneWeights[j].z = -0.1f;
                    outBoneWeights[j].w = -0.1f;
                }
                else if (bonePerVertex == 1)
                {
                    outBoneWeights[j].x = 1.0f;
                    outBoneWeights[j].y = -0.1f;
                    outBoneWeights[j].z = -0.1f;
                    outBoneWeights[j].w = -0.1f;
                }
            }
        }

        private static void SetupVertexCache_ForMeshRenderer(
            AnimationTextureData texture,
            VertexCache vertexCache,
            MaterialBlock block,
            MeshRenderer render,
            Transform[] allBones,
            int instancingPackageSize)
        {
            int boneIndex = -1;
            if (allBones != null)
            {
                for (int k = 0; k != allBones.Length; ++k)
                {
                    if (render.transform.parent.name.GetHashCode() == allBones[k].name.GetHashCode())
                    {
                        boneIndex = k;
                        break;
                    }
                }
            }
            if (boneIndex >= 0)
            {
                //todo
                BindAttachmentToBone(vertexCache, vertexCache, vertexCache.mesh, boneIndex);
            }

            AddBoneDataToMesh(vertexCache.mesh, vertexCache.weight, vertexCache.boneIndex);

            GenerateInstancePackages(texture, vertexCache, block, render.sharedMaterials, instancingPackageSize);
        }

        private static void GenerateInstancePackages(
            AnimationTextureData texture, 
            VertexCache vertexCache, 
            MaterialBlock block, 
            Material[] materials, 
            int instancingPackageSize)
        {
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                var package = CreatePackage(
                    block.instanceData, 
                    vertexCache.mesh,
                    materials, 
                    i, 
                    instancingPackageSize);

                block.packageList[i].Add(package);

                if (texture != null)
                {
                    PreparePackageMaterial(package, texture, i);
                }
            }
        }

        public static void AddBoneDataToMesh(Mesh mesh, Vector4[] boneWeights, Vector4[] boneIndices)
        {
            var colors = new Color[boneWeights.Length];
            for (int i = 0; i != colors.Length; ++i)
            {
                colors[i].r = boneWeights[i].x;
                colors[i].g = boneWeights[i].y;
                colors[i].b = boneWeights[i].z;
                colors[i].a = boneWeights[i].w;
            }
            mesh.colors = colors;
            mesh.SetUVs(2, boneIndices);
            mesh.UploadMeshData(false);
        }

        public static void BindAttachmentToBone(VertexCache parentCache,
            VertexCache attachmentCache,
            Mesh sharedMesh,
            int boneIndex)
        {
            var mat = parentCache.bindPose[boneIndex].inverse;
            attachmentCache.mesh = Object.Instantiate(sharedMesh);
            var offset = (Vector3)mat.GetColumn(3);
            var q = RuntimeHelper.QuaternionFromMatrix(mat);
            var vertices = attachmentCache.mesh.vertices;
            for (int k = 0; k != attachmentCache.mesh.vertexCount; ++k)
            {
                vertices[k] = q * vertices[k];
                vertices[k] = vertices[k] + offset;
            }
            attachmentCache.mesh.vertices = vertices;

            for (int j = 0; j != attachmentCache.mesh.vertexCount; ++j)
            {
                attachmentCache.weight[j].x = 1.0f;
                attachmentCache.weight[j].y = -0.1f;
                attachmentCache.weight[j].z = -0.1f;
                attachmentCache.weight[j].w = -0.1f;
                attachmentCache.boneIndex[j].x = boneIndex;
            }
        }

        public static void PreparePackageMaterial(
            InstancingPackage package,
            AnimationTextureData texture,
            int aniTextureIndex)
        {
            for (int i = 0; i != package.subMeshCount; ++i)
            {
                package.material[i].SetTexture("_boneTexture", texture.bakedBoneTextures[aniTextureIndex]);
                package.material[i].SetInt("_boneTextureWidth", texture.bakedBoneTextures[aniTextureIndex].width);
                package.material[i].SetInt("_boneTextureHeight", texture.bakedBoneTextures[aniTextureIndex].height);
                package.material[i].SetInt("_boneTextureBlockWidth", texture.textureBlockWidth);
                package.material[i].SetInt("_boneTextureBlockHeight", texture.textureBlockHeight);
            }
        }
    }
}