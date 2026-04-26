#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.HeaderAutoHide
{
    [InitializeOnLoad]
    internal static class HeaderAutoHideController
    {
#if UNITY_EDITOR_WIN
        const string MenuRoot = "Window/Header Auto-Hide/";
        const string PinShortcut = " %&h"; // Ctrl+Alt+H
        const string MinimizeShortcut = " %#m"; // Ctrl+Shift+M

        const int HeaderRegionPx = 80;

        static readonly IHeaderSegment[] _segments;
        static readonly RevealStateMachine _fsm = new RevealStateMachine();

        static double _lastPollTime;
        static bool _initialized;

        static HeaderAutoHideController()
        {
            _segments = new IHeaderSegment[]
            {
                new ToolbarSegment(),
                new MenuBarSegment(),
                new TitleBarSegment(),
            };

            EditorApplication.delayCall += DeferredInitialize;
            EditorApplication.quitting += OnQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            HeaderAutoHideSettings.Changed += OnSettingsChanged;

            try { AppDomain.CurrentDomain.ProcessExit += (_, __) => ForceShowAllSafe(); } catch { }
        }

        static void DeferredInitialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Defensive: if last shutdown left bars hidden, force them visible before installing.
            if (HeaderAutoHideSettings.HiddenAtShutdown)
            {
                ForceShowAllSafe();
                HeaderAutoHideSettings.HiddenAtShutdown = false;
            }

            foreach (var s in _segments)
            {
                try { if (s.IsAvailable) s.CaptureBaseline(); }
                catch (Exception e) { Debug.LogWarning($"[HeaderAutoHide] {s.Name}.CaptureBaseline failed: {e.Message}"); }
            }

            EditorApplication.update += OnEditorUpdate;
            ApplyState(forceShow: !ShouldRunActiveLogic());
        }

        static bool ShouldRunActiveLogic()
        {
            if (HeaderAutoHideSettings.KillSwitch) return false;
            if (!HeaderAutoHideSettings.Enabled) return false;
            return AnySegmentEnabled();
        }

        static bool AnySegmentEnabled()
            => HeaderAutoHideSettings.HideToolbar
            || HeaderAutoHideSettings.HideMenuBar
            || HeaderAutoHideSettings.HideTitleBar;

        static bool IsSegmentEnabled(IHeaderSegment seg)
        {
            switch (seg.Name)
            {
                case "Toolbar":  return HeaderAutoHideSettings.HideToolbar;
                case "MenuBar":  return HeaderAutoHideSettings.HideMenuBar;
                case "TitleBar": return HeaderAutoHideSettings.HideTitleBar;
                default: return false;
            }
        }

        static void OnEditorUpdate()
        {
            // EnforceState runs every editor update (drives toolbar slide animation, defeats Reflow).
            // Cheap when nothing's animating. Runs even when ShouldRunActiveLogic is false so the
            // segment can settle/deactivate cleanly.
            foreach (var s in _segments)
            {
                try { if (s.IsAvailable) s.EnforceState(); }
                catch { }
            }

            if (!ShouldRunActiveLogic())
            {
                EnsureAllShown();
                return;
            }

            // Static mode: skip cursor poll. Each bar stays in the state its per-segment
            // toggle dictates. Honor the pin hotkey so users can still force bars visible
            // when locked out (e.g. menu bar hidden, can't reach Preferences).
            if (HeaderAutoHideSettings.StaticMode)
            {
                ApplyState(forceShow: _fsm.IsPinned);
                return;
            }

            // Auto-hide mode: throttled cursor poll drives FSM transitions.
            int hz = Mathf.Max(1, HeaderAutoHideSettings.PollHz);
            double interval = 1.0 / hz;
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastPollTime < interval) return;
            _lastPollTime = now;

            var sample = CursorTracker.Sample(HeaderAutoHideSettings.RevealZonePx, HeaderRegionPx);
            if (!sample.WindowFound) return;

            if (_fsm.Tick(sample, out bool shouldShow, out bool shouldHide))
            {
                if (shouldShow) ApplyState(forceShow: true);
                else if (shouldHide) ApplyState(forceShow: false);
            }
        }

        static void ApplyState(bool forceShow)
        {
            HeaderAutoHideSettings.HiddenAtShutdown = !forceShow && ShouldRunActiveLogic();

            foreach (var s in _segments)
            {
                try
                {
                    if (!s.IsAvailable) continue;
                    bool shouldHide = !forceShow && IsSegmentEnabled(s);
                    if (shouldHide) s.Hide();
                    else s.Show();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[HeaderAutoHide] {s.Name}.ApplyState failed: {e.Message}");
                }
            }
        }

        static void EnsureAllShown()
        {
            HeaderAutoHideSettings.HiddenAtShutdown = false;
            foreach (var s in _segments)
            {
                try { if (s.IsAvailable && s.IsCurrentlyHidden) s.Show(); }
                catch (Exception e) { Debug.LogWarning($"[HeaderAutoHide] {s.Name}.Show failed: {e.Message}"); }
            }
        }

        static void OnSettingsChanged()
        {
            if (!_initialized) return;
            // Snap visible after toggles flip; cursor poll will re-hide if appropriate.
            _fsm.ForceShown();
            ApplyState(forceShow: true);
        }

        static void OnQuitting() => ForceShowAllSafe();

        static void OnBeforeAssemblyReload()
        {
            // Bars are managed via Win32 / UIE which survive a domain reload. Force-restore
            // so the user is never stuck staring at a hidden header during recompile.
            ForceShowAllSafe();
        }

        static void ForceShowAllSafe()
        {
            foreach (var s in _segments)
            {
                try { if (s.IsAvailable) s.ForceShowImmediate(); }
                catch { }
            }
            HeaderAutoHideSettings.HiddenAtShutdown = false;
        }

        public static void ForceShowAll() => ForceShowAllSafe();

        // ---- Menu items / hotkeys ----

        [MenuItem(MenuRoot + "Toggle Pin" + PinShortcut)]
        static void Menu_TogglePin()
        {
            if (!ShouldRunActiveLogic())
            {
                Debug.Log("[HeaderAutoHide] Enable the feature in Preferences > Header Auto-Hide first.");
                return;
            }
            _fsm.TogglePin();
            ApplyState(forceShow: _fsm.IsPinned);
        }

        [MenuItem(MenuRoot + "Show All Now")]
        static void Menu_ShowAll()
        {
            _fsm.ForceShown();
            ApplyState(forceShow: true);
        }

        [MenuItem(MenuRoot + "Drag Window (when title bar hidden)")]
        static void Menu_Drag() => TitleBarSegment.TryStartDrag();

        [MenuItem(MenuRoot + "Minimize" + MinimizeShortcut)]
        static void Menu_Minimize() => TitleBarSegment.TryMinimize();

        [MenuItem(MenuRoot + "Open Settings...")]
        static void Menu_OpenSettings() => SettingsService.OpenUserPreferences("Preferences/Header Auto-Hide");

        // Emergency: force kill switch on + show all bars, even when the menu bar is hidden.
        // Uses Unity's MenuItem shortcut handling which works regardless of OS menu visibility.
        [MenuItem(MenuRoot + "Emergency Disable" + " %&#h")] // Ctrl+Alt+Shift+H
        static void Menu_EmergencyDisable()
        {
            HeaderAutoHideSettings.KillSwitch = true;
            ForceShowAllSafe();
            HeaderAutoHideSettings.RaiseChanged();
            Debug.Log("[HeaderAutoHide] Emergency disable: kill switch is ON, all bars restored. Toggle off in Preferences > Header Auto-Hide when ready.");
        }

        [MenuItem(MenuRoot + "Diagnose Toolbar")]
        static void Menu_DiagnoseToolbar()
        {
            var seg = _segments.OfType<ToolbarSegment>().FirstOrDefault();
            if (seg == null) { Debug.Log("[HeaderAutoHide] Toolbar segment not found."); return; }
            Debug.Log(seg.Diagnose());
        }

        [MenuItem(MenuRoot + "Diagnose Title Bar")]
        static void Menu_DiagnoseTitleBar()
        {
            var seg = _segments.OfType<TitleBarSegment>().FirstOrDefault();
            if (seg == null) { Debug.Log("[HeaderAutoHide] TitleBar segment not found."); return; }
            Debug.Log(seg.Diagnose());
        }
#else
        static HeaderAutoHideController()
        {
            // Non-Windows editor: feature is a no-op.
        }

        public static void ForceShowAll() { }
#endif
    }
}
#endif
