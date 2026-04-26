#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystem
{
    [CustomEditor(typeof(GrassPatch))]
    public class GrassPatchEditor : UnityEditor.Editor
    {
        GrassPatch _patch;

        void OnEnable()
        {
            _patch = (GrassPatch)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (_patch.cellSpacing.x > 0f && _patch.cellSpacing.y > 0f)
            {
                int countX = Mathf.Max(1, Mathf.FloorToInt(_patch.areaSize.x / _patch.cellSpacing.x));
                int countZ = Mathf.Max(1, Mathf.FloorToInt(_patch.areaSize.y / _patch.cellSpacing.y));
                EditorGUILayout.HelpBox($"Estimated blades: {countX * countZ}\nGrid: {countX} x {countZ}", MessageType.Info);
            }

            if (_patch.mesh == null)
                EditorGUILayout.HelpBox("No mesh assigned. This patch will produce no grass.", MessageType.Warning);
            if (_patch.material == null)
                EditorGUILayout.HelpBox("No material assigned. This patch will produce no grass.", MessageType.Warning);
        }

        void OnSceneGUI()
        {
            if (_patch == null) return;

            var t = _patch.transform;
            var center = t.position;

            // Draw area size handles
            EditorGUI.BeginChangeCheck();
            Handles.matrix = Matrix4x4.TRS(center, t.rotation, Vector3.one);
            Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);

            float halfX = _patch.areaSize.x * 0.5f;
            float halfZ = _patch.areaSize.y * 0.5f;

            // Drag handles on each edge to resize
            var newPosX = Handles.Slider(new Vector3(halfX, 0, 0), Vector3.right, HandleUtility.GetHandleSize(Vector3.right * halfX) * 0.06f, Handles.DotHandleCap, 0.1f);
            var newNegX = Handles.Slider(new Vector3(-halfX, 0, 0), Vector3.left, HandleUtility.GetHandleSize(Vector3.left * halfX) * 0.06f, Handles.DotHandleCap, 0.1f);
            var newPosZ = Handles.Slider(new Vector3(0, 0, halfZ), Vector3.forward, HandleUtility.GetHandleSize(Vector3.forward * halfZ) * 0.06f, Handles.DotHandleCap, 0.1f);
            var newNegZ = Handles.Slider(new Vector3(0, 0, -halfZ), Vector3.back, HandleUtility.GetHandleSize(Vector3.back * halfZ) * 0.06f, Handles.DotHandleCap, 0.1f);

            Handles.matrix = Matrix4x4.identity;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_patch, "Resize Grass Patch");
                _patch.areaSize = new Vector2(
                    Mathf.Max(0.1f, newPosX.x - newNegX.x),
                    Mathf.Max(0.1f, newPosZ.z - newNegZ.z));
                EditorUtility.SetDirty(_patch);
            }

            // Sample dot grid (show up to ~200 dots for perf)
            if (_patch.cellSpacing.x > 0f && _patch.cellSpacing.y > 0f)
            {
                int countX = Mathf.Max(1, Mathf.FloorToInt(_patch.areaSize.x / _patch.cellSpacing.x));
                int countZ = Mathf.Max(1, Mathf.FloorToInt(_patch.areaSize.y / _patch.cellSpacing.y));

                int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(countX * countZ / 200f)));

                Handles.color = new Color(0.3f, 0.9f, 0.3f, 0.5f);
                float dotSize = Mathf.Min(_patch.cellSpacing.x, _patch.cellSpacing.y) * 0.1f;

                for (int z = 0; z < countZ; z += step)
                {
                    for (int x = 0; x < countX; x += step)
                    {
                        float localX = -halfX + (x + 0.5f) * _patch.cellSpacing.x;
                        float localZ = -halfZ + (z + 0.5f) * _patch.cellSpacing.y;
                        var localPos = new Vector3(localX, 0f, localZ);
                        var worldPos = t.TransformPoint(localPos);

                        Handles.DotHandleCap(0, worldPos, Quaternion.identity, dotSize, EventType.Repaint);
                    }
                }
            }
        }
    }
}
#endif
