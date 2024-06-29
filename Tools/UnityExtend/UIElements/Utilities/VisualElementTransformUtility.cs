using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements.Utilities
{
    public static class VisualElementTransformUtility
    {

        public static Rect CalculateWorldBoundOfChildren(IEnumerable<VisualElement> children)
        {
            if(!children.Any()) { return new Rect(); }

            var xMin = children.Min(c => c.worldBound.x);
            var xMax = children.Max(c => c.worldBound.x + c.worldBound.width);
            var yMin = children.Min(c => c.worldBound.y);
            var yMax = children.Max(c => c.worldBound.y + c.worldBound.height);

            return new Rect(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
        }
        public static Rect CombineBound(Rect b1, Rect b2)
        {
            var xMin = Mathf.Min(b1.x, b2.x);
            var yMin = Mathf.Min(b1.y, b2.y);
            var xMax = Mathf.Max(b1.x + b1.width, b2.x + b2.width);
            var yMax = Mathf.Max(b1.y + b1.height, b2.y + b2.height);

            return new Rect(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
        }
        public static Vector2 GetPosition(VisualElement element)
        {
            return element.LocalToWorld(Vector2.zero);
        }
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static bool Approximately(float a, float b, float tolerance = 1e-5f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        public static float CrossProduct2D(Vector2 a, Vector2 b)
        {
            return a.x * b.y - b.x * a.y;
        }

        public static bool IntersectLineSegments2D(Vector2 p1start, Vector2 p1end, Vector2 p2start, Vector2 p2end,
       out Vector2 intersection)
        {
            var p = p1start;
            var r = p1end - p1start;
            var q = p2start;
            var s = p2end - p2start;
            var qminusp = q - p;
            float cross_rs = CrossProduct2D(r, s);


            if (Approximately(cross_rs, 0f))
            {
                if (Approximately(CrossProduct2D(qminusp, r), 0f))
                {
                    float rdotr = Vector2.Dot(r, r);
                    float sdotr = Vector2.Dot(s, r);
                    float t0 = Vector2.Dot(qminusp, r / rdotr);
                    float t1 = t0 + sdotr / rdotr;
                    if (sdotr < 0)
                    {
                        Swap(ref t0, ref t1);
                    }

                    if (t0 <= 1 && t1 >= 0)
                    {
                        float t = Mathf.Lerp(Mathf.Max(0, t0), Mathf.Min(1, t1), 0.5f);
                        intersection = p + t * r;
                        return true;
                    }
                    else
                    {
                        intersection = Vector2.zero;
                        return false;
                    }
                }
                else
                {
                    intersection = Vector2.zero;
                    return false;
                }
            }
            else
            {
                float t = CrossProduct2D(qminusp, s) / cross_rs;
                float u = CrossProduct2D(qminusp, r) / cross_rs;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                {
                    intersection = p + t * r;
                    return true;
                }
                else
                {
                    intersection = Vector2.zero;
                    return false;
                }
            }
        }
        public static IEnumerable<VisualElement> TraverseTree(VisualElement root)
        {
            foreach (var c in root.Children())
            {
                yield return c;

                foreach (var c2 in TraverseTree(c))
                {
                    yield return c2;
                }
            }
        }

    }
}