#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snm.Tools.InspectorExtensions
{
    public class SceneReferenceFinderWindow : EditorWindow
    {
        public Object target;
        private Vector2 scroll;

        [SerializeField] private List<ReferenceResult> results = new();

        [MenuItem("Tools/Snm/Scene Reference Finder")]
        public static void Open()
        {
            GetWindow<SceneReferenceFinderWindow>("Scene Reference Finder");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            target = EditorGUILayout.ObjectField(
                "Target Object",
                target,
                typeof(Object),
                true);

            EditorGUILayout.Space();

            GUI.enabled = target != null;

            if (GUILayout.Button("Find References In Open Scene", GUILayout.Height(30)))
            {
                FindReferences();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
            DrawResults();
        }

        private void DrawResults()
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var r in results)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = r.Component;
                    EditorGUIUtility.PingObject(r.Component);
                }

                EditorGUILayout.LabelField(
                    $"{r.GameObjectPath} | {r.Component.GetType().Name} | {r.PropertyPath}");

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        public void FindReferences()
        {
            results.Clear();

            if (target == null)
                return;

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                TraverseGameObject(root);
            }
        }

        private void TraverseGameObject(GameObject go)
        {
            // Check all components
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                    continue;

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == target)
                        {
                            results.Add(new ReferenceResult
                            {
                                GameObjectPath = GetHierarchyPath(go),
                                Component = comp,
                                PropertyPath = prop.propertyPath
                            });
                        }
                    }
                }
            }

            // Traverse children
            foreach (Transform child in go.transform)
            {
                TraverseGameObject(child.gameObject);
            }
        }

        private string GetHierarchyPath(GameObject go)
        {
            string path = go.name;
            Transform current = go.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private class ReferenceResult
        {
            public string GameObjectPath;
            public Component Component;
            public string PropertyPath;
        }
    }
}

#endif