using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemHandle
    {
        private readonly Texture trampleTex;
        private readonly Action cleanupCallback;

        public GrassTrampleBrushRegistry BrushRegistry { get; }

        public GrassTrampleSystemHandle(
            Texture trampleTex, 
            Action cleanupCallback,
            GrassTrampleBrushRegistry brushRegistry)
        {
            this.trampleTex = trampleTex;
            this.cleanupCallback = cleanupCallback;

            BrushRegistry = brushRegistry;
        }


        public void Cleanup()
        {
            cleanupCallback();
        }

        public Texture GetTrampleTexture()
        {
            return trampleTex;
        }
    }
}