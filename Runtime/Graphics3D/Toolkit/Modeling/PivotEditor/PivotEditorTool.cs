#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    [EditorTool("Pivot Editor", typeof(MeshFilter))]
    public class PivotEditorTool : EditorTool
    {
        MeshFilter _meshFilter;
        Mesh _mesh;
        Vector3 _pivotWorldPos;

        public override GUIContent toolbarIcon =>
            new("PE", "Pivot Editor — Move the mesh pivot point");

        bool EnsureTarget()
        {
            if (target is not MeshFilter mf || mf.sharedMesh == null) return false;
            _meshFilter = mf;
            _mesh = mf.sharedMesh;
            return true;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView) return;
            if (!EnsureTarget()) return;

            Transform t = _meshFilter.transform;
            _pivotWorldPos = t.position;

            // Draw current pivot
            float handleSize = HandleUtility.GetHandleSize(_pivotWorldPos);
            Handles.color = new Color(1f, 0.5f, 0f, 1f);
            Handles.SphereHandleCap(0, _pivotWorldPos, Quaternion.identity, handleSize * 0.06f, EventType.Repaint);

            // Draw pivot move handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPivotWorld = Handles.PositionHandle(_pivotWorldPos, t.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                if (_mesh.isReadable)
                    MovePivotTo(newPivotWorld);
            }

            // Draw mesh bounds for reference
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Bounds localBounds = _mesh.bounds;
            Handles.DrawWireCube(
                t.TransformPoint(localBounds.center),
                Vector3.Scale(localBounds.size, t.lossyScale));

            // Scene panel
            DrawSceneGUI();
        }

        void MovePivotTo(Vector3 newWorldPos)
        {
            Transform t = _meshFilter.transform;
            Vector3 worldDelta = newWorldPos - t.position;
            Vector3 localDelta = t.InverseTransformVector(worldDelta);

            MeshUndoHelper.RecordMeshAndTransform(_mesh, t, "Move Pivot");

            // Offset all vertices in the opposite direction
            var verts = _mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] -= localDelta;
            _mesh.vertices = verts;
            _mesh.RecalculateBounds();

            // Move transform in the world direction
            t.position += worldDelta;
        }

        void SetPivotToLocal(Vector3 localPivot)
        {
            if (!_mesh.isReadable) return;

            Transform t = _meshFilter.transform;
            Vector3 worldDelta = t.TransformVector(localPivot);

            MeshUndoHelper.RecordMeshAndTransform(_mesh, t, "Set Pivot");

            var verts = _mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] -= localPivot;
            _mesh.vertices = verts;
            _mesh.RecalculateBounds();

            t.position += worldDelta;
        }

        void DrawSceneGUI()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 170, 220));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Pivot Editor", EditorStyles.boldLabel);
            GUILayout.Label("Drag handle to move pivot", EditorStyles.miniLabel);

            GUILayout.Space(4);
            GUILayout.Label("Presets:", EditorStyles.miniLabel);

            if (GUILayout.Button("Center of Bounds"))
                SetPivotToLocal(_mesh.bounds.center);

            if (GUILayout.Button("Bottom Center"))
            {
                var b = _mesh.bounds;
                SetPivotToLocal(new Vector3(b.center.x, b.min.y, b.center.z));
            }

            if (GUILayout.Button("Top Center"))
            {
                var b = _mesh.bounds;
                SetPivotToLocal(new Vector3(b.center.x, b.max.y, b.center.z));
            }

            if (GUILayout.Button("Origin (0,0,0)"))
                SetPivotToLocal(Vector3.zero);

            // Center of selection (if mesh editor has selection)
            var sel = MeshSelection.GetOrCreate(_mesh);
            if (sel.HasSelection)
            {
                if (GUILayout.Button("Center of Selection"))
                {
                    var em = EditableMesh.FromMesh(_mesh);
                    Vector3 center = sel.GetSelectionCenter(em);
                    SetPivotToLocal(center);
                }
            }

            GUILayout.Space(4);
            GUILayout.Label($"Bounds: {_mesh.bounds.size:F2}", EditorStyles.miniLabel);

            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
#endif
