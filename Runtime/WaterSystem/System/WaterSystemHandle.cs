using System;

namespace Snm.Runtime.WaterSystem
{
    public class WaterSystemHandle
    {
        public IDisposable Destroyer { get; }
        public PreviewReflectionTexture PreviewReflectionTexture { get; }

        public WaterSystemHandle(
            IDisposable destroyer,
            PreviewReflectionTexture previewReflectionTexture)
        {
            Destroyer = destroyer;
            PreviewReflectionTexture = previewReflectionTexture;
        }
    }
}