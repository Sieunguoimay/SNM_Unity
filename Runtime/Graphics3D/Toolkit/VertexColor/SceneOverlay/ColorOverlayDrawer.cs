#if UNITY_EDITOR
using UnityEngine;

namespace Snm.Graphics3D.VertexColor
{
    /// <summary>
    /// Renders vertex colors as a mesh overlay in the scene view.
    /// Clones the source mesh and applies vertex colors directly.
    /// Caches the overlay mesh and only rebuilds when marked dirty.
    /// </summary>
    public class ColorOverlayDrawer
    {
        private Mesh _overlayMesh;
        private Material _material;
        private bool _isDirty = true;

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void Draw(VertexColorDocument doc)
        {
            if (doc == null || doc.sourceMesh == null || doc.vertexColors == null) return;

            EnsureMaterial();

            if (_isDirty || _overlayMesh == null)
            {
                RebuildOverlayMesh(doc);
                _isDirty = false;
            }

            if (_overlayMesh != null && _material != null)
            {
                Graphics.DrawMesh(_overlayMesh, Matrix4x4.identity, _material, 0);
            }
        }

        public void Cleanup()
        {
            if (_overlayMesh != null)
            {
                Object.DestroyImmediate(_overlayMesh);
                _overlayMesh = null;
            }

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
                _material = null;
            }
        }

        private void EnsureMaterial()
        {
            if (_material != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                _material = new Material(shader);
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _material.SetInt("_ZWrite", 1);
                _material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
        }

        private void RebuildOverlayMesh(VertexColorDocument doc)
        {
            if (_overlayMesh != null)
                Object.DestroyImmediate(_overlayMesh);

            var sourceMesh = doc.sourceMesh;
            _overlayMesh = Object.Instantiate(sourceMesh);
            _overlayMesh.name = "VertexColorOverlay";

            int vertexCount = sourceMesh.vertexCount;
            var colors = new Color[vertexCount];
            int copyCount = Mathf.Min(doc.vertexColors.Length, vertexCount);
            System.Array.Copy(doc.vertexColors, colors, copyCount);
            _overlayMesh.colors = colors;

            // Slight offset along normals to avoid z-fighting
            var verts = _overlayMesh.vertices;
            var normals = sourceMesh.normals;
            if (normals != null && normals.Length == vertexCount)
            {
                for (int i = 0; i < vertexCount; i++)
                    verts[i] += normals[i] * 0.001f;
                _overlayMesh.vertices = verts;
            }
        }
    }
}
#endif
