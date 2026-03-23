using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_DEBUG || DEVELOPMENT_BUILD
namespace Snm.Runtime.DebugVisualize
{
    public class DebugStatEntry : IDisposable
    {
        private readonly TextMeshPro _labelText;
        private readonly TextMeshPro _valueText;
        private readonly Image _barFill;
        private readonly RectTransform _barBackground;
        private readonly Canvas _canvas;
        private readonly RectTransform _rectTransform;
        private Transform _target;
        private Vector3 _offset;
        private float _duration;
        private float _elapsed;
        private bool _autoHideOffScreen;
        private Camera _camera;
        private bool _isVisible = true;
        private Func<float> _currentValueGetter;
        private Func<float> _maxValueGetter;
        private bool _showBar;
        private bool _autoUpdate;

        public bool IsExpired => _duration > 0 && _elapsed >= _duration;

        public DebugStatEntry(
            TextMeshPro labelText,
            TextMeshPro valueText,
            Image barFill,
            RectTransform barBackground,
            Canvas canvas,
            RectTransform rectTransform)
        {
            _labelText = labelText;
            _valueText = valueText;
            _barFill = barFill;
            _barBackground = barBackground;
            _canvas = canvas;
            _rectTransform = rectTransform;
        }

        public void Setup(
            string label,
            Transform target,
            Vector3 offset,
            float duration,
            Color color,
            bool showBar,
            Func<float> currentGetter,
            Func<float> maxGetter,
            bool autoUpdate,
            DebugVisualizeSettings settings)
        {
            _target = target;
            _offset = offset;
            _duration = duration;
            _elapsed = 0;
            _autoHideOffScreen = settings.AutoHideOffScreen;
            _camera = Camera.main;
            _currentValueGetter = currentGetter;
            _maxValueGetter = maxGetter;
            _showBar = showBar;
            _autoUpdate = autoUpdate;

            _labelText.text = label;
            _labelText.color = color;
            _valueText.color = color;

            _barBackground.gameObject.SetActive(showBar);
            _barFill.gameObject.SetActive(showBar);

            if (showBar)
            {
                _barFill.color = settings.BarFillColor;
            }

            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 10f;

            UpdateValue();
        }

        public void UpdateValue()
        {
            if (_currentValueGetter != null)
            {
                var current = _currentValueGetter();
                _valueText.text = current.ToString("F1");

                if (_showBar && _maxValueGetter != null)
                {
                    var max = _maxValueGetter();
                    var fillAmount = max > 0 ? current / max : 0;
                    _barFill.fillAmount = Mathf.Clamp01(fillAmount);
                }
            }
        }

        public void Update()
        {
            if (_target == null)
            {
                _elapsed = _duration + 1;
                return;
            }

            _elapsed += Time.deltaTime;

            var position = _target.position + _offset;
            _rectTransform.position = position;

            if (_autoHideOffScreen && _camera != null)
            {
                var screenPos = _camera.WorldToScreenPoint(position);
                var wasVisible = _isVisible;

                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    _isVisible = true;
                }
                else
                {
                    _isVisible = false;
                }

                if (wasVisible != _isVisible)
                {
                    _canvas.enabled = _isVisible;
                }
            }

            if (_autoUpdate)
            {
                UpdateValue();
            }

            _labelText.transform.rotation = _camera != null ? Quaternion.LookRotation(_camera.transform.forward) : Quaternion.identity;
            _valueText.transform.rotation = _labelText.transform.rotation;
            _rectTransform.rotation = _labelText.transform.rotation;
        }

