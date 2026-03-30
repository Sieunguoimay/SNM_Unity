#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

            EditorGUILayout.LabelField("Normals Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select a GameObject with a MeshFilter, then activate the Normals Editor tool " +
                "from the Scene View toolbar to visualize and interactively edit normals.\n\n" +
                "Click a vertex to select it, then use the rotation handle to adjust its normal.",
                MessageType.Info);

            var go = Selection.activeGameObject;
            var mf = go != null ? go.GetComponent<MeshFilter>() : null;
            var mesh = mf != null ? mf.sharedMesh : null;

            if (mesh == null)
            {
                EditorGUILayout.HelpBox("No mesh selected.", MessageType.Warning);
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
            EditorGUILayout.LabelField("Vertices", mesh.vertexCount.ToString());
            EditorGUILayout.LabelField("Has Normals", (mesh.normals?.Length > 0).ToString());
            EditorGUILayout.LabelField("Has Tangents", (mesh.tangents?.Length > 0).ToString());

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Global Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Recalculate Normals (Area-Weighted)"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Recalculate Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.RecalculateNormals(em);
                em.ToMesh(mesh);
            }

            if (GUILayout.Button("Recalculate Tangents"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Recalculate Tangents");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.RecalculateTangents(em);
                em.ToMesh(mesh);
            }

            if (GUILayout.Button("Flip All Normals"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Flip Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.FlipNormals(em);
                em.ToMesh(mesh);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Smooth / Harden", EditorStyles.boldLabel);

            _smoothDistance = EditorGUILayout.FloatField("Smooth Distance", _smoothDistance);
            if (GUILayout.Button("Smooth Normals"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Smooth Normals");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.SmoothNormals(em, _smoothDistance);
                em.ToMesh(mesh);
            }

            _hardenAngle = EditorGUILayout.Slider("Harden Angle", _hardenAngle, 0f, 180f);
            if (GUILayout.Button("Harden Edges"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Harden Edges");
                var em = EditableMesh.FromMesh(mesh);
                NormalsOperations.HardenEdges(em, _hardenAngle);
                em.ToMesh(mesh);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Transfer Normals", EditorStyles.boldLabel);

            _transferSource = (Mesh)EditorGUILayout.ObjectField("Source Mesh", _transferSource, typeof(Mesh), false);
            EditorGUI.BeginDisabledGroup(_transferSource == null);
            if (GUILayout.Button("Transfer Normals from Source"))
            {
                MeshUndoHelper.RecordMesh(mesh, "Transfer Normals");
                var source = EditableMesh.FromMesh(_transferSource);
                var target = EditableMesh.FromMesh(mesh);
                NormalsOperations.TransferNormals(source, target);
                target.ToMesh(mesh);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
