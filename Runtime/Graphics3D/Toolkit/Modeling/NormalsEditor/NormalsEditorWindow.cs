#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class NormalsEditorWindow : EditorWindow
    {
        float _smoothDistance = 0.01f;
        float _hardenAngle = 30f;
        Mesh _transferSource;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Normals Editor", priority = 40)]
        public static void Open()
        {
            GetWindow<NormalsEditorWindow>("Normals Editor");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Normals Editor");
            EditorGUILayout.HelpBox(
                "Select a GameObject with a MeshFilter, then activate the Normals Editor tool " +
                "from the Scene View toolbar to visualize and interactively edit normals.\n\n" +
                "Click a vertex to select it, then use the rotation handle to adjust its normal.",
                MessageType.Info);

            var go = Selection.activeGameObject;
            var mf = go != null ? go.GetComponent<MeshFilter>() : null;
            var mesh = mf != null ? mf.sharedMesh : null;

            if (!ToolkitGUI.ValidateMesh(mesh, "Select a GameObject with a MeshFilter."))
                return;

            ToolkitGUI.MeshInfo(mesh);
            ToolkitGUI.StatusRow("Normals", mesh.normals?.Length > 0);
            ToolkitGUI.StatusRow("Tangents", mesh.tangents?.Length > 0);

            ToolkitGUI.SectionHeader("Global Operations");

            if (ToolkitGUI.ActionButton("Recalculate Normals (Area-Weighted)"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Recalculate Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.RecalculateNormals(em);
                em.ToMesh(mesh);
            }

            if (ToolkitGUI.ActionButton("Recalculate Tangents"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Recalculate Tangents");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.RecalculateTangents(em);
                em.ToMesh(mesh);
            }

            if (ToolkitGUI.ActionButton("Flip All Normals"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Flip Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.FlipNormals(em);
                em.ToMesh(mesh);
            }

            ToolkitGUI.SectionHeader("Smooth / Harden");

            _smoothDistance = EditorGUILayout.FloatField("Smooth Distance", _smoothDistance);
            if (ToolkitGUI.ActionButton("Smooth Normals"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Smooth Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.SmoothNormals(em, _smoothDistance);
                em.ToMesh(mesh);
            }

            _hardenAngle = EditorGUILayout.Slider("Harden Angle", _hardenAngle, 0f, 180f);
            if (ToolkitGUI.ActionButton("Harden Edges"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Harden Edges");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.HardenEdges(em, _hardenAngle);
                em.ToMesh(mesh);
            }

            ToolkitGUI.SectionHeader("Transfer Normals");

            _transferSource = (Mesh)EditorGUILayout.ObjectField("Source Mesh", _transferSource, typeof(Mesh), false);
            EditorGUI.BeginDisabledGroup(_transferSource == null);
            if (ToolkitGUI.ActionButton("Transfer Normals from Source"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Transfer Normals");
                var source = EditableMesh.FromMesh(_transferSource);
                var target = EditableMesh.FromMesh(mesh);
                NormalsOperations.TransferNormals(source, target);
                target.ToMesh(mesh);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
