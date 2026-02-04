using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class PreviewReflectionTexture
    {
        public event Action PreviewReflectionTextureUpdated;
        public RenderTexture RenderTexture { get; }

        public PreviewReflectionTexture(RenderTexture renderTexture)
        {
            RenderTexture = renderTexture;
        }

        public void InvokeUpdated() => PreviewReflectionTextureUpdated?.Invoke();
    }
}