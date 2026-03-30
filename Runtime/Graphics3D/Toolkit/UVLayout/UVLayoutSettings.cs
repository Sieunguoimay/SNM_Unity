#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Snm.Graphics3D.UVLayout
{
    [Serializable]
    public class UVLayoutSettings
    {
        // Core
        public int uvChannel;
        public int resolution = 1024;

        // Wireframe
        public Color lineColor = Color.white;
        public float lineWidth = 1f;

        // Background
        public Color backgroundColor = Color.black;
        public bool transparentBackground;

        // Fill
        public bool showFill;
        public Color fillColor = new(0.2f, 0.2f, 0.2f, 0.5f);

        // Grid
        public bool showGrid = true;
        public Color gridColor = new(0.3f, 0.3f, 0.3f, 1f);
        public int gridSubdivisions = 4;

        // Overlaps
        public bool highlightOverlaps;
        public Color overlapColor = new(1f, 0.2f, 0.2f, 0.8f);

        // Submesh
        public bool colorBySubmesh;

        // Texel density heatmap
        public bool showTexelDensity;
        public float texelDensityMin = 0.1f;
        public float texelDensityMax = 10f;

        // Island coloring
        public bool colorByIsland;

        // Seams
        public bool showSeams;
        public Color seamColor = new(0f, 1f, 0.2f, 1f);

        // Out-of-bounds
        public bool highlightOutOfBounds;
        public Color outOfBoundsColor = new(1f, 1f, 0f, 0.5f);

        // UDIM
        public bool showUDIM;

        // Vertex density
        public bool showVertexDensity;
        public float vertexDensityRadius = 0.02f;

        // Texture overlay
        public Texture2D overlayTexture;
        public float overlayOpacity = 0.5f;

        // Scene view
        public bool checkerPatternScene;
        public bool texelDensityScene;
        public int checkerScale = 8;

        // Comparison
        public bool compareMode;
        public int compareUVChannel = 1;

        public static readonly int[] ResolutionOptions = { 256, 512, 1024, 2048, 4096 };
    }
}
#endif
