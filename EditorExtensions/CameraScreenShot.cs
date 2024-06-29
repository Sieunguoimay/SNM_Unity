using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class CameraScreenShort : MonoBehaviour
{
    public string screenshotFileName = "Screenshot";

#if UNITY_EDITOR
    [ContextMenu("Capture")]
    private void CamCapture()
    {
        Camera mainCamera = Camera.main;

        var renderTexture = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 24);
        mainCamera.targetTexture = renderTexture;
        var screenshotTexture = new Texture2D(mainCamera.pixelWidth, mainCamera.pixelHeight, TextureFormat.RGB24, false);

        mainCamera.Render();
        RenderTexture.active = renderTexture;
        screenshotTexture.ReadPixels(new Rect(0, 0, mainCamera.pixelWidth, mainCamera.pixelHeight), 0, 0);
        mainCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        string projectFolderPath = Application.dataPath.Replace("/Assets", "/Assets/Blender/Ref");

        var time = System.DateTime.Now.ToString("yyMMddHHmmss");
        string screenshotPath = System.IO.Path.Combine(projectFolderPath, $"{screenshotFileName}_{time}.png");

        byte[] bytes = screenshotTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(screenshotPath, bytes);

        AssetDatabase.Refresh();
        Debug.Log("Screenshot captured and saved as " + screenshotPath);
    }
#endif
}