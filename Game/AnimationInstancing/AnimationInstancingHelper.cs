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
                    int identify = GetIdentify(materials);
                    var rendererIndex = smrIndex;

                    NewMethod(vertexCachePool, allBones, bindPose, textureData,
                        mesh, render, renderName, aliasName,
                        materials, identify, bonePerVertex,
                        out MaterialBlock matBlock, out VertexCache vertexCache);

                    lod.materialBlockList[rendererIndex] = matBlock;
                    lod.vertexCacheList[rendererIndex] = vertexCache;
                    vertexCachePool[renderName + aliasName] = vertexCache;
                }

                for (int mrIndex = 0; mrIndex != lod.meshRenderer.Length; ++mrIndex)
                {
                    var mesh = lod.meshFilter[mrIndex].sharedMesh;
                    if (mesh == null) continue;

                    var render = lod.meshRenderer[mrIndex];
                    int renderName = render.name.GetHashCode();
                    int aliasName = alias != null ? alias.GetHashCode() : 0;
                    var materials = render.sharedMaterials;
                    int identify = GetIdentify(materials);
                    var rendererIndex = lod.skinnedMeshRenderer.Length + mrIndex;

                    NewMethod(vertexCachePool, allBones, bindPose, textureData,
                        mesh, render, renderName, aliasName,
                        materials, identify, bonePerVertex,
                        out MaterialBlock matBlock, out VertexCache vertexCache);

                    lod.materialBlockList[rendererIndex] = matBlock;
                    lod.vertexCacheList[rendererIndex] = vertexCache;
                    vertexCachePool[renderName + aliasName] = vertexCache;
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void NewMethod(
            Dictionary<int, VertexCache> vertexCachePool,
            Transform[] allBones, Matrix4x4[] bindPose,
            AnimationTextureData textureData,
            Mesh mesh, Renderer render, int renderName, int aliasName,
            Material[] materials, int identify, int bonePerVertex,
            out MaterialBlock matBlock, out VertexCache vertexCache)
        {
            if (vertexCachePool.TryGetValue(renderName + aliasName, out vertexCache))
            {
                if (!vertexCache.instanceBlockList.TryGetValue(identify, out matBlock))
                {
                    matBlock = CreateMaterialBlock(textureData);

                    AddToMaterialBlock(matBlock, textureData, materials, mesh.subMeshCount, 1);

                    vertexCache.instanceBlockList.Add(identify, matBlock);
                }
            }
            else
            {
                DoStuffs(allBones, bindPose, mesh, render, bonePerVertex,
                    out Mesh me, out Vector4[] newBoneWeights, out Vector4[] newBoneIndices);

                matBlock = CreateMaterialBlock(textureData);
                AddToMaterialBlock(matBlock, textureData, materials, mesh.subMeshCount, 1);

                vertexCache = CreateVertexCache(bindPose, me, renderName, aliasName,
                    materials, new Dictionary<int, MaterialBlock>() { { identify, matBlock } },
                    newBoneWeights, newBoneIndices);


                AddBoneDataToMesh(vertexCache.mesh, vertexCache.weight, vertexCache.boneIndex);
                AddToMaterialBlock(matBlock, textureData, materials, mesh.subMeshCount, 0);
            }

        }

        private static void DoStuffs(Transform[] allBones, Matrix4x4[] bindPose, Mesh mesh, Renderer render, int bonePerVertex, out Mesh me, out Vector4[] newBoneWeights, out Vector4[] newBoneIndices)
        {
            if (render is SkinnedMeshRenderer smr)
            {
                var boneIndicesMap = CalcBoneIndicesMap(smr.bones, allBones, render.transform.parent);

                UnityEngine.Profiling.Profiler.BeginSample("Copy the vertex data in SetupVertexCache()");
                CopyBoneWeightsAndBoneIndices(
                    smr.sharedMesh.boneWeights,
                    smr.sharedMesh.vertexCount,
                    bonePerVertex, boneIndicesMap,
                    out newBoneWeights,
                    out newBoneIndices);
                UnityEngine.Profiling.Profiler.EndSample();
                me = mesh;
            }
            else
            {

                SetupAttachment(
                    allBones, bindPose,
                    mesh, render,
                    out me, out newBoneWeights, out newBoneIndices);
            }
        }

        private static bool SetupAttachment(Transform[] allBones, Matrix4x4[] bindPose,
            Mesh mesh, Renderer render,
            out Mesh me,
            out Vector4[] newBoneWeights,
            out Vector4[] newBoneIndices)
        {
            me = mesh;
            newBoneWeights = new Vector4[mesh.vertexCount];
            newBoneIndices = new Vector4[mesh.vertexCount];

            int boneIndex = GetBoneToAttach(allBones, render as MeshRenderer);
            if (boneIndex >= 0)
            {
                //todo

                me = CopySharedMesh(bindPose, mesh, boneIndex);

                AssignAllVerticesToBone(newBoneWeights, newBoneIndices, mesh.vertexCount, boneIndex);

                return true;
            }
            return false;
        }

        private static VertexCache CreateVertexCache(
            Matrix4x4[] bindPose, Mesh mesh,
            int renderName, int aliasName,
            Material[] materials, Dictionary<int, MaterialBlock> instanceBlockList, Vector4[] weight, Vector4[] boneIndex
            )
        {
            return new VertexCache
            {
                nameCode = renderName + aliasName,
                mesh = mesh,
                weight = weight,
                boneIndex = boneIndex,
                instanceBlockList = instanceBlockList,
                bindPose = bindPose,
                materials = materials,
            };
        }

        private static int GetBoneToAttach(Transform[] allBones, MeshRenderer render)
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

            return boneIndex;
        }

        private static MaterialBlock CreateMaterialBlock(
            AnimationTextureData textureData)
        {
            var textureCount = textureData != null ? textureData.bakedBoneTextures.Length : 1;

            var instanceData = new InstanceData
            {
                worldMatrix = new List<Matrix4x4[]>[textureCount],
                frameIndex = new List<float[]>[textureCount],
                preFrameIndex = new List<float[]>[textureCount],
                transitionProgress = new List<float[]>[textureCount],
            };

            var packageList = new List<InstancingPackage>[textureCount];
            for (int textureIndex = 0; textureIndex != textureCount; ++textureIndex)
            {
                packageList[textureIndex] = new List<InstancingPackage>();
            }

            var matBlock = new MaterialBlock
            {
                instanceData = instanceData,
                packageList = packageList,
                runtimePackageIndex = new int[textureCount]
            };

            return matBlock;
        }

        private static void AddToMaterialBlock(
            MaterialBlock matBlock,
            AnimationTextureData textureData,
            Material[] materials,
            int subMeshCount, int instancingCount)
        {
            var textureCount = textureData != null ? textureData.bakedBoneTextures.Length : 1;
            for (int textureIndex = 0; textureIndex != textureCount; ++textureIndex)
            {
                matBlock.instanceData.worldMatrix[textureIndex] = new() { new Matrix4x4[instancingPackageSize] };
                matBlock.instanceData.frameIndex[textureIndex] = new() { new float[instancingPackageSize] };
                matBlock.instanceData.preFrameIndex[textureIndex] = new() { new float[instancingPackageSize] };
                matBlock.instanceData.transitionProgress[textureIndex] = new() { new float[instancingPackageSize] };

                matBlock.packageList[textureIndex].Add(
                    new InstancingPackage()
                    {
                        material = SetupInstancingMaterials(materials, subMeshCount, textureData, textureIndex),
                        subMeshCount = subMeshCount,
                        // size = 1,
                        instancingCount = instancingCount,
                        propertyBlock = new MaterialPropertyBlock()
                    }
                );
            }
        }

        private static Material[] SetupInstancingMaterials(
            Material[] materials, int count,
            AnimationTextureData textureData, int textureIndex)
        {
            var copyMaterials = new Material[count];

            for (int subMeshIndex = 0; subMeshIndex != count; ++subMeshIndex)
            {
                copyMaterials[subMeshIndex] = new Material(materials[subMeshIndex]);
#if UNITY_5_6_OR_NEWER
                copyMaterials[subMeshIndex].enableInstancing = true;
#endif
                //if (useInstancing)
                copyMaterials[subMeshIndex].EnableKeyword("INSTANCING_ON");
                //else
                //copyMaterials[i].DisableKeyword("INSTANCING_ON");

                copyMaterials[subMeshIndex].EnableKeyword("USE_CONSTANT_BUFFER");
                copyMaterials[subMeshIndex].DisableKeyword("USE_COMPUTE_BUFFER");

                if (textureData != null)
                {
                    copyMaterials[subMeshIndex].SetTexture("_boneTexture", textureData.bakedBoneTextures[textureIndex]);
                    copyMaterials[subMeshIndex].SetInt("_boneTextureWidth", textureData.bakedBoneTextures[textureIndex].width);
                    copyMaterials[subMeshIndex].SetInt("_boneTextureHeight", textureData.bakedBoneTextures[textureIndex].height);
                    copyMaterials[subMeshIndex].SetInt("_boneTextureBlockWidth", textureData.textureBlockWidth);
                    copyMaterials[subMeshIndex].SetInt("_boneTextureBlockHeight", textureData.textureBlockHeight);
                }
            }

            return copyMaterials;
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

        public static void AssignAllVerticesToBone(Vector4[] weight, Vector4[] boneIndexList, int vertexCount, int boneIndex)
        {
            for (int j = 0; j != vertexCount; ++j)
            {
                weight[j].x = 1.0f;
                weight[j].y = -0.1f;
                weight[j].z = -0.1f;
                weight[j].w = -0.1f;
                boneIndexList[j].x = boneIndex;
            }

        }

        private static Mesh CopySharedMesh(Matrix4x4[] bindPose, Mesh sharedMesh, int boneIndex)
        {
            Mesh mesh;
            var mat = bindPose[boneIndex].inverse;
            var offset = (Vector3)mat.GetColumn(3);
            var q = RuntimeHelper.QuaternionFromMatrix(mat);

            mesh = Object.Instantiate(sharedMesh);
            var vertices = mesh.vertices;
            for (int k = 0; k != mesh.vertexCount; ++k)
            {
                vertices[k] = q * vertices[k];
                vertices[k] = vertices[k] + offset;
            }
            mesh.vertices = vertices;
            return mesh;
        }
    }
}