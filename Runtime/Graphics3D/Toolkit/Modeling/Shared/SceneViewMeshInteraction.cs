#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public static class SceneViewMeshInteraction
    {
        #region Picking

        public static int PickVertex(EditableMesh mesh, Matrix4x4 localToWorld, Event evt,
            float maxScreenDistance = -1)
        {
            if (maxScreenDistance < 0) maxScreenDistance = MeshToolStyles.VertexPickSize * 200f;

            Vector2 mousePos = evt.mousePosition;
            float bestDist = maxScreenDistance;
            int bestIdx = -1;

            Camera cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null) return -1;

            for (int i = 0; i < mesh.VertexCount; i++)
            {
                Vector3 worldPos = localToWorld.MultiplyPoint3x4(mesh.Positions[i]);
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                float dist = Vector2.Distance(mousePos, screenPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx;
        }

        public static (int v0, int v1) PickEdge(EditableMesh mesh, Matrix4x4 localToWorld, Event evt)
        {
            Vector2 mousePos = evt.mousePosition;
            float bestDist = MeshToolStyles.EdgePickDistance;
            int bestV0 = -1, bestV1 = -1;

            var edges = mesh.GetAllEdges();
            foreach (long key in edges)
            {
                var (v0, v1) = EditableMesh.EdgeFromKey(key);
                Vector3 w0 = localToWorld.MultiplyPoint3x4(mesh.Positions[v0]);
                Vector3 w1 = localToWorld.MultiplyPoint3x4(mesh.Positions[v1]);

                Vector2 s0 = HandleUtility.WorldToGUIPoint(w0);
                Vector2 s1 = HandleUtility.WorldToGUIPoint(w1);

                float dist = HandleUtility.DistancePointToLineSegment(mousePos, s0, s1);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestV0 = v0;
                    bestV1 = v1;
                }
            }

            return (bestV0, bestV1);
        }

        public static int PickFace(EditableMesh mesh, Matrix4x4 localToWorld, Event evt)
        {
            Camera cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null) return -1;

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (MeshGeometryUtils.RayMeshIntersection(ray, mesh.Positions, mesh.Triangles,
                    localToWorld, out int triIdx, out _, out _))
                return triIdx;

            return -1;
        }

        #endregion

        #region Box Selection

        public static HashSet<int> BoxSelectVertices(
            EditableMesh mesh, Matrix4x4 localToWorld, Rect screenRect)
        {
            var result = new HashSet<int>();
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                Vector3 worldPos = localToWorld.MultiplyPoint3x4(mesh.Positions[i]);
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                if (screenRect.Contains(screenPos))
                    result.Add(i);
            }
            return result;
        }

        public static HashSet<int> BoxSelectFaces(
            EditableMesh mesh, Matrix4x4 localToWorld, Rect screenRect)
        {
            var result = new HashSet<int>();
            for (int t = 0; t < mesh.TriangleCount; t++)
            {
                int i0 = mesh.Triangles[t * 3], i1 = mesh.Triangles[t * 3 + 1], i2 = mesh.Triangles[t * 3 + 2];
                Vector2 s0 = HandleUtility.WorldToGUIPoint(localToWorld.MultiplyPoint3x4(mesh.Positions[i0]));
                Vector2 s1 = HandleUtility.WorldToGUIPoint(localToWorld.MultiplyPoint3x4(mesh.Positions[i1]));
                Vector2 s2 = HandleUtility.WorldToGUIPoint(localToWorld.MultiplyPoint3x4(mesh.Positions[i2]));

                // Face is selected if its center is inside the rect
                Vector2 center = (s0 + s1 + s2) / 3f;
                if (screenRect.Contains(center))
                    result.Add(t);
            }
            return result;
        }

        #endregion

        #region Drawing

        static Material _overlayMaterial;

        static Material OverlayMaterial
        {
            get
            {
                if (_overlayMaterial == null)
                {
                    var shader = Shader.Find("Hidden/Internal-Colored");
                    if (shader == null) return null;
                    _overlayMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    _overlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _overlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _overlayMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _overlayMaterial.SetInt("_ZWrite", 0);
                    _overlayMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                return _overlayMaterial;
            }
        }

        public static void DrawVertexHandles(
            EditableMesh mesh, MeshSelection selection, Matrix4x4 localToWorld)
        {
            Camera cam = SceneView.lastActiveSceneView?.camera;
            if (cam == null) return;

            float size = MeshToolStyles.VertexHandleSize;

            for (int i = 0; i < mesh.VertexCount; i++)
            {
                Vector3 worldPos = localToWorld.MultiplyPoint3x4(mesh.Positions[i]);
                float handleSize = HandleUtility.GetHandleSize(worldPos) * size;

                bool selected = selection.Vertices.Contains(i);
                Handles.color = selected ? MeshToolStyles.VertexSelectedColor : MeshToolStyles.VertexColor;
                Handles.DotHandleCap(0, worldPos, Quaternion.identity, handleSize, EventType.Repaint);
            }
        }

        public static void DrawEdgeHighlights(
            EditableMesh mesh, MeshSelection selection, Matrix4x4 localToWorld)
        {
            if (selection.Edges.Count == 0) return;

            Handles.color = MeshToolStyles.EdgeSelectedColor;
            foreach (long key in selection.Edges)
            {
                var (v0, v1) = EditableMesh.EdgeFromKey(key);
                Vector3 w0 = localToWorld.MultiplyPoint3x4(mesh.Positions[v0]);
                Vector3 w1 = localToWorld.MultiplyPoint3x4(mesh.Positions[v1]);
                Handles.DrawLine(w0, w1, 2f);
            }
        }

        public static void DrawFaceHighlights(
            EditableMesh mesh, MeshSelection selection, Matrix4x4 localToWorld)
        {
            if (selection.Faces.Count == 0) return;
            if (OverlayMaterial == null) return;

            OverlayMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(localToWorld);

            GL.Begin(GL.TRIANGLES);
            GL.Color(MeshToolStyles.FaceSelectedColor);

            foreach (int f in selection.Faces)
            {
                int i0 = mesh.Triangles[f * 3], i1 = mesh.Triangles[f * 3 + 1], i2 = mesh.Triangles[f * 3 + 2];
                GL.Vertex(mesh.Positions[i0]);
                GL.Vertex(mesh.Positions[i1]);
                GL.Vertex(mesh.Positions[i2]);
            }

            GL.End();
            GL.PopMatrix();
        }

        public static void DrawWireframeOverlay(
            EditableMesh mesh, Matrix4x4 localToWorld, Color color)
        {
            if (OverlayMaterial == null) return;
            OverlayMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(localToWorld);

            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                int i0 = mesh.Triangles[i], i1 = mesh.Triangles[i + 1], i2 = mesh.Triangles[i + 2];
                Vector3 a = mesh.Positions[i0], b = mesh.Positions[i1], c = mesh.Positions[i2];
                GL.Vertex(a); GL.Vertex(b);
                GL.Vertex(b); GL.Vertex(c);
                GL.Vertex(c); GL.Vertex(a);
            }

            GL.End();
            GL.PopMatrix();
        }

        #endregion
    }
}
#endif
