using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleSystemHandle
    {
        private readonly Texture trampleTex;
        private readonly Action cleanupCallback;

        public GrassDisturberTracker Tracker { get; }

        public GrassTrampleSystemHandle(
            Texture trampleTex,
            GrassDisturberTracker tracker,
            Action cleanupCallback)
        {
            this.trampleTex = trampleTex;
            Tracker = tracker;
            this.cleanupCallback = cleanupCallback;
        }

        public void SetDisturbers(IReadOnlyList<IGrassDisturber> disturbers)
        {
            Tracker.SetExternalDisturbers(disturbers);
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
