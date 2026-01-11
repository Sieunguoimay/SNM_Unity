using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemHandle
    {
        private readonly Texture trampleTex;
        private readonly Action cleanupCallback;

        public GrassTrampleSystemHandle(Texture trampleTex, Action cleanupCallback)
        {
            this.trampleTex = trampleTex;
            this.cleanupCallback = cleanupCallback;
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