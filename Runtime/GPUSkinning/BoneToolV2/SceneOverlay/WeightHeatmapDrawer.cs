#if UNITY_EDITOR
using UnityEngine;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Generates and renders a vertex-colored mesh overlay showing bone weights as a heatmap.
    /// Blue = 0 weight, green = 0.5, red = 1.0, magenta = unpainted vertex (total weight near 0).
    /// Uses Graphics.DrawMesh with an unlit vertex-color material.
    /// Caches the overlay mesh and only rebuilds when marked dirty.
    /// </summary>
    public class WeightHeatmapDrawer
    {
        private Mesh _overlayMesh;
        private Material _material;
        private bool _isDirty = true;
        private int _lastBoneIndex = -1;
        private int _lastVertexWeightHash;

        /// <summary>
        /// Marks the heatmap as needing a rebuild on the next Draw call.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Draws the weight heatmap overlay for the specified bone index.
        /// </summary>
        public void Draw(RigDocument doc, int boneIndex)
        {
            if (doc == null || doc.sourceMesh == null || doc.vertexWeights == null) return;
            if (boneIndex < 0 || boneIndex >= doc.bones.Count) return;

            EnsureMaterial();

            // Rebuild if dirty or bone changed
            if (_isDirty || boneIndex != _lastBoneIndex || _overlayMesh == null)
            {
                RebuildOverlayMesh(doc, boneIndex);
                _lastBoneIndex = boneIndex;
                _isDirty = false;
            }

            if (_overlayMesh != null && _material != null)
            {
                // Draw slightly offset toward camera to avoid z-fighting
                Graphics.DrawMesh(_overlayMesh, Matrix4x4.identity, _material, 0);
            }
        }

        /// <summary>
        /// Cleans up the overlay mesh and material.
        /// </summary>
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

            // Use a simple unlit vertex color shader
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                // Fallback: try another common unlit shader
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                _material = new Material(shader);
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _material.SetInt("_ZWrite", 0);
                _material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                // Enable vertex colors
                _material.SetPass(0);
            }
        }

        private void RebuildOverlayMesh(RigDocument doc, int boneIndex)
        {
            if (_overlayMesh != null)
                Object.DestroyImmediate(_overlayMesh);

            var sourceMesh = doc.sourceMesh;
            _overlayMesh = Object.Instantiate(sourceMesh);
            _overlayMesh.name = "WeightHeatmapOverlay";

            int vertexCount = sourceMesh.vertexCount;
            var colors = new Color[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                float totalWeight = doc.vertexWeights[v].TotalWeight;

                if (totalWeight < 0.001f)
                {
                    // Unpainted vertex: magenta
                    colors[v] = new Color(1f, 0f, 1f, 0.6f);
                }
                else
                {
                    float w = doc.vertexWeights[v].GetWeight(boneIndex);
                    colors[v] = WeightToColor(w);
                }
            }

            _overlayMesh.colors = colors;

            // Slight scale offset to avoid z-fighting
            var verts = _overlayMesh.vertices;
            var normals = sourceMesh.normals;
            if (normals != null && normals.Length == vertexCount)
            {
                for (int i = 0; i < vertexCount; i++)
                    verts[i] += normals[i] * 0.001f;
                _overlayMesh.vertices = verts;
            }
        }

        /// <summary>
        /// Maps a weight value [0,1] to a heatmap color: blue (0) -> green (0.5) -> red (1.0).
        /// </summary>
        private static Color WeightToColor(float weight)
        {
            weight = Mathf.Clamp01(weight);
            float alpha = 0.5f;

            if (weight < 0.5f)
            {
                // Blue to green
                float t = weight * 2f;
                return new Color(0f, t, 1f - t, alpha);
            }
            else
            {
                // Green to red
                float t = (weight - 0.5f) * 2f;
                return new Color(t, 1f - t, 0f, alpha);
            }
        }
    }
}
#endif
