using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    /// <summary>
    /// Collects all <see cref="GrassPatch"/> instances, builds their matrices,
    /// and groups them by (Mesh, Material) for efficient batched rendering.
    /// </summary>
    public static class GrassPatchCollector
    {
        public struct RenderGroup
        {
            public Mesh Mesh;
            public Material Material;
            public Matrix4x4[] Matrices;
        }

        /// <summary>
        /// Collect all patches and return render groups sorted by (mesh, material).
        /// Each group maps to one <see cref="GrassRenderer"/> / draw call.
        /// </summary>
        public static List<RenderGroup> Collect(GrassPatch[] patches)
        {
            // Key: (mesh instance ID, material instance ID) → list of matrices
            var groups = new Dictionary<(int, int), List<Matrix4x4>>();
            var meshLookup = new Dictionary<(int, int), Mesh>();
            var materialLookup = new Dictionary<(int, int), Material>();

            foreach (var patch in patches)
            {
                if (patch == null || patch.mesh == null || patch.material == null)
                    continue;

                var matrices = patch.BuildMatrices();
                if (matrices.Length == 0) continue;

                var key = (patch.mesh.GetInstanceID(), patch.material.GetInstanceID());

                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<Matrix4x4>(matrices.Length);
                    groups[key] = list;
                    meshLookup[key] = patch.mesh;
                    materialLookup[key] = patch.material;
                }

                list.AddRange(matrices);
            }

            var result = new List<RenderGroup>(groups.Count);
            foreach (var kvp in groups)
            {
                result.Add(new RenderGroup
                {
                    Mesh = meshLookup[kvp.Key],
                    Material = materialLookup[kvp.Key],
                    Matrices = kvp.Value.ToArray()
                });
            }

            return result;
        }

        /// <summary>
        /// Compute a combined world bounds enclosing all patches.
        /// </summary>
        public static Bounds ComputeWorldBounds(GrassPatch[] patches)
        {
            var bounds = new Bounds();
            bool first = true;

            foreach (var patch in patches)
            {
                if (patch == null) continue;

                var center = patch.transform.position;
                var halfX = patch.areaSize.x * 0.5f;
                var halfZ = patch.areaSize.y * 0.5f;
                var patchBounds = new Bounds(center, new Vector3(halfX * 2f, 10f, halfZ * 2f));

                if (first)
                {
                    bounds = patchBounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(patchBounds);
                }
            }

            return bounds;
        }
    }
}
