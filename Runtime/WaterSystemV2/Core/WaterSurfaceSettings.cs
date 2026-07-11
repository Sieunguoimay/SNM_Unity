using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// The surface geometry + material of one water body. The size here is
    /// the single source of truth for both the auto quad and the shore bake
    /// (V1 kept two separate sizes that could drift apart).
    /// </summary>
    [Serializable]
    public class WaterSurfaceSettings
    {
        public enum MeshSource
        {
            [Tooltip("Flat quad generated at runtime. No shoreline data.")]
            AutoQuad,
            [Tooltip("Mesh baked by the Bake Shore Mesh button (UV1 carries shore distance).")]
            Baked,
            [Tooltip("Any mesh you assign yourself.")]
            Custom,
        }

        [Tooltip("World size of the water rectangle (X × Z).")]
        public Vector2 size = new(30f, 30f);

        public MeshSource meshSource = MeshSource.AutoQuad;

        [Tooltip("Output of the Bake Shore Mesh button.")]
        public Mesh bakedMesh;

        [Tooltip("Used when Mesh Source is Custom.")]
        public Mesh customMesh;

        [Tooltip("Leave empty to auto-create an instance of the V2 water shader. Assign to use a shared material asset instead.")]
        public Material materialOverride;

        /// <summary>Resolves the mesh to render, or null when the chosen source is missing.</summary>
        public Mesh ResolveMesh(Mesh autoQuad) => meshSource switch
        {
            MeshSource.Baked => bakedMesh,
            MeshSource.Custom => customMesh,
            _ => autoQuad,
        };
    }

    /// <summary>
    /// Inputs for the editor-side shore mesh bake (marching squares over the
    /// terrain objects). Kept on the component so a scene needs no extra
    /// assets; the bake itself lives in Editor/WaterShoreBaker.
    /// </summary>
    [Serializable]
    public class WaterShoreBakeSettings
    {
        [Tooltip("Solid objects that form the shore. Use the Scan button to auto-fill from colliders overlapping the water rectangle.")]
        public List<GameObject> terrainObjects = new();

        [Tooltip("Bake grid cell size in world units. Smaller = more triangles, smoother shoreline.")]
        public float gridCellSize = 1f;

        [Tooltip("Distance (world units) over which shoreline foam fades out.")]
        public float maxShoreDistance = 4f;

        [Tooltip("Tiling of the along-shore coordinate (UV1.y).")]
        public float alongShoreTiling = 0.1f;
    }
}
