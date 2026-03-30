#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

            EditorGUILayout.LabelField("Mesh Decimator", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            sourceMesh = (Mesh)EditorGUILayout.ObjectField("Source Mesh", sourceMesh, typeof(Mesh), false);
            if (EditorGUI.EndChangeCheck()) DestroyPreview();

            if (GUILayout.Button("Use Selection")) TryAutoSelect();

            if (sourceMesh == null)
            {
                EditorGUILayout.HelpBox("Select a mesh to decimate.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!sourceMesh.isReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable.", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Space(4);
            int sourceTris = sourceMesh.triangles.Length / 3;
            EditorGUILayout.LabelField("Source Vertices", sourceMesh.vertexCount.ToString("N0"));
            EditorGUILayout.LabelField("Source Triangles", sourceTris.ToString("N0"));

            EditorGUILayout.Space(4);
            _reductionPercent = EditorGUILayout.Slider("Reduction %", _reductionPercent, 1f, 99f);

            int targetTris = Mathf.Max(4, Mathf.RoundToInt(sourceTris * (1f - _reductionPercent / 100f)));
            EditorGUILayout.LabelField("Target Triangles", targetTris.ToString("N0"));

            EditorGUILayout.Space(4);
            _preserveBoundary = EditorGUILayout.Toggle("Preserve Boundary", _preserveBoundary);
            _preserveUVSeams = EditorGUILayout.Toggle("Preserve UV Seams", _preserveUVSeams);
            if (_preserveBoundary)
                _boundaryPenalty = EditorGUILayout.FloatField("Boundary Penalty", _boundaryPenalty);

            EditorGUILayout.Space(8);

            // Preview
            if (GUILayout.Button("Preview Decimation"))
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
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Result Vertices", _previewMesh.vertexCount.ToString("N0"));
                EditorGUILayout.LabelField("Result Triangles", (_previewMesh.triangles.Length / 3).ToString("N0"));

                float actualReduction = 1f - (float)(_previewMesh.triangles.Length / 3) / sourceTris;
                EditorGUILayout.LabelField("Actual Reduction", $"{actualReduction * 100f:F1}%");

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Apply to Selected Object"))
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

                if (GUILayout.Button("Save as Asset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Decimated Mesh", _previewMesh.name, "asset", "Save decimated mesh");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(_previewMesh, path);
                        AssetDatabase.SaveAssets();
                        _previewMesh = null;
                    }
                }

                if (GUILayout.Button("Create as LOD GameObject"))
                {
                    var go = new GameObject($"{sourceMesh.name}_LOD");
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = _previewMesh;
                    var mr = go.AddComponent<MeshRenderer>();

                    // Try to copy material from source
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

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
