#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystem.Editor
{
    [CustomEditor(typeof(GrassMapVisualizer))]
    public class GrassMapVisualizerEditor : UnityEditor.Editor
    {
        SerializedProperty _showTrample, _trampleChannel;
        SerializedProperty _showWind, _windChannel;
        SerializedProperty _opacity, _heightOffset;
        SerializedProperty _showBounds, _showHeightPlanes, _showBladeMesh, _showBladePositions;

        void OnEnable()
        {
            _showTrample = serializedObject.FindProperty("showTrampleMap");
            _trampleChannel = serializedObject.FindProperty("trampleChannel");
            _showWind = serializedObject.FindProperty("showWindMap");
            _windChannel = serializedObject.FindProperty("windChannel");
            _opacity = serializedObject.FindProperty("opacity");
            _heightOffset = serializedObject.FindProperty("heightOffset");
            _showBounds = serializedObject.FindProperty("showBounds");
            _showHeightPlanes = serializedObject.FindProperty("showHeightPlanes");
            _showBladeMesh = serializedObject.FindProperty("showBladeMesh");
            _showBladePositions = serializedObject.FindProperty("showBladePositions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var visualizer = (GrassMapVisualizer)target;
            var grassSystem = visualizer.GetComponent<GrassSystem>();

            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showBounds, new GUIContent("Bounds"));
            EditorGUILayout.PropertyField(_showHeightPlanes, new GUIContent("Height Planes"));
            EditorGUILayout.PropertyField(_showBladeMesh, new GUIContent("Blade Mesh"));
            EditorGUILayout.PropertyField(_showBladePositions, new GUIContent("Blade Positions"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Map Overlays", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_showTrample, new GUIContent("Trample Map"));
            if (_showTrample.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_trampleChannel, new GUIContent("Channel"));
                if (Application.isPlaying && grassSystem != null && grassSystem.Trample != null)
                {
                    var rt = grassSystem.Trample.OutputTexture;
                    if (rt != null)
                    {
                        var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                        EditorGUI.DrawTextureTransparent(rect, rt);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(_showWind, new GUIContent("Wind Map"));
            if (_showWind.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_windChannel, new GUIContent("Channel"));
                if (grassSystem != null && grassSystem.Config.wind.windMap != null)
                {
                    var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, grassSystem.Config.wind.windMap);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_opacity);
            EditorGUILayout.PropertyField(_heightOffset, new GUIContent("Height Offset"));

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                EditorUtility.SetDirty(target);
        }
    }
}
#endif
