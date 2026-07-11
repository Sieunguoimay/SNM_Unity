using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// GrassType inspector with inline validation — the same "no grass, no
    /// mystery" policy as the world inspector, applied at the asset level.
    /// </summary>
    [CustomEditor(typeof(GrassType))]
    [CanEditMultipleObjects]
    public class GrassTypeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var type = (GrassType)target;

            if (type.meshLod0 == null)
            {
                EditorGUILayout.HelpBox("LOD0 mesh is required — this type cannot render without it.", MessageType.Error);
            }

            if (type.material == null)
            {
                EditorGUILayout.HelpBox("Material is required.", MessageType.Error);
            }
            else if (type.material.shader == null || type.material.shader.name != GrassHealthCheck.ShaderName)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(
                    $"Material must use the '{GrassHealthCheck.ShaderName}' shader to render.",
                    MessageType.Error);
                if (GUILayout.Button("Switch shader", GUILayout.Width(110f), GUILayout.Height(38f)))
                {
                    Undo.RecordObject(type.material, "Switch grass shader");
                    type.material.shader = Shader.Find(GrassHealthCheck.ShaderName);
                    EditorUtility.SetDirty(type.material);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (type.meshLod1 != null && type.meshLod0 != null &&
                type.meshLod1.vertexCount >= type.meshLod0.vertexCount)
            {
                EditorGUILayout.HelpBox(
                    $"LOD1 ({type.meshLod1.vertexCount} verts) is not lighter than LOD0 " +
                    $"({type.meshLod0.vertexCount} verts) — it will cost more, not less.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();

            if (type.meshLod0 != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    $"Blade height: {type.BladeHeight:F2} m · LOD0 {type.meshLod0.vertexCount} verts" +
                    (type.HasLod1 ? $" · LOD1 {type.meshLod1.vertexCount} verts" : " · no LOD1"),
                    EditorStyles.miniLabel);
            }
        }
    }
}
