using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class BoneAndMesh
    {
        public Mesh mesh;
        public Vector4[] boneWeights;
        public Vector4[] boneIndices;

        public Mesh sharedMesh;
        public Transform[] allBones;
        public Matrix4x4[] bindPose;
        public Renderer render;
        public int bonePerVertex;

        public BoneAndMesh(Mesh sharedMesh, Transform[] allBones,
            Matrix4x4[] bindPose, Renderer render, int bonePerVertex)
        {
            this.sharedMesh = sharedMesh;
            this.allBones = allBones;
            this.bindPose = bindPose;
            this.render = render;
            this.bonePerVertex = bonePerVertex;

            ExtractBonesAndMesh();
            AddBoneWeightsAndIndicesToMesh();
        }

        private void ExtractBonesAndMesh()
        {
            if (render is SkinnedMeshRenderer smr)
            {
                var boneIndicesMap = CalcBoneIndicesMap(smr.bones, allBones, render.transform.parent);

                UnityEngine.Profiling.Profiler.BeginSample("Copy the vertex data in SetupVertexCache()");
                CopyBoneWeightsAndBoneIndices(
                    smr.sharedMesh.boneWeights,
                    smr.sharedMesh.vertexCount,
                    bonePerVertex, boneIndicesMap,
                    out boneWeights,
                    out boneIndices);
                UnityEngine.Profiling.Profiler.EndSample();
                mesh = sharedMesh;
            }
            else
            {
                mesh = sharedMesh;
                boneWeights = new Vector4[sharedMesh.vertexCount];
                boneIndices = new Vector4[sharedMesh.vertexCount];

                int boneIndex = GetBoneToAttach(allBones, render as MeshRenderer);
                if (boneIndex >= 0)
                {
                    var worldToBindPose = bindPose[boneIndex].inverse;
                    var worldToRootBone = render.GetComponentInParent<AnimationInstancingRenderer>().RootTransform.worldToLocalMatrix;
                    var meshToWorld = render.transform.localToWorldMatrix;
                    var worldToParentLocal = render.transform.parent.worldToLocalMatrix;
                    mesh = DuplicateMeshAndTransformToBoneLocal(sharedMesh, worldToBindPose);

                    for (int j = 0; j != sharedMesh.vertexCount; ++j)
                    {
                        boneWeights[j].x = 1.0f;
                        boneWeights[j].y = -0.1f;
                        boneWeights[j].z = -0.1f;
                        boneWeights[j].w = -0.1f;
                        boneIndices[j].x = boneIndex;
                    }
                }
            }
        }

        private static int GetBoneToAttach(Transform[] allBones, MeshRenderer render)
        {
            if (allBones != null)
            {
                for (var i = 0; i < allBones.Length; i++)
                {
                    if (render.transform.parent == allBones[i])
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static Mesh DuplicateMeshAndTransformToBoneLocal(Mesh sharedMesh, Matrix4x4 boneMatrix)
        {
            var mesh = Object.Instantiate(sharedMesh);
            var vertices = mesh.vertices;
            var worldToLocal = boneMatrix;

            var offset = (Vector3)worldToLocal.GetColumn(3);
            var q = RuntimeHelper.QuaternionFromMatrix(worldToLocal);

            for (int i = 0; i != mesh.vertexCount; ++i)
            {
                // vertices[i] = q * vertices[i];
                // vertices[i] = vertices[i] + offset;
                vertices[i] = worldToLocal.MultiplyPoint(vertices[i]);
            }
            mesh.vertices = vertices;
            return mesh;
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

        private void AddBoneWeightsAndIndicesToMesh()
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
            //             var uv2 = new List<Vector4>(boneIndices.Length);
            // for (int i = 0; i != boneIndices.Length; ++i)
            // {
            //     uv2.Add(boneIndices[i]);
            // }
            mesh.SetUVs(2, boneIndices);
            mesh.UploadMeshData(false);
        }
    }
}