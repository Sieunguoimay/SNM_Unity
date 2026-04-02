#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Toolkit
{
    public static class ToolkitGUI
    {
        /// <summary>Draw a styled window title. Omit when inside the hub (tabs already show the name).</summary>
        public static void Title(string text)
        {
            GUILayout.Space(2);
            EditorGUILayout.LabelField(text, ToolkitWindowStyles.WindowTitle);
            Separator();
        }

        /// <summary>Draw a thin horizontal separator line.</summary>
        public static void Separator()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ToolkitWindowStyles.Separator);
            GUILayout.Space(1);
        }

        /// <summary>Draw a compact section header with accent background.</summary>
        public static void SectionHeader(string text)
        {
            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);
            var rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ToolkitWindowStyles.HeaderBg);

            // Accent left edge
            var accent = new Rect(rect.x, rect.y, 3, rect.height);
            EditorGUI.DrawRect(accent, ToolkitWindowStyles.Accent);

            GUI.Label(rect, text, ToolkitWindowStyles.SectionHeader);
        }

        /// <summary>Draw a foldout with section header styling.</summary>
        public static bool SectionFoldout(bool foldout, string text)
        {
            GUILayout.Space(ToolkitWindowStyles.SectionSpacing);
            var rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ToolkitWindowStyles.HeaderBg);

            var accent = new Rect(rect.x, rect.y, 3, rect.height);
            EditorGUI.DrawRect(accent, ToolkitWindowStyles.Accent);

            return EditorGUI.Foldout(rect, foldout, text, true, ToolkitWindowStyles.SectionHeader);
        }

        /// <summary>Draw a standard action button.</summary>
        public static bool ActionButton(string text)
        {
            return GUILayout.Button(text, ToolkitWindowStyles.ActionButton,
                GUILayout.Height(ToolkitWindowStyles.ActionButtonHeight));
        }

        /// <summary>Draw a large primary action button.</summary>
        public static bool BigButton(string text)
        {
            return GUILayout.Button(text, ToolkitWindowStyles.ActionButton,
                GUILayout.Height(ToolkitWindowStyles.BigButtonHeight));
        }

        /// <summary>Draw a compact label-value pair.</summary>
        public static void StatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(ToolkitWindowStyles.StatLabelWidth));
            EditorGUILayout.LabelField(value, ToolkitWindowStyles.StatValue);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw a colored status indicator row.</summary>
        public static void StatusRow(string label, bool positive, string positiveText = "Yes", string negativeText = "No")
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(ToolkitWindowStyles.StatLabelWidth));
            var prev = GUI.color;
            GUI.color = positive ? ToolkitWindowStyles.PositiveColor : ToolkitWindowStyles.MutedColor;
            EditorGUILayout.LabelField(positive ? positiveText : negativeText, ToolkitWindowStyles.StatValue);
            GUI.color = prev;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw a colored issue count row (green=0, orange>0).</summary>
        public static void IssueRow(string label, int count)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(ToolkitWindowStyles.IssueLabelWidth));
            var prev = GUI.color;
            GUI.color = count > 0 ? ToolkitWindowStyles.WarningColor : ToolkitWindowStyles.PositiveColor;
            EditorGUILayout.LabelField(count > 0 ? count.ToString() : "None", ToolkitWindowStyles.StatValue);
            GUI.color = prev;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Show a validation message and return false if mesh is null or not readable.</summary>
        public static bool ValidateMesh(Mesh mesh, string nullMessage = "Select a mesh.")
        {
            if (mesh == null)
            {
                EditorGUILayout.HelpBox(nullMessage, MessageType.Info);
                return false;
            }
            if (!mesh.isReadable)
            {
                EditorGUILayout.HelpBox("Mesh is not readable. Enable Read/Write in import settings.", MessageType.Error);
                return false;
            }
            return true;
        }

        /// <summary>Show mesh basic info (name, verts, tris).</summary>
        public static void MeshInfo(Mesh mesh)
        {
            StatRow("Mesh", mesh.name);
            StatRow("Vertices", mesh.vertexCount.ToString("N0"));
            StatRow("Triangles", (mesh.triangles.Length / 3).ToString("N0"));
        }
    }
}
#endif
