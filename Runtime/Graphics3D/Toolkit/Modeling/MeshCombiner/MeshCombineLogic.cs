#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.Graphics3D.Modeling
{
    public static class MeshCombineLogic
    {
        public struct CombineInput
        {
            public Mesh Mesh;
            public Matrix4x4 Transform;
            public Material[] Materials;
        }

        public struct CombineResult
        {
            public Mesh CombinedMesh;
            public Material[] Materials;
        }

        public static CombineResult CombineByMaterial(List<CombineInput> inputs)
        {
            // Group submeshes by material
            var materialGroups = new Dictionary<Material, List<(CombineInput input, int submeshIdx)>>();

            foreach (var input in inputs)
            {
                if (input.Mesh == null) continue;
                for (int sm = 0; sm < input.Mesh.subMeshCount; sm++)
                {
                    Material mat = (input.Materials != null && sm < input.Materials.Length)
                        ? input.Materials[sm] : null;

                    if (!materialGroups.TryGetValue(mat, out var list))
                    {
                        list = new List<(CombineInput, int)>();
                        materialGroups[mat] = list;
                    }
                    list.Add((input, sm));
                }
            }

            return BuildCombinedMesh(materialGroups);
        }

        public static CombineResult CombineAsSubmeshes(List<CombineInput> inputs)
        {
            // Each input mesh becomes its own submesh group
            var materialGroups = new Dictionary<Material, List<(CombineInput input, int submeshIdx)>>();
            int idx = 0;

            foreach (var input in inputs)
            {
                if (input.Mesh == null) continue;
                for (int sm = 0; sm < input.Mesh.subMeshCount; sm++)
                {
                    Material mat = (input.Materials != null && sm < input.Materials.Length)
                        ? input.Materials[sm] : null;

                    // Use a unique key per submesh to keep them separate
                    // We'll use the actual material but append index info
                    var key = mat; // This naturally groups same materials
                    if (!materialGroups.TryGetValue(key, out var list))
                    {
                        list = new List<(CombineInput, int)>();
                        materialGroups[key] = list;
                    }
                    list.Add((input, sm));
                }
                idx++;
            }

            return BuildCombinedMesh(materialGroups);
        }

        static CombineResult BuildCombinedMesh(
            Dictionary<Material, List<(CombineInput input, int submeshIdx)>> groups)
        {
            var allPositions = new List<Vector3>();
            var allNormals = new List<Vector3>();
            var allTangents = new List<Vector4>();
            var allColors = new List<Color>();
            var allUVs = new List<Vector2>[8];
            for (int ch = 0; ch < 8; ch++) allUVs[ch] = new List<Vector2>();

            var submeshTriangles = new List<List<int>>();
            var materials = new List<Material>();

            bool hasNormals = false, hasTangents = false, hasColors = false;
            bool[] hasUV = new bool[8];

            // Pre-scan for attribute presence
            foreach (var group in groups.Values)
            foreach (var (input, _) in group)
            {
                var m = input.Mesh;
                if (m.normals?.Length > 0) hasNormals = true;
                if (m.tangents?.Length > 0) hasTangents = true;
                if (m.colors?.Length > 0) hasColors = true;
                for (int ch = 0; ch < 8; ch++)
                {
                    var uvs = new List<Vector2>();
                    m.GetUVs(ch, uvs);
                    if (uvs.Count > 0) hasUV[ch] = true;
                }
            }

            foreach (var kvp in groups)
            {
                Material mat = kvp.Key;
                var group = kvp.Value;
                materials.Add(mat);

                var tris = new List<int>();

                foreach (var (input, submeshIdx) in group)
                {
                    var mesh = input.Mesh;
                    int vertexOffset = allPositions.Count;

                    // Transform positions and normals to world space
                    var positions = mesh.vertices;
                    for (int i = 0; i < positions.Length; i++)
                        positions[i] = input.Transform.MultiplyPoint3x4(positions[i]);
                    allPositions.AddRange(positions);

                    if (hasNormals)
                    {
                        var normals = mesh.normals;
                        if (normals != null && normals.Length == mesh.vertexCount)
                        {
                            var normalMatrix = input.Transform.inverse.transpose;
                            for (int i = 0; i < normals.Length; i++)
                                normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                            allNormals.AddRange(normals);
                        }
                        else
                        {
                            for (int i = 0; i < mesh.vertexCount; i++)
                                allNormals.Add(Vector3.up);
                        }
                    }

                    if (hasTangents)
                    {
                        var tangents = mesh.tangents;
                        if (tangents != null && tangents.Length == mesh.vertexCount)
                            allTangents.AddRange(tangents);
                        else
                            for (int i = 0; i < mesh.vertexCount; i++)
                                allTangents.Add(new Vector4(1, 0, 0, 1));
                    }

                    if (hasColors)
                    {
                        var colors = mesh.colors;
                        if (colors != null && colors.Length == mesh.vertexCount)
                            allColors.AddRange(colors);
                        else
                            for (int i = 0; i < mesh.vertexCount; i++)
                                allColors.Add(Color.white);
                    }

                    for (int ch = 0; ch < 8; ch++)
                    {
                        if (!hasUV[ch]) continue;
                        var uvs = new List<Vector2>();
                        mesh.GetUVs(ch, uvs);
                        if (uvs.Count == mesh.vertexCount)
                            allUVs[ch].AddRange(uvs);
                        else
                            for (int i = 0; i < mesh.vertexCount; i++)
                                allUVs[ch].Add(Vector2.zero);
                    }

                    // Get triangles for this submesh
                    var desc = mesh.GetSubMesh(submeshIdx);
                    var meshTris = mesh.triangles;
                    for (int i = desc.indexStart; i < desc.indexStart + desc.indexCount; i++)
                        tris.Add(meshTris[i] + vertexOffset);
                }

                submeshTriangles.Add(tris);
            }

            // Build mesh
            var result = new Mesh { name = "Combined" };
            if (allPositions.Count > 65535)
                result.indexFormat = IndexFormat.UInt32;

            result.SetVertices(allPositions);
            if (hasNormals) result.SetNormals(allNormals);
            if (hasTangents) result.SetTangents(allTangents);
            if (hasColors) result.SetColors(allColors);
            for (int ch = 0; ch < 8; ch++)
                if (hasUV[ch]) result.SetUVs(ch, allUVs[ch]);

            var allTris = new List<int>();
            foreach (var smTris in submeshTriangles)
                allTris.AddRange(smTris);

            result.triangles = allTris.ToArray();
            result.subMeshCount = submeshTriangles.Count;
            int offset = 0;
            for (int sm = 0; sm < submeshTriangles.Count; sm++)
            {
                result.SetSubMesh(sm, new SubMeshDescriptor(offset, submeshTriangles[sm].Count));
                offset += submeshTriangles[sm].Count;
            }

            result.RecalculateBounds();
            if (!hasNormals) result.RecalculateNormals();

            return new CombineResult
            {
                CombinedMesh = result,
                Materials = materials.ToArray()
            };
        }

        public static List<CombineInput> CollectFromGameObjects(GameObject[] gameObjects, bool includeChildren)
        {
            var inputs = new List<CombineInput>();
            var processed = new HashSet<int>();

            foreach (var go in gameObjects)
            {
                var renderers = includeChildren
                    ? go.GetComponentsInChildren<Renderer>()
                    : go.GetComponents<Renderer>();

                foreach (var renderer in renderers)
                {
                    int id = renderer.GetInstanceID();
                    if (!processed.Add(id)) continue;

                    Mesh mesh = null;
                    if (renderer is MeshRenderer)
                    {
                        var mf = renderer.GetComponent<MeshFilter>();
                        if (mf != null) mesh = mf.sharedMesh;
                    }
                    else if (renderer is SkinnedMeshRenderer smr)
                    {
                        // Bake current pose
                        mesh = new Mesh();
                        smr.BakeMesh(mesh);
                    }

                    if (mesh == null || !mesh.isReadable) continue;

                    inputs.Add(new CombineInput
                    {
                        Mesh = mesh,
                        Transform = renderer.transform.localToWorldMatrix,
                        Materials = renderer.sharedMaterials
                    });
                }
            }

            return inputs;
        }
    }
}
#endif
