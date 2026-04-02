#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Toolkit
{
    public static class ToolkitWindowStyles
    {
        // Layout
        public const float SectionSpacing = 6f;
        public const float ItemSpacing = 2f;
        public const float ActionButtonHeight = 24f;
        public const float BigButtonHeight = 28f;
        public const float StatLabelWidth = 120f;
        public const float IssueLabelWidth = 160f;

        // Colors
        static readonly Color HeaderBgDark = new(0.2f, 0.2f, 0.2f, 1f);
        static readonly Color HeaderBgLight = new(0.76f, 0.76f, 0.76f, 1f);
        static readonly Color SeparatorDark = new(0.15f, 0.15f, 0.15f, 1f);
        static readonly Color SeparatorLight = new(0.6f, 0.6f, 0.6f, 1f);
        static readonly Color AccentDark = new(0.25f, 0.47f, 0.85f, 0.35f);
        static readonly Color AccentLight = new(0.3f, 0.5f, 0.85f, 0.25f);
        public static readonly Color PositiveColor = new(0.4f, 0.9f, 0.4f);
        public static readonly Color WarningColor = new(1f, 0.65f, 0.3f);
        public static readonly Color MutedColor = new(0.55f, 0.55f, 0.55f);

        public static Color HeaderBg => EditorGUIUtility.isProSkin ? HeaderBgDark : HeaderBgLight;
        public static Color Separator => EditorGUIUtility.isProSkin ? SeparatorDark : SeparatorLight;
        public static Color Accent => EditorGUIUtility.isProSkin ? AccentDark : AccentLight;

        // Cached styles
        static GUIStyle _sectionHeader;
        public static GUIStyle SectionHeader
        {
            get
            {
                if (_sectionHeader == null)
                {
                    _sectionHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 10,
                        padding = new RectOffset(6, 4, 2, 2),
                    };
                    if (EditorGUIUtility.isProSkin)
                        _sectionHeader.normal.textColor = new Color(0.75f, 0.82f, 0.95f);
                }
                return _sectionHeader;
            }
        }

        static GUIStyle _windowTitle;
        public static GUIStyle WindowTitle
        {
            get
            {
                if (_windowTitle == null)
                {
                    _windowTitle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        padding = new RectOffset(2, 2, 2, 2),
                    };
                }
                return _windowTitle;
            }
        }

        static GUIStyle _statValue;
        public static GUIStyle StatValue
        {
            get
            {
                if (_statValue == null)
                {
                    _statValue = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        padding = new RectOffset(0, 4, 0, 0),
                    };
                    if (EditorGUIUtility.isProSkin)
                        _statValue.normal.textColor = new Color(0.78f, 0.78f, 0.78f);
                }
                return _statValue;
            }
        }

        static GUIStyle _actionButton;
        public static GUIStyle ActionButton
        {
            get
            {
                if (_actionButton == null)
                {
                    _actionButton = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 11,
                        padding = new RectOffset(8, 8, 3, 3),
                    };
                }
                return _actionButton;
            }
        }
    }
}
#endif
