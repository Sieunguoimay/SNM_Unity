using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class VertexCachePool
    {
        public static readonly int InstancingPackageSize = 200;
        public static readonly Dictionary<int, VertexCache> VertexCacheDic = new();

        public static VertexCache GetOrCreateVertexCache(
            Transform[] allBones, Matrix4x4[] bindPose,
            AnimationTextureData textureData, int bonePerVertex,
            Mesh sharedMesh, Renderer render, int key,
            Material[] sharedMaterials, RenderingConfig renderingConfig)
        {
            if (!VertexCacheDic.TryGetValue(key, out var vertexCache))
            {
                DoStuffsWithBonesAndMesh(allBones, bindPose, sharedMesh, render, bonePerVertex,
                    out Mesh mesh,
                    out Vector4[] newBoneWeights,
                    out Vector4[] newBoneIndices);

                AddBoneWeightsAndIndicesToMesh(mesh, newBoneWeights, newBoneIndices);

                vertexCache = new VertexCache
                {
                    mesh = mesh,
                    weight = newBoneWeights,
                    boneIndex = newBoneIndices,
                    instanceBlockDic = new Dictionary<int, MaterialBlock>(),
                    bindPose = bindPose,
                    bonePose = allBones,
                    materials = sharedMaterials,
                    textureData = textureData,
                    renderingConfig = renderingConfig,
                };

                VertexCacheDic[key] = vertexCache;
            }

            return vertexCache;
        }

        public static MaterialBlock GetOrCreateMaterialBlock(VertexCache vertexCache, int identify, int textureCount)
        {
            if (!vertexCache.instanceBlockDic.TryGetValue(identify, out MaterialBlock matBlock))
            {
                matBlock = CreateMaterialBlock(vertexCache, textureCount);

                for (int texIndex = 0; texIndex != textureCount; ++texIndex)
                {
                    matBlock.materialBlockUnits[texIndex].packageStack.Add(CreateInstancingPackage(1));
                }

                vertexCache.instanceBlockDic.Add(identify, matBlock);
            }

            return matBlock;
        }

        private static void DoStuffsWithBonesAndMesh(
            Transform[] allBones, Matrix4x4[] bindPose,
            Mesh sharedMesh, Renderer render, int bonePerVertex,
            out Mesh mesh, out Vector4[] newBoneWeights, out Vector4[] newBoneIndices)
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
                mesh = sharedMesh;
            }
            else
            {
                mesh = sharedMesh;
                newBoneWeights = new Vector4[sharedMesh.vertexCount];
                newBoneIndices = new Vector4[sharedMesh.vertexCount];

                int boneIndex = GetBoneToAttach(allBones, render as MeshRenderer);
                if (boneIndex >= 0)
                {
                    mesh = DuplicateMeshAndTransformToBoneLocal(sharedMesh, bindPose[boneIndex]);

                    for (int j = 0; j != sharedMesh.vertexCount; ++j)
                    {
                        newBoneWeights[j].x = 1.0f;
                        newBoneWeights[j].y = -0.1f;
                        newBoneWeights[j].z = -0.1f;
                        newBoneWeights[j].w = -0.1f;
                        newBoneIndices[j].x = boneIndex;
                    }
                }
            }
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

        private static Mesh DuplicateMeshAndTransformToBoneLocal(Mesh sharedMesh, Matrix4x4 boneMatrix)
        {
            var mesh = Object.Instantiate(sharedMesh);
            var vertices = mesh.vertices;
            var inversedMat = boneMatrix.inverse;

            var offset = (Vector3)inversedMat.GetColumn(3);
            var q = RuntimeHelper.QuaternionFromMatrix(inversedMat);

            for (int k = 0; k != mesh.vertexCount; ++k)
            {
                vertices[k] = q * vertices[k];
                vertices[k] = vertices[k] + offset;
            }
            mesh.vertices = vertices;
            return mesh;
        }

        private static MaterialBlock CreateMaterialBlock(VertexCache vertexCache, int textureCount)
        {
            var materialBlockUnits = new MaterialBlockUnit[textureCount];

            for (int i = 0; i != textureCount; ++i)
            {
                var clonedMaterials = DuplicateMaterials(
                    vertexCache.materials,
                    vertexCache.mesh.subMeshCount,
                    vertexCache.textureData, i);

                materialBlockUnits[i] = new MaterialBlockUnit()
                {
                    clonedMaterials = clonedMaterials,
                    packageStack = new List<InstancingPackage>(),
                    instanceCountPerPackage = InstancingPackageSize
                };
            }

            return new MaterialBlock
            {
                materialBlockUnits = materialBlockUnits,
            };
        }

        public static InstancingPackage CreateInstancingPackage(int instancingCount)
        {
            return new InstancingPackage()
            {
                // material = materials,
                // subMeshCount = vertexCache.mesh.subMeshCount,
                // size = 1,
                instancingCount = instancingCount,
                propertyBlock = new MaterialPropertyBlock(),
                worldMatrixArray = new Matrix4x4[InstancingPackageSize],
                frameIndexArray = new float[InstancingPackageSize],
                preFrameIndexArray = new float[InstancingPackageSize],
                transitionProgressArray = new float[InstancingPackageSize],
            };
        }

        private static Material[] DuplicateMaterials(
            Material[] materials, int count,
            AnimationTextureData textureData, int textureIndex)
        {
            var copyMaterials = new Material[count];

            for (int i = 0; i != count; ++i)
            {
                copyMaterials[i] = new Material(materials[i]);
#if UNITY_5_6_OR_NEWER
                copyMaterials[i].enableInstancing = true;
#endif
                //if (useInstancing)
                // copyMaterials[subMeshIndex].EnableKeyword("INSTANCING_ON");
                //else
                //copyMaterials[i].DisableKeyword("INSTANCING_ON");

                copyMaterials[i].EnableKeyword("USE_CONSTANT_BUFFER");
                copyMaterials[i].DisableKeyword("USE_COMPUTE_BUFFER");

                if (textureData != null)
                {
                    copyMaterials[i].SetTexture("_boneTexture", textureData.bakedBoneTextures[textureIndex]);
                    copyMaterials[i].SetInt("_boneTextureWidth", textureData.bakedBoneTextures[textureIndex].width);
                    copyMaterials[i].SetInt("_boneTextureHeight", textureData.bakedBoneTextures[textureIndex].height);
                    copyMaterials[i].SetInt("_boneTextureBlockWidth", textureData.textureBlockWidth);
                    copyMaterials[i].SetInt("_boneTextureBlockHeight", textureData.textureBlockHeight);
                }
            }

            return copyMaterials;
        }

        public static int GetIdentify(Material[] materials)
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

        public static void AddBoneWeightsAndIndicesToMesh(Mesh mesh, Vector4[] boneWeights, Vector4[] boneIndices)
        {
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i != mesh.vertexCount; ++i)
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

    }

    public class VertexCache
    {
        // public int nameCode;
        public Mesh mesh = null;
        public Dictionary<int, MaterialBlock> instanceBlockDic;
        public Vector4[] weight;
        public Vector4[] boneIndex;
        public Material[] materials = null;
        public Matrix4x4[] bindPose;
        public Transform[] bonePose;
        public AnimationTextureData textureData;

        // these are temporary, should be moved to InstancingPackage
        public RenderingConfig renderingConfig;
    }

    public class RenderingConfig
    {
        public UnityEngine.Rendering.ShadowCastingMode shadowcastingMode;
        public bool receiveShadow;
        public int layer;
    }

    public class MaterialBlock
    {
        public MaterialBlockUnit[] materialBlockUnits;
    }

    public class MaterialBlockUnit
    {
        public int instanceCountPerPackage;
        public Material[] clonedMaterials;
        public List<InstancingPackage> packageStack;

        private int _topPackageIndex = 0;
        public InstancingPackage TopPackage => packageStack[_topPackageIndex];

        public int NextInstanceIndex()
        {
            Debug.Assert(_topPackageIndex < packageStack.Count);

            if (TopPackage.instancingCount >= instanceCountPerPackage)
            {
                _topPackageIndex++;

                if (_topPackageIndex >= packageStack.Count)
                {
                    packageStack.Add(VertexCachePool.CreateInstancingPackage(1));
                }
                else
                {
                    TopPackage.instancingCount = 1;
                }
            }
            else
            {
                TopPackage.instancingCount++;
            }

            return TopPackage.instancingCount - 1;
        }

        public void ResetStack()
        {
            _topPackageIndex = 0;
        }

    }

    public class InstancingPackage
    {
        // public Material[] material;
        // public int animationTextureIndex = 0;
        // public int subMeshCount = 1;
        public int instancingCount;
        // public int size;
        public MaterialPropertyBlock propertyBlock;

        public Matrix4x4[] worldMatrixArray;
        public float[] frameIndexArray;
        public float[] preFrameIndexArray;
        public float[] transitionProgressArray;
    }
}