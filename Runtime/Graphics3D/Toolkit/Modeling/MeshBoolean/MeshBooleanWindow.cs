#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Snm.Graphics3D.Toolkit;

namespace Snm.Graphics3D.Modeling
{
    public class MeshBooleanWindow : EditorWindow
    {
        [SerializeField] GameObject objectA;
        [SerializeField] GameObject objectB;
        MeshBooleanCSG.Operation _operation = MeshBooleanCSG.Operation.Union;
        Mesh _resultMesh;
        Vector2 _scrollPos;

        [MenuItem("Tools/Snm/3D Toolkit/Modeling/Boolean Tool", priority = 20)]
        public static void Open()
        {
            var w = GetWindow<MeshBooleanWindow>("Mesh Boolean");
            w.minSize = new Vector2(320, 350);
        }

        void OnDisable()
        {
            if (_resultMesh != null) DestroyImmediate(_resultMesh);
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            EditorGUILayout.EndScrollView();
        }

        internal void DrawContent()
        {
            ToolkitGUI.Title("Mesh Boolean (CSG)");
            EditorGUILayout.HelpBox(
                "Select two GameObjects with MeshFilters. The Boolean operation " +
                "will be performed in world space.\n\n" +
                "Union: A + B combined\n" +
                "Subtract: A minus B\n" +
                "Intersect: Only where A and B overlap",
                MessageType.Info);

            ToolkitGUI.SectionHeader("Inputs");

            objectA = (GameObject)EditorGUILayout.ObjectField("Object A", objectA, typeof(GameObject), true);
            objectB = (GameObject)EditorGUILayout.ObjectField("Object B", objectB, typeof(GameObject), true);

            EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

            if (GUILayout.Button("Use Selection (first 2)"))
            {
                var sel = Selection.gameObjects;
                if (sel.Length >= 2)
                {
                    objectA = sel[0];
                    objectB = sel[1];
                }
                else if (sel.Length == 1)
                {
                    if (objectA == null) objectA = sel[0];
                    else objectB = sel[0];
                }
            }

            ToolkitGUI.SectionHeader("Operation");

            _operation = (MeshBooleanCSG.Operation)EditorGUILayout.EnumPopup("Operation", _operation);

            var meshA = GetMesh(objectA);
            var meshB = GetMesh(objectB);

            if (meshA != null) ToolkitGUI.StatRow("A", $"{meshA.name} ({meshA.triangles.Length / 3} tris)");
            if (meshB != null) ToolkitGUI.StatRow("B", $"{meshB.name} ({meshB.triangles.Length / 3} tris)");

            bool valid = meshA != null && meshB != null && meshA.isReadable && meshB.isReadable;

            if (!valid && (meshA != null || meshB != null))
            {
                if (meshA != null && !meshA.isReadable)
                    EditorGUILayout.HelpBox("Mesh A is not readable.", MessageType.Error);
                if (meshB != null && !meshB.isReadable)
                    EditorGUILayout.HelpBox("Mesh B is not readable.", MessageType.Error);
            }

            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);

            EditorGUI.BeginDisabledGroup(!valid);

            if (ToolkitGUI.BigButton("Execute"))
            {
                if (_resultMesh != null) DestroyImmediate(_resultMesh);

                _resultMesh = MeshBooleanCSG.Execute(
                    meshA, objectA.transform.localToWorldMatrix,
                    meshB, objectB.transform.localToWorldMatrix,
                    _operation);
            }

            EditorGUI.EndDisabledGroup();

            if (_resultMesh != null)
            {
                ToolkitGUI.SectionHeader("Result");
                ToolkitGUI.StatRow("Vertices", _resultMesh.vertexCount.ToString("N0"));
                ToolkitGUI.StatRow("Triangles", (_resultMesh.triangles.Length / 3).ToString("N0"));

                EditorGUILayout.Space(ToolkitWindowStyles.ItemSpacing);

                if (ToolkitGUI.ActionButton("Create GameObject"))
                {
                    var go = new GameObject($"Boolean_{_operation}");
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = _resultMesh;
                    var mr = go.AddComponent<MeshRenderer>();

                    var srcMr = objectA.GetComponent<MeshRenderer>();
                    if (srcMr != null) mr.sharedMaterials = srcMr.sharedMaterials;

                    MeshUndoHelper.RegisterCreatedGameObject(go, $"Boolean {_operation}");
                    Selection.activeGameObject = go;
                    _resultMesh = null;
                }

                if (ToolkitGUI.ActionButton("Save as Asset"))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Save Boolean Result", _resultMesh.name, "asset", "Save boolean result mesh");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(_resultMesh, path);
                        AssetDatabase.SaveAssets();
                        _resultMesh = null;
                    }
                }
            }
        }

        static Mesh GetMesh(GameObject go)
        {
            if (go == null) return null;
            var mf = go.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }
    }
}
#endif
