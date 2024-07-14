using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class AnimationInstancingHelper
    {
        private static readonly int instancingPackageSize = 200;

        public static void AddMeshVertex(
            Dictionary<int, VertexCache> vertexCachePool,
            LodInfo[] lodInfo,
            Transform[] bones,
            Matrix4x4[] bindPose,
            AnimationTexture texture,
            int bonePerVertex,
            string alias)
        {
            var packageCount = texture != null ? texture.boneTexture.Length : 1;

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
                    int identify = GetIdentify(lod.skinnedMeshRenderer[i].sharedMaterials);

                    if (vertexCachePool.TryGetValue(renderName + aliasName, out VertexCache cache))
                    {
                        if (!cache.instanceBlockList.TryGetValue(identify, out MaterialBlock block))
                        {
                            block = CreateBlock(
                                cache.mesh,
                                lod.skinnedMeshRenderer[i].sharedMaterials,
                                texture,
                                packageCount,
                                instancingPackageSize);
                            cache.instanceBlockList.Add(identify, block);
                        }
                        lod.vertexCacheList[i] = cache;
                        lod.materialBlockList[i] = block;
                    }
                    else
                    {
                        var vertexCache = CreateVertexCache(renderName + aliasName, m, bindPose);
                        var matBlock = CreateBlock(
                            vertexCache.mesh, 
                            lod.skinnedMeshRenderer[i].sharedMaterials,
                            texture, 
                            packageCount, 
                            instancingPackageSize);

                        vertexCache.instanceBlockList.Add(identify, matBlock);

                        SetupVertexCache_ForSkinnedMeshRenderer(
                            texture, 
                            vertexCache, 
                            matBlock,
                            lod.skinnedMeshRenderer[i], 
                            bones, 
                            bonePerVertex,
                            instancingPackageSize);
                        lod.vertexCacheList[i] = vertexCache;
                        lod.materialBlockList[i] = matBlock;

                        vertexCachePool[renderName + aliasName] = vertexCache;
                    }
                }

                for (int i = 0, j = lod.skinnedMeshRenderer.Length; i != lod.meshRenderer.Length; ++i, ++j)
                {
                    var m = lod.meshFilter[i].sharedMesh;
                    if (m == null) continue;

                    int renderName = lod.meshRenderer[i].name.GetHashCode();
                    int aliasName = alias != null ? alias.GetHashCode() : 0;
                    int identify = GetIdentify(lod.meshRenderer[i].sharedMaterials);

                    if (vertexCachePool.TryGetValue(renderName + aliasName, out VertexCache cache))
                    {
                        if (!cache.instanceBlockList.TryGetValue(identify, out MaterialBlock block))
                        {
                            block = CreateBlock(
                                cache.mesh,
                                lod.meshRenderer[i].sharedMaterials,
                                texture,
                                packageCount,
                                instancingPackageSize);
                            cache.instanceBlockList.TryAdd(identify, block);
                        }
                        lod.vertexCacheList[j] = cache;
                        lod.materialBlockList[j] = block;
                    }
                    else
                    {
                        var vertexCache = CreateVertexCache(renderName + aliasName, m, bindPose);
                        var matBlock = CreateBlock(
                            vertexCache.mesh, 
                            lod.meshRenderer[i].sharedMaterials,
                            texture, 
                            packageCount, 
                            instancingPackageSize);

                        vertexCache.instanceBlockList.Add(identify, matBlock);

                        SetupVertexCache_ForMeshRenderer(
                            texture, 
                            vertexCache, 
                            matBlock, 
                            lod.meshRenderer[i],
                            bones, 
                            instancingPackageSize);
                        lod.vertexCacheList[lod.skinnedMeshRenderer.Length + i] = vertexCache;
                        lod.materialBlockList[lod.skinnedMeshRenderer.Length + i] = matBlock;

                        vertexCachePool[renderName + aliasName] = vertexCache;
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
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
            AnimationTexture texture,
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
            AnimationTexture texture,
            VertexCache vertexCache,
            MaterialBlock block,
            SkinnedMeshRenderer render,
            Transform[] boneTransform,
            int bonePerVertex,
            int instancingPackageSize)
        {
            int[] boneIndex = null;
            if (render.bones.Length != boneTransform.Length)
            {
                if (render.bones.Length == 0)
                {
                    boneIndex = new int[1];
                    int hashRenderParentName = render.transform.parent.name.GetHashCode();
                    for (int k = 0; k != boneTransform.Length; ++k)
                    {
                        if (hashRenderParentName == boneTransform[k].name.GetHashCode())
                        {
                            boneIndex[0] = k;
                            break;
                        }
                    }
                }
                else
                {
                    boneIndex = new int[render.bones.Length];
                    for (int j = 0; j != render.bones.Length; ++j)
                    {
                        boneIndex[j] = -1;
                        Transform trans = render.bones[j];
                        int hashTransformName = trans.name.GetHashCode();
                        for (int k = 0; k != boneTransform.Length; ++k)
                        {
                            if (hashTransformName == boneTransform[k].name.GetHashCode())
                            {
                                boneIndex[j] = k;
                                break;
                            }
                        }
                    }

                    if (boneIndex.Length == 0)
                    {
                        boneIndex = null;
                    }
                }
            }

            UnityEngine.Profiling.Profiler.BeginSample("Copy the vertex data in SetupVertexCache()");
            Mesh m = render.sharedMesh;
            BoneWeight[] boneWeights = m.boneWeights;
            Debug.Assert(boneWeights.Length > 0);
            for (int j = 0; j != m.vertexCount; ++j)
            {
                vertexCache.weight[j].x = boneWeights[j].weight0;
                Debug.Assert(vertexCache.weight[j].x > 0.0f);
                vertexCache.weight[j].y = boneWeights[j].weight1;
                vertexCache.weight[j].z = boneWeights[j].weight2;
                vertexCache.weight[j].w = boneWeights[j].weight3;
                vertexCache.boneIndex[j].x
                    = boneIndex == null ? boneWeights[j].boneIndex0 : boneIndex[boneWeights[j].boneIndex0];
                vertexCache.boneIndex[j].y
                    = boneIndex == null ? boneWeights[j].boneIndex1 : boneIndex[boneWeights[j].boneIndex1];
                vertexCache.boneIndex[j].z
                    = boneIndex == null ? boneWeights[j].boneIndex2 : boneIndex[boneWeights[j].boneIndex2];
                vertexCache.boneIndex[j].w
                    = boneIndex == null ? boneWeights[j].boneIndex3 : boneIndex[boneWeights[j].boneIndex3];
                Debug.Assert(vertexCache.boneIndex[j].x >= 0);
                if (bonePerVertex == 3)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y + vertexCache.weight[j].z);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = vertexCache.weight[j].z * rate;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 2)
                {
                    float rate = 1.0f / (vertexCache.weight[j].x + vertexCache.weight[j].y);
                    vertexCache.weight[j].x = vertexCache.weight[j].x * rate;
                    vertexCache.weight[j].y = vertexCache.weight[j].y * rate;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
                else if (bonePerVertex == 1)
                {
                    vertexCache.weight[j].x = 1.0f;
                    vertexCache.weight[j].y = -0.1f;
                    vertexCache.weight[j].z = -0.1f;
                    vertexCache.weight[j].w = -0.1f;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (vertexCache.materials == null)
                vertexCache.materials = render.sharedMaterials;
            SetupAdditionalData(vertexCache);
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                var package = CreatePackage(block.instanceData, vertexCache.mesh,
                    render.sharedMaterials, i, instancingPackageSize);
                block.packageList[i].Add(package);
                //vertexCache.packageList[i].Add(package);

                if (texture != null)
                {
                    PreparePackageMaterial(package, texture, i);
                }
            }
        }


        private static void SetupVertexCache_ForMeshRenderer(
            AnimationTexture texture,
            VertexCache vertexCache,
            MaterialBlock block,
            MeshRenderer render,
            Transform[] boneTransform,
            int instancingPackageSize)
        {
            int boneIndex = -1;
            if (boneTransform != null)
            {
                for (int k = 0; k != boneTransform.Length; ++k)
                {
                    if (render.transform.parent.name.GetHashCode() == boneTransform[k].name.GetHashCode())
                    {
                        boneIndex = k;
                        break;
                    }
                }
            }
            if (boneIndex >= 0)
            {
                //todo
                BindAttachment(vertexCache, vertexCache, vertexCache.mesh, boneIndex);
            }
            vertexCache.materials ??= render.sharedMaterials;
            SetupAdditionalData(vertexCache);
            for (int i = 0; i != block.packageList.Length; ++i)
            {
                var package = CreatePackage(block.instanceData, vertexCache.mesh,
                    render.sharedMaterials, i, instancingPackageSize);
                block.packageList[i].Add(package);

                if (texture != null)
                {
                    PreparePackageMaterial(package, texture, i);
                }
            }
        }

        public static void SetupAdditionalData(VertexCache vertexCache)
        {
            var colors = new Color[vertexCache.weight.Length];
            for (int i = 0; i != colors.Length; ++i)
            {
                colors[i].r = vertexCache.weight[i].x;
                colors[i].g = vertexCache.weight[i].y;
                colors[i].b = vertexCache.weight[i].z;
                colors[i].a = vertexCache.weight[i].w;
            }
            vertexCache.mesh.colors = colors;

            var uv2 = new List<Vector4>(vertexCache.boneIndex.Length);
            for (int i = 0; i != vertexCache.boneIndex.Length; ++i)
            {
                uv2.Add(vertexCache.boneIndex[i]);
            }
            vertexCache.mesh.SetUVs(2, uv2);
            vertexCache.mesh.UploadMeshData(false);
        }

        public static void BindAttachment(VertexCache parentCache, VertexCache attachmentCache, Mesh sharedMesh, int boneIndex)
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
            AnimationTexture texture,
            int aniTextureIndex)
        {
            for (int i = 0; i != package.subMeshCount; ++i)
            {
                package.material[i].SetTexture("_boneTexture", texture.boneTexture[aniTextureIndex]);
                package.material[i].SetInt("_boneTextureWidth", texture.boneTexture[aniTextureIndex].width);
                package.material[i].SetInt("_boneTextureHeight", texture.boneTexture[aniTextureIndex].height);
                package.material[i].SetInt("_boneTextureBlockWidth", texture.blockWidth);
                package.material[i].SetInt("_boneTextureBlockHeight", texture.blockHeight);
            }
        }

        public class AnimationTexture
        {
            public string name { get; set; }
            public Texture2D[] boneTexture { get; set; }
            public int blockWidth { get; set; }
            public int blockHeight { get; set; }
        }

    }
}