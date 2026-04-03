#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class MeshEditorWindow : EditorWindow
    {
        Vector2 _scrollPos;
        float _extrudeAmount = 0.1f;
        float _weldThreshold = 0.001f;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Mesh Editor", priority = 1)]
        public static void Open()
        {
            GetWindow<MeshEditorWindow>("Mesh Editor");
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Mesh Editor");
            EditorGUILayout.HelpBox(
                "Select a GameObject with a MeshFilter, then activate the Mesh Editor tool " +
                "from the Scene View toolbar (or Component Tools overlay).\n\n" +
                "Shortcuts:\n" +
                "  1/2/3 — Vertex/Edge/Face mode\n" +
                "  W/E/R — Move/Rotate/Scale\n" +
                "  Shift+Click — Add to selection\n" +
                "  Ctrl+Click — Remove from selection\n" +
                "  Ctrl+A — Select All\n" +
                "  Ctrl+I — Invert Selection\n" +
                "  Delete/X — Delete selection\n" +
                "  Double-click edge — Loop select",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // Get current selection context
            var go = Selection.activeGameObject;
            var mf = go != null ? go.GetComponent<MeshFilter>() : null;
            var mesh = mf != null ? mf.sharedMesh : null;

            if (!ToolkitGUI.ValidateMesh(mesh, "Select a GameObject with a MeshFilter."))
                return;

            ToolkitGUI.MeshStatus(mesh);
            mesh = ToolkitGUI.ImportedMeshGuard(mesh, mf);

            var sel = MeshSelection.GetOrCreate(mesh);

            EditorGUILayout.LabelField("Mesh", mesh.name);
            EditorGUILayout.LabelField("Selection Mode", sel.Mode.ToString());

            // Selection mode buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Vertex")) sel.Mode = SelectionMode.Vertex;
            if (GUILayout.Button("Edge")) sel.Mode = SelectionMode.Edge;
            if (GUILayout.Button("Face")) sel.Mode = SelectionMode.Face;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Vertices", sel.Vertices.Count.ToString());
            EditorGUILayout.LabelField("Edges", sel.Edges.Count.ToString());
            EditorGUILayout.LabelField("Faces", sel.Faces.Count.ToString());

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

            // Extrude
            _extrudeAmount = EditorGUILayout.FloatField("Extrude Distance", _extrudeAmount);
            EditorGUI.BeginDisabledGroup(sel.Faces.Count == 0);
            if (GUILayout.Button("Extrude Faces"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Extrude Faces");
                    var em = EditableMesh.FromMesh(mesh);
                    MeshEditorOperations.ExtrudeFaces(em, sel.Faces, _extrudeAmount);
                    em.ToMesh(mesh);
                    sel.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();

            // Subdivide
            EditorGUI.BeginDisabledGroup(sel.Faces.Count == 0);
            if (GUILayout.Button("Subdivide Faces"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Subdivide");
                    var em = EditableMesh.FromMesh(mesh);
                    MeshEditorOperations.SubdivideFaces(em, sel.Faces);
                    em.ToMesh(mesh);
                    sel.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();

            // Merge
            _weldThreshold = EditorGUILayout.FloatField("Weld Threshold", _weldThreshold);
            EditorGUI.BeginDisabledGroup(sel.Vertices.Count < 2);
            if (GUILayout.Button("Merge Selected Vertices"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Merge Vertices");
                    var em = EditableMesh.FromMesh(mesh);
                    MeshEditorOperations.MergeVertices(em, sel.Vertices);
                    em.ToMesh(mesh);
                    sel.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Weld All (by threshold)"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Weld Vertices");
                    var em = EditableMesh.FromMesh(mesh);
                    em.WeldVertices(_weldThreshold);
                    em.ToMesh(mesh);
                }
            }

            // Flip normals
            if (GUILayout.Button("Flip Normals"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Flip Normals");
                    var em = EditableMesh.FromMesh(mesh);
                    MeshEditorOperations.FlipNormals(em, sel.Faces.Count > 0 ? sel.Faces : null);
                    em.ToMesh(mesh);
                }
            }

            // Delete
            EditorGUI.BeginDisabledGroup(!sel.HasSelection);
            if (GUILayout.Button("Delete Selection"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Delete");
                    var em = EditableMesh.FromMesh(mesh);
                    switch (sel.Mode)
                    {
                        case SelectionMode.Vertex:
                            MeshEditorOperations.DeleteVertices(em, sel.Vertices);
                            break;
                        case SelectionMode.Edge:
                            MeshEditorOperations.DeleteEdges(em, sel.Edges);
                            break;
                        case SelectionMode.Face:
                            MeshEditorOperations.DeleteFaces(em, sel.Faces);
                            break;
                    }
                    em.ToMesh(mesh);
                    sel.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Cleanup", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove Degenerate Triangles"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Remove Degenerates");
                    var em = EditableMesh.FromMesh(mesh);
                    em.RemoveDegenerateTriangles();
                    em.ToMesh(mesh);
                }
            }

            if (GUILayout.Button("Remove Unused Vertices"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Remove Unused");
                    var em = EditableMesh.FromMesh(mesh);
                    em.RemoveUnusedVertices();
                    em.ToMesh(mesh);
                }
            }

            if (GUILayout.Button("Recalculate Normals"))
            {
                if (mesh.isReadable)
                {
                    MeshUndoHelper.RecordMesh(mesh, "Recalculate Normals");
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);

            if (GUILayout.Button("Save as New Asset"))
            {
                ToolkitGUI.SaveMeshCopy(mesh, mf, mesh.name);
            }
        }
    }
}
#endif
