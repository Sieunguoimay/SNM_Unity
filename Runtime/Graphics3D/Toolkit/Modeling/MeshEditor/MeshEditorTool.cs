#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    [EditorTool("Mesh Editor", typeof(MeshFilter))]
    public class MeshEditorTool : EditorTool
    {
        EditableMesh _editMesh;
        MeshSelection _selection;
        MeshFilter _meshFilter;
        Mesh _mesh;

        // Gizmo state
        Tool _activeTool = Tool.Move;
        Vector3 _handlePosition;
        Quaternion _handleRotation = Quaternion.identity;

        // Box selection
        bool _boxSelecting;
        Vector2 _boxStart;

        // Extrude drag
        bool _extruding;
        Vector3 _extrudeStartPos;

        public override GUIContent toolbarIcon =>
            new("ME", "Mesh Editor — Select and modify vertices, edges, faces");

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        void OnUndoRedo()
        {
            if (_mesh != null)
            {
                _editMesh = EditableMesh.FromMesh(_mesh);
                SceneView.RepaintAll();
            }
        }

        bool EnsureTarget()
        {
            if (target is not MeshFilter mf) return false;
            if (mf.sharedMesh == null) return false;

            if (_meshFilter != mf || _mesh != mf.sharedMesh)
            {
                _meshFilter = mf;
                _mesh = mf.sharedMesh;

                if (!_mesh.isReadable)
                {
                    // Try instantiate
                    var copy = Object.Instantiate(_mesh);
                    if (copy.vertexCount > 0)
                    {
                        MeshUndoHelper.RecordMeshFilter(_meshFilter, "Make Mesh Editable");
                        MeshUndoHelper.RegisterCreatedMesh(copy, "Make Mesh Editable");
                        _mesh = copy;
                        _mesh.name = mf.sharedMesh.name;
                        _meshFilter.sharedMesh = _mesh;
                    }
                }

                _editMesh = EditableMesh.FromMesh(_mesh);
                _selection = MeshSelection.GetOrCreate(_mesh);
            }
            return true;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;
            if (!EnsureTarget()) return;

            Matrix4x4 localToWorld = _meshFilter.transform.localToWorldMatrix;
            Event evt = Event.current;

            // Draw wireframe and selection
            SceneViewMeshInteraction.DrawWireframeOverlay(_editMesh, localToWorld, MeshToolStyles.WireframeColor);

            switch (_selection.Mode)
            {
                case SelectionMode.Vertex:
                    SceneViewMeshInteraction.DrawVertexHandles(_editMesh, _selection, localToWorld);
                    break;
                case SelectionMode.Edge:
                    SceneViewMeshInteraction.DrawEdgeHighlights(_editMesh, _selection, localToWorld);
                    break;
                case SelectionMode.Face:
                    SceneViewMeshInteraction.DrawFaceHighlights(_editMesh, _selection, localToWorld);
                    break;
            }

            // Handle input
            HandleKeyboardShortcuts(evt);
            HandleMouseInput(evt, localToWorld);
            HandleBoxSelection(evt, localToWorld);

            // Manipulation handle
            if (_selection.HasSelection)
                DrawManipulationHandle(localToWorld);

            // In-scene control panel
            DrawSceneGUI();
        }

        #region Input Handling

        void HandleKeyboardShortcuts(Event evt)
        {
            if (evt.type != EventType.KeyDown) return;

            switch (evt.keyCode)
            {
                case KeyCode.Alpha1:
                    _selection.Mode = SelectionMode.Vertex;
                    evt.Use();
                    break;
                case KeyCode.Alpha2:
                    _selection.Mode = SelectionMode.Edge;
                    evt.Use();
                    break;
                case KeyCode.Alpha3:
                    _selection.Mode = SelectionMode.Face;
                    evt.Use();
                    break;
                case KeyCode.A when evt.control:
                    _selection.SelectAll(_editMesh);
                    evt.Use();
                    break;
                case KeyCode.I when evt.control:
                    _selection.InvertSelection(_editMesh);
                    evt.Use();
                    break;
                case KeyCode.Delete:
                case KeyCode.X when !evt.control:
                    DeleteSelection();
                    evt.Use();
                    break;
                case KeyCode.W:
                    _activeTool = Tool.Move;
                    evt.Use();
                    break;
                case KeyCode.E when !evt.control:
                    _activeTool = Tool.Rotate;
                    evt.Use();
                    break;
                case KeyCode.R:
                    _activeTool = Tool.Scale;
                    evt.Use();
                    break;
            }
        }

        void HandleMouseInput(Event evt, Matrix4x4 localToWorld)
        {
            if (evt.type != EventType.MouseDown || evt.button != 0) return;
            if (evt.alt) return; // don't interfere with orbit

            // Check if click is on a handle — if so, let handles process it
            int controlId = HandleUtility.nearestControl;
            if (controlId != 0 && GUIUtility.hotControl != 0) return;

            bool additive = evt.shift;
            bool subtractive = evt.control;

            switch (_selection.Mode)
            {
                case SelectionMode.Vertex:
                {
                    int v = SceneViewMeshInteraction.PickVertex(_editMesh, localToWorld, evt);
                    if (v >= 0)
                    {
                        if (!additive && !subtractive) _selection.Clear();
                        if (subtractive) _selection.Vertices.Remove(v);
                        else _selection.Vertices.Add(v);
                        evt.Use();
                    }
                    else if (!additive && !subtractive)
                    {
                        _selection.Clear();
                        evt.Use();
                    }
                    break;
                }
                case SelectionMode.Edge:
                {
                    var (v0, v1) = SceneViewMeshInteraction.PickEdge(_editMesh, localToWorld, evt);
                    if (v0 >= 0)
                    {
                        long key = EditableMesh.EdgeKey(v0, v1);
                        if (!additive && !subtractive) _selection.Clear();

                        // Double-click for loop select
                        if (evt.clickCount == 2)
                        {
                            var loop = _editMesh.GetEdgeLoop(v0, v1);
                            foreach (long k in loop) _selection.Edges.Add(k);
                        }
                        else
                        {
                            if (subtractive) _selection.Edges.Remove(key);
                            else _selection.Edges.Add(key);
                        }
                        evt.Use();
                    }
                    else if (!additive && !subtractive)
                    {
                        _selection.Clear();
                        evt.Use();
                    }
                    break;
                }
                case SelectionMode.Face:
                {
                    int f = SceneViewMeshInteraction.PickFace(_editMesh, localToWorld, evt);
                    if (f >= 0)
                    {
                        if (!additive && !subtractive) _selection.Clear();
                        if (subtractive) _selection.Faces.Remove(f);
                        else _selection.Faces.Add(f);
                        evt.Use();
                    }
                    else if (!additive && !subtractive)
                    {
                        _selection.Clear();
                        evt.Use();
                    }
                    break;
                }
            }
        }

        void HandleBoxSelection(Event evt, Matrix4x4 localToWorld)
        {
            if (evt.type == EventType.MouseDown && evt.button == 0 && evt.shift && evt.alt)
            {
                _boxSelecting = true;
                _boxStart = evt.mousePosition;
                evt.Use();
            }

            if (_boxSelecting && evt.type == EventType.MouseDrag)
            {
                // Draw box
                Vector2 current = evt.mousePosition;
                Rect boxRect = new(
                    Mathf.Min(_boxStart.x, current.x), Mathf.Min(_boxStart.y, current.y),
                    Mathf.Abs(current.x - _boxStart.x), Mathf.Abs(current.y - _boxStart.y));

                Handles.BeginGUI();
                EditorGUI.DrawRect(boxRect, new Color(0, 0.5f, 1f, 0.1f));
                Handles.EndGUI();

                evt.Use();
                SceneView.RepaintAll();
            }

            if (_boxSelecting && evt.type == EventType.MouseUp && evt.button == 0)
            {
                _boxSelecting = false;
                Vector2 current = evt.mousePosition;
                Rect boxRect = new(
                    Mathf.Min(_boxStart.x, current.x), Mathf.Min(_boxStart.y, current.y),
                    Mathf.Abs(current.x - _boxStart.x), Mathf.Abs(current.y - _boxStart.y));

                if (boxRect.width > 5 && boxRect.height > 5)
                {
                    if (_selection.Mode == SelectionMode.Vertex)
                    {
                        var selected = SceneViewMeshInteraction.BoxSelectVertices(_editMesh, localToWorld, boxRect);
                        _selection.Vertices.UnionWith(selected);
                    }
                    else if (_selection.Mode == SelectionMode.Face)
                    {
                        var selected = SceneViewMeshInteraction.BoxSelectFaces(_editMesh, localToWorld, boxRect);
                        _selection.Faces.UnionWith(selected);
                    }
                }
                evt.Use();
            }
        }

        #endregion

        #region Manipulation Handle

        void DrawManipulationHandle(Matrix4x4 localToWorld)
        {
            Vector3 center = localToWorld.MultiplyPoint3x4(_selection.GetSelectionCenter(_editMesh));
            _handlePosition = center;

            EditorGUI.BeginChangeCheck();

            switch (_activeTool)
            {
                case Tool.Move:
                {
                    Vector3 newPos = Handles.PositionHandle(center, UnityEditor.Tools.pivotRotation == PivotRotation.Global
                        ? Quaternion.identity : _meshFilter.transform.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 worldDelta = newPos - center;
                        Vector3 localDelta = _meshFilter.transform.InverseTransformVector(worldDelta);
                        MeshUndoHelper.RecordMesh(_mesh, "Move Vertices");
                        MeshEditorOperations.MoveVertices(_editMesh, _selection.GetSelectedVertices(_editMesh), localDelta);
                        ApplyMesh();
                    }
                    break;
                }
                case Tool.Rotate:
                {
                    Quaternion newRot = Handles.RotationHandle(_handleRotation, center);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Quaternion delta = newRot * Quaternion.Inverse(_handleRotation);
                        _handleRotation = newRot;

                        Vector3 localCenter = _selection.GetSelectionCenter(_editMesh);
                        MeshUndoHelper.RecordMesh(_mesh, "Rotate Vertices");
                        MeshEditorOperations.RotateVertices(_editMesh, _selection.GetSelectedVertices(_editMesh), localCenter, delta);
                        ApplyMesh();
                    }
                    break;
                }
                case Tool.Scale:
                {
                    Vector3 newScale = Handles.ScaleHandle(Vector3.one, center,
                        UnityEditor.Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : _meshFilter.transform.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 localCenter = _selection.GetSelectionCenter(_editMesh);
                        MeshUndoHelper.RecordMesh(_mesh, "Scale Vertices");
                        MeshEditorOperations.ScaleVertices(_editMesh, _selection.GetSelectedVertices(_editMesh), localCenter, newScale);
                        ApplyMesh();
                    }
                    break;
                }
            }
        }

        #endregion

        #region Scene GUI Panel

        void DrawSceneGUI()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 180, 300));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Mesh Editor", EditorStyles.boldLabel);

            // Selection mode
            GUILayout.Label("Mode (1/2/3):", EditorStyles.miniLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_selection.Mode == SelectionMode.Vertex, "Vert", "Button"))
                _selection.Mode = SelectionMode.Vertex;
            if (GUILayout.Toggle(_selection.Mode == SelectionMode.Edge, "Edge", "Button"))
                _selection.Mode = SelectionMode.Edge;
            if (GUILayout.Toggle(_selection.Mode == SelectionMode.Face, "Face", "Button"))
                _selection.Mode = SelectionMode.Face;
            GUILayout.EndHorizontal();

            // Tool
            GUILayout.Label("Tool (W/E/R):", EditorStyles.miniLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_activeTool == Tool.Move, "Move", "Button"))
                _activeTool = Tool.Move;
            if (GUILayout.Toggle(_activeTool == Tool.Rotate, "Rot", "Button"))
                _activeTool = Tool.Rotate;
            if (GUILayout.Toggle(_activeTool == Tool.Scale, "Scale", "Button"))
                _activeTool = Tool.Scale;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Operations
            if (_selection.HasSelection)
            {
                GUILayout.Label("Operations:", EditorStyles.miniLabel);

                if (_selection.Mode == SelectionMode.Face && GUILayout.Button("Extrude"))
                    ExtrudeSelection(0.1f);

                if (_selection.Mode == SelectionMode.Edge && GUILayout.Button("Extrude Edges"))
                    ExtrudeEdgeSelection(0.1f);

                if (_selection.Mode == SelectionMode.Face && GUILayout.Button("Subdivide"))
                    SubdivideSelection();

                if (_selection.Mode == SelectionMode.Vertex && _selection.Vertices.Count >= 2
                    && GUILayout.Button("Merge"))
                    MergeSelection();

                if (GUILayout.Button("Flip Normals"))
                    FlipSelection();

                if (GUILayout.Button("Delete"))
                    DeleteSelection();

                GUILayout.Space(4);
                GUILayout.Label("Selection:", EditorStyles.miniLabel);
                if (GUILayout.Button("Grow")) _selection.GrowSelection(_editMesh);
                if (GUILayout.Button("Shrink")) _selection.ShrinkSelection(_editMesh);
                if (GUILayout.Button("Linked")) _selection.SelectLinked(_editMesh);
            }

            // Stats
            GUILayout.Space(4);
            GUILayout.Label($"V:{_editMesh.VertexCount} T:{_editMesh.TriangleCount}", EditorStyles.miniLabel);

            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        #endregion

        #region Operations

        void ExtrudeSelection(float distance)
        {
            MeshUndoHelper.RecordMesh(_mesh, "Extrude Faces");
            var newVerts = MeshEditorOperations.ExtrudeFaces(_editMesh, _selection.Faces, distance);
            ApplyMesh();

            // Update selection to extruded verts
            _selection.Clear();
            _selection.Mode = SelectionMode.Vertex;
            _selection.Vertices.UnionWith(newVerts);
        }

        void ExtrudeEdgeSelection(float distance)
        {
            MeshUndoHelper.RecordMesh(_mesh, "Extrude Edges");
            var newVerts = MeshEditorOperations.ExtrudeEdges(_editMesh, _selection.Edges, distance);
            ApplyMesh();

            _selection.Clear();
            _selection.Mode = SelectionMode.Vertex;
            _selection.Vertices.UnionWith(newVerts);
        }

        void SubdivideSelection()
        {
            MeshUndoHelper.RecordMesh(_mesh, "Subdivide");
            MeshEditorOperations.SubdivideFaces(_editMesh, _selection.Faces);
            ApplyMesh();
            _selection.Clear();
        }

        void MergeSelection()
        {
            MeshUndoHelper.RecordMesh(_mesh, "Merge Vertices");
            MeshEditorOperations.MergeVertices(_editMesh, _selection.Vertices);
            ApplyMesh();
            _selection.Clear();
        }

        void FlipSelection()
        {
            MeshUndoHelper.RecordMesh(_mesh, "Flip Normals");
            MeshEditorOperations.FlipNormals(_editMesh,
                _selection.Mode == SelectionMode.Face ? _selection.Faces : null);
            ApplyMesh();
        }

        void DeleteSelection()
        {
            MeshUndoHelper.RecordMesh(_mesh, "Delete");
            switch (_selection.Mode)
            {
                case SelectionMode.Vertex:
                    MeshEditorOperations.DeleteVertices(_editMesh, _selection.Vertices);
                    break;
                case SelectionMode.Edge:
                    MeshEditorOperations.DeleteEdges(_editMesh, _selection.Edges);
                    break;
                case SelectionMode.Face:
                    MeshEditorOperations.DeleteFaces(_editMesh, _selection.Faces);
                    break;
            }
            ApplyMesh();
            _selection.Clear();
        }

        void ApplyMesh()
        {
            _editMesh.ToMesh(_mesh);
            _editMesh = EditableMesh.FromMesh(_mesh);
        }

        #endregion
    }
}
#endif
