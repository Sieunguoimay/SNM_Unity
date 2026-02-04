#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class RenderTexturePreviewWindow : EditorWindow
{
    private RenderTexture _renderTexture;

    public void SetRenderTexture(RenderTexture renderTexture)
    {
        _renderTexture = renderTexture;
    }

    private void OnGUI()
    {
        if (_renderTexture == null) return;
        var ratio = _renderTexture.width / (float)_renderTexture.height;
        var rect = GUILayoutUtility.GetRect(position.width, position.width / ratio);
        EditorGUI.DrawPreviewTexture(rect, _renderTexture, null, ScaleMode.ScaleToFit);
    }
}
#endif