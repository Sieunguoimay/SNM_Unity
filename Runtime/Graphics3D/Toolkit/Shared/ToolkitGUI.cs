#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Toolkit
{
    public enum MeshLocation
    {
        /// <summary>Standalone .asset file — fully editable, persists on Ctrl+S.</summary>
        Asset,
        /// <summary>Sub-asset of an imported model (.fbx/.obj) or prefab — typically read-only.</summary>
        SubAsset,
        /// <summary>Exists only in memory — will be lost on domain reload or scene close.</summary>
        Unsaved
    }

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

        // ─────────────────────────────────────────────────────────────
        //  Mesh location & save helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>Determines where a mesh lives: project asset, imported sub-asset, or unsaved.</summary>
        public static MeshLocation GetMeshLocation(Mesh mesh)
        {
            if (mesh == null) return MeshLocation.Unsaved;
            string path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return MeshLocation.Unsaved;
            if (AssetDatabase.IsSubAsset(mesh)) return MeshLocation.SubAsset;
            return MeshLocation.Asset;
        }

        /// <summary>
        /// Draws a colored status row showing where the mesh lives.
        /// Green [Asset], Yellow [Imported], Orange [Unsaved].
        /// </summary>
        public static void MeshStatus(Mesh mesh)
        {
            if (mesh == null) return;

            var location = GetMeshLocation(mesh);
            string path = AssetDatabase.GetAssetPath(mesh);

            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.color;
            string label;
            switch (location)
            {
                case MeshLocation.Asset:
                    GUI.color = ToolkitWindowStyles.PositiveColor;
                    label = "Asset";
                    break;
                case MeshLocation.SubAsset:
                    GUI.color = new Color(1f, 0.85f, 0.3f);
                    label = "Imported";
                    break;
                default:
                    GUI.color = ToolkitWindowStyles.WarningColor;
                    label = "Unsaved";
                    break;
            }

            EditorGUILayout.LabelField($"[{label}]", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            GUI.color = prevColor;

            string detail = location switch
            {
                MeshLocation.Asset => path,
                MeshLocation.SubAsset => $"read-only sub-asset of {path}",
                _ => "in memory only — save to keep"
            };
            EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// If the mesh is an imported sub-asset, shows a warning and a "Duplicate as Editable"
        /// button. Returns the mesh to use — either the original or the newly duplicated one.
        /// Pass the MeshFilter so the duplicate can be swapped in automatically.
        /// </summary>
        public static Mesh ImportedMeshGuard(Mesh mesh, MeshFilter mf)
        {
            if (mesh == null) return mesh;
            if (GetMeshLocation(mesh) != MeshLocation.SubAsset) return mesh;

            EditorGUILayout.HelpBox(
                "This mesh is part of an imported model and cannot be modified directly.\n" +
                "Duplicate it as an editable asset first.",
                MessageType.Warning);

            if (GUILayout.Button("Duplicate as Editable Asset"))
            {
                var duplicate = SaveMeshCopy(mesh, mf, mesh.name + "_editable");
                if (duplicate != null) return duplicate;
            }

            return mesh;
        }

        /// <summary>
        /// Shows a SaveFilePanel, creates a new mesh asset, and pings it.
        /// Returns the saved mesh, or null if cancelled.
        /// </summary>
        public static Mesh SaveMeshAsset(Mesh mesh, string defaultName)
        {
            if (mesh == null) return null;

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Mesh", defaultName, "asset", "Choose a location to save the mesh");
            if (string.IsNullOrEmpty(path)) return null;

            // If the mesh is already a project asset, we need to instantiate it first
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
            {
                var copy = Object.Instantiate(mesh);
                copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(copy, path);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(copy);
                return copy;
            }

            mesh.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(mesh);
            return mesh;
        }

        /// <summary>
        /// Clones a mesh, saves it as a new asset, and optionally swaps it into the MeshFilter.
        /// Returns the saved copy, or null if cancelled.
        /// </summary>
        public static Mesh SaveMeshCopy(Mesh mesh, MeshFilter mf, string defaultName)
        {
            if (mesh == null) return null;

            var copy = Object.Instantiate(mesh);
            copy.name = defaultName;

            var saved = SaveMeshAsset(copy, defaultName);
            if (saved == null)
            {
                Object.DestroyImmediate(copy);
                return null;
            }

            if (mf != null)
            {
                Undo.RecordObject(mf, "Swap to Editable Mesh");
                mf.sharedMesh = saved;
            }

            return saved;
        }
    }
}
#endif
