#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

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
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Pivot Editor");
            EditorGUILayout.HelpBox(
                "Moves the mesh pivot by offsetting all vertices and adjusting the Transform.\n\n" +
                "You can also activate the Pivot Editor tool from the Scene View toolbar " +
                "to drag the pivot interactively.",
                MessageType.Info);

            var go = Selection.activeGameObject;
            var mf = go != null ? go.GetComponent<MeshFilter>() : null;
            var mesh = mf != null ? mf.sharedMesh : null;

            if (!ToolkitGUI.ValidateMesh(mesh, "Select a GameObject with a MeshFilter."))
                return;

            ToolkitGUI.StatRow("Mesh", mesh.name);
            ToolkitGUI.StatRow("Bounds Center", mesh.bounds.center.ToString("F3"));
            ToolkitGUI.StatRow("Bounds Size", mesh.bounds.size.ToString("F3"));

            ToolkitGUI.SectionHeader("Presets");

            if (ToolkitGUI.ActionButton("Center of Bounds"))
                ApplyPivot(mf, mesh, mesh.bounds.center);

            if (ToolkitGUI.ActionButton("Bottom Center"))
            {
                var b = mesh.bounds;
                ApplyPivot(mf, mesh, new Vector3(b.center.x, b.min.y, b.center.z));
            }

            if (ToolkitGUI.ActionButton("Top Center"))
            {
                var b = mesh.bounds;
                ApplyPivot(mf, mesh, new Vector3(b.center.x, b.max.y, b.center.z));
            }

            if (ToolkitGUI.ActionButton("Origin (0,0,0)"))
                ApplyPivot(mf, mesh, Vector3.zero);

            ToolkitGUI.SectionHeader("Custom");

            _customPivot = EditorGUILayout.Vector3Field("Pivot (Local)", _customPivot);
            if (ToolkitGUI.ActionButton("Apply Custom Pivot"))
                ApplyPivot(mf, mesh, _customPivot);

            var sel = MeshSelection.GetOrCreate(mesh);
            if (sel.HasSelection)
            {
                EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);
                if (ToolkitGUI.ActionButton("Center of Selection"))
                {
                    var em = EditableMesh.FromMesh(mesh);
                    ApplyPivot(mf, mesh, sel.GetSelectionCenter(em));
                }
            }
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
