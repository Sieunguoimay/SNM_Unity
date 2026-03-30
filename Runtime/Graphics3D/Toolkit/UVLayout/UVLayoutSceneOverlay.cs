#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.UVLayout
{
    public static class UVLayoutSceneOverlay
    {
        static Material _checkerMaterial;
        static Texture2D _checkerTexture;
        static Material _vertexColorMaterial;
        static int _lastCheckerScale;

        static Mesh _targetMesh;
        static Transform _targetTransform;
        static UVLayoutSettings _settings;
        static bool _registered;

        public static void Enable(Mesh mesh, Transform transform, UVLayoutSettings settings)
        {
            _targetMesh = mesh;
            _targetTransform = transform;
            _settings = settings;

            if (!_registered)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                _registered = true;
            }
            SceneView.RepaintAll();
        }

        public static void Disable()
        {
            if (_registered)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                _registered = false;
            }
            SceneView.RepaintAll();
        }

        public static bool IsActive => _registered;

        static void OnSceneGUI(SceneView sceneView)
        {
            if (_targetMesh == null || _targetTransform == null || _settings == null)
            {
                Disable();
                return;
            }

            Matrix4x4 matrix = _targetTransform.localToWorldMatrix;

            if (_settings.checkerPatternScene)
                DrawCheckerOverlay(matrix);

            if (_settings.texelDensityScene)
                DrawTexelDensityOverlay(matrix);
        }

        static void DrawCheckerOverlay(Matrix4x4 matrix)
        {
            EnsureCheckerMaterial();
            if (_checkerMaterial == null) return;

            if (_checkerTexture == null || _lastCheckerScale != _settings.checkerScale)
                RegenerateCheckerTexture(_settings.checkerScale);

            _checkerMaterial.mainTexture = _checkerTexture;
            _checkerMaterial.SetPass(0);
            Graphics.DrawMeshNow(_targetMesh, matrix);
        }

        static void DrawTexelDensityOverlay(Matrix4x4 matrix)
        {
            EnsureVertexColorMaterial();
            if (_vertexColorMaterial == null) return;

            var densities = UVLayoutAnalyzer.ComputeTexelDensity(_targetMesh, _settings.uvChannel);
            var triangles = _targetMesh.triangles;
            int vertCount = _targetMesh.vertexCount;

            // Build per-vertex color from triangle density
            var colors = new Color[vertCount];
            var counts = new int[vertCount];

            int triCount = triangles.Length / 3;
            for (int t = 0; t < triCount; t++)
            {
                Color col = UVLayoutAnalyzer.DensityToColor(
                    densities[t], _settings.texelDensityMin, _settings.texelDensityMax);

                for (int e = 0; e < 3; e++)
                {
                    int v = triangles[t * 3 + e];
                    if (v >= vertCount) continue;
                    colors[v] += col;
                    counts[v]++;
                }
            }

            for (int v = 0; v < vertCount; v++)
            {
                if (counts[v] > 0)
                    colors[v] /= counts[v];
                colors[v].a = 1f;
            }

            // Create temp mesh with vertex colors
            var tempMesh = Object.Instantiate(_targetMesh);
            tempMesh.colors = colors;

            _vertexColorMaterial.SetPass(0);
            Graphics.DrawMeshNow(tempMesh, matrix);
            Object.DestroyImmediate(tempMesh);
        }

        static void EnsureCheckerMaterial()
        {
            if (_checkerMaterial != null) return;

            var shader = Shader.Find("Unlit/Texture");
            if (shader == null) return;

            _checkerMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        static void EnsureVertexColorMaterial()
        {
            if (_vertexColorMaterial != null) return;

            // Use a shader that shows vertex colors
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;

            _vertexColorMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _vertexColorMaterial.SetInt("_ZWrite", 1);
            _vertexColorMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
        }

        static void RegenerateCheckerTexture(int scale)
        {
            _lastCheckerScale = scale;
            int size = scale * 2;
            if (_checkerTexture != null) Object.DestroyImmediate(_checkerTexture);

            _checkerTexture = new Texture2D(size, size, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            Color32 white = new(230, 230, 230, 255);
            Color32 gray = new(128, 128, 128, 255);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool isWhite = ((x / 1) + (y / 1)) % 2 == 0;
                pixels[y * size + x] = isWhite ? white : gray;
            }

            _checkerTexture.SetPixels32(pixels);
            _checkerTexture.Apply();
        }
    }
}
#endif
