#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Visual.Noises
{

    /// <summary>
    /// Editor window that generates Perlin noise textures with four output modes:
    /// Grayscale (raw noise), NormalMapZUp (tangent-space, Z=up), NormalMapYUp (object-space, Y=up),
    /// and DUDV (derivative/distortion map for water-like effects).
    /// Supports seamless tiling via toroidal mapping.
    /// </summary>
    public class NoiseTextureGenerator : EditorWindow
    {
        private enum OutputMode { Grayscale, NormalMapZUp, NormalMapYUp, DUDV }

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
            // Use a default Inspector editor to expose serialized fields as UI controls
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
                        // If saved inside the project, refresh the AssetDatabase so it appears in the editor
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

        /// <summary>
        /// Validates a save path, ensuring it is within the Assets folder,
        /// and converts the absolute path to a project-relative path.
        /// </summary>
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

        /// <summary>
        /// Core generation method. Builds a Perlin-noise heightmap, then converts it
        /// to the selected output format (Grayscale, NormalMapZUp, NormalMapYUp, or DUDV).
        /// </summary>
        [ContextMenu("BakeTexture")]
        private Texture2D CreateTexture()
        {
            // Random origin offset so each generation produces a unique pattern
            float xOrg = Random.Range(0f, 10000f);
            float yOrg = Random.Range(0f, 10000f);

            // --- Step 1: Generate the heightmap (raw Perlin noise values) ---
            var heightMap = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    heightMap[y * width + x] = seamless
                        ? SampleSeamless(x, y, xOrg, yOrg)       // Tileable noise via toroidal mapping
                        : Mathf.PerlinNoise(                       // Standard 2D Perlin noise sample
                            xOrg + (float)x / width * scale,
                            yOrg + (float)y / height * scale);
                }
            }

            // --- Step 2a: Grayscale — map noise values directly to pixel brightness ---
            if (outputMode == OutputMode.Grayscale)
            {
                var texture = new Texture2D(width, height);
                var pixels = new Color32[width * height];

                for (int i = 0; i < pixels.Length; i++)
                {
                    float s = heightMap[i];
                    pixels[i] = new Color(s, s, s); // R=G=B for grayscale
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                return texture;
            }

            // --- Step 2b: NormalMap / DUDV — derive surface direction from heightmap gradients ---
            // Linear color space (last two bools: linear=true, mipChain=true)
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
            var pixels2 = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Wrap-around neighbor indices for seamless gradient calculation
                    int xL = (x - 1 + width) % width;   // left neighbor
                    int xR = (x + 1) % width;            // right neighbor
                    int yD = (y - 1 + height) % height;  // bottom neighbor
                    int yU = (y + 1) % height;            // top neighbor

                    // Central-difference gradient: how steeply the height changes in X and Y
                    // normalStrength amplifies the perceived bumpiness
                    float dX = (heightMap[y * width + xR] - heightMap[y * width + xL]) * normalStrength;
                    float dY = (heightMap[yU * width + x] - heightMap[yD * width + x]) * normalStrength;

                    if (outputMode == OutputMode.DUDV)
                    {
                        // DUDV map: stores raw derivatives (dU, dV) packed into [0,1].
                        // Used for UV distortion effects (e.g., water refraction).
                        // 0.5 = zero displacement; <0.5 = negative; >0.5 = positive.
                        byte r = (byte)Mathf.Clamp((dX * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((dY * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels2[y * width + x] = new Color32(r, g, 128, 255); // B=128 (neutral Z)
                    }
                    else if (outputMode == OutputMode.NormalMapYUp)
                    {
                        // Object-space normal map with Y as up (Unity convention).
                        // n = normalize(-dX, 1, -dY) — flat surface normal is (0,1,0).
                        // Sampled normal can be used directly in Unity without TBN transform.
                        var n = new Vector3(-dX, 1f, -dY).normalized;
                        byte r = (byte)Mathf.Clamp((n.x * 0.5f + 0.5f) * 255f, 0, 255);
                        byte g = (byte)Mathf.Clamp((n.y * 0.5f + 0.5f) * 255f, 0, 255);
                        byte b = (byte)Mathf.Clamp((n.z * 0.5f + 0.5f) * 255f, 0, 255);
                        pixels2[y * width + x] = new Color32(r, g, b, 255);
                    }
                    else
                    {
                        // Tangent-space normal map with Z as up (standard texture convention).
                        // n = normalize(-dX, -dY, 1) — flat surface normal is (0,0,1).
                        // Requires TBN matrix transform when sampling in shader.
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

        /// <summary>
        /// Samples Perlin noise that tiles seamlessly using edge cross-blending.
        ///
        /// The idea: sample noise at 4 positions — the base point plus copies shifted
        /// by one full period in X, Y, and both. Then bilinearly blend them using the
        /// UV coordinate as the weight. At the left edge u=0 (100% base), at the right
        /// edge u=1 (100% shifted copy) — so opposing edges see identical values and
        /// the texture wraps without seams.
        /// </summary>
        private float SampleSeamless(int x, int y, float xOrg, float yOrg)
        {
            float u = (float)x / width;
            float v = (float)y / height;

            // Sample noise at 4 tiled positions (base + shifted back by one period in X/Y).
            // Shifting back ensures that at u=1 the blended result equals noise(0),
            // matching the left edge perfectly.
            float n00 = Mathf.PerlinNoise(xOrg + u * scale, yOrg + v * scale);
            float n10 = Mathf.PerlinNoise(xOrg + (u - 1f) * scale, yOrg + v * scale);
            float n01 = Mathf.PerlinNoise(xOrg + u * scale, yOrg + (v - 1f) * scale);
            float n11 = Mathf.PerlinNoise(xOrg + (u - 1f) * scale, yOrg + (v - 1f) * scale);

            // Smoothstep the blend weights to avoid visible linear transitions at edges
            float bu = u * u * (3f - 2f * u);
            float bv = v * v * (3f - 2f * v);

            // Bilinear blend: opposing edges match perfectly
            return Mathf.Lerp(
                Mathf.Lerp(n00, n10, bu),
                Mathf.Lerp(n01, n11, bu),
                bv);
        }
    }
}
#endif
