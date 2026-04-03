#if UNITY_EDITOR
using System.Collections.Generic;
using Snm.Graphics3D.Rigging;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.VertexColor
{
    /// <summary>
    /// Brush-based vertex color painting in the scene view.
    /// Left-drag paints the brush color onto vertices. Ctrl = erase (paint black), Shift = smooth.
    /// Follows the same architecture as WeightPaintMode from the Bone Editor.
    /// </summary>
    public class VertexColorPaintMode
    {
        private VertexColorDocument _doc;
        private BrushSettings _brush;
        private MeshQueryAccel _accel;
        private ColorOverlayDrawer _overlay;
        private Vector3[] _meshVertices;
        private int[] _meshTriangles;
        private bool _isPainting;
        private int _undoGroup;
        private Mesh _cachedMesh;

        public BrushSettings Brush => _brush;

        public VertexColorPaintMode(BrushSettings brush)
        {
            _brush = brush ?? new BrushSettings();
        }

        public void OnEnter(VertexColorDocument doc)
        {
            _doc = doc;
            _isPainting = false;

            if (doc.sourceMesh != null)
            {
                _cachedMesh = doc.sourceMesh;
                _meshVertices = doc.sourceMesh.vertices;
                _meshTriangles = doc.sourceMesh.triangles;
                _accel = new MeshQueryAccel();
                _accel.Build(_meshVertices, _brush.radius * 2f);
            }

            _overlay = new ColorOverlayDrawer();
            doc.EnsureVertexColors();
        }

        public void OnExit()
        {
            _overlay?.Cleanup();
            _overlay = null;
            _accel = null;
            _meshVertices = null;
            _meshTriangles = null;
            _isPainting = false;
            _doc = null;
        }

        public bool OnKeyDown(KeyCode key)
        {
            if (key == KeyCode.LeftBracket)
            {
                _brush.radius = Mathf.Max(0.01f, _brush.radius - 0.02f);
                RebuildAccel();
                return true;
            }
            if (key == KeyCode.RightBracket)
            {
                _brush.radius = Mathf.Min(5f, _brush.radius + 0.02f);
                RebuildAccel();
                return true;
            }
            return false;
        }

        public void OnSceneGUI(SceneView view)
        {
            if (_doc == null || _doc.sourceMesh == null) return;

            // Check if mesh changed
            if (_doc.sourceMesh != _cachedMesh)
            {
                _cachedMesh = _doc.sourceMesh;
                _meshVertices = _doc.sourceMesh.vertices;
                _meshTriangles = _doc.sourceMesh.triangles;
                _doc.EnsureVertexColors();
                RebuildAccel();
            }

            // Draw overlay
            if (_overlay != null)
                _overlay.Draw(_doc);

            // Draw brush cursor
            DrawBrushCursor(view);

            // Handle painting input
            HandlePaintInput(view);

            // Draw help text
            DrawHelpText(view);
        }

        private void DrawBrushCursor(SceneView view)
        {
            var e = Event.current;
            if (e.type != EventType.Repaint) return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (RaycastMesh(ray, out var hitPoint, out var hitNormal))
            {
                // Outer disc: full brush radius
                Handles.color = GetBrushCursorColor();
                Handles.DrawWireDisc(hitPoint, hitNormal, _brush.radius);

                // Inner disc: falloff core
                float innerRadius = _brush.radius * (1f - _brush.falloff);
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.3f);
                Handles.DrawWireDisc(hitPoint, hitNormal, innerRadius);
            }

            view.Repaint();
        }

        private Color GetBrushCursorColor()
        {
            var e = Event.current;
            if (e.control) return Color.red;
            if (e.shift) return Color.cyan;
            // Show the actual brush color for the cursor outline
            return _doc != null ? _doc.brushColor : Color.white;
        }

        private void DrawHelpText(SceneView view)
        {
            Handles.BeginGUI();
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f) },
                alignment = TextAnchor.LowerLeft
            };
            var rect = new Rect(8f, view.position.height - 40f, 500f, 20f);
            GUI.Label(rect, "LMB = Paint | Ctrl = Erase | Shift = Smooth | [ ] = Radius", style);
            Handles.EndGUI();
        }

        private void HandlePaintInput(SceneView view)
        {
            var e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        GUIUtility.hotControl = controlId;
                        _isPainting = true;
                        Undo.IncrementCurrentGroup();
                        _undoGroup = Undo.GetCurrentGroup();
                        PaintAtMouse();
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isPainting && e.button == 0)
                    {
                        PaintAtMouse();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isPainting && e.button == 0)
                    {
                        _isPainting = false;
                        GUIUtility.hotControl = 0;
                        Undo.CollapseUndoOperations(_undoGroup);
                        e.Use();
                    }
                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
            }
        }

        private void PaintAtMouse()
        {
            if (_meshVertices == null || _doc.vertexColors == null) return;

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!RaycastMesh(ray, out var hitPoint, out _)) return;

            _doc.Record("Paint Vertex Color");

            // Determine operation from modifier keys
            var op = _brush.operation;
            if (Event.current.control) op = BrushSettings.BrushOp.Subtract;
            if (Event.current.shift) op = BrushSettings.BrushOp.Smooth;

            // Get vertices in brush sphere
            List<int> affected;
            if (_accel != null)
            {
                affected = _accel.GetVerticesInSphere(hitPoint, _brush.radius);
            }
            else
            {
                affected = new List<int>();
                for (int i = 0; i < _meshVertices.Length; i++)
                {
                    if (Vector3.Distance(_meshVertices[i], hitPoint) <= _brush.radius)
                        affected.Add(i);
                }
            }

            if (affected.Count == 0) return;

            // Pre-compute neighbor averages for smooth operation
            Color[] neighborAverages = null;
            if (op == BrushSettings.BrushOp.Smooth)
                neighborAverages = ComputeNeighborAverages(affected);

            for (int a = 0; a < affected.Count; a++)
            {
                int vi = affected[a];
                float dist = Vector3.Distance(_meshVertices[vi], hitPoint);
                float normalizedDist = dist / _brush.radius;

                float falloffFactor = 1f - Mathf.Pow(normalizedDist, 1f / Mathf.Max(_brush.falloff, 0.01f));
                falloffFactor = Mathf.Clamp01(falloffFactor);

                float paintAmount = _brush.strength * falloffFactor;
                Color current = _doc.vertexColors[vi];

                switch (op)
                {
                    case BrushSettings.BrushOp.Add:
                        _doc.vertexColors[vi] = Color.Lerp(current, _doc.brushColor, paintAmount);
                        break;

                    case BrushSettings.BrushOp.Subtract:
                        _doc.vertexColors[vi] = Color.Lerp(current, Color.black, paintAmount);
                        break;

                    case BrushSettings.BrushOp.Smooth:
                        if (neighborAverages != null)
                            _doc.vertexColors[vi] = Color.Lerp(current, neighborAverages[a], paintAmount);
                        break;
                }
            }

            if (_overlay != null)
                _overlay.MarkDirty();
        }

        private Color[] ComputeNeighborAverages(List<int> affected)
        {
            var averages = new Color[affected.Count];
            for (int a = 0; a < affected.Count; a++)
            {
                Color sum = Color.clear;
                int count = 0;
                var pos = _meshVertices[affected[a]];

                for (int b = 0; b < affected.Count; b++)
                {
                    if (a == b) continue;
                    float d = Vector3.Distance(pos, _meshVertices[affected[b]]);
                    if (d < _brush.radius * 0.5f)
                    {
                        sum += _doc.vertexColors[affected[b]];
                        count++;
                    }
                }

                averages[a] = count > 0
                    ? new Color(sum.r / count, sum.g / count, sum.b / count, sum.a / count)
                    : _doc.vertexColors[affected[a]];
            }
            return averages;
        }

        #region Mesh Raycast

        private bool RaycastMesh(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;

            if (_doc == null || _doc.sourceMesh == null || _meshVertices == null || _meshTriangles == null)
                return false;

            var bounds = _doc.sourceMesh.bounds;
            if (!bounds.IntersectRay(ray, out _))
            {
                var expandedBounds = bounds;
                expandedBounds.Expand(0.5f);
                if (!expandedBounds.IntersectRay(ray))
                    return false;
            }

            float closestDist = float.MaxValue;
            bool hit = false;

            for (int t = 0; t < _meshTriangles.Length; t += 3)
            {
                var v0 = _meshVertices[_meshTriangles[t]];
                var v1 = _meshVertices[_meshTriangles[t + 1]];
                var v2 = _meshVertices[_meshTriangles[t + 2]];

                if (RayTriangleIntersect(ray, v0, v1, v2, out float dist))
                {
                    if (dist > 0f && dist < closestDist)
                    {
                        closestDist = dist;
                        hitPoint = ray.GetPoint(dist);
                        hitNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                        hit = true;
                    }
                }
            }

            if (!hit)
            {
                var plane = new Plane(Vector3.up, bounds.center);
                if (plane.Raycast(ray, out float enter))
                {
                    hitPoint = ray.GetPoint(enter);
                    hitNormal = Vector3.up;
                    return true;
                }
            }

            return hit;
        }

        private static bool RayTriangleIntersect(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
        {
            t = 0f;
            const float epsilon = 1e-8f;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -epsilon && a < epsilon) return false;

            float f = 1f / a;
            var s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;

            var q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f) return false;

            t = f * Vector3.Dot(edge2, q);
            return t > epsilon;
        }

        #endregion

        public void RebuildAccel()
        {
            if (_doc != null && _doc.sourceMesh != null)
            {
                _cachedMesh = _doc.sourceMesh;
                _meshVertices = _doc.sourceMesh.vertices;
                _meshTriangles = _doc.sourceMesh.triangles;
            }
            if (_meshVertices != null)
            {
                _accel = new MeshQueryAccel();
                _accel.Build(_meshVertices, _brush.radius * 2f);
            }
        }
    }
}
#endif
