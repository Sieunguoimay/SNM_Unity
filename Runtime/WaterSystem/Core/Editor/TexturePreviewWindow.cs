#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class TexturePreviewWindow : EditorWindow
{
    private Texture _texture;

    public void SetTexture(Texture texture)
    {
        _texture = texture;
        CreateGUI();
    }

    private void CreateGUI()
    {
        if (_texture != null)
        {
            var image = new Image { image = _texture, style = { flexGrow = 1 } };
            rootVisualElement.Add(image);
            image.schedule.Execute(image.MarkDirtyRepaint).Every(100);
        }
    }
}
#endif