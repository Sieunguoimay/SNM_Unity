#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{

    [EditorWindowTitle(title = "SceneGameObjectArrangementWindow")]
    public class SceneGameObjectArrangementWindow : EditorWindow
    {
        [MenuItem("Tools/Snm/SceneGameObjectArrangementWindow")]
        public static void Open()
        {
            var window = GetWindow<SceneGameObjectArrangementWindow>(false, "SceneGameObjectArrangementWindow", true);
            window.Show();
        }

        private Transform[] _selectedTransforms;
        private void OnGUI()
        {
            DrawArrgementConfig();
            if (GUILayout.Button("Capture selection"))
            {
                _selectedTransforms = Selection.objects.Where(o => o is GameObject).Select(o => (o as GameObject).GetComponent<Transform>()).ToArray();
            }
            DrawSelectedTransforms();
        }

        private void DrawSelectedTransforms()
        {
            if (_selectedTransforms == null) return;

            EditorGUILayout.BeginVertical();
            foreach (Transform t in _selectedTransforms)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(t, typeof(Transform), true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private Vector3 _spacing;

        private void DrawArrgementConfig()
        {
            _spacing = EditorGUILayout.Vector3Field("Spacing", _spacing);
            if (GUILayout.Button("Apply"))
            {
                ApplySpacing();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Group"))
            {
                GroupIntoParent();
            }
            if (GUILayout.Button("Set Position To Zero"))
            {
                SetToZero();
            }
            EditorGUILayout.EndHorizontal();
        }
        private void ApplySpacing()
        {
            if (_selectedTransforms == null) return;

            var center = Vector3.zero;
            foreach (Transform t in _selectedTransforms)
            {
                center += t.position;
            }
            center /= _selectedTransforms.Length;

            var dir = _spacing.normalized;
            var spacing = _spacing.magnitude;
            var origin = center - dir * spacing * _selectedTransforms.Length / 2;
            for (int i = 0; i < _selectedTransforms.Length; i++)
            {
                _selectedTransforms[i].position = origin + i * spacing * dir;
            }
        }
        private void GroupIntoParent()
        {
            if (_selectedTransforms == null || _selectedTransforms.Length == 0) return;
            var parent = _selectedTransforms.FirstOrDefault().parent;

            var newGameObject = new GameObject("Group");
            newGameObject.transform.SetParent(parent);

            foreach (var item in _selectedTransforms)
            {
                item.SetParent(newGameObject.transform);
            }
        }
        private void SetToZero()
        {
            foreach (var item in _selectedTransforms)
            {
                item.position = Vector3.zero;
            }
        }
    }
}
#endif