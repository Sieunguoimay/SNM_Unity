using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Snm.Runtime.DebugVisualize
{
    public class DebugTextEntry : IDisposable
    {
        private readonly TextMeshPro _text;
        private readonly Canvas _canvas;
        private readonly RectTransform _rectTransform;
        private Transform _target;
        private Vector3 _offset;
        private float _duration;
        private float _elapsed;
        private bool _autoHideOffScreen;
        private Camera _camera;
        private bool _isVisible = true;

        public TextMeshPro Text => _text;
        public bool IsExpired => _duration > 0 && _elapsed >= _duration;

        public DebugTextEntry(TextMeshPro text, Canvas canvas, RectTransform rectTransform)
        {
            _text = text;
            _canvas = canvas;
            _rectTransform = rectTransform;
        }

        public void Setup(string content, Transform target, Vector3 offset, Color color, float fontSize, float duration, bool autoHideOffScreen)
        {
            _target = target;
            _offset = offset;
            _duration = duration;
            _elapsed = 0;
            _autoHideOffScreen = autoHideOffScreen;
            _camera = Camera.main;

            _text.text = content;
            _text.color = color;
            _text.fontSize = fontSize;
            _text.alignment = TextAlignmentOptions.Center;
            // _text.enableShadowing = true;
            // _text.shadowColor = new Color(0, 0, 0, 0.5f);
            // _text.shadowOffset = new Vector2(1, -1);
            // _text.shadowSoftness = 2;

            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 10f;
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

            _text.transform.rotation = _camera != null ? Quaternion.LookRotation(_camera.transform.forward) : Quaternion.identity;
        }

        public void Dispose()
        {
            _target = null;
            _text.text = string.Empty;
            _canvas.enabled = false;
        }
    }

    public class TextDisplaySystem : IDisposable
    {
        private readonly DebugVisualizeSettings _settings;
        private readonly ObjectPool<DebugTextEntry> _pool;
        private readonly Queue<DebugTextEntry> _available = new();
        private readonly List<DebugTextEntry> _active = new();
        private readonly List<DebugTextEntry> _toRemove = new();
        private GameObject _container;

        public TextDisplaySystem(DebugVisualizeSettings settings)
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
            _container = new GameObject("DebugTextContainer");
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

        private DebugTextEntry CreateNewEntry()
        {
            var go = new GameObject("DebugText");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(_container.transform, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition3D = Vector3.zero;

            var text = go.AddComponent<TextMeshPro>();
            text.font = GetDefaultFont();
            text.fontSize = _settings.DefaultFontSize;
            text.text = string.Empty;
            text.autoSizeTextContainer = true;

            var canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            var graphicRaycaster = go.AddComponent<GraphicRaycaster>();
            graphicRaycaster.ignoreReversedGraphics = false;

            canvas.enabled = false;

            return new DebugTextEntry(text, canvas, rectTransform);
        }

        public DebugTextEntry ShowText(string content, Transform target, Vector3 offset, Color? color = null, float fontSize = 0, float duration = 0)
        {
            if (_available.Count == 0 && _active.Count >= _settings.TextPoolSize)
            {
                return null;
            }

            var entry = _available.Count > 0 ? _available.Dequeue() : CreateNewEntry();
            
            entry.Setup(
                content,
                target,
                offset == Vector3.zero ? _settings.DefaultTextOffset : offset,
                color ?? _settings.TextColor,
                fontSize > 0 ? fontSize : _settings.DefaultFontSize,
                duration > 0 ? duration : _settings.DefaultDuration,
                _settings.AutoHideOffScreen
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
