#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public class ObjectPickerWindow : EditorWindow
    {
        [SerializeField] private UnityEngine.Object[] _objects = Array.Empty<UnityEngine.Object>();

        private Action<UnityEngine.Object> _onPicked;
        private Vector2 _scroll;

        public static void Show(UnityEngine.Object[] objectList, Action<UnityEngine.Object> onPickedCallback)
        {
            var window = CreateInstance<ObjectPickerWindow>();
            window._objects = objectList;
            window._onPicked = onPickedCallback;
            window.titleContent = new GUIContent("Pick an Object");
            window.ShowUtility(); // or Show() if you want it dockable
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var obj in _objects)
            {
                if (obj == null) continue;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object), false);

                if (GUILayout.Button("Pick", GUILayout.Width(60)))
                {
                    _onPicked?.Invoke(obj);
                    Close();
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif