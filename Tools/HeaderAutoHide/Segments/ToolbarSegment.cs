#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.HeaderAutoHide
{
    // Slide-animation model:
    //   When Activate'd, MainView's children positions are fully managed by us each tick.
    //   Toolbar height animates between 0 (hidden) and saved baseline (~36, shown). The
    //   MainContent's y/height follow the toolbar height, so it slides smoothly. No reflow
    //   snap, no overlap — just a brief glide on hide/show.
    //
    //   We override children positions because MainView.Reflow() re-stamps them from a
    //   const (kToolbarHeight=36) and ignores m_UseTopView for layout. EnforceState
    //   re-asserts our layout every editor tick to defeat Reflow.
    internal sealed class ToolbarSegment : IHeaderSegment
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        EditorWindow _toolbarWindow;
        VisualElement _root;
        object _hostView;
        object _mainView;

        FieldInfo _f_UseTopView;
        FieldInfo _f_TopViewHeight;
        FieldInfo _f_HostToolbarHeight;
        PropertyInfo _p_MainViewPosition;
        PropertyInfo _p_MainViewChildren;

        object _toolbarChild;
        object _mainContentChild;
        object _statusBarChild;

        bool _baselineCaptured;
        bool _savedUseTopView;
        float _savedTopViewHeight;
        float _savedHostToolbarHeight;
        float _savedToolbarChildHeight = 36f;
        float _savedStatusBarChildHeight = 20f;

        bool _active;
        bool _hidden;
        float _currentToolbarH = 36f;
        double _lastAnimTime;

        public string Name => "Toolbar";

        public bool IsAvailable
        {
            get
            {
                Resolve();
                return _mainView != null && _p_MainViewChildren != null && _p_MainViewPosition != null;
            }
        }

        public bool IsCurrentlyHidden => _hidden;

        public void CaptureBaseline()
        {
            Resolve();
            if (_baselineCaptured || _mainView == null) return;
            try
            {
                if (_f_UseTopView != null)
                    _savedUseTopView = (bool)_f_UseTopView.GetValue(_mainView);
                if (_f_TopViewHeight != null)
                    _savedTopViewHeight = (float)_f_TopViewHeight.GetValue(_mainView);
                if (_f_HostToolbarHeight != null && _hostView != null)
                    _savedHostToolbarHeight = (float)_f_HostToolbarHeight.GetValue(_hostView);

                var children = _p_MainViewChildren.GetValue(_mainView) as Array;
                if (children != null)
                {
                    if (children.Length >= 1)
                    {
                        _toolbarChild = children.GetValue(0);
                        if (TryGetViewHeight(_toolbarChild, out var h0)) _savedToolbarChildHeight = h0;
                    }
                    if (children.Length >= 2)
                        _mainContentChild = children.GetValue(1);
                    if (children.Length >= 3)
                    {
                        _statusBarChild = children.GetValue(2);
                        if (TryGetViewHeight(_statusBarChild, out var h2)) _savedStatusBarChildHeight = h2;
                    }
                }

                _currentToolbarH = _savedToolbarChildHeight;
                _baselineCaptured = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] Toolbar baseline capture failed: {e.Message}");
            }
        }

        public void Hide()
        {
            Resolve();
            if (!IsAvailable) return;
            EnsureActive();
            if (_hidden) return;
            _hidden = true;
            // Animation target updates; EnforceState drives the actual lerp.
        }

        public void Show()
        {
            Resolve();
            if (!IsAvailable) return;
            if (!ShouldBeActive)
            {
                if (_active) Deactivate();
                return;
            }
            EnsureActive();
            _hidden = false;
        }

        public void ForceShowImmediate()
        {
            if (!_active && !_hidden) return;
            // Snap to fully shown, then hand layout back to Unity.
            _hidden = false;
            _currentToolbarH = _savedToolbarChildHeight;
            ApplyLayout();
            Deactivate();
        }

        public void EnforceState()
        {
            if (!_active) return;
            UpdateAnimation();
            ApplyLayout();
        }

        void EnsureActive()
        {
            if (_active) return;
            CaptureBaseline();
            _active = true;
            _lastAnimTime = EditorApplication.timeSinceStartup;
        }

        void Deactivate()
        {
            _active = false;
            _hidden = false;
            // Hand layout back to Unity. MainView.Reflow will rebuild children from kToolbarHeight.
            TrySet(_f_UseTopView, _mainView, _savedUseTopView);
            TrySet(_f_TopViewHeight, _mainView, _savedTopViewHeight);
            TrySet(_f_HostToolbarHeight, _hostView, _savedHostToolbarHeight);
            try { InvokeNoArg(_mainView, _mainView?.GetType(), "Reflow"); } catch { }
            try { _toolbarWindow?.Repaint(); } catch { }
        }

        bool ShouldBeActive
        {
            get
            {
                if (HeaderAutoHideSettings.KillSwitch) return false;
                if (!HeaderAutoHideSettings.Enabled) return false;
                return HeaderAutoHideSettings.HideToolbar;
            }
        }

        // ---------- Animation + layout ----------

        void UpdateAnimation()
        {
            double now = EditorApplication.timeSinceStartup;
            double dt = now - _lastAnimTime;
            _lastAnimTime = now;
            if (dt < 0) dt = 0;
            if (dt > 0.1) dt = 0.1; // clamp on hitches

            float target = _hidden ? 0f : _savedToolbarChildHeight;

            // User-tunable duration; convert to exp-decay rate so the lerp is frame-rate independent.
            int durMs = Mathf.Max(1, HeaderAutoHideSettings.AnimDurationMs);
            float rate = 5000f / durMs; // ~5 time-constants over the duration → effectively settled
            float alpha = 1f - Mathf.Exp(-rate * (float)dt);
            _currentToolbarH = Mathf.Lerp(_currentToolbarH, target, alpha);
            if (Mathf.Abs(_currentToolbarH - target) < 0.5f) _currentToolbarH = target;
        }

        void ApplyLayout()
        {
            if (_mainView == null || _p_MainViewPosition == null || _mainContentChild == null) return;
            try
            {
                var mvPos = (Rect)_p_MainViewPosition.GetValue(_mainView);
                float w = mvPos.width;
                float h = mvPos.height;
                float statusH = (_statusBarChild != null) ? _savedStatusBarChildHeight : 0f;
                float toolbarH = _currentToolbarH;

                SetViewPosition(_toolbarChild, new Rect(0f, 0f, w, toolbarH));
                SetViewPosition(_mainContentChild, new Rect(0f, toolbarH, w, h - toolbarH - statusH));
                if (_statusBarChild != null)
                    SetViewPosition(_statusBarChild, new Rect(0f, h - statusH, w, statusH));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] ApplyLayout failed: {e.Message}");
            }
        }

        void SetViewPosition(object view, Rect newPos)
        {
            if (view == null) return;
            try
            {
                var t = view.GetType();
                var setPos = t.GetMethod("SetPosition", Flags, null, new[] { typeof(Rect) }, null);
                if (setPos != null) { setPos.Invoke(view, new object[] { newPos }); return; }

                var prop = t.GetProperty("position", Flags);
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(Rect))
                {
                    prop.SetValue(view, newPos);
                    return;
                }
                var field = t.GetField("m_Pos", Flags);
                if (field != null && field.FieldType == typeof(Rect))
                    field.SetValue(view, newPos);
            }
            catch { }
        }

        bool TryGetViewHeight(object view, out float h)
        {
            h = 0f;
            if (view == null) return false;
            try
            {
                var prop = view.GetType().GetProperty("position", Flags);
                if (prop != null && prop.PropertyType == typeof(Rect))
                {
                    h = ((Rect)prop.GetValue(view)).height;
                    return true;
                }
            }
            catch { }
            return false;
        }

        // ---------- Resolution ----------

        void Resolve()
        {
            if (_mainView != null && _hostView != null && _toolbarWindow != null) return;

            _toolbarWindow = null;
            _root = null;
            _hostView = null;
            _mainView = null;

            try
            {
                var asm = typeof(Editor).Assembly;
                var mtwType = asm.GetType("UnityEditor.MainToolbarWindow");
                if (mtwType == null || !typeof(EditorWindow).IsAssignableFrom(mtwType))
                {
                    Debug.LogWarning("[HeaderAutoHide] UnityEditor.MainToolbarWindow not found. Toolbar segment is Unity 6.3+ only.");
                    return;
                }

                var instances = Resources.FindObjectsOfTypeAll(mtwType);
                if (instances == null || instances.Length == 0) return;

                _toolbarWindow = instances[0] as EditorWindow;
                if (_toolbarWindow == null) return;

                _root = _toolbarWindow.rootVisualElement;
                _hostView = typeof(EditorWindow).GetField("m_Parent", Flags)?.GetValue(_toolbarWindow);
                if (_hostView == null) return;

                object cursor = _hostView;
                for (int i = 0; cursor != null && i < 6; i++)
                {
                    if (cursor.GetType().FullName == "UnityEditor.MainView")
                    {
                        _mainView = cursor;
                        break;
                    }
                    var parentField = cursor.GetType().GetField("m_Parent", Flags);
                    var parentProp = cursor.GetType().GetProperty("parent", Flags);
                    cursor = parentField?.GetValue(cursor) ?? parentProp?.GetValue(cursor);
                }

                if (_mainView != null)
                {
                    var mvType = _mainView.GetType();
                    _f_UseTopView = StripIfReadOnly(mvType.GetField("m_UseTopView", Flags));
                    _f_TopViewHeight = StripIfReadOnly(mvType.GetField("m_TopViewHeight", Flags));
                    _p_MainViewPosition = mvType.GetProperty("position", Flags);
                    _p_MainViewChildren = mvType.GetProperty("children", Flags);
                }

                _f_HostToolbarHeight = StripIfReadOnly(_hostView.GetType().GetField("ToolbarHeight", Flags));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] Toolbar resolve failed: {e.Message}");
                _toolbarWindow = null;
                _root = null;
                _hostView = null;
                _mainView = null;
            }
        }

        static FieldInfo StripIfReadOnly(FieldInfo f)
        {
            if (f == null) return null;
            return (f.IsLiteral || f.IsInitOnly) ? null : f;
        }

        static void TrySet(FieldInfo f, object target, object value)
        {
            if (f == null || target == null) return;
            try { f.SetValue(target, value); } catch { }
        }

        static void InvokeNoArg(object target, Type t, string methodName)
        {
            if (target == null || t == null) return;
            try
            {
                var m = t.GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
                m?.Invoke(target, null);
            }
            catch { }
        }

        // ---------- Diagnostic ----------

        public string Diagnose()
        {
            Resolve();
            var sb = new StringBuilder();
            sb.AppendLine("[HeaderAutoHide] Toolbar diagnostic:");
            sb.AppendLine($"  MainToolbarWindow:    {_toolbarWindow != null}");
            sb.AppendLine($"  MainView:             {_mainView != null}");
            sb.AppendLine($"  active:               {_active}");
            sb.AppendLine($"  hidden:               {_hidden}");
            sb.AppendLine($"  currentToolbarH:      {_currentToolbarH:F2}");
            sb.AppendLine($"  savedToolbarH:        {_savedToolbarChildHeight:F2}");
            sb.AppendLine($"  savedStatusH:         {_savedStatusBarChildHeight:F2}");

            if (_mainView != null && _p_MainViewChildren != null)
            {
                try
                {
                    var children = _p_MainViewChildren.GetValue(_mainView) as Array;
                    if (children != null)
                    {
                        for (int i = 0; i < children.Length; i++)
                        {
                            var c = children.GetValue(i);
                            var posProp = c?.GetType().GetProperty("position", Flags);
                            Rect r = (posProp != null) ? (Rect)posProp.GetValue(c) : default;
                            sb.AppendLine($"  child[{i}] {c?.GetType().Name}  pos={r}");
                        }
                    }
                }
                catch (Exception e) { sb.AppendLine($"  (children dump failed: {e.Message})"); }
            }
            return sb.ToString();
        }
    }
}
#endif
