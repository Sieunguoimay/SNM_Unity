#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class MeshDecimatorWindow : EditorWindow
    {
        [SerializeField] Mesh sourceMesh;
        float _reductionPercent = 50f;
        bool _preserveBoundary = true;
        bool _preserveUVSeams = true;
        float _boundaryPenalty = 100f;
        Mesh _previewMesh;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Decimator", priority = 21)]
        public static void Open()
        {
            var w = GetWindow<MeshDecimatorWindow>("Mesh Decimator");
            w.minSize = new Vector2(320, 400);
        }

        void OnEnable() => Selection.selectionChanged += TryAutoSelect;
        void OnDisable()
        {
            Selection.selectionChanged -= TryAutoSelect;
            DestroyPreview();
        }

        void TryAutoSelect()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                sourceMesh = mf.sharedMesh;
                DestroyPreview();
                Repaint();
            }
        }

        void DestroyPreview()
        {
            if (_previewMesh != null) { DestroyImmediate(_previewMesh); _previewMesh = null; }
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Mesh Decimator");

            EditorGUI.BeginChangeCheck();
            sourceMesh = (Mesh)EditorGUILayout.ObjectField("Source Mesh", sourceMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck()) DestroyPreview();

            if (GUILayout.Button("Use Selection")) TryAutoSelect();

            if (!ToolkitGUI.ValidateMesh(sourceMesh, "Select a mesh to decimate."))
                return;

            ToolkitGUI.MeshStatus(sourceMesh);

            ToolkitGUI.SectionHeader("Source Info");

            int sourceTris = sourceMesh.triangles.Length / 3;
            ToolkitGUI.StatRow("Vertices", sourceMesh.vertexCount.ToString("N0"));
            ToolkitGUI.StatRow("Triangles", sourceTris.ToString("N0"));

            ToolkitGUI.SectionHeader("Settings");

            _reductionPercent = EditorGUILayout.Slider("Reduction %", _reductionPercent, 1f, 99f);

            int targetTris = Mathf.Max(4, Mathf.RoundToInt(sourceTris * (1f - _reductionPercent / 100f)));
            ToolkitGUI.StatRow("Target Triangles", targetTris.ToString("N0"));

            EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);
            _preserveBoundary = EditorGUILayout.Toggle("Preserve Boundary", _preserveBoundary);
            _preserveUVSeams = EditorGUILayout.Toggle("Preserve UV Seams", _preserveUVSeams);
            if (_preserveBoundary)
                _boundaryPenalty = EditorGUILayout.FloatField("Boundary Penalty", _boundaryPenalty);

            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);

            if (ToolkitGUI.BigButton("Preview Decimation"))
            {
                DestroyPreview();
                _previewMesh = QuadricErrorDecimator.Decimate(sourceMesh, new QuadricErrorDecimator.DecimationSettings
                {
                    TargetTriangleCount = targetTris,
                    PreserveBoundary = _preserveBoundary,
                    PreserveUVSeams = _preserveUVSeams,
                    BoundaryPenalty = _boundaryPenalty
                });
            }

            if (_previewMesh != null)
            {
                ToolkitGUI.SectionHeader("Result");
                ToolkitGUI.MeshStatus(_previewMesh);
                ToolkitGUI.StatRow("Vertices", _previewMesh.vertexCount.ToString("N0"));
                ToolkitGUI.StatRow("Triangles", (_previewMesh.triangles.Length / 3).ToString("N0"));

                float actualReduction = 1f - (float)(_previewMesh.triangles.Length / 3) / sourceTris;
                ToolkitGUI.StatRow("Actual Reduction", $"{actualReduction * 100f:F1}%");

                EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

                if (ToolkitGUI.ActionButton("Apply to Selected Object"))
                {
                    var go = Selection.activeGameObject;
                    var mf = go != null ? go.GetComponent<MeshFilter>() : null;
                    if (mf != null)
                    {
                        MeshUndoHelper.RecordMeshFilter(mf, "Apply Decimation");
                        mf.sharedMesh = _previewMesh;
                        _previewMesh = null;
                    }
                }

                if (ToolkitGUI.ActionButton("Save as Asset"))
                {
                    var saved = ToolkitGUI.SaveMeshAsset(_previewMesh, _previewMesh.name);
                    if (saved != null) _previewMesh = null;
                }

                if (ToolkitGUI.ActionButton("Create as LOD GameObject"))
                {
                    var go = new GameObject($"{sourceMesh.name}_LOD");
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = _previewMesh;
                    var mr = go.AddComponent<MeshRenderer>();

                    var srcGo = Selection.activeGameObject;
                    if (srcGo != null)
                    {
                        var srcMr = srcGo.GetComponent<MeshRenderer>();
                        if (srcMr != null) mr.sharedMaterials = srcMr.sharedMaterials;
                        go.transform.position = srcGo.transform.position;
                    }

                    MeshUndoHelper.RegisterCreatedGameObject(go, "Create LOD");
                    _previewMesh = null;
                }
            }
        }
    }
}
#endif
