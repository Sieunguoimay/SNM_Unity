#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools
{
    public class NamespaceModifyToolWindow : EditorWindow
    {
        [SerializeField] private SerializedData serializedData = new();

        [MenuItem("Tools/Snm/Edit/Namespace Modify Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<NamespaceModifyToolWindow>();
            window.titleContent = new GUIContent("Namespace Modify Tool");
            window.minSize = new Vector2(200, 200);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(NamespaceModifyToolVECreator.Create(serializedData));
        }
    }

    public static class NamespaceModifyToolVECreator
    {
        public static VisualElement Create(SerializedData serializedData)
        {
            var root = new VisualElement();
            var textField_TargetPath = new TextField("Target Path") { value = serializedData.targetPath };
            var button_SelectTargetPath = new Button() { text = "Select Target Path", clickable = new(() => textField_TargetPath.value = AssetDatabase.GetAssetPath(Selection.activeObject)) };
            var textField_namespace = new TextField() { label = "Namespace", value = serializedData.namespaceString };
            var button_AddOrReplace = new Button()
            {
                text = "Add or Replace Namespace",
                clickable = new(() => ModifyNamespace(textField_TargetPath.value, textField_namespace.value))
            };

            root.Add(textField_TargetPath);
            root.Add(button_SelectTargetPath);
            root.Add(textField_namespace);
            root.Add(button_AddOrReplace);
            return root;
        }

        private static void ModifyNamespace(string targetPath, string namespaceStr)
        {

            if (string.IsNullOrEmpty(targetPath) || string.IsNullOrEmpty(namespaceStr))
            {
                Debug.LogError("Target Path or Namespace is empty");
                return;
            }

            var files = GetFilesFromPath(targetPath);
            var modified = false;
            foreach (var path in files)
            {
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var content = text.text;

                var lines = content.Split('\n').ToList();

                if (ModifyNamespace(lines, namespaceStr))
                {
                    content = string.Join("\n", lines);
                    System.IO.File.WriteAllText(path, content);
                    modified = true;

                    Debug.Log("Namespace Modified Successfully for " + path, text);
                }
            }

            if (modified)
            {
                AssetDatabase.Refresh();
            }
        }

        private static IEnumerable<string> GetFilesFromPath(string targetPathString)
        {
            if (AssetDatabase.IsValidFolder(targetPathString))
                return AssetDatabase.FindAssets("t:Script", new[] { targetPathString })
                    .Select(AssetDatabase.GUIDToAssetPath);
            else
            {
                if (targetPathString.EndsWith(".cs"))
                    return new[] { targetPathString };
                else
                {
                    Debug.LogError("Invalid Path");
                    return new string[0];
                }
            }
        }

        private static bool ModifyNamespace(List<string> lines, string namespaceStr)
        {
            if (ReplaceNamespace(lines, namespaceStr))
            {
                return true;
            }
            else
            {
                if (AddNamespace(lines, namespaceStr))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ReplaceNamespace(List<string> lines, string namespaceStr)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("namespace"))
                {
                    lines[i] = $"namespace {namespaceStr}";
                    return true;
                }
            }
            return false;
        }

        private static bool AddNamespace(List<string> lines, string namespaceStr)
        {
            var lastUsingLineIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("using"))
                {
                    lastUsingLineIndex = i;
                }
            }

            lines.Insert(lastUsingLineIndex + 1, string.Format("namespace {0}{{", namespaceStr));
            lines.Add("}");
            return true;
        }

    }

    [Serializable]
    public class SerializedData
    {
        public string targetPath = "Assets/Scripts";
        public string namespaceString = "Default.Namesapce";
    }
}
#endif