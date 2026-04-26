#if UNITY_EDITOR && UNITY_EDITOR_WIN
using System;

namespace Snm.Tools.HeaderAutoHide
{
    internal struct CursorSample
    {
        public bool InRevealZone;
        public bool InHeaderRegion;
        public bool WindowFound;
    }

    internal static class CursorTracker
    {
        public static CursorSample Sample(int revealZonePx, int headerRegionPx)
        {
            var s = new CursorSample();
            if (!Win32Native.GetCursorPos(out var pt)) return s;
            if (!MainWindowResolver.TryGetWindowRect(out var rect)) return s;
            s.WindowFound = true;

            float dpi = MainWindowResolver.GetDpiScale();
            int rzScaled = Math.Max(1, (int)Math.Round(revealZonePx * dpi));
            int hrScaled = Math.Max(rzScaled, (int)Math.Round(headerRegionPx * dpi));

            bool xInside = pt.X >= rect.Left && pt.X <= rect.Right;
            if (!xInside) return s;

            int dy = pt.Y - rect.Top;
            s.InRevealZone = dy >= 0 && dy <= rzScaled;
            s.InHeaderRegion = dy >= 0 && dy <= hrScaled;
            return s;
        }
    }
}
#endif
