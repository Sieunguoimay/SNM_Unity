#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SNMTools
{
    public class NamespaceModifyToolWindow : EditorWindow
    {
        [SerializeField] private NamespaceModifyToolVE.SerializedData serializedData = new();

        [MenuItem("Tools/Snm/Namespace Modify Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<NamespaceModifyToolWindow>();
            window.titleContent = new GUIContent("Namespace Modify Tool");
            window.minSize = new Vector2(200, 200);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new NamespaceModifyToolVE(serializedData));
        }
    }

    public class NamespaceModifyToolVE : VisualElement
    {
        private readonly SerializedData serializedData;

        public NamespaceModifyToolVE(SerializedData serializedData)
        {
            this.serializedData = serializedData;

            TextField targetPath;
            Add(targetPath = new TextField("Target Path")
            {
                value = serializedData.targetPath
            });

            Button selectTargetPath;
            Add(selectTargetPath = new(() =>
            {
                targetPath.value = AssetDatabase.GetAssetPath(Selection.activeObject);
            })
            { text = "Select Target Path" });

            TextField namespaceString;
            Add(namespaceString = new TextField("Namespace")
            {
                value = serializedData.namespaceString
            });

            Button addNameSpace;
            Add(addNameSpace = new(() =>
            {
                ModifyNamespace(targetPath, namespaceString);
            })
            { text = "Add Namespace" });
        }

        private static void ModifyNamespace(TextField targetPath, TextField namespaceString)
        {
            var targetPathString = targetPath.value;
            var namespaceStringString = namespaceString.value;

            if (string.IsNullOrEmpty(targetPathString) || string.IsNullOrEmpty(namespaceStringString))
            {
                Debug.LogError("Target Path or Namespace is empty");
                return;
            }

            var files = GetFilesFromPath(targetPathString);
            var modified = false;
            foreach (var path in files)
            {
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var content = text.text;

                var lines = content.Split('\n').ToList();

                if (ModifyNamespace(lines, namespaceStringString))
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

        private static bool ModifyNamespace(List<string> lines, string namespaceStringString)
        {
            if (ReplaceNamespace(lines, namespaceStringString))
            {
                return true;
            }
            else
            {
                if (AddNamespace(lines, namespaceStringString))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ReplaceNamespace(List<string> lines, string namespaceStringString)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("namespace"))
                {
                    lines[i] = $"namespace {namespaceStringString}";
                    return true;
                }
            }
            return false;
        }

        private static bool AddNamespace(List<string> lines, string namespaceStringString)
        {
            var lastUsingLineIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("using"))
                {
                    lastUsingLineIndex = i;
                }
            }

            lines.Insert(lastUsingLineIndex + 1, string.Format("namespace {0}{{", namespaceStringString));
            lines.Add("}");
            return true;
        }

        [Serializable]
        public class SerializedData
        {
            public string targetPath = "Assets/Scripts";
            public string namespaceString = "Default.Namesapce";
        }
    }

}
#endif