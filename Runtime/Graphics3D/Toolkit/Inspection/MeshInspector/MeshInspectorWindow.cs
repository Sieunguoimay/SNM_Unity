#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Snm.Graphics3D.Modeling;

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
            EditorGUI.BeginChangeCheck();
            targetMesh = (Mesh)EditorGUILayout.ObjectField("Mesh", targetMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck()) Refresh();

            if (GUILayout.Button("Use Selection")) TryAutoSelect();

            if (!_analyzed || targetMesh == null)
            {
                EditorGUILayout.HelpBox("Select a mesh to inspect.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawOverview();
            DrawAttributes();
            DrawSubMeshes();
            DrawTopology();
            DrawFixButtons();

            EditorGUILayout.EndScrollView();
        }

        void DrawOverview()
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Name", targetMesh.name);
            EditorGUILayout.LabelField("Vertices", _stats.VertexCount.ToString("N0"));
            EditorGUILayout.LabelField("Triangles", _stats.TriangleCount.ToString("N0"));
            EditorGUILayout.LabelField("Edges", _stats.EdgeCount.ToString("N0"));
            EditorGUILayout.LabelField("Sub-meshes", _stats.SubMeshCount.ToString());
            EditorGUILayout.LabelField("Index Format",
                _stats.IndexFormat == IndexFormat.UInt32 ? "32-bit" : "16-bit");
            EditorGUILayout.LabelField("Readable", _stats.IsReadable ? "Yes" : "No");
            EditorGUILayout.LabelField("Memory (est.)",
                MeshInspectorAnalyzer.FormatBytes(_stats.EstimatedMemoryBytes));

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Bounds Center", _stats.Bounds.center.ToString("F3"));
            EditorGUILayout.LabelField("Bounds Size", _stats.Bounds.size.ToString("F3"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawAttributes()
        {
            _showAttributes = EditorGUILayout.Foldout(_showAttributes, "Attributes", true);
            if (!_showAttributes) return;

            EditorGUI.indentLevel++;

            DrawAttributeRow("Normals", _stats.HasNormals);
            DrawAttributeRow("Tangents", _stats.HasTangents);
            DrawAttributeRow("Vertex Colors", _stats.HasColors);
            DrawAttributeRow("Bone Weights", _stats.HasBoneWeights);

            for (int ch = 0; ch < 8; ch++)
                DrawAttributeRow($"UV{ch}", _stats.HasUV[ch]);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawAttributeRow(string label, bool present)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            var prevColor = GUI.color;
            GUI.color = present ? new Color(0.4f, 1f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUILayout.LabelField(present ? "Present" : "—", EditorStyles.miniLabel);
            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();
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
            _showTopology = EditorGUILayout.Foldout(_showTopology, "Topology Analysis", true);
            if (!_showTopology) return;

            EditorGUI.indentLevel++;

            if (!_stats.IsReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable. Enable Read/Write to analyze topology.",
                    MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            DrawIssueRow("Degenerate Triangles", _stats.DegenerateTriangles);
            DrawIssueRow("Unused Vertices", _stats.UnusedVertices);
            DrawIssueRow("Non-manifold Edges", _stats.NonManifoldEdges);
            DrawIssueRow("Boundary Edges", _stats.BoundaryEdges);

            if (_stats.DuplicateVertices >= 0)
                DrawIssueRow("Duplicate Vertices", _stats.DuplicateVertices);
            else
                EditorGUILayout.LabelField("Duplicate Vertices", "Skipped (mesh too large)");

            bool hasIssues = _stats.DegenerateTriangles > 0 || _stats.UnusedVertices > 0 ||
                             _stats.NonManifoldEdges > 0;

            if (!hasIssues)
                EditorGUILayout.HelpBox("No topology issues found.", MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        void DrawIssueRow(string label, int count)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(180));

            var prevColor = GUI.color;
            GUI.color = count > 0 ? new Color(1f, 0.6f, 0.3f) : new Color(0.4f, 1f, 0.4f);
            EditorGUILayout.LabelField(count > 0 ? count.ToString() : "None", EditorStyles.miniLabel);
            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();
        }

        void DrawFixButtons()
        {
            if (!_stats.IsReadable) return;
            bool hasIssues = _stats.DegenerateTriangles > 0 || _stats.UnusedVertices > 0 ||
                             (_stats.DuplicateVertices > 0);
            if (!hasIssues) return;

            _showFix = EditorGUILayout.Foldout(_showFix, "Fix Issues", true);
            if (!_showFix) return;

            EditorGUI.indentLevel++;

            if (_stats.DegenerateTriangles > 0)
            {
                if (GUILayout.Button($"Remove {_stats.DegenerateTriangles} Degenerate Triangles"))
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
                if (GUILayout.Button($"Remove {_stats.UnusedVertices} Unused Vertices"))
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
                if (GUILayout.Button($"Weld {_stats.DuplicateVertices} Duplicate Vertices"))
                {
                    MeshUndoHelper.RecordMesh(targetMesh, "Weld Duplicate Vertices");
                    var em = EditableMesh.FromMesh(targetMesh);
                    em.WeldVertices(0.0001f);
                    em.ToMesh(targetMesh);
                    Refresh();
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);

            // Copy stats
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