        public void Dispose()
        {
            _target = null;
            _labelText.text = string.Empty;
            _valueText.text = string.Empty;
            _canvas.enabled = false;
        }
    }

    public class StatsDisplaySystem : IDisposable
    {
        private readonly DebugVisualizeSettings _settings;
        private readonly Queue<DebugStatEntry> _available = new();
        private readonly List<DebugStatEntry> _active = new();
        private readonly List<DebugStatEntry> _toRemove = new();
        private GameObject _container;

        public StatsDisplaySystem(DebugVisualizeSettings settings)
        {
            _settings = settings;
            CreateContainer();
            Prewarm();
        }

        private TMP_FontAsset GetDefaultFont()
        {
            if (_settings.Font != null)
                return _settings.Font;

            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in fonts)
            {
                if (font.name.Contains("LibreFranklin") || font.name.Contains("Roboto") || font.name.Contains("Noto"))
                    return font;
            }

            return fonts.Length > 0 ? fonts[0] : TMP_Settings.defaultFontAsset;
        }

        private void CreateContainer()
        {
            _container = new GameObject("DebugStatContainer");
            _container.hideFlags = HideFlags.HideAndDontSave;
        }

        private void Prewarm()
        {
            for (int i = 0; i < _settings.TextPoolSize; i++)
            {
                var entry = CreateNewEntry();
                _available.Enqueue(entry);
            }
        }

        private DebugStatEntry CreateNewEntry()
        {
            var go = new GameObject("DebugStat");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_container.transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(2, 1);
            rectTransform.anchoredPosition3D = Vector3.zero;

            var labelGo = new GameObject("Label");
            labelGo.hideFlags = HideFlags.HideAndDontSave;
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(1.5f, 0.4f);
            var labelText = labelGo.AddComponent<TextMeshPro>();
            labelText.font = GetDefaultFont();
            labelText.fontSize = 2f;
            labelText.alignment = TextAlignmentOptions.BottomRight;

            var valueGo = new GameObject("Value");
            valueGo.hideFlags = HideFlags.HideAndDontSave;
            valueGo.transform.SetParent(go.transform, false);
            var valueRect = valueGo.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 1);
            valueRect.anchorMax = new Vector2(0, 1);
            valueRect.pivot = new Vector2(0.5f, 1);
            valueRect.anchoredPosition = new Vector2(0.9f, 0);
            valueRect.sizeDelta = new Vector2(0.6f, 0.4f);
            var valueText = valueGo.AddComponent<TextMeshPro>();
            valueText.font = GetDefaultFont();
            valueText.fontSize = 2f;
            valueText.alignment = TextAlignmentOptions.BottomLeft;

            var barBgGo = new GameObject("BarBackground");
            barBgGo.hideFlags = HideFlags.HideAndDontSave;
            barBgGo.transform.SetParent(go.transform, false);
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 0);
            barBgRect.anchorMax = new Vector2(1, 0);
            barBgRect.pivot = new Vector2(0.5f, 0);
            barBgRect.anchoredPosition = new Vector2(0, -0.45f);
            barBgRect.sizeDelta = new Vector2(1.5f, 0.15f);
            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = _settings.BarBackgroundColor;

            var barFillGo = new GameObject("BarFill");
            barFillGo.hideFlags = HideFlags.HideAndDontSave;
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin = new Vector2(0, 0);
            barFillRect.anchorMax = new Vector2(1, 1);
            barFillRect.pivot = new Vector2(0, 0.5f);
            barFillRect.anchoredPosition = Vector2.zero;
            barFillRect.sizeDelta = Vector2.zero;
            var barFillImage = barFillGo.AddComponent<Image>();
            barFillImage.color = _settings.BarFillColor;
            barFillImage.type = Image.Type.Filled;
            barFillImage.fillMethod = Image.FillMethod.Horizontal;

            canvas.enabled = false;

            return new DebugStatEntry(labelText, valueText, barFillImage, barBgRect, canvas, rectTransform);
        }

        public DebugStatEntry ShowStat(
            string label,
            Transform target,
            Vector3 offset,
            Color? color,
            bool showBar,
            Func<float> currentGetter,
            Func<float> maxGetter,
            bool autoUpdate,
            float duration)
        {
            if (_available.Count == 0 && _active.Count >= _settings.TextPoolSize)
            {
                return null;
            }

            var entry = _available.Count > 0 ? _available.Dequeue() : CreateNewEntry();

            entry.Setup(
                label,
                target,
                offset == Vector3.zero ? _settings.DefaultTextOffset : offset,
                duration > 0 ? duration : _settings.DefaultDuration,
                color ?? _settings.StatColor,
                showBar,
                currentGetter,
                maxGetter,
                autoUpdate,
                _settings
            );

            _active.Add(entry);
            return entry;
        }

        public void Update()
        {
            _toRemove.Clear();

            foreach (var entry in _active)
            {
                entry.Update();
                if (entry.IsExpired)
                {
                    _toRemove.Add(entry);
                }
            }

            foreach (var entry in _toRemove)
            {
                _active.Remove(entry);
                entry.Dispose();
                _available.Enqueue(entry);
            }
        }

        public void Clear()
        {
            foreach (var entry in _active)
            {
                entry.Dispose();
                _available.Enqueue(entry);
            }
            _active.Clear();
        }

        public void Dispose()
        {
            Clear();
            if (_container != null)
            {
                UnityEngine.Object.Destroy(_container);
                _container = null;
            }
        }
    }
}
#endif
