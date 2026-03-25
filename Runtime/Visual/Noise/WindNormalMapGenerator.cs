#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Visual.Noises
{

    public class WindNormalMapGenerator : EditorWindow
    {
        private enum OutputMode
        {
            NormalMap,
            DUDV
        }

        [SerializeField] private int width = 256;
        [SerializeField] private int height = 256;
        [SerializeField] private float scale = 6f;
        [SerializeField] private int octaves = 3;
        [SerializeField] [Range(0f, 1f)] private float persistence = 0.4f;
        [SerializeField] private float lacunarity = 2f;
        [SerializeField] private float normalStrength = 8f;
        [SerializeField] private bool seamless = true;
        [SerializeField] private OutputMode outputMode = OutputMode.NormalMap;

        private Editor _editor;
        private Texture2D _preview;

        [MenuItem("Tools/Snm/Game/WindNormalMapGenerator")]
        public static void ShowWindow()
        {
            GetWindow<WindNormalMapGenerator>().Show();
        }

        private void OnGUI()
        {
            (_editor ??= Editor.CreateEditor(this)).OnInspectorGUI();

            if (GUILayout.Button("Preview"))
            {
                if (_preview != null) DestroyImmediate(_preview);
                _preview = Generate();
            }

            if (_preview != null)
            {
                var rect = GUILayoutUtility.GetAspectRect(1f, GUILayout.MaxWidth(256), GUILayout.MaxHeight(256));
                EditorGUI.DrawPreviewTexture(rect, _preview);
            }

            if (GUILayout.Button("Save"))
            {
                var texture = _preview ?? Generate();

                if (TryGetPathToSave(out var path))
                {
                    var bytes = texture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(ToRelativePath(path));

                    Debug.Log($"Created wind normal map {path} {width}x{height}");
                    _preview = null;
                }
            }
        }

        private void OnDestroy()
        {
            if (_preview != null)
                DestroyImmediate(_preview);
            if (_editor != null)
                DestroyImmediate(_editor);
        }

        private bool TryGetPathToSave(out string absolutePath)
        {
            absolutePath = "";
            var path = EditorUtility.SaveFilePanel(
                "Save Texture as PNG",
                "Assets",
                "WindNormalMap.png",
                "png");

            if (path.Length == 0)
                return false;

            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogError("Error: Cannot save outside the Assets folder.");
                return false;
            }

            absolutePath = path;
            return true;
        }

        private static string ToRelativePath(string absolutePath)
        {
            return "Assets" + absolutePath[Application.dataPath.Length..];
        }

        private Texture2D Generate()
        {
            var heightMap = GenerateFbmHeightMap();
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int xL = (x - 1 + width) % width;
                    int xR = (x + 1) % width;
                    int yD = (y - 1 + height) % height;
                    int yU = (y + 1) % height;

                    float dX = (heightMap[y * width + xR] - heightMap[y * width + xL]) * normalStrength;
                    float dY = (heightMap[yU * width + x] - heightMap[yD * width + x]) * normalStrength;

                    if (outputMode == OutputMode.DUDV)
                    {
                        // DUDV: XY = displacement direction, remapped to [0,1]
                        // Grass shader reads .xy * 2 - 1
                        byte r = (byte)Mathf.Clamp((dX * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((dY * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels[y * width + x] = new Color32(r, g, 128, 255);
                    }
                    else
                    {
                        // Normal map: tangent-space normal from height gradients
                        // Water ScrollNormal shader reads .rgb * 2 - 1
                        var normal = new Vector3(-dX, -dY, 1f).normalized;
                        byte r = (byte)Mathf.Clamp((normal.x * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((normal.y * 0.5f + 0.5f) * 255f, 0, 255);
                        byte b = (byte)Mathf.Clamp((normal.z * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels[y * width + x] = new Color32(r, g, b, 255);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private float[] GenerateFbmHeightMap()
        {
            var map = new float[width * height];

            float xOrg = Random.Range(0f, 10000f);
            float yOrg = Random.Range(0f, 10000f);

            float maxAmplitude = 0f;
            float amplitude = 1f;

            for (int o = 0; o < octaves; o++)
            {
                maxAmplitude += amplitude;
                amplitude *= persistence;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = 0f;
                    amplitude = 1f;
                    float frequency = 1f;

                    for (int o = 0; o < octaves; o++)
                    {
                        float sample;

                        if (seamless)
                            sample = SampleSeamless(x, y, frequency, xOrg, yOrg);
                        else
                            sample = SamplePerlin(x, y, frequency, xOrg, yOrg);

                        value += sample * amplitude;
                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    map[y * width + x] = value / maxAmplitude;
                }
            }

            return map;
        }

        private float SamplePerlin(int x, int y, float frequency, float xOrg, float yOrg)
        {
            float xCoord = xOrg + (float)x / width * scale * frequency;
            float yCoord = yOrg + (float)y / height * scale * frequency;
            return Mathf.PerlinNoise(xCoord, yCoord);
        }

        private float SampleSeamless(int x, int y, float frequency, float xOrg, float yOrg)
        {
            // Map 2D grid onto a torus in 4D Perlin space for seamless tiling
            float s = (float)x / width;
            float t = (float)y / height;

            float r = scale * frequency / (2f * Mathf.PI);

            float nx = xOrg + Mathf.Cos(s * 2f * Mathf.PI) * r;
            float ny = yOrg + Mathf.Sin(s * 2f * Mathf.PI) * r;
            float nz = xOrg + 31.7f + Mathf.Cos(t * 2f * Mathf.PI) * r;
            float nw = yOrg + 31.7f + Mathf.Sin(t * 2f * Mathf.PI) * r;

            // Approximate 4D sampling with two 2D Perlin lookups blended
            float a = Mathf.PerlinNoise(nx, nz);
            float b = Mathf.PerlinNoise(ny, nw);
            return (a + b) * 0.5f;
        }
    }
}
#endif
