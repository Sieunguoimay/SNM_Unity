#if UNITY_EDITOR
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class MeshToolStyles
    {
        // Selection colors
        public static readonly Color VertexColor = new(1f, 0.6f, 0f, 1f);
        public static readonly Color VertexSelectedColor = new(1f, 1f, 0f, 1f);
        public static readonly Color EdgeColor = new(0.8f, 0.8f, 0.8f, 0.5f);
        public static readonly Color EdgeSelectedColor = new(0f, 0.8f, 1f, 1f);
        public static readonly Color FaceSelectedColor = new(0f, 0.5f, 1f, 0.25f);
        public static readonly Color FaceHoverColor = new(1f, 1f, 1f, 0.1f);

        // Wireframe
        public static readonly Color WireframeColor = new(0f, 0f, 0f, 0.3f);
        public static readonly Color WireframeOverlayColor = new(0.2f, 0.2f, 0.2f, 0.5f);

        // Normals
        public static readonly Color NormalColor = new(0.3f, 0.5f, 1f, 1f);
        public static readonly Color TangentColor = new(1f, 0.3f, 0.3f, 1f);
        public static readonly Color BitangentColor = new(0.3f, 1f, 0.3f, 1f);

        // Handle sizes
        public const float VertexHandleSize = 0.02f;
        public const float VertexPickSize = 0.04f;
        public const float EdgePickDistance = 8f; // screen pixels
        public const float NormalLineLength = 0.15f;

        // Scene GUI
        public const float PanelWidth = 200f;
        public const float PanelPadding = 8f;
    }
}
#endif
