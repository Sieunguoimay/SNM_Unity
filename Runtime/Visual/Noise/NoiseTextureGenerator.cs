#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Visual.Noises
{

    public class NoiseTextureGenerator : EditorWindow
    {
        [SerializeField] private int width = 256;
        [SerializeField] private int height = 256;
        [SerializeField] private float scale = 20f;

        private Editor _editor;

        [MenuItem("Tools/Snm/Game/NoiseTextureGenerator")]
        public static void ShowWindow()
        {
            GetWindow<NoiseTextureGenerator>().Show();
        }

        private void OnGUI()
        {
            (_editor ??= Editor.CreateEditor(this)).OnInspectorGUI();
            if (GUILayout.Button("Create"))
            {

                var textureToSave = CreatePerlinNoise();

                if (TryGetPathToSave(out var path))
                {
                    AssetDatabase.CreateAsset(textureToSave, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Debug.Log($"Created noise texture {path} {width}x{height} {scale}", textureToSave);
                }
            }
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
        private Texture2D CreatePerlinNoise()
        {
            var texture = new Texture2D(width, height);

            float xOrg = Random.Range(0, 10000);
            float yOrg = Random.Range(0, 10000);

            Color32[] pixels = new Color32[texture.width * texture.height];

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float xCoord = xOrg + (float)x / texture.width * scale;
                    float yCoord = yOrg + (float)y / texture.height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);

                    pixels[y * texture.width + x] = new Color(sample, sample, sample);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
#endif