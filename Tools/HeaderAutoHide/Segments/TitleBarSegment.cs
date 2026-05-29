#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Snm.Tools.HeaderAutoHide
{
    internal sealed class TitleBarSegment : IHeaderSegment
    {
        long _savedStyle;
        bool _baselineCaptured;
        bool _hidden;
        bool _wasMaximized;
        Win32Native.RECT _savedWinRect;
        bool _savedWinRectCaptured;

        public string Name => "TitleBar";

        public bool IsAvailable => MainWindowResolver.GetMainHwnd() != IntPtr.Zero;

        public bool IsCurrentlyHidden => _hidden;

        public void CaptureBaseline()
        {
            if (_baselineCaptured) return;
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero) return;
            _savedStyle = Win32Native.GetWindowStyle(hwnd);
            _baselineCaptured = true;
        }

        public void Hide()
        {
            if (!IsAvailable || _hidden) return;
            CaptureBaseline();
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero) return;

            try
            {
                _wasMaximized = MainWindowResolver.IsMaximized();

                // Snapshot current window rect BEFORE we change anything. Unity's editor often runs
                // in a custom borderless-maximize state where IsMaximized()==false but the rect
                // already extends to (or past) the monitor edges. Show() needs this to restore.
                if (Win32Native.GetWindowRect(hwnd, out var origRect))
                {
                    _savedWinRect = origRect;
                    _savedWinRectCaptured = true;
                }

                if (_wasMaximized)
                    Win32Native.ShowWindow(hwnd, Win32Native.SW_RESTORE);

                long current = Win32Native.GetWindowStyle(hwnd);
                long stripped = current & ~(Win32Native.WS_CAPTION | Win32Native.WS_THICKFRAME);
                Win32Native.SetWindowStyle(hwnd, stripped);

                Win32Native.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    Win32Native.SWP_FRAMECHANGED | Win32Native.SWP_NOMOVE | Win32Native.SWP_NOSIZE |
                    Win32Native.SWP_NOZORDER | Win32Native.SWP_NOACTIVATE);

                if (_wasMaximized && TryGetMonitorRect(hwnd, out var monitorRect))
                {
                    // OS-maximize was on. Manually fill monitor (avoiding SW_MAXIMIZE which would
                    // set showCmd=MAX → DWM treats as fullscreen → suppresses auto-hide taskbar).
                    Win32Native.SetWindowPos(hwnd, IntPtr.Zero,
                        monitorRect.Left, monitorRect.Top, monitorRect.Width, monitorRect.Height,
                        Win32Native.SWP_NOZORDER | Win32Native.SWP_NOACTIVATE);
                }

                // Apply gap regardless of how we got here. Handles Unity's custom borderless
                // maximize (where the rect extends beyond the monitor by 8-56 px) and ensures
                // the auto-hide taskbar's reveal hot zone is reachable.
                ApplyTaskbarGap(hwnd);

                _hidden = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] TitleBar.Hide failed: {e.Message}");
            }
        }

        public void Show()
        {
            if (!_hidden) return;
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero) return;

            try
            {
                Win32Native.SetWindowStyle(hwnd, _savedStyle);

                Win32Native.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    Win32Native.SWP_FRAMECHANGED | Win32Native.SWP_NOMOVE | Win32Native.SWP_NOSIZE |
                    Win32Native.SWP_NOZORDER | Win32Native.SWP_NOACTIVATE);

                if (_wasMaximized)
                {
                    Win32Native.ShowWindow(hwnd, Win32Native.SW_MAXIMIZE);
                }
                else if (_savedWinRectCaptured)
                {
                    // Unity custom borderless: restore exact original rect (including any padding past monitor).
                    Win32Native.SetWindowPos(hwnd, IntPtr.Zero,
                        _savedWinRect.Left, _savedWinRect.Top, _savedWinRect.Width, _savedWinRect.Height,
                        Win32Native.SWP_NOZORDER | Win32Native.SWP_NOACTIVATE);
                }

                _hidden = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] TitleBar.Show failed: {e.Message}");
            }
        }

        // If the current window rect covers an auto-hide taskbar's reveal hot zone, shrink it
        // by a few px on that edge so the taskbar can still peek through.
        static void ApplyTaskbarGap(IntPtr hwnd)
        {
            if (!Win32Native.GetWindowRect(hwnd, out var winRect)) return;
            if (!TryGetMonitorRect(hwnd, out var monRect)) return;

            var edge = FindAutoHideTaskbarEdge(monRect);
            if (edge == uint.MaxValue) return; // no auto-hide bar — nothing to do

            const int Gap = 2;
            int newLeft = winRect.Left, newTop = winRect.Top, newRight = winRect.Right, newBottom = winRect.Bottom;
            bool changed = false;

            switch (edge)
            {
                case Win32Native.ABE_BOTTOM:
                {
                    int target = monRect.Bottom - Gap;
                    if (newBottom > target) { newBottom = target; changed = true; }
                    break;
                }
                case Win32Native.ABE_TOP:
                {
                    int target = monRect.Top + Gap;
                    if (newTop < target) { newTop = target; changed = true; }
                    break;
                }
                case Win32Native.ABE_LEFT:
                {
                    int target = monRect.Left + Gap;
                    if (newLeft < target) { newLeft = target; changed = true; }
                    break;
                }
                case Win32Native.ABE_RIGHT:
                {
                    int target = monRect.Right - Gap;
                    if (newRight > target) { newRight = target; changed = true; }
                    break;
                }
            }

            if (changed)
            {
                Win32Native.SetWindowPos(hwnd, IntPtr.Zero,
                    newLeft, newTop, newRight - newLeft, newBottom - newTop,
                    Win32Native.SWP_NOZORDER | Win32Native.SWP_NOACTIVATE);
            }
        }

        static bool TryGetMonitorRect(IntPtr hwnd, out Win32Native.RECT rect)
        {
            rect = default;
            try
            {
                var monitor = Win32Native.MonitorFromWindow(hwnd, Win32Native.MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero) return false;
                var mi = new Win32Native.MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(Win32Native.MONITORINFO));
                if (!Win32Native.GetMonitorInfo(monitor, ref mi)) return false;
                rect = mi.rcMonitor;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] TryGetMonitorRect failed: {e.Message}");
                return false;
            }
        }

        // Returns the edge index where an auto-hide taskbar lives on the given monitor,
        // or uint.MaxValue if no auto-hide taskbar is detected.
        static uint FindAutoHideTaskbarEdge(Win32Native.RECT monitorRect)
        {
            try
            {
                foreach (uint edge in new[] { Win32Native.ABE_BOTTOM, Win32Native.ABE_TOP, Win32Native.ABE_LEFT, Win32Native.ABE_RIGHT })
                {
                    var abd = new Win32Native.APPBARDATA();
                    abd.cbSize = Marshal.SizeOf(typeof(Win32Native.APPBARDATA));
                    abd.uEdge = edge;
                    abd.rc = monitorRect;
                    var result = Win32Native.SHAppBarMessage(Win32Native.ABM_GETAUTOHIDEBAREX, ref abd);
                    if (result != IntPtr.Zero) return edge;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] FindAutoHideTaskbarEdge failed: {e.Message}");
            }
            return uint.MaxValue;
        }

        public string Diagnose()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[HeaderAutoHide] TitleBar diagnostic:");
            var hwnd = MainWindowResolver.GetMainHwnd();
            sb.AppendLine($"  hwnd:                 {hwnd}");
            sb.AppendLine($"  hidden flag:          {_hidden}");
            sb.AppendLine($"  was maximized:        {_wasMaximized}");

            if (hwnd == IntPtr.Zero) return sb.ToString();

            try
            {
                if (Win32Native.GetWindowRect(hwnd, out var wr))
                    sb.AppendLine($"  GetWindowRect:        ({wr.Left},{wr.Top}) -> ({wr.Right},{wr.Bottom}) size {wr.Width}x{wr.Height}");

                var wp = new Win32Native.WINDOWPLACEMENT { length = Marshal.SizeOf(typeof(Win32Native.WINDOWPLACEMENT)) };
                if (Win32Native.GetWindowPlacement(hwnd, ref wp))
                    sb.AppendLine($"  WindowPlacement:      showCmd={wp.showCmd}  (1=NORMAL, 2=MIN, 3=MAX)");

                long style = Win32Native.GetWindowStyle(hwnd);
                bool hasCaption = (style & Win32Native.WS_CAPTION) != 0;
                bool hasThickFrame = (style & Win32Native.WS_THICKFRAME) != 0;
                sb.AppendLine($"  WS_CAPTION:           {hasCaption}");
                sb.AppendLine($"  WS_THICKFRAME:        {hasThickFrame}");

                if (TryGetMonitorRect(hwnd, out var mr))
                {
                    sb.AppendLine($"  Monitor rect:         ({mr.Left},{mr.Top}) -> ({mr.Right},{mr.Bottom}) size {mr.Width}x{mr.Height}");

                    var monitor = Win32Native.MonitorFromWindow(hwnd, Win32Native.MONITOR_DEFAULTTONEAREST);
                    var mi = new Win32Native.MONITORINFO { cbSize = Marshal.SizeOf(typeof(Win32Native.MONITORINFO)) };
                    if (Win32Native.GetMonitorInfo(monitor, ref mi))
                        sb.AppendLine($"  Work area:            ({mi.rcWork.Left},{mi.rcWork.Top}) -> ({mi.rcWork.Right},{mi.rcWork.Bottom}) size {mi.rcWork.Width}x{mi.rcWork.Height}");

                    var edge = FindAutoHideTaskbarEdge(mr);
                    string edgeName = edge switch
                    {
                        Win32Native.ABE_LEFT => "LEFT",
                        Win32Native.ABE_TOP => "TOP",
                        Win32Native.ABE_RIGHT => "RIGHT",
                        Win32Native.ABE_BOTTOM => "BOTTOM",
                        _ => "(none detected)"
                    };
                    sb.AppendLine($"  Auto-hide bar edge:   {edgeName}");
                }

                var emptyAbd = new Win32Native.APPBARDATA { cbSize = Marshal.SizeOf(typeof(Win32Native.APPBARDATA)) };
                var stateResult = Win32Native.SHAppBarMessage(Win32Native.ABM_GETSTATE, ref emptyAbd);
                long stateBits = stateResult.ToInt64();
                sb.AppendLine($"  ABM_GETSTATE:         0x{stateBits:X}  (autohide={(stateBits & Win32Native.ABS_AUTOHIDE) != 0})");
            }
            catch (Exception e)
            {
                sb.AppendLine($"  diagnostic failed: {e.Message}");
            }

            return sb.ToString();
        }

        public void ForceShowImmediate() => Show();

        public void EnforceState() { }

        public static void TryStartDrag()
        {
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd == IntPtr.Zero) return;
            try
            {
                Win32Native.ReleaseCapture();
                Win32Native.SendMessage(hwnd, Win32Native.WM_NCLBUTTONDOWN, new IntPtr(Win32Native.HTCAPTION), IntPtr.Zero);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HeaderAutoHide] Drag-by-message failed: {e.Message}");
            }
        }

        public static void TryMinimize()
        {
            var hwnd = MainWindowResolver.GetMainHwnd();
            if (hwnd != IntPtr.Zero) Win32Native.ShowWindow(hwnd, Win32Native.SW_MINIMIZE);
        }
    }
}
#endif
