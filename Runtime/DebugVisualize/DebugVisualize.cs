using System;
using UnityEngine;

#if UNITY_DEBUG || DEVELOPMENT_BUILD
namespace Snm.Runtime.DebugVisualize
{
    public static class DebugVisualize
    {
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                DebugVisualizeManager.EnsureInitialized();
                _initialized = true;
            }
        }

        public static bool Enabled
        {
            get => DebugVisualizeManager.IsEnabled;
            set => DebugVisualizeManager.IsEnabled = value;
        }

        public static void ShowText(string text, Transform target, Vector3 offset = default, Color? color = null, float fontSize = 0, float duration = 0)
        {
            if (!DebugVisualizeManager.IsEnabled) return;
            EnsureInitialized();
            DebugVisualizeManager.TextSystem.ShowText(text, target, offset, color, fontSize, duration);
        }

        public static void ShowText(string text, Vector3 position, Color? color = null, float fontSize = 0, float duration = 0)
        {
            if (!DebugVisualizeManager.IsEnabled) return;
            EnsureInitialized();
            var dummy = new GameObject("DebugTextPos");
            dummy.hideFlags = HideFlags.HideAndDontSave;
            dummy.transform.position = position;
            DebugVisualizeManager.TextSystem.ShowText(text, dummy.transform, Vector3.zero, color, fontSize, duration);
        }

        public static DebugStatEntry ShowStat(string label, Func<float> currentGetter, Func<float> maxGetter = null, bool showBar = false, bool autoUpdate = true, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0)
        {
            if (!DebugVisualizeManager.IsEnabled) return null;
            EnsureInitialized();
            return DebugVisualizeManager.StatsSystem.ShowStat(label, target ?? CreateDummyTransform(), offset, color, showBar, currentGetter, maxGetter, autoUpdate, duration);
        }

        public static DebugStatEntry ShowStat(string label, float current, float max, bool showBar = true, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0)
        {
            var currentVal = current;
            var maxVal = max;
            return ShowStat(label, () => currentVal, () => maxVal, showBar, false, target, offset, color, duration);
        }

        public static DebugStatEntry ShowStat(string label, float value, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0)
        {
            if (!DebugVisualizeManager.IsEnabled) return null;
            EnsureInitialized();
            return DebugVisualizeManager.StatsSystem.ShowStat(label, target ?? CreateDummyTransform(), offset, color, false, () => value, null, false, duration);
        }

        private static Transform CreateDummyTransform()
        {
            var go = new GameObject("DebugStatDummy");
            go.hideFlags = HideFlags.HideAndDontSave;
            return go.transform;
        }

        public static class Draw
        {
            public static void Line(Vector3 start, Vector3 end, Color? color = null, float width = 0, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Line(start, end, color, width, duration);
            }

            public static void Ray(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Ray(origin, direction, color, width, duration);
            }

            public static void Arrow(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0, float headLength = 0.2f, float headWidth = 0.1f)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Arrow(origin, direction, color, width, duration, headLength, headWidth);
            }

            public static void Sphere(Vector3 center, float radius, Color? color = null, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Sphere(center, radius, color, duration);
            }

            public static void Box(Vector3 center, Vector3 size, Color? color = null, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Box(center, size, color, duration);
            }

            public static void Box(Bounds bounds, Color? color = null, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Box(bounds, color, duration);
            }

            public static void Circle(Vector3 center, Vector3 normal, float radius, Color? color = null, int segments = 32, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Circle(center, normal, radius, color, segments, duration);
            }

            public static void Cone(Vector3 origin, Vector3 direction, float angle, float length, Color? color = null, int segments = 16, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Cone(origin, direction, angle, length, color, segments, duration);
            }

            public static void Frustum(Camera camera, Color? color = null, float duration = 0)
            {
                if (!DebugVisualizeManager.IsEnabled) return;
                EnsureInitialized();
                DebugVisualizeManager.ShapeDrawer.Frustum(camera, color, duration);
            }
        }
    }
}
#else
namespace Snm.Runtime.DebugVisualize
{
    public class DebugStatEntry { }

    public static class DebugVisualize
    {
        public static bool Enabled { get; set; } = false;
        public static void ShowText(string text, Transform target, Vector3 offset = default, Color? color = null, float fontSize = 0, float duration = 0) { }
        public static void ShowText(string text, Vector3 position, Color? color = null, float fontSize = 0, float duration = 0) { }
        public static DebugStatEntry ShowStat(string label, Func<float> currentGetter, Func<float> maxGetter = null, bool showBar = false, bool autoUpdate = true, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0) => null;
        public static DebugStatEntry ShowStat(string label, float current, float max, bool showBar = true, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0) => null;
        public static DebugStatEntry ShowStat(string label, float value, Transform target = null, Vector3 offset = default, Color? color = null, float duration = 0) => null;

        public static class Draw
        {
            public static void Line(Vector3 start, Vector3 end, Color? color = null, float width = 0, float duration = 0) { }
            public static void Ray(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0) { }
            public static void Arrow(Vector3 origin, Vector3 direction, Color? color = null, float width = 0, float duration = 0, float headLength = 0.2f, float headWidth = 0.1f) { }
            public static void Sphere(Vector3 center, float radius, Color? color = null, float duration = 0) { }
            public static void Box(Vector3 center, Vector3 size, Color? color = null, float duration = 0) { }
            public static void Box(Bounds bounds, Color? color = null, float duration = 0) { }
            public static void Circle(Vector3 center, Vector3 normal, float radius, Color? color = null, int segments = 32, float duration = 0) { }
            public static void Cone(Vector3 origin, Vector3 direction, float angle, float length, Color? color = null, int segments = 16, float duration = 0) { }
            public static void Frustum(Camera camera, Color? color = null, float duration = 0) { }
        }
    }
}
#endif
