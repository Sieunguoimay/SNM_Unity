using System;
using Snm.Runtime.Dispose;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugToolVECreator
    {
        public static VisualElement Create(GrassDebugTool tool)
        {
            var root = new VisualElement();
            var button_ShowWindMap = new Button() { text = "Toggle Wind Map", clickable = new(tool.ToggleWindMap) };
            var button_ShowTrampleMap = new Button() { text = "Toggle Trample Map", clickable = new(tool.ToggleTrampleMap) };

            root.Add(button_ShowWindMap);
            root.Add(CreateTexturePreviewVE(tool.TrampleTexture, () => {
                Debug.Log("Clear Trample Texture");
            }, out var _));
            root.Add(button_ShowTrampleMap);
            return root;
        }

        private static VisualElement CreateTexturePreviewVE(
            RenderTexture renderTexture,
            Action clearClickCallback,
            out IDisposable disposable)
        {
            var root = new VisualElement();
            var canvasVE = new VisualElement
            {
                style = {
                    width = 200,
                    height = 200,
                    backgroundImage = Background.FromRenderTexture(renderTexture),
                }
            };
            var button_Clear = new Button() { text = "Clear", clickable = new(clearClickCallback) };

            root.Add(canvasVE);
            root.Add(button_Clear);

            disposable = new DisposeCallback(() =>
            {
                canvasVE.style.backgroundImage = null;
            });
            root.schedule.Execute(() =>
            {
                canvasVE.MarkDirtyRepaint();
            }).Every(100);

            return root;
        }
    }
}