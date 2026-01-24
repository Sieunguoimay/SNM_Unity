using System.Collections.Generic;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Runtime.Debugging
{
    public class ScreenDrawManager : MonoBehaviour
    {
        private readonly List<ScreenLine> lines = new();
        private static Material _lineMat;
        private static ScreenDrawManager _instance;

        public static ScreenDrawManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngineUtility.CreateGameObjectWithComponent<ScreenDrawManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

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

        void OnPostRender()
        {
            if (lines.Count == 0) return;

            CreateMaterial();
            _lineMat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            GL.Begin(GL.LINES);

            float now = Time.time;

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (now > lines[i].endTime)
                {
                    lines.RemoveAt(i);
                    continue;
                }

                GL.Color(lines[i].color);
                GL.Vertex(lines[i].a);
                GL.Vertex(lines[i].b);
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void CreateMaterial()
        {
            if (_lineMat != null) return;

            _lineMat = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMat.SetInt("_ZWrite", 0);
        }

        struct ScreenLine
        {
            public Vector2 a;
            public Vector2 b;
            public Color color;
            public float width;
            public float endTime;
        }
    }
}
