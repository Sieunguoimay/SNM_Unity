#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Weight painting mode: brush-based painting of bone weights onto mesh vertices.
    /// Left-drag paints weight for the selected bone. Ctrl = erase, Shift = smooth.
    /// Auto-normalizes after each stroke. Falls back to click-per-vertex when MeshQueryAccel
    /// is unavailable.
    /// </summary>
    public class WeightPaintMode : IToolMode
    {
        private RigDocument _doc;
        private BrushSettings _brush;
        private MeshQueryAccel _accel;
        private WeightHeatmapDrawer _heatmap;
        private Material _plainMeshMaterial;
        private Vector3[] _meshVertices;
        private int[] _meshTriangles;
        private bool _isPainting;
        private int _undoGroup;
        private Mesh _cachedMesh;

        // Cached color legend textures
        private static Texture2D _legendRed;
        private static Texture2D _legendGreen;
        private static Texture2D _legendBlue;
        private static Texture2D _legendMagenta;

        public string DisplayName => "Paint";

        public BrushSettings Brush => _brush;

        public WeightPaintMode()
        {
            _brush = new BrushSettings();
        }

        public WeightPaintMode(BrushSettings brush)
        {
            _brush = brush ?? new BrushSettings();
        }

        public void OnEnter(RigDocument doc)
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

            _heatmap = new WeightHeatmapDrawer();
            doc.EnsureVertexWeights();
        }

        private void DrawPlainMesh(SceneView view)
        {
            if (_doc.sourceMesh == null) return;
            if (_plainMeshMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) return;
                _plainMeshMaterial = new Material(shader);
                _plainMeshMaterial.SetInt("_ZWrite", 1);
                _plainMeshMaterial.SetInt("_Cull", 0);
                _plainMeshMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.4f));
            }
            for (int sub = 0; sub < _doc.sourceMesh.subMeshCount; sub++)
                Graphics.DrawMesh(_doc.sourceMesh, Matrix4x4.identity, _plainMeshMaterial, 0, view.camera, sub);
        }

        public void OnExit()
        {
            _heatmap?.Cleanup();
            _heatmap = null;
            if (_plainMeshMaterial != null) { Object.DestroyImmediate(_plainMeshMaterial); _plainMeshMaterial = null; }
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
            if (key == KeyCode.A)
            {
                if (_doc == null || _doc.sourceMesh == null || _doc.bones.Count == 0) return false;
                if (HasAnyExistingWeights())
                {
                    if (!EditorUtility.DisplayDialog("Auto Weights",
                        "This will overwrite existing weights. Continue?", "Yes", "Cancel"))
                        return true;
                }
                AutoWeightService.AssignAutoWeights(_doc);
                if (_heatmap != null) _heatmap.MarkDirty();
                return true;
            }
            return false;
        }

        private bool HasAnyExistingWeights()
        {
            if (_doc?.vertexWeights == null) return false;
            for (int i = 0; i < _doc.vertexWeights.Length; i++)
                if (_doc.vertexWeights[i].TotalWeight > 0.001f) return true;
            return false;
        }

        public void OnSceneGUI(SceneView view)
        {
            if (_doc == null || _doc.sourceMesh == null) return;

            // Check if mesh changed and rebuild accel if needed (#9)
            if (_doc.sourceMesh != _cachedMesh)
            {
                _cachedMesh = _doc.sourceMesh;
                _meshVertices = _doc.sourceMesh.vertices;
                _meshTriangles = _doc.sourceMesh.triangles;
                RebuildAccel();
            }

            // Draw mesh — heatmap if bone selected, plain mesh otherwise
            if (_doc.selectedBoneIndex >= 0 && _heatmap != null)
            {
                _heatmap.Draw(_doc, _doc.selectedBoneIndex);
            }
            else
            {
                DrawPlainMesh(view);
            }

            // Draw bones (dimmed) for reference
            BoneGizmoDrawer.DrawAllBones(_doc, Matrix4x4.identity);

            // Draw brush circle at mouse position
            DrawBrushCursor(view);

            // Handle painting input — block if no bone selected (#1)
            if (_doc.selectedBoneIndex < 0)
            {
                // Consume mouse events so they don't fall through
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    int controlId = GUIUtility.GetControlID(FocusType.Passive);
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                else if (e.type == EventType.Layout)
                {
                    int controlId = GUIUtility.GetControlID(FocusType.Passive);
                    HandleUtility.AddDefaultControl(controlId);
                }
            }
            else
            {
                HandlePaintInput(view);
            }

            // Draw "select a bone" label when no bone selected (#1)
            if (_doc.selectedBoneIndex < 0)
            {
                Handles.BeginGUI();
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(1f, 0.8f, 0.2f) },
                    alignment = TextAnchor.MiddleCenter
                };
                var rect = new Rect(view.position.width * 0.5f - 150f, 30f, 300f, 30f);
                GUI.Label(rect, "Select a bone to start painting", style);
                Handles.EndGUI();
            }

            // Draw color legend in top-right (#4)
            DrawColorLegend(view);

            // Draw help text in bottom-left (#3)
            DrawHelpText(view);
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void DrawColorLegend(SceneView view)
        {
            if (_doc.selectedBoneIndex < 0) return;

            if (_legendRed == null) _legendRed = MakeColorTexture(Color.red);
            if (_legendGreen == null) _legendGreen = MakeColorTexture(Color.green);
            if (_legendBlue == null) _legendBlue = MakeColorTexture(Color.blue);
            if (_legendMagenta == null) _legendMagenta = MakeColorTexture(Color.magenta);

            Handles.BeginGUI();
            float x = view.position.width - 90f;
            float y = 10f;
            float swatchW = 16f;
            float swatchH = 12f;
            float rowH = 18f;

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = Color.white } };

            GUI.DrawTexture(new Rect(x, y, swatchW, swatchH), _legendRed);
            GUI.Label(new Rect(x + swatchW + 4f, y - 1f, 50f, rowH), "1.0", labelStyle);
            y += rowH;

            GUI.DrawTexture(new Rect(x, y, swatchW, swatchH), _legendGreen);
            GUI.Label(new Rect(x + swatchW + 4f, y - 1f, 50f, rowH), "0.5", labelStyle);
            y += rowH;

            GUI.DrawTexture(new Rect(x, y, swatchW, swatchH), _legendBlue);
            GUI.Label(new Rect(x + swatchW + 4f, y - 1f, 50f, rowH), "0.0", labelStyle);
            y += rowH;

            GUI.DrawTexture(new Rect(x, y, swatchW, swatchH), _legendMagenta);
            GUI.Label(new Rect(x + swatchW + 4f, y - 1f, 50f, rowH), "None", labelStyle);

            Handles.EndGUI();
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
            GUI.Label(rect, "LMB = Paint | Ctrl = Erase | Shift = Smooth | [ ] = Radius | A = Auto Weight", style);
            Handles.EndGUI();
        }

        private void DrawBrushCursor(SceneView view)
        {
            var e = Event.current;
            if (e.type != EventType.Repaint) return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            // Raycast against an approximate surface
            if (RaycastMesh(ray, out var hitPoint, out var hitNormal))
            {
                Handles.color = GetBrushColor();
                Handles.DrawWireDisc(hitPoint, hitNormal, _brush.radius);

                // Inner disc for falloff
                float innerRadius = _brush.radius * (1f - _brush.falloff);
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.3f);
                Handles.DrawWireDisc(hitPoint, hitNormal, innerRadius);
            }

            // Force repaint so the brush follows the mouse
            view.Repaint();
        }

        private Color GetBrushColor()
        {
            var e = Event.current;
            if (e.control) return Color.red;    // Erase
            if (e.shift) return Color.blue;     // Smooth
            return Color.green;                  // Add
        }

        private void HandlePaintInput(SceneView view)
        {
            if (_doc.selectedBoneIndex < 0 || _doc.selectedBoneIndex >= _doc.bones.Count) return;

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
            if (_meshVertices == null || _doc.vertexWeights == null) return;

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!RaycastMesh(ray, out var hitPoint, out _)) return;

            UndoHelper.Record(_doc, "Paint Weight");

            // Determine operation from modifier keys
            var op = _brush.operation;
            if (Event.current.control) op = BrushSettings.BrushOp.Subtract;
            if (Event.current.shift) op = BrushSettings.BrushOp.Smooth;

            int boneIdx = _doc.selectedBoneIndex;

            // Get vertices in brush sphere
            List<int> affected;
            if (_accel != null)
            {
                affected = _accel.GetVerticesInSphere(hitPoint, _brush.radius);
            }
            else
            {
                // Fallback: brute-force search
                affected = new List<int>();
                for (int i = 0; i < _meshVertices.Length; i++)
                {
                    if (Vector3.Distance(_meshVertices[i], hitPoint) <= _brush.radius)
                        affected.Add(i);
                }
            }

            if (affected.Count == 0) return;

            // Pre-compute neighbor averages for smooth operation
            float[] neighborAverages = null;
            if (op == BrushSettings.BrushOp.Smooth)
            {
                neighborAverages = ComputeNeighborAverages(affected, boneIdx);
            }

            for (int a = 0; a < affected.Count; a++)
            {
                int vi = affected[a];
                float dist = Vector3.Distance(_meshVertices[vi], hitPoint);
                float normalizedDist = dist / _brush.radius;

                // Compute falloff: 1.0 at center, decreasing toward edge
                float falloffFactor = 1f - Mathf.Pow(normalizedDist, 1f / Mathf.Max(_brush.falloff, 0.01f));
                falloffFactor = Mathf.Clamp01(falloffFactor);

                float paintAmount = _brush.strength * falloffFactor;

                float currentWeight = _doc.vertexWeights[vi].GetWeight(boneIdx);

                switch (op)
                {
                    case BrushSettings.BrushOp.Add:
                        _doc.vertexWeights[vi].SetWeight(boneIdx, Mathf.Clamp01(currentWeight + paintAmount));
                        break;

                    case BrushSettings.BrushOp.Subtract:
                        _doc.vertexWeights[vi].SetWeight(boneIdx, Mathf.Max(0f, currentWeight - paintAmount));
                        break;

                    case BrushSettings.BrushOp.Smooth:
                        if (neighborAverages != null)
                        {
                            float smoothed = Mathf.Lerp(currentWeight, neighborAverages[a], paintAmount);
                            _doc.vertexWeights[vi].SetWeight(boneIdx, smoothed);
                        }
                        break;
                }

                _doc.vertexWeights[vi].Normalize();
            }

            // Mark heatmap as dirty
            if (_heatmap != null)
                _heatmap.MarkDirty();
        }

        /// <summary>
        /// Computes the average weight of neighboring vertices (within brush) for smoothing.
        /// </summary>
        private float[] ComputeNeighborAverages(List<int> affected, int boneIdx)
        {
            var averages = new float[affected.Count];
            for (int a = 0; a < affected.Count; a++)
            {
                float sum = 0f;
                int count = 0;
                var pos = _meshVertices[affected[a]];

                for (int b = 0; b < affected.Count; b++)
                {
                    if (a == b) continue;
                    float d = Vector3.Distance(pos, _meshVertices[affected[b]]);
                    if (d < _brush.radius * 0.5f)
                    {
                        sum += _doc.vertexWeights[affected[b]].GetWeight(boneIdx);
                        count++;
                    }
                }

                averages[a] = count > 0 ? sum / count : _doc.vertexWeights[affected[a]].GetWeight(boneIdx);
            }
            return averages;
        }

        /// <summary>
        /// Performs a simple raycast against the mesh triangles.
        /// Uses a basic approach: cast ray against mesh bounds, then check triangles near the hit.
        /// </summary>
        private bool RaycastMesh(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;

            if (_doc == null || _doc.sourceMesh == null || _meshVertices == null || _meshTriangles == null) return false;

            var mesh = _doc.sourceMesh;
            var bounds = mesh.bounds;

            // Quick bounds check
            if (!bounds.IntersectRay(ray, out float boundsEnter))
            {
                // Try expanding bounds
                var expandedBounds = bounds;
                expandedBounds.Expand(0.5f);
                if (!expandedBounds.IntersectRay(ray))
                    return false;
            }

            // Brute-force triangle raycast
            var vertices = _meshVertices;
            var triangles = _meshTriangles;
            float closestDist = float.MaxValue;
            bool hit = false;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                var v0 = vertices[triangles[t]];
                var v1 = vertices[triangles[t + 1]];
                var v2 = vertices[triangles[t + 2]];

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

            // Fallback to a plane if no triangle hit
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

        /// <summary>
        /// Moller-Trumbore ray-triangle intersection.
        /// </summary>
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
