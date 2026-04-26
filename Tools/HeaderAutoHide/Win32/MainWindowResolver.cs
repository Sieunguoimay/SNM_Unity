#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;
using System.Diagnostics;

namespace Snm.Tools.HeaderAutoHide
{
    internal static class MainWindowResolver
    {
        private static IntPtr _cached = IntPtr.Zero;

        public static IntPtr GetMainHwnd()
        {
            if (_cached != IntPtr.Zero && Win32Native.IsWindow(_cached))
                return _cached;

            try
            {
                _cached = Process.GetCurrentProcess().MainWindowHandle;
            }
            catch
            {
                _cached = IntPtr.Zero;
            }

            if (_cached != IntPtr.Zero && !Win32Native.IsWindow(_cached))
                _cached = IntPtr.Zero;

            return _cached;
        }

        public static bool TryGetWindowRect(out Win32Native.RECT rect)
        {
            rect = default;
            var hwnd = GetMainHwnd();
            return hwnd != IntPtr.Zero && Win32Native.GetWindowRect(hwnd, out rect);
        }

        public static float GetDpiScale()
        {
            var hwnd = GetMainHwnd();
            if (hwnd == IntPtr.Zero) return 1f;
            try
            {
                var dpi = Win32Native.GetDpiForWindow(hwnd);
                return dpi <= 0 ? 1f : dpi / 96f;
            }
            catch
            {
                return 1f;
            }
        }

        public static bool IsMaximized()
        {
            var hwnd = GetMainHwnd();
            if (hwnd == IntPtr.Zero) return false;
            var wp = new Win32Native.WINDOWPLACEMENT { length = System.Runtime.InteropServices.Marshal.SizeOf<Win32Native.WINDOWPLACEMENT>() };
            return Win32Native.GetWindowPlacement(hwnd, ref wp) && wp.showCmd == Win32Native.SW_MAXIMIZE;
        }

        public static void Invalidate()
        {
            _cached = IntPtr.Zero;
        }
    }
}
#endif
