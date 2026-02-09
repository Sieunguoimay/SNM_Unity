#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Snm.Tools.InspectorExtra
{
    /// <summary>
    /// Unity Editor tool to find all references to a selected object in the current scene.
    /// Uses SerializedObject for proper Unity serialization handling.
    /// Place this script in an "Editor" folder in your project.
    /// </summary>
    public class SceneReferencesFinderWindow : EditorWindow
    {
        private UnityEngine.Object targetObject;
        private Vector2 scrollPosition;
        private List<ReferenceInfo> foundReferences = new List<ReferenceInfo>();
        private bool searchComplete = false;

        private class ReferenceInfo
        {
            public GameObject gameObject;
            public Component component;
            public string propertyPath;
            public string path;
        }

        [MenuItem("Tools/Find References in Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneReferencesFinderWindow>("Find References");
        }

        private void OnGUI()
        {
            GUILayout.Label("Find Scene References", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Target object field
            var newTarget = EditorGUILayout.ObjectField("Target Object:", targetObject, typeof(UnityEngine.Object), true);

            if (newTarget != targetObject)
            {
                targetObject = newTarget;
                searchComplete = false;
                foundReferences.Clear();
            }

            EditorGUILayout.Space();

            // Search button
            EditorGUI.BeginDisabledGroup(targetObject == null);
            if (GUILayout.Button("Find References", GUILayout.Height(30)))
            {
                FindAllReferences();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Display results
            if (searchComplete)
            {
                if (foundReferences.Count == 0)
                {
                    EditorGUILayout.HelpBox("No references found in the scene.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField($"Found {foundReferences.Count} reference(s):", EditorStyles.boldLabel);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                    foreach (var refInfo in foundReferences)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // GameObject reference (clickable)
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("→", GUILayout.Width(30)))
                        {
                            Selection.activeGameObject = refInfo.gameObject;
                            EditorGUIUtility.PingObject(refInfo.gameObject);
                        }
                        EditorGUILayout.LabelField($"GameObject: {refInfo.path}");
                        EditorGUILayout.EndHorizontal();

                        // Component and property info
                        EditorGUILayout.LabelField($"Component: {refInfo.component.GetType().Name}");
                        EditorGUILayout.LabelField($"Property: {refInfo.propertyPath}");

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        public void Find(UnityEngine.Object target)
        {
            targetObject = target;
            FindAllReferences();
        }

        private void FindAllReferences()
        {
            foundReferences.Clear();
            searchComplete = false;

            if (targetObject == null)
            {
                Debug.LogWarning("Please select a target object to search for.");
                return;
            }

            // Find all GameObjects in the scene
            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject go in allObjects)
            {
                // Get all components on this GameObject
                Component[] components = go.GetComponents<Component>();

                foreach (Component comp in components)
                {
                    if (comp == null) continue;

                    // Use SerializedObject to inspect the component
                    CheckComponentForReferences(go, comp);
                }
            }

            foundReferences = foundReferences
                .GroupBy(r => r.component)
                .Select(g => g.First())
                .ToList();

            searchComplete = true;
            Debug.Log($"Search complete. Found {foundReferences.Count} reference(s) to {targetObject.name}");
        }

        private void CheckComponentForReferences(GameObject go, Component comp)
        {
            SerializedObject serializedObject = new SerializedObject(comp);
            SerializedProperty property = serializedObject.GetIterator();

            // Iterate through all serialized properties
            while (property.Next(true))
            {
                // Check if this property is an object reference
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.objectReferenceValue == targetObject)
                    {
                        AddReference(go, comp, property.propertyPath);
                    }
                }
                // Handle arrays and lists
                else if (property.isArray && property.propertyType != SerializedPropertyType.String)
                {
                    CheckArrayProperty(go, comp, property);
                }
            }
        }

        private void CheckArrayProperty(GameObject go, Component comp, SerializedProperty arrayProperty)
        {
            int arraySize = arrayProperty.arraySize;

            for (int i = 0; i < arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);

                if (element.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (element.objectReferenceValue == targetObject)
                    {
                        AddReference(go, comp, $"{arrayProperty.propertyPath}[{i}]");
                    }
                }
                // Handle nested structures
                else if (element.hasChildren)
                {
                    CheckNestedProperty(go, comp, element);
                }
            }
        }

        private void CheckNestedProperty(GameObject go, Component comp, SerializedProperty parentProperty)
        {
            SerializedProperty property = parentProperty.Copy();
            SerializedProperty endProperty = property.GetEndProperty();

            while (property.Next(true) && !SerializedProperty.EqualContents(property, endProperty))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.objectReferenceValue == targetObject)
                    {
                        AddReference(go, comp, property.propertyPath);
                    }
                }
            }
        }

        private void AddReference(GameObject go, Component comp, string propertyPath)
        {
            foundReferences.Add(new ReferenceInfo
            {
                gameObject = go,
                component = comp,
                propertyPath = propertyPath,
                path = GetGameObjectPath(go)
            });
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}

#endif