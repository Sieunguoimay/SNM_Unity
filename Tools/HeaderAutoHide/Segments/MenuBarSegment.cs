#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;
using UnityEngine;

namespace Snm.Tools.HeaderAutoHide
{
    internal sealed class MenuBarSegment : IHeaderSegment
    {
        IntPtr _savedHmenu = IntPtr.Zero;
        bool _baselineCaptured;
        bool _hidden;
        bool _availableChecked;
        bool _available;

        public string Name => "MenuBar";

        public bool IsAvailable
        {
            get
            {
                if (!_availableChecked)
                {
                    _availableChecked = true;
                    var hwnd = MainWindowResolver.GetMainHwnd();
                    if (hwnd != IntPtr.Zero)
                    {
                        var hmenu = Win32Native.GetMenu(hwnd);
                        _available = hmenu != IntPtr.Zero;
                        if (!_available)
                            Debug.Log("[HeaderAutoHide] Menu bar segment unavailable: Unity 6 appears to draw the menu itself, not via Win32 HMENU. Hiding the menu bar is not supported in this Unity version.");
                    }
                }
                return _available;
            }
        }

        public bool IsCurrentlyHidden => _hidden;

        public void CaptureBaseline()
        {
            if (_baselineCaptured) return;
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero) return;
            _savedHmenu = Win32Native.GetMenu(hwnd);
            _baselineCaptured = _savedHmenu != IntPtr.Zero;
        }

        public void Hide()
        {
            if (!IsAvailable || _hidden) return;
            CaptureBaseline();
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero || _savedHmenu == IntPtr.Zero) return;

            try
            {
                Win32Native.SetMenu(hwnd, IntPtr.Zero);
                Win32Native.DrawMenuBar(hwnd);
                _hidden = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] MenuBar.Hide failed: {e.Message}");
            }
        }

        public void Show()
        {
            if (!_hidden) return;
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero || _savedHmenu == IntPtr.Zero) return;

            try
            {
                Win32Native.SetMenu(hwnd, _savedHmenu);
                Win32Native.DrawMenuBar(hwnd);
                _hidden = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] MenuBar.Show failed: {e.Message}");
            }
        }

        public void ForceShowImmediate() => Show();

        public void EnforceState() { }
    }
}
#endif
