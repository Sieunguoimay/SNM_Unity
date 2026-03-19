using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemHandle
    {
        private readonly Texture trampleTex;
        private readonly GrassDisturberTracker tracker;
        private readonly Action cleanupCallback;

        public GrassTrampleSystemHandle(
            Texture trampleTex,
            GrassDisturberTracker tracker,
            Action cleanupCallback)
        {
            this.trampleTex = trampleTex;
            this.tracker = tracker;
            this.cleanupCallback = cleanupCallback;
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            tracker.SetExternalDisturbers(disturbers);
        }

        public void RegisterLocal(IGrassDisturber disturber)
        {
            tracker.RegisterLocal(disturber);
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
