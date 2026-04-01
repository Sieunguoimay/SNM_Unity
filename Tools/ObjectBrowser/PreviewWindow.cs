#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.ObjectBrowser
{
    public class PreviewWindow : EditorWindow
    {
        private Object target;
        private Editor previewEditor;

        public static void Open(Object target)
        {
            if (target == null) return;

            var existing = Resources.FindObjectsOfTypeAll<PreviewWindow>()
                .FirstOrDefault(w => w.target == target);

            if (existing != null)
            {
                existing.Focus();
                return;
            }

            var window = CreateWindow<PreviewWindow>();
            window.target = target;
            window.titleContent = new GUIContent($"Preview: {target.name}");
            window.minSize = new Vector2(256, 256);
            window.Show();
        }

        private void OnEnable()
        {
            if (target != null)
                previewEditor = Editor.CreateEditor(target);
        }

        private void OnDisable()
        {
            if (previewEditor != null)
                DestroyImmediate(previewEditor);
        }

        private void OnGUI()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Target is null.", MessageType.Warning);
                return;
            }

            if (previewEditor == null)
                previewEditor = Editor.CreateEditor(target);

            // Info header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(target.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(target.GetType().Name, EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (target is Texture tex)
            {
                EditorGUILayout.LabelField($"{tex.width} x {tex.height}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            // Preview
            if (previewEditor.HasPreviewGUI())
            {
                var previewRect = GUILayoutUtility.GetRect(
                    position.width - 20, position.height - 80,
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                previewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.HelpBox("No preview available for this type.", MessageType.Info);
            }
        }
    }
}
#endif
