#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Modeling
{
    public class PivotEditorWindow : EditorWindow
    {
        Vector3 _customPivot;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Pivot Editor", priority = 41)]
        public static void Open()
        {
            GetWindow<PivotEditorWindow>("Pivot Editor");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Pivot Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Moves the mesh pivot by offsetting all vertices and adjusting the Transform.\n\n" +
                "You can also activate the Pivot Editor tool from the Scene View toolbar " +
                "to drag the pivot interactively.",
                MessageType.Info);

            var go = Selection.activeGameObject;
            var mf = go != null ? go.GetComponent<MeshFilter>() : null;
            var mesh = mf != null ? mf.sharedMesh : null;

            if (mesh == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a MeshFilter.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!mesh.isReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable.", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("Mesh", mesh.name);
            EditorGUILayout.LabelField("Bounds Center", mesh.bounds.center.ToString("F3"));
            EditorGUILayout.LabelField("Bounds Size", mesh.bounds.size.ToString("F3"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            if (GUILayout.Button("Center of Bounds"))
                ApplyPivot(mf, mesh, mesh.bounds.center);

            if (GUILayout.Button("Bottom Center"))
            {
                var b = mesh.bounds;
                ApplyPivot(mf, mesh, new Vector3(b.center.x, b.min.y, b.center.z));
            }

            if (GUILayout.Button("Top Center"))
            {
                var b = mesh.bounds;
                ApplyPivot(mf, mesh, new Vector3(b.center.x, b.max.y, b.center.z));
            }

            if (GUILayout.Button("Origin (0,0,0)"))
                ApplyPivot(mf, mesh, Vector3.zero);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Custom", EditorStyles.boldLabel);

            _customPivot = EditorGUILayout.Vector3Field("Pivot (Local)", _customPivot);
            if (GUILayout.Button("Apply Custom Pivot"))
                ApplyPivot(mf, mesh, _customPivot);

            // Selection-based pivot
            var sel = MeshSelection.GetOrCreate(mesh);
            if (sel.HasSelection)
            {
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Center of Selection"))
                {
                    var em = EditableMesh.FromMesh(mesh);
                    ApplyPivot(mf, mesh, sel.GetSelectionCenter(em));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        static void ApplyPivot(MeshFilter mf, Mesh mesh, Vector3 localPivot)
        {

            Transform t = mf.transform;
            MeshUndoHelper.RecordMeshAndTransform(mesh, t, "Set Pivot");

            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] -= localPivot;
            mesh.vertices = verts;
            mesh.RecalculateBounds();

            t.position += t.TransformVector(localPivot);
        }
    }
}
#endif
