#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Visual.Noises
{

    public class NoiseTextureGenerator : EditorWindow
    {
        private enum OutputMode { Grayscale, NormalMap, DUDV }

        private enum SaveFormat { Asset, PNG }

        [SerializeField] private int width = 256;
        [SerializeField] private int height = 256;
        [SerializeField] private float scale = 20f;
        [SerializeField] private bool seamless = true;
        [SerializeField] private OutputMode outputMode = OutputMode.Grayscale;
        [SerializeField] private float normalStrength = 8f;
        [SerializeField] private SaveFormat saveFormat = SaveFormat.PNG;

        private Editor _editor;
        private Texture2D _preview;

        [MenuItem("Tools/Snm/Game/NoiseTextureGenerator")]
        public static void ShowWindow()
        {
            GetWindow<NoiseTextureGenerator>().Show();
        }

        private void OnGUI()
        {
            (_editor ??= Editor.CreateEditor(this)).OnInspectorGUI();

            if (GUILayout.Button("Preview"))
            {
                if (_preview != null) DestroyImmediate(_preview);
                _preview = CreateTexture();
            }

            if (_preview != null)
            {
                var rect = GUILayoutUtility.GetAspectRect(1f, GUILayout.MaxWidth(256), GUILayout.MaxHeight(256));
                EditorGUI.DrawPreviewTexture(rect, _preview);
            }

            if (GUILayout.Button("Save"))
            {
                var textureToSave = _preview ?? CreateTexture();

                if (saveFormat == SaveFormat.PNG)
                {
                    var path = EditorUtility.SaveFilePanel("Save Texture as PNG", "Assets", "NoiseTexture.png", "png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        System.IO.File.WriteAllBytes(path, textureToSave.EncodeToPNG());
                        if (path.StartsWith(Application.dataPath))
                            AssetDatabase.ImportAsset("Assets" + path[Application.dataPath.Length..]);
                        Debug.Log($"Created noise texture {path} {width}x{height} {scale}");
                        _preview = null;
                    }
                }
                else if (TryGetPathToSave(out var path2))
                {
                    AssetDatabase.CreateAsset(textureToSave, path2);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"Created noise texture {path2} {width}x{height} {scale}", textureToSave);
                    _preview = null;
                }
            }
        }

        private void OnDestroy()
        {
            if (_preview != null) DestroyImmediate(_preview);
            if (_editor != null) DestroyImmediate(_editor);
        }

        private bool TryGetPathToSave(out string relativePath)
        {
            relativePath = "";
            var path = EditorUtility.SaveFilePanel(
                "Save Texture as Asset",
                "Assets",
                "NewTexture.asset",
                "asset");

            if (path.Length == 0)
                return false;

            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogError("Error: Cannot save outside the Assets folder.");
                return false;
            }

            relativePath = "Assets" + path[Application.dataPath.Length..];
            return true;
        }

        [ContextMenu("BakeTexture")]
        private Texture2D CreateTexture()
        {
            float xOrg = Random.Range(0f, 10000f);
            float yOrg = Random.Range(0f, 10000f);

            var heightMap = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    heightMap[y * width + x] = seamless
                        ? SampleSeamless(x, y, xOrg, yOrg)
                        : Mathf.PerlinNoise(xOrg + (float)x / width * scale, yOrg + (float)y / height * scale);
                }
            }

            if (outputMode == OutputMode.Grayscale)
            {
                var texture = new Texture2D(width, height);
                var pixels = new Color32[width * height];

                for (int i = 0; i < pixels.Length; i++)
                {
                    float s = heightMap[i];
                    pixels[i] = new Color(s, s, s);
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                return texture;
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
            var pixels2 = new Color32[width * height];

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
                        byte r = (byte)Mathf.Clamp((dX * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((dY * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels2[y * width + x] = new Color32(r, g, 128, 255);
                    }
                    else
                    {
                        var n = new Vector3(-dX, -dY, 1f).normalized;
                        byte r = (byte)Mathf.Clamp((n.x * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((n.y * 0.5f + 0.5f) * 255f, 0, 255);
                        byte b = (byte)Mathf.Clamp((n.z * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels2[y * width + x] = new Color32(r, g, b, 255);
                    }
                }
            }

            tex.SetPixels32(pixels2);
            tex.Apply();
            return tex;
        }
        private float SampleSeamless(int x, int y, float xOrg, float yOrg)
        {
            float s = (float)x / width;
            float t = (float)y / height;
            float r = scale / (2f * Mathf.PI);

            float nx = xOrg + Mathf.Cos(s * 2f * Mathf.PI) * r;
            float ny = yOrg + Mathf.Sin(s * 2f * Mathf.PI) * r;
            float nz = xOrg + 31.7f + Mathf.Cos(t * 2f * Mathf.PI) * r;
            float nw = yOrg + 31.7f + Mathf.Sin(t * 2f * Mathf.PI) * r;

            float a = Mathf.PerlinNoise(nx, nz);
            float b = Mathf.PerlinNoise(ny, nw);
            return (a + b) * 0.5f;
        }
    }
}
#endif
