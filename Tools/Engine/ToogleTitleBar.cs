#if UNITY_EDITOR
using UnityEditor;

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace Snm.Tools.Engine
{
    [InitializeOnLoad]
    public static class ToggleTitleBar
    {
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern IntPtr GetMenu(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr SetMenu(IntPtr hWnd, IntPtr hMenu);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_SYSMENU = 0x00080000;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int SW_MAXIMIZE = 3;
        private const string MenuPath = "Tools/Snm/Toggle Title Bar _F11";

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private static bool hidden = false;
        private static IntPtr hwnd = IntPtr.Zero;

        private static void SetHandles()
        {
            var foregroundHwnd = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundHwnd, out var processId);
            var currentProcess = Process.GetProcessById(processId);
            hwnd = currentProcess.MainWindowHandle;
        }

        private static void Show()
        {
            SetHandles();
            if (hwnd == IntPtr.Zero) return;

            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) | WS_CAPTION);
            SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);

            Menu.SetChecked(MenuPath, false);
            hidden = false;
        }

        private static void Hide()
        {
            SetHandles();
            if (hwnd == IntPtr.Zero) return;

            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_CAPTION);
            SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED); // unsure why this prevents add component search box to show

            // ShowWindow(hwnd, SW_MAXIMIZE); // also maximize
            Menu.SetChecked(MenuPath, true);
            hidden = true;
        }

        [MenuItem(MenuPath)]
        public static void Toggle()
        {
            if (hidden) Show();
            else Hide();
        }
    }
}
#endif