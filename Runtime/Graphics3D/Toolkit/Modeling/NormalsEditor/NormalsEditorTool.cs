#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    [EditorTool("Normals Editor", typeof(MeshFilter))]
    public class NormalsEditorTool : EditorTool
    {
        EditableMesh _editMesh;
        MeshSelection _selection;
        MeshFilter _meshFilter;
        Mesh _mesh;

        float _normalLength = 0.15f;
        bool _showTangents;
        bool _showBitangents;
        int _editingVertex = -1;

        public override GUIContent toolbarIcon =>
            new("NE", "Normals Editor — Visualize and edit vertex normals");

        void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

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
            if (target is not MeshFilter mf || mf.sharedMesh == null) return false;

            if (_meshFilter != mf || _mesh != mf.sharedMesh)
            {
                _meshFilter = mf;
                _mesh = mf.sharedMesh;
                _editMesh = EditableMesh.FromMesh(_mesh);
                _selection = MeshSelection.GetOrCreate(_mesh);
                _selection.Mode = SelectionMode.Vertex;
            }
            return true;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;
            if (!EnsureTarget()) return;
            if (_editMesh.Normals == null) return;

            Matrix4x4 ltw = _meshFilter.transform.localToWorldMatrix;
            Event evt = Event.current;

            // Draw normals
            DrawNormalLines(ltw);

            // Picking
            HandlePicking(evt, ltw);

            // Normal rotation handle for selected vertex
            if (_editingVertex >= 0 && _editingVertex < _editMesh.VertexCount)
                DrawNormalRotationHandle(ltw);

            // Scene panel
            DrawSceneGUI();
        }

        void DrawNormalLines(Matrix4x4 ltw)
        {
            for (int i = 0; i < _editMesh.VertexCount; i++)
            {
                if (i >= _editMesh.Normals.Length) break;

                Vector3 worldPos = ltw.MultiplyPoint3x4(_editMesh.Positions[i]);
                float size = HandleUtility.GetHandleSize(worldPos);

                bool selected = _selection.Vertices.Contains(i);

                // Normal
                Vector3 normalEnd = ltw.MultiplyPoint3x4(
                    _editMesh.Positions[i] + _editMesh.Normals[i] * _normalLength);
                Handles.color = selected ? MeshToolStyles.VertexSelectedColor : MeshToolStyles.NormalColor;
                Handles.DrawLine(worldPos, normalEnd, selected ? 2f : 1f);

                // Tangent
                if (_showTangents && _editMesh.Tangents != null && i < _editMesh.Tangents.Length)
                {
                    Vector3 t = new(_editMesh.Tangents[i].x, _editMesh.Tangents[i].y, _editMesh.Tangents[i].z);
                    Vector3 tangentEnd = ltw.MultiplyPoint3x4(_editMesh.Positions[i] + t * _normalLength * 0.7f);
                    Handles.color = MeshToolStyles.TangentColor;
                    Handles.DrawLine(worldPos, tangentEnd);
                }

                // Bitangent
                if (_showBitangents && _editMesh.Tangents != null && i < _editMesh.Tangents.Length)
                {
                    Vector4 t4 = _editMesh.Tangents[i];
                    Vector3 t = new(t4.x, t4.y, t4.z);
                    Vector3 bitangent = Vector3.Cross(_editMesh.Normals[i], t) * t4.w;
                    Vector3 btEnd = ltw.MultiplyPoint3x4(_editMesh.Positions[i] + bitangent * _normalLength * 0.7f);
                    Handles.color = MeshToolStyles.BitangentColor;
                    Handles.DrawLine(worldPos, btEnd);
                }

                // Vertex dot
                if (selected)
                {
                    Handles.color = MeshToolStyles.VertexSelectedColor;
                    Handles.DotHandleCap(0, worldPos, Quaternion.identity,
                        size * MeshToolStyles.VertexHandleSize, EventType.Repaint);
                }
            }
        }

        void HandlePicking(Event evt, Matrix4x4 ltw)
        {
            if (evt.type != EventType.MouseDown || evt.button != 0 || evt.alt) return;

            int v = SceneViewMeshInteraction.PickVertex(_editMesh, ltw, evt);
            if (v >= 0)
            {
                bool additive = evt.shift;
                bool subtractive = evt.control;

                if (!additive && !subtractive) _selection.Clear();
                if (subtractive) _selection.Vertices.Remove(v);
                else _selection.Vertices.Add(v);

                _editingVertex = v;
                evt.Use();
            }
            else if (!evt.shift && !evt.control)
            {
                _selection.Clear();
                _editingVertex = -1;
                evt.Use();
            }
        }

        void DrawNormalRotationHandle(Matrix4x4 ltw)
        {
            int v = _editingVertex;
            Vector3 worldPos = ltw.MultiplyPoint3x4(_editMesh.Positions[v]);
            Vector3 worldNormal = ltw.MultiplyVector(_editMesh.Normals[v]).normalized;

            // Show a rotation handle offset along the normal
            Vector3 handlePos = worldPos + worldNormal * HandleUtility.GetHandleSize(worldPos) * 0.3f;
            Quaternion currentRot = Quaternion.LookRotation(worldNormal);

            EditorGUI.BeginChangeCheck();
            Quaternion newRot = Handles.RotationHandle(currentRot, handlePos);
            if (EditorGUI.EndChangeCheck())
            {
                MeshUndoHelper.RecordMesh(_mesh, "Rotate Normal");
                // Convert back to local space
                Vector3 newWorldNormal = newRot * Vector3.forward;
                _editMesh.Normals[v] = ltw.inverse.MultiplyVector(newWorldNormal).normalized;
                _editMesh.ToMesh(_mesh);
                _editMesh = EditableMesh.FromMesh(_mesh);
            }
        }

        void DrawSceneGUI()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 180, 280));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Normals Editor", EditorStyles.boldLabel);

            _normalLength = EditorGUILayout.Slider("Length", _normalLength, 0.01f, 1f);
            _showTangents = GUILayout.Toggle(_showTangents, "Show Tangents");
            _showBitangents = GUILayout.Toggle(_showBitangents, "Show Bitangents");

            GUILayout.Space(4);

            if (GUILayout.Button("Recalculate Normals"))
            {
                MeshUndoHelper.RecordMesh(_mesh, "Recalculate Normals");
                NormalsOperations.RecalculateNormals(_editMesh);
                ApplyMesh();
            }

            if (GUILayout.Button("Recalculate Tangents"))
            {
                MeshUndoHelper.RecordMesh(_mesh, "Recalculate Tangents");
                NormalsOperations.RecalculateTangents(_editMesh);
                ApplyMesh();
            }

            if (GUILayout.Button("Flip All Normals"))
            {
                MeshUndoHelper.RecordMesh(_mesh, "Flip Normals");
                NormalsOperations.FlipNormals(_editMesh);
                ApplyMesh();
            }

            if (_selection.Vertices.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label($"Selected: {_selection.Vertices.Count}", EditorStyles.miniLabel);

                if (GUILayout.Button("Flip Selected"))
                {
                    MeshUndoHelper.RecordMesh(_mesh, "Flip Selected Normals");
                    NormalsOperations.FlipNormals(_editMesh, _selection.Vertices);
                    ApplyMesh();
                }

                if (GUILayout.Button("Set to Up"))
                {
                    MeshUndoHelper.RecordMesh(_mesh, "Set Normals Up");
                    NormalsOperations.SetNormalDirection(_editMesh, _selection.Vertices,
                        NormalsOperations.NormalDirection.Up);
                    ApplyMesh();
                }

                if (GUILayout.Button("Spherize"))
                {
                    MeshUndoHelper.RecordMesh(_mesh, "Spherize Normals");
                    NormalsOperations.SetNormalDirection(_editMesh, _selection.Vertices,
                        NormalsOperations.NormalDirection.Spherized);
                    ApplyMesh();
                }

                if (GUILayout.Button("Face Average"))
                {
                    MeshUndoHelper.RecordMesh(_mesh, "Face Average Normals");
                    NormalsOperations.SetNormalDirection(_editMesh, _selection.Vertices,
                        NormalsOperations.NormalDirection.FaceAverage);
                    ApplyMesh();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        void ApplyMesh()
        {
            _editMesh.ToMesh(_mesh);
            _editMesh = EditableMesh.FromMesh(_mesh);
        }
    }
}
#endif
