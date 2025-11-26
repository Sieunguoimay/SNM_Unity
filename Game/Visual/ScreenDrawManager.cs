using System.Collections.Generic;
using UnityEngine;

namespace Snm.Visual
{
    public class ScreenDrawManager : MonoBehaviour
    {
        private static ScreenDrawManager _instance;

        public static ScreenDrawManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                GameObject go = new GameObject("[ScreenDraw]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ScreenDrawManager>();
                return _instance;
            }
        }

        struct ScreenLine
        {
            public Vector2 a;
            public Vector2 b;
            public Color color;
            public float width;
            public float endTime;
        }

        private readonly List<ScreenLine> lines = new();

        public void AddLine(Vector2 a, Vector2 b, Color color, float width, float duration)
        {
            lines.Add(new ScreenLine
            {
                a = a,
                b = b,
                color = color,
                width = width,
                endTime = Time.time + duration
            });
        }

        void OnGUI()
        {
            float now = Time.time;

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (now > lines[i].endTime)
                {
                    lines.RemoveAt(i);
                    continue;
                }

                DrawLineGUI(lines[i].a, lines[i].b, lines[i].color, lines[i].width);
            }
        }

        // Simple GUI-based line drawing
        private static Texture2D _tex;

        private void DrawLineGUI(Vector2 a, Vector2 b, Color color, float width)
        {
            if (_tex == null)
            {
                _tex = new Texture2D(1, 1);
                _tex.SetPixel(0, 0, Color.white);
                _tex.Apply();
            }

            Matrix4x4 matrixBackup = GUI.matrix;

            Color savedColor = GUI.color;
            GUI.color = color;

            Vector2 delta = b - a;
            float angle = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
            float length = delta.magnitude;

            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), _tex);

            GUI.color = savedColor;
            GUI.matrix = matrixBackup;
        }
    }
}