using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Snm.Runtime.DebugVisualize
{
    [CreateAssetMenu(fileName = "DebugVisualizeSettings", menuName = "DebugVisualize/Settings")]
    public class DebugVisualizeSettings : ScriptableObject
    {
        [Header("General")]
        [SerializeField] private bool _enabled = true;

        [Header("Pooling")]
        [SerializeField] private int _lineRendererPoolSize = 200;
        [SerializeField] private int _textPoolSize = 200;
        [SerializeField] private int _meshPoolSize = 100;

        [Header("Defaults")]
        [SerializeField] private float _defaultDuration = 3f;
        [SerializeField] private float _defaultLineWidth = 0.05f;
        [SerializeField] private float _defaultFontSize = 3f;
        [SerializeField] private Vector3 _defaultTextOffset = Vector3.up * 2f;

        [Header("Color Palette")]
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _lineColor = Color.yellow;
        [SerializeField] private Color _sphereColor = Color.red;
        [SerializeField] private Color _boxColor = Color.cyan;
        [SerializeField] private Color _circleColor = Color.green;
        [SerializeField] private Color _arrowColor = Color.magenta;
        [SerializeField] private Color _statColor = Color.white;
        [SerializeField] private Color _barBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _barFillColor = Color.green;

        [Header("Text Settings")]
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private bool _autoHideOffScreen = true;

        public bool Enabled => _enabled;
        public int LineRendererPoolSize => _lineRendererPoolSize;
        public int TextPoolSize => _textPoolSize;
        public int MeshPoolSize => _meshPoolSize;
        public float DefaultDuration => _defaultDuration;
        public float DefaultLineWidth => _defaultLineWidth;
        public float DefaultFontSize => _defaultFontSize;
        public Vector3 DefaultTextOffset => _defaultTextOffset;
        public Color TextColor => _textColor;
        public Color LineColor => _lineColor;
        public Color SphereColor => _sphereColor;
        public Color BoxColor => _boxColor;
        public Color CircleColor => _circleColor;
        public Color ArrowColor => _arrowColor;
        public Color StatColor => _statColor;
        public Color BarBackgroundColor => _barBackgroundColor;
        public Color BarFillColor => _barFillColor;
        public TMP_FontAsset Font => _font;
        public bool AutoHideOffScreen => _autoHideOffScreen;

        public Color GetColor(DebugVisualizeCategory category)
        {
            return category switch
            {
                DebugVisualizeCategory.Text => _textColor,
                DebugVisualizeCategory.Line => _lineColor,
                DebugVisualizeCategory.Sphere => _sphereColor,
                DebugVisualizeCategory.Box => _boxColor,
                DebugVisualizeCategory.Circle => _circleColor,
                DebugVisualizeCategory.Arrow => _arrowColor,
                DebugVisualizeCategory.Stat => _statColor,
                _ => Color.white
            };
        }
    }

    public enum DebugVisualizeCategory
    {
        Text,
        Line,
        Sphere,
        Box,
        Circle,
        Arrow,
        Stat
    }
}
