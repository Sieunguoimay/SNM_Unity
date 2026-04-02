#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Snm.Graphics3D.Modeling;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Inspection
{
    public class MeshInspectorWindow : EditorWindow
    {
        [SerializeField] Mesh targetMesh;
        MeshInspectorAnalyzer.MeshStats _stats;
        bool _analyzed;
        Vector2 _scrollPos;
        bool _showAttributes = true;
        bool _showTopology = true;
        bool _showSubMeshes = true;
        bool _showFix;

        [MenuItem("Tools/Snm/3D Toolkit/Inspect/Mesh Inspector", priority = 42)]
        public static void Open()
        {
            var w = GetWindow<MeshInspectorWindow>("Mesh Inspector");
            w.minSize = new Vector2(320, 400);
        }

        void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += Refresh;
            TryAutoSelect();
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= Refresh;
        }

        void OnSelectionChanged()
        {
            TryAutoSelect();
            Repaint();
        }

        void TryAutoSelect()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;

            Mesh mesh = null;
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;

            if (mesh == null)
            {
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (smr != null) mesh = smr.sharedMesh;
            }

            if (mesh != null && mesh != targetMesh)
            {
                targetMesh = mesh;
                Refresh();
            }
        }

        void Refresh()
        {
            if (targetMesh != null)
            {
                _stats = MeshInspectorAnalyzer.Analyze(targetMesh);
                _analyzed = true;
            }
            else
            {
                _analyzed = false;
            }
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Mesh Inspector");

            EditorGUI.BeginChangeCheck();
            targetMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", targetMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck()) Refresh();

            if (GUILayout.Button("Use Selection")) TryAutoSelect();

            if (!_analyzed || targetMesh == null)
            {
                EditorGUILayout.HelpBox("Select a mesh to inspect.", MessageType.Info);
                return;
            }

            DrawOverview();
            DrawAttributes();
            DrawSubMeshes();
            DrawTopology();
            DrawFixButtons();
        }

        void DrawOverview()
        {
            ToolkitGUI.SectionHeader("Overview");

            ToolkitGUI.StatRow("Name", targetMesh.name);
            ToolkitGUI.StatRow("Vertices", _stats.VertexCount.ToString("N0"));
            ToolkitGUI.StatRow("Triangles", _stats.TriangleCount.ToString("N0"));
            ToolkitGUI.StatRow("Edges", _stats.EdgeCount.ToString("N0"));
            ToolkitGUI.StatRow("Sub-meshes", _stats.SubMeshCount.ToString());
            ToolkitGUI.StatRow("Index Format",
                _stats.IndexFormat == IndexFormat.UInt32 ? "32-bit" : "16-bit");
            ToolkitGUI.StatusRow("Readable", _stats.IsReadable);
            ToolkitGUI.StatRow("Memory (est.)",
                MeshInspectorAnalyzer.FormatBytes(_stats.EstimatedMemoryBytes));

            EditorGUILayout.Space(2);
            ToolkitGUI.StatRow("Bounds Center", _stats.Bounds.center.ToString("F3"));
            ToolkitGUI.StatRow("Bounds Size", _stats.Bounds.size.ToString("F3"));
        }

        void DrawAttributes()
        {
            _showAttributes = ToolkitGUI.SectionFoldout(_showAttributes, "Attributes");
            if (!_showAttributes) return;

            ToolkitGUI.StatusRow("Normals", _stats.HasNormals, "Present", "\u2014");
            ToolkitGUI.StatusRow("Tangents", _stats.HasTangents, "Present", "\u2014");
            ToolkitGUI.StatusRow("Vertex Colors", _stats.HasColors, "Present", "\u2014");
            ToolkitGUI.StatusRow("Bone Weights", _stats.HasBoneWeights, "Present", "\u2014");

            for (int ch = 0; ch < 8; ch++)
                ToolkitGUI.StatusRow($"UV{ch}", _stats.HasUV[ch], "Present", "\u2014");
        }

        void DrawSubMeshes()
        {
            if (_stats.SubMeshCount <= 1) return;

            _showSubMeshes = EditorGUILayout.Foldout(_showSubMeshes, $"Sub-meshes ({_stats.SubMeshCount})", true);
            if (!_showSubMeshes) return;

            EditorGUI.indentLevel++;

            int maxTris = 0;
            foreach (int c in _stats.SubMeshTriCounts)
                if (c > maxTris) maxTris = c;

            for (int i = 0; i < _stats.SubMeshCount; i++)
            {
                int tris = _stats.SubMeshTriCounts[i];
                float ratio = maxTris > 0 ? (float)tris / maxTris : 0;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                EditorGUILayout.LabelField($"{tris:N0} tris", GUILayout.Width(80));

                // Bar
                Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
                var fillRect = new Rect(barRect.x, barRect.y, barRect.width * ratio, barRect.height);
                Color barColor = Color.HSVToRGB((float)i / _stats.SubMeshCount, 0.6f, 0.8f);
                EditorGUI.DrawRect(fillRect, barColor);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawTopology()
        {
            _showTopology = ToolkitGUI.SectionFoldout(_showTopology, "Topology Analysis");
            if (!_showTopology) return;

            if (!_stats.IsReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable. Enable Read/Write to analyze topology.",
                    MessageType.Warning);
                return;
            }

            ToolkitGUI.IssueRow("Degenerate Triangles", _stats.DegenerateTriangles);
            ToolkitGUI.IssueRow("Unused Vertices", _stats.UnusedVertices);
            ToolkitGUI.IssueRow("Non-manifold Edges", _stats.NonManifoldEdges);
            ToolkitGUI.IssueRow("Boundary Edges", _stats.BoundaryEdges);

            if (_stats.DuplicateVertices >= 0)
                ToolkitGUI.IssueRow("Duplicate Vertices", _stats.DuplicateVertices);
            else
                ToolkitGUI.StatRow("Duplicate Vertices", "Skipped (mesh too large)");

            bool hasIssues = _stats.DegenerateTriangles > 0 || _stats.UnusedVertices > 0 ||
                             _stats.NonManifoldEdges > 0;

            if (!hasIssues)
                EditorGUILayout.HelpBox("No topology issues found.", MessageType.Info);
        }

        void DrawFixButtons()
        {
            if (!_stats.IsReadable) return;
            bool hasIssues = _stats.DegenerateTriangles > 0 || _stats.UnusedVertices > 0 ||
                             (_stats.DuplicateVertices > 0);
            if (!hasIssues) return;

            _showFix = ToolkitGUI.SectionFoldout(_showFix, "Fix Issues");
            if (!_showFix) return;

            if (_stats.DegenerateTriangles > 0)
            {
                if (ToolkitGUI.ActionButton($"Remove {_stats.DegenerateTriangles} Degenerate Triangles"))
                {
                    MeshUndoHelper.RecordMesh(targetMesh, "Remove Degenerate Triangles");
                    var em = EditableMesh.FromMesh(targetMesh);
                    em.RemoveDegenerateTriangles();
                    em.ToMesh(targetMesh);
                    Refresh();
                }
            }

            if (_stats.UnusedVertices > 0)
            {
                if (ToolkitGUI.ActionButton($"Remove {_stats.UnusedVertices} Unused Vertices"))
                {
                    MeshUndoHelper.RecordMesh(targetMesh, "Remove Unused Vertices");
                    var em = EditableMesh.FromMesh(targetMesh);
                    em.RemoveUnusedVertices();
                    em.ToMesh(targetMesh);
                    Refresh();
                }
            }

            if (_stats.DuplicateVertices > 0)
            {
                if (ToolkitGUI.ActionButton($"Weld {_stats.DuplicateVertices} Duplicate Vertices"))
                {
                    MeshUndoHelper.RecordMesh(targetMesh, "Weld Duplicate Vertices");
                    var em = EditableMesh.FromMesh(targetMesh);
                    em.WeldVertices(0.0001f);
                    em.ToMesh(targetMesh);
                    Refresh();
                }
            }

            EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

            if (GUILayout.Button("Copy Stats to Clipboard"))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Mesh: {targetMesh.name}");
                sb.AppendLine($"Vertices: {_stats.VertexCount:N0}");
                sb.AppendLine($"Triangles: {_stats.TriangleCount:N0}");
                sb.AppendLine($"Edges: {_stats.EdgeCount:N0}");
                sb.AppendLine($"Sub-meshes: {_stats.SubMeshCount}");
                sb.AppendLine($"Index Format: {(_stats.IndexFormat == IndexFormat.UInt32 ? "32-bit" : "16-bit")}");
                sb.AppendLine($"Memory: {MeshInspectorAnalyzer.FormatBytes(_stats.EstimatedMemoryBytes)}");
                sb.AppendLine($"Readable: {_stats.IsReadable}");
                sb.AppendLine($"Bounds: center={_stats.Bounds.center:F3} size={_stats.Bounds.size:F3}");
                sb.AppendLine($"--- Attributes ---");
                sb.AppendLine($"Normals: {_stats.HasNormals}  Tangents: {_stats.HasTangents}");
                sb.AppendLine($"Colors: {_stats.HasColors}  BoneWeights: {_stats.HasBoneWeights}");
                for (int ch = 0; ch < 8; ch++)
                    if (_stats.HasUV[ch]) sb.AppendLine($"UV{ch}: Present");
                if (_stats.IsReadable)
                {
                    sb.AppendLine($"--- Topology ---");
                    sb.AppendLine($"Degenerate Tris: {_stats.DegenerateTriangles}");
                    sb.AppendLine($"Unused Verts: {_stats.UnusedVertices}");
                    sb.AppendLine($"Non-manifold Edges: {_stats.NonManifoldEdges}");
                    sb.AppendLine($"Boundary Edges: {_stats.BoundaryEdges}");
                }
                GUIUtility.systemCopyBuffer = sb.ToString();
                Debug.Log("Mesh stats copied to clipboard.");
            }
        }
    }
}
#endif
