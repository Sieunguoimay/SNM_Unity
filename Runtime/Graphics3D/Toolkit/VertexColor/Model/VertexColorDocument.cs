#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.VertexColor
{
    /// <summary>
    /// Central data model for the vertex color painter. Lives in memory only (never saved to disk).
    /// All mutations should go through Undo.RecordObject for full undo/redo support.
    /// </summary>
    public class VertexColorDocument : ScriptableObject
    {
        public Mesh sourceMesh;
        public Color[] vertexColors;
        public Color brushColor = Color.white;

        /// <summary>
        /// Ensures the vertexColors array matches the source mesh vertex count.
        /// Initializes new vertices to white.
        /// </summary>
        public void EnsureVertexColors()
        {
            if (sourceMesh == null) return;
            int vertCount = sourceMesh.vertexCount;
            if (vertexColors != null && vertexColors.Length == vertCount) return;

            Undo.RecordObject(this, "Init Vertex Colors");
            var newColors = new Color[vertCount];

            // Preserve existing colors if array was just the wrong size
            if (vertexColors != null)
            {
                int copyCount = Mathf.Min(vertexColors.Length, vertCount);
                System.Array.Copy(vertexColors, newColors, copyCount);
                for (int i = copyCount; i < vertCount; i++)
                    newColors[i] = Color.white;
            }
            else
            {
                // Try loading existing colors from mesh
                var existing = sourceMesh.colors;
                if (existing != null && existing.Length == vertCount)
                {
                    System.Array.Copy(existing, newColors, vertCount);
                }
                else
                {
                    for (int i = 0; i < vertCount; i++)
                        newColors[i] = Color.white;
                }
            }

            vertexColors = newColors;
        }

        /// <summary>
        /// Fills all vertex colors with the given color.
        /// </summary>
        public void FillAll(Color color)
        {
            if (vertexColors == null) return;
            Record("Fill All Colors");
            for (int i = 0; i < vertexColors.Length; i++)
                vertexColors[i] = color;
        }

        /// <summary>
        /// Resets all vertex colors to white.
        /// </summary>
        public void ClearAll()
        {
            FillAll(Color.white);
        }

        public void Record(string operationName)
        {
            Undo.RecordObject(this, operationName);
        }
    }
}
#endif
